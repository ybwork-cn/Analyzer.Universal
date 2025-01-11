using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ybwork.Analyzer.Universal
{
    [Generator]
    public class PickFieldsAttribute_SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var attributedClasses = new List<(INamedTypeSymbol typeSymbol, AttributeData[] attributes)>();

            // Collect all the classes/structs with the [PickAttribute] attribute
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot(context.CancellationToken);
                var model = context.Compilation.GetSemanticModel(syntaxTree);

                IEnumerable<INamedTypeSymbol> namedTypes = root.DescendantNodes()
                    .Where(node => node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax)
                    .Select(node => model.GetDeclaredSymbol(node))
                    .Where(symbol => symbol is INamedTypeSymbol)
                    .Cast<INamedTypeSymbol>();
                foreach (INamedTypeSymbol namedTypeSymbol in namedTypes)
                {
                    var attributes = namedTypeSymbol.GetAttributes()
                        .Where(attr => attr.AttributeClass != null)
                        .Where(attr => attr.AttributeClass.Name is nameof(PickFieldsAttribute))
                        .ToArray();

                    if (attributes.Length > 0)
                    {
                        attributedClasses.Add((namedTypeSymbol, attributes));
                    }
                }
            }

            // Generate partial classes for each collected symbol
            foreach ((INamedTypeSymbol typeSymbol, AttributeData[] attributes) in attributedClasses)
            {
                GeneratePartialClass(context, typeSymbol, attributes);
            }
        }

        private static void GeneratePartialClass(GeneratorExecutionContext context, INamedTypeSymbol classSymbol, AttributeData[] attributes)
        {
            string className = classSymbol.Name;

            IFieldSymbol[] fields = attributes
                .Select(attribute => attribute.ConstructorArguments[0].Value as INamedTypeSymbol)
                .SelectMany(typeSymbol => GetFields(typeSymbol))
                .ToArray();

            FieldDeclarationSyntax[] fieldDeclarationSyntax = fields
                .Select(field =>
                {
                    TypeSyntax type = SyntaxFactory.ParseTypeName(field.Type.ToDisplayString());
                    VariableDeclaratorSyntax variable = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(field.Name));
                    VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(type).AddVariables(variable);
                    return SyntaxFactory.FieldDeclaration(default, GetModifiers(field), variableDeclaration);
                })
                .ToArray();

            // 创建分部类声明
            TypeDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                // 添加生成的字段
                .AddMembers(fieldDeclarationSyntax);

            // 包装父类(外部类)
            while (classSymbol.ContainingType != null)
            {
                classDeclaration = SyntaxFactory.ClassDeclaration(classSymbol.ContainingType.Name)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                    .AddMembers(classDeclaration);
                classSymbol = classSymbol.ContainingType;
            }

            // 生成结果：最外层
            MemberDeclarationSyntax root = classDeclaration;

            // 创建命名空间
            if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                NamespaceDeclarationSyntax namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                    SyntaxFactory.ParseName(namespaceName));
                // 将类声明添加到命名空间
                root = namespaceDeclaration.AddMembers(classDeclaration);
            }

            // 创建编译单元
            CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit()
                // 引用命名空间
                .WithUsings(SyntaxFactory.SingletonList(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"))))
                // 主内容
                .WithMembers(SyntaxFactory.SingletonList(root));

            // 格式化生成的代码
            string formattedCode = compilationUnit.NormalizeWhitespace().ToFullString();

            // 添加生成的代码
            context.AddSource($"{className}_PickFields.cs", SourceText.From(formattedCode, Encoding.UTF8));
        }

        private static IFieldSymbol[] GetFields(INamedTypeSymbol classSymbol)
        {
            IFieldSymbol[] members = classSymbol.GetMembers()
                .Where(symbol => symbol is IFieldSymbol)
                .Cast<IFieldSymbol>()
                .Where(field => !field.Name.Contains("<"))
                .ToArray();
            return members;
        }

        private static SyntaxTokenList GetModifiers(IFieldSymbol fieldSymbol)
        {
            var modifiers = new List<SyntaxKind>();

            // 添加访问修饰符
            switch (fieldSymbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    modifiers.Add(SyntaxKind.PublicKeyword);
                    break;
                case Accessibility.Protected:
                    modifiers.Add(SyntaxKind.ProtectedKeyword);
                    break;
                case Accessibility.Internal:
                    modifiers.Add(SyntaxKind.InternalKeyword);
                    break;
                case Accessibility.Private:
                    modifiers.Add(SyntaxKind.PrivateKeyword);
                    break;
                case Accessibility.ProtectedAndInternal:
                    modifiers.Add(SyntaxKind.ProtectedKeyword);
                    modifiers.Add(SyntaxKind.InternalKeyword);
                    break;
                case Accessibility.ProtectedOrInternal:
                    modifiers.Add(SyntaxKind.ProtectedKeyword);
                    modifiers.Add(SyntaxKind.InternalKeyword);
                    break;
            }

            // 检查是否是静态字段
            if (fieldSymbol.IsStatic)
            {
                modifiers.Add(SyntaxKind.StaticKeyword);
            }

            // 检查是否是只读字段
            if (fieldSymbol.IsReadOnly)
            {
                modifiers.Add(SyntaxKind.ReadOnlyKeyword);
            }

            return SyntaxFactory.TokenList(modifiers.Select(m => SyntaxFactory.Token(m)));
        }
    }
}

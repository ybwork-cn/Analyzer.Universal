using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ybwork.Analyzer.Universal
{
    [Generator]
    public class ParametersOptionalAttribute_SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var attributedClasses = new List<INamedTypeSymbol>();

            // Collect all the classes/structs with the [ParametersOptionalAttribute] attribute
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
                    bool hasParametersOptionalAttribute = namedTypeSymbol.GetAttributes()
                        .Where(attr => attr.AttributeClass != null)
                        .Select(attr => attr.AttributeClass.Name)
                        .Any(attr => attr is nameof(ParametersOptionalAttribute));
                    if (hasParametersOptionalAttribute)
                    {
                        attributedClasses.Add(namedTypeSymbol);
                    }
                }
            }

            // Generate partial classes for each collected symbol
            foreach (var namedTypeSymbol in attributedClasses)
            {
                GeneratePartialClass(context, namedTypeSymbol);
            }
        }

        private static void GeneratePartialClass(GeneratorExecutionContext context, INamedTypeSymbol classSymbol)
        {
            string className = classSymbol.Name;

            var members = classSymbol.GetMembers()
                .Where(s =>
                {
                    if (s is IFieldSymbol field)
                        return field.IsReadOnly && !field.Name.Contains("<");

                    if (s is IPropertySymbol property)
                    {
                        if (!property.IsReadOnly)
                            return false;
                        var declaring = property.DeclaringSyntaxReferences.First().GetSyntax() as PropertyDeclarationSyntax;
                        if (declaring.ChildNodes().Any(node => node is ArrowExpressionClauseSyntax))
                            return false;
                        return true;
                    }

                    return false;
                })
                .ToArray();
            var parameters = members
                .Select(member =>
                {
                    var memberType = member switch
                    {
                        IFieldSymbol field => field.Type,
                        IPropertySymbol property => property.Type,
                        _ => throw new NotImplementedException()
                    };
                    TypeSyntax type = SyntaxFactory.ParseTypeName(memberType.ToDisplayString());
                    return SyntaxFactory.Parameter(SyntaxFactory.Identifier(member.Name))
                        .WithType(type)
                        .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(type)));
                }).ToArray();
            var statements = members
                .Select(member =>
                {
                    AssignmentExpressionSyntax assignment = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression(),
                            SyntaxFactory.IdentifierName(member.Name)),
                        SyntaxFactory.IdentifierName(member.Name));
                    return SyntaxFactory.ExpressionStatement(assignment);
                }).ToArray();
            var constructorDeclarationSyntax = SyntaxFactory.ConstructorDeclaration(className)
                  .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
                  .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                  .WithBody(SyntaxFactory.Block(statements));

            // 创建分部类声明
            TypeDeclarationSyntax classDeclaration = classSymbol.TypeKind switch
            {
                TypeKind.Struct => SyntaxFactory.StructDeclaration(className),
                TypeKind.Class => SyntaxFactory.ClassDeclaration(className),
                _ => throw new NotImplementedException(),
            };
            classDeclaration = classDeclaration
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                // 添加生成的构造函数
                .AddMembers(constructorDeclarationSyntax);

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
            context.AddSource($"{className}_Partial.cs", SourceText.From(formattedCode, Encoding.UTF8));
        }
    }
}

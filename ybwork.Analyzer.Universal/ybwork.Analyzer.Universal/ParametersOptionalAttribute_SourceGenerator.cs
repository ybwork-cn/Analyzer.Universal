﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace ybwork.Analyzer.Universal
{
    [Generator]
    public class ParametersOptionalAttribute_SourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 获取所有带有 [ParametersOptionalAttribute] 属性的类
            var attributedClasses = context.SyntaxProvider.CreateSyntaxProvider(
                (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax,
                (context, cancellationToken) =>
                {
                    if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol namedTypeSymbol &&
                        namedTypeSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "ParametersOptionalAttribute"))
                    {
                        return namedTypeSymbol;
                    }
                    return null;
                })
                .Where(symbol => symbol != null)
                .Collect();

            // 注册生成器
            context.RegisterSourceOutput(attributedClasses, (spc, sourceClasses) =>
            {
                foreach (var namedTypeSymbol in sourceClasses)
                {
                    GeneratePartialClass(spc, namedTypeSymbol);
                }
            });
        }

        private static void GeneratePartialClass(SourceProductionContext context, INamedTypeSymbol classSymbol)
        {
            string className = classSymbol.Name;

            var members = classSymbol.GetMembers().Where(s => s is IFieldSymbol)
                .Cast<IFieldSymbol>().ToArray();
            var parameters = members
                .Select(field =>
                {
                    TypeSyntax type = SyntaxFactory.ParseTypeName(field.Type.ToDisplayString());
                    return SyntaxFactory.Parameter(SyntaxFactory.Identifier(field.Name))
                        .WithType(type)
                        .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(type)));
                }).ToArray();
            var statements = members
                .Select(field =>
                {
                    AssignmentExpressionSyntax assignment = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ThisExpression(),
                            SyntaxFactory.IdentifierName(field.Name)),
                        SyntaxFactory.IdentifierName(field.Name));
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
                _ => throw new System.NotImplementedException(),
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
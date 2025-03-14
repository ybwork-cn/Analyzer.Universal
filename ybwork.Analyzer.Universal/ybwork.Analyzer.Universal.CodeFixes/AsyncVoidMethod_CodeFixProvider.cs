﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ybwork.Analyzer.Universal
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidMethod_CodeFixProvider)), Shared]
    public class AsyncVoidMethod_CodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsyncVoidMethod_Analyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindNode(diagnosticSpan) as MethodDeclarationSyntax;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: AsyncVoidMethod_Analyzer.Title,
                    createChangedDocument: c => ReplaceVoidToYueTaskAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(AsyncVoidMethod_Analyzer.Title)),
                diagnostic);
        }

        private async Task<Document> ReplaceVoidToYueTaskAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            // 创建新的返回类型节点
            var newReturnType = SyntaxFactory.ParseTypeName("YueTask").WithTriviaFrom(methodDeclaration.ReturnType);

            // 替换旧的返回类型
            var newMethodDeclaration = methodDeclaration
                .WithReturnType(newReturnType);

            // 获取根节点
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // 替换旧的方法声明
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

            // 返回修改后的文档
            return document.WithSyntaxRoot(newRoot);
        }
    }
}

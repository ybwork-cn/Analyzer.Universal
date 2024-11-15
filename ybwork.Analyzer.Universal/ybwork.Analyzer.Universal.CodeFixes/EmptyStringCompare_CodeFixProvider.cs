using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ybwork.Analyzer.Universal
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyStringCompare_CodeFixProvider)), Shared]
    public class EmptyStringCompare_CodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EmptyStringCompare_Analyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var expression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: EmptyStringCompare_Analyzer.Title,
                    createChangedDocument: c => UseNamedParameterAsync(context.Document, expression, c),
                    equivalenceKey: EmptyStringCompare_Analyzer.Title),
                diagnostic);
        }

        private async Task<Document> UseNamedParameterAsync(Document document, BinaryExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
        {
            // Get the symbol representing the type to be renamed.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            ExpressionSyntax v = expressionSyntax.Left is LiteralExpressionSyntax
                ? expressionSyntax.Right
                : expressionSyntax.Left;

            var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParseExpression("string"),
                    SyntaxFactory.IdentifierName("IsNullOrEmpty")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(v))));

            var newRoot = root.ReplaceNode(expressionSyntax, newExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}

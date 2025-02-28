//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Rename;
//using Microsoft.CodeAnalysis.Text;
//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Composition;
//using System.Data;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ybwork.Analyzer.Universal
//{
//    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamedParameterCodeFixProvider)), Shared]
//    public class NamedParameterCodeFixProvider : CodeFixProvider
//    {
//        public sealed override ImmutableArray<string> FixableDiagnosticIds
//        {
//            get { return ImmutableArray.Create(NamedParameterAnalyzer.DiagnosticId); }
//        }

//        public sealed override FixAllProvider GetFixAllProvider()
//        {
//            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
//            return WellKnownFixAllProviders.BatchFixer;
//        }

//        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

//            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
//            var diagnostic = context.Diagnostics.First();
//            var diagnosticSpan = diagnostic.Location.SourceSpan;

//            // Find the type declaration identified by the diagnostic.
//            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentSyntax>().First();

//            // Register a code action that will invoke the fix.
//            context.RegisterCodeFix(
//                CodeAction.Create(
//                    title: NamedParameterAnalyzer.Title,
//                    createChangedDocument: c => UseNamedParameterAsync(context.Document, declaration, c),
//                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
//                diagnostic);
//        }

//        private async Task<Document> UseNamedParameterAsync(Document document, ArgumentSyntax argumentSyntax, CancellationToken cancellationToken)
//        {
//            // Get the symbol representing the type to be renamed.
//            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
//            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

//            InvocationExpressionSyntax invocationExpression = argumentSyntax.Parent.Parent as InvocationExpressionSyntax;
//            IMethodSymbol methodSymbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;
//            int argumentIndex = invocationExpression.ArgumentList.Arguments.IndexOf(argumentSyntax);

//            string name = methodSymbol.Parameters[argumentIndex].Name;
//            ArgumentSyntax newargumentSyntax = argumentSyntax.WithNameColon(SyntaxFactory.NameColon(name));
//            var newRoot = root.ReplaceNode(argumentSyntax, newargumentSyntax);
//            return document.WithSyntaxRoot(newRoot);
//        }
//    }
//}

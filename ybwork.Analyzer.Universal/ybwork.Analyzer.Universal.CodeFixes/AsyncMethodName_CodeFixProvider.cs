using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ybwork.Analyzer.Universal
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncMethodName_Analyzer)), Shared]
    public class AsyncMethodName_CodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsyncMethodName_Analyzer.DiagnosticId); }
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
                    title: AsyncMethodName_Analyzer.Title,
                    createChangedSolution: c => ReplaceTaskMethodNameAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(AsyncMethodName_Analyzer.Title)),
                diagnostic);
        }

        private async Task<Solution> ReplaceTaskMethodNameAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            // 替换方法名
            string newMethodName = methodDecl.Identifier.Text + "Async";
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);

            // 使用Rename API进行重命名
            return await Renamer.RenameSymbolAsync(document.Project.Solution, methodSymbol, newMethodName, null, cancellationToken);
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ybwork.Analyzer.Universal
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncVoidMethod_Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "YBU011";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        public const string Title = "禁止使用async void方法";
        private const string MessageFormat = "异步方法 '{0}' 返回值应改为YueTask";
        private const string Description = "async void方法应改为async YueTask.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            MethodDeclarationSyntax methodDeclaration = context.Node as MethodDeclarationSyntax;
            bool isAsync = methodDeclaration.Modifiers.Any(modifier => modifier.ValueText == "async");
            bool isVoid = methodDeclaration.ReturnType.GetText().ToString().Trim() == "void";
            if (isAsync && isVoid)
            {
                string str = methodDeclaration.Identifier.Text;
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation(), str);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

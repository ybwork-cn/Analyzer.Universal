using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ybwork.Analyzer.Universal
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncVoidLambda_Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "YBU012";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        public const string Title = "async表达式返回值禁止使为void";
        private const string MessageFormat = "async表达式返回值禁止使为void";
        private const string Description = "async表达式返回值禁止使为void.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var lambda = (LambdaExpressionSyntax)context.Node;

            // 获取语义模型
            var semanticModel = context.SemanticModel;

            if (lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            {
                // 获取lambda表达式的类型
                var delegeteTypeInfo = semanticModel.GetTypeInfo(lambda);
                ITypeSymbol delegeteType = delegeteTypeInfo.ConvertedType;
                IMethodSymbol invokeMethod = ((INamedTypeSymbol)delegeteType).DelegateInvokeMethod;
                ITypeSymbol returnType = invokeMethod.ReturnType;
                bool isVoid = returnType.Name == "Void";
                if (isVoid)
                {
                    var diagnostic = Diagnostic.Create(Rule, lambda.GetLocation(), "");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}

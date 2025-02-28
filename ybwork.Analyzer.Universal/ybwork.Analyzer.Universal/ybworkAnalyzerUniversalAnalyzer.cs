using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamedParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "YBU001";
    public static readonly LocalizableString Title = "为可选参数使用命名参数";
    private static readonly LocalizableString MessageFormat = "方法 '{0}' 的可选参数 '{1}' 应该使用命名参数";
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
        {
            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[i];
                if (argument.NameColon == null && methodSymbol.Parameters[i].IsOptional)
                {
                    var diagnostic = Diagnostic.Create(Rule, argument.GetLocation(), methodSymbol.Name, methodSymbol.Parameters[i].Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}

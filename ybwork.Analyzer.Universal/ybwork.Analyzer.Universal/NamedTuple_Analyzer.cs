using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamedTuple_Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "YBU003";
    public static readonly LocalizableString Title = "元组元素定义必须显式命名";
    private static readonly LocalizableString MessageFormat = "元组元素定义必须显式命名";
    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeTupleElement, SyntaxKind.TupleElement);
    }

    private void AnalyzeTupleElement(SyntaxNodeAnalysisContext context)
    {
        var tupleElement = (TupleElementSyntax)context.Node;
        if (tupleElement.Identifier.Value == null)
        {
            var diagnostic = Diagnostic.Create(Rule, tupleElement.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}

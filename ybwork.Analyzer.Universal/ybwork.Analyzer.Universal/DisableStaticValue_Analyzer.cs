using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using ybwork.Analyzer.Universal;

/// <summary>
/// 禁止使用静态字段或属性（除非标记了[EnableStaticValue]）
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisableStaticValue_Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "YBU0021";
    public static readonly LocalizableString Title = "禁止使用静态字段或属性（除非标记了[EnableStaticValue]）";
    private static readonly LocalizableString MessageFormat = "禁止使用静态{0} `{1}`";
    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeTupleElement, SyntaxKind.FieldDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private void AnalyzeTupleElement(SyntaxNodeAnalysisContext context)
    {
        MemberDeclarationSyntax memberDeclaration = context.Node as MemberDeclarationSyntax;

        bool typeEnable = memberDeclaration.FirstAncestorOrSelf<TypeDeclarationSyntax>().AttributeLists
            .SelectMany(attribute => attribute.Attributes)
            .Select(attribute => context.SemanticModel.GetSymbolInfo(attribute).Symbol.ContainingType)
            .Any(attribute => attribute.Name is nameof(EnableStaticValueAttribute));

        bool enable = typeEnable;
        if (!enable)
        {
            bool memberEnable = memberDeclaration.AttributeLists
                .SelectMany(attribute => attribute.Attributes)
                .Select(attribute => context.SemanticModel.GetSymbolInfo(attribute).Symbol.ContainingType)
                .Any(attribute => attribute.Name is nameof(EnableStaticValueAttribute));
            enable = memberEnable;
        }

        if (enable)
            return;

        if (memberDeclaration is FieldDeclarationSyntax fieldDeclaration)
        {
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var variables = fieldDeclaration.Declaration.Variables;
                foreach (VariableDeclaratorSyntax variable in variables)
                {
                    string name = variable.Identifier.ValueText;
                    var diagnostic = Diagnostic.Create(Rule, fieldDeclaration.GetLocation(), "属性", name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        else if (memberDeclaration is PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                string name = propertyDeclaration.Identifier.ValueText;
                var diagnostic = Diagnostic.Create(Rule, propertyDeclaration.GetLocation(), "属性", name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

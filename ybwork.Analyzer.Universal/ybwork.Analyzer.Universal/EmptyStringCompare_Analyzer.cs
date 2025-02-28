using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ybwork.Analyzer.Universal
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyStringCompare_Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "YBU002";
        public static readonly string Title = "使用string.IsNullOrEmpty";
        private static readonly LocalizableString MessageFormat = "可使用string.IsNullOrEmpty替换字符串判空逻辑";
        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeEqualsExpression, SyntaxKind.EqualsExpression);
        }

        private void AnalyzeEqualsExpression(SyntaxNodeAnalysisContext context)
        {
            var expressionSyntax = (BinaryExpressionSyntax)context.Node;
            var left = expressionSyntax.Left;
            var right = expressionSyntax.Right;

            var semanticModel = context.SemanticModel;

            Check(left, right);
            Check(right, left);

            //Check if the second side is null and the first side is a string
            void Check(ExpressionSyntax first, ExpressionSyntax second)
            {
                if (second is LiteralExpressionSyntax literal)
                {
                    if (literal.IsKind(SyntaxKind.NullLiteralExpression)
                        || literal.IsKind(SyntaxKind.StringLiteralExpression) && literal.Token.Text == "\"\"")
                    {
                        var typeInfo = semanticModel.GetTypeInfo(first);

                        if (typeInfo.Type != null && typeInfo.Type.SpecialType == SpecialType.System_String)
                        {
                            var diagnostic = Diagnostic.Create(Rule, expressionSyntax.GetLocation(), first.ToString());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}

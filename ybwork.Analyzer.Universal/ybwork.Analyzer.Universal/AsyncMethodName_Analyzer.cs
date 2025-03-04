using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ybwork.Analyzer.Universal
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncMethodName_Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "YBU013";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        public const string Title = "异步方法名应有Async后缀";
        private const string MessageFormat = "异步方法 '{0}' 重命名为 '{1}Async'";
        private const string Description = "异步方法名应有Async后缀.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
            string name = methodDeclaration.Identifier.Text;
            if (name.EndsWith("Async"))
                return;

            bool isAsync = methodDeclaration.Modifiers.Any(modifier => modifier.ValueText == "async");
            bool isTask = IsTypeTask(context.SemanticModel, methodDeclaration.ReturnType);
            if (isAsync || isTask)
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation(), name, name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsTypeTask(SemanticModel semanticModel, TypeSyntax type)
        {
            if (type == null)
                return false;

            // 获取 Task 类型
            INamedTypeSymbol taskType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskType == null)
                return false;

            // 检查是否是 Task 或者 Task<T>
            ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(type).Type;
            return SymbolEqualityComparer.Default.Equals(typeSymbol, taskType)
                || (typeSymbol is INamedTypeSymbol namedType
                    && namedType.IsGenericType
                    && SymbolEqualityComparer.Default.Equals(namedType.BaseType, taskType));
        }
    }
}

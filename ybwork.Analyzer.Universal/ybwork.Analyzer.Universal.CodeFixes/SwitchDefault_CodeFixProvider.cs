using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ybwork.Analyzer.Universal
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchDefault_CodeFixProvider)), Shared]
    public class SwitchDefault_CodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create("IDE0010"); }
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

            var statement = root.FindNode(diagnosticSpan) as SwitchStatementSyntax;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "添加throw默认示例",
                    createChangedDocument: c => ReplaceVoidToYueTaskAsync(context.Document, statement, c),
                    equivalenceKey: nameof(AsyncVoidMethod_Analyzer.Title)),
                diagnostic);
        }

        private async Task<Document> ReplaceVoidToYueTaskAsync(Document document, SwitchStatementSyntax switchStatement, CancellationToken cancellationToken)
        {
            // 创建一个空的BlockSyntax作为default case的内容
            var defaultThrowBlock = SyntaxFactory.Block()
                .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName(nameof(NotImplementedException)))
                            .WithArgumentList(SyntaxFactory.ArgumentList()))
                    ));

            // 创建新的SwitchSectionSyntax带有default标签和空的块
            var defaultSection = SyntaxFactory.SwitchSection()
                .AddLabels(SyntaxFactory.DefaultSwitchLabel())
                .AddStatements(defaultThrowBlock.Statements.ToArray());

            // 将新创建的default section添加到switch语句的section列表中
            var newSections = switchStatement.Sections.Add(defaultSection);

            // 使用新的sections更新switch语句
            var newSwitchStatement = switchStatement.WithSections(newSections);

            // 获取根节点
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // 替换旧的方法声明
            var newRoot = root.ReplaceNode(switchStatement, newSwitchStatement);

            // 返回修改后的文档
            return document.WithSyntaxRoot(newRoot);
        }
    }
}

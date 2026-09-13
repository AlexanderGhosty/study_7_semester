using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer;

internal static class Analyzer
{
    private const string Rule = "SA2001_LoopCounterModifiedInBody";

    public static IReadOnlyList<Warning> Analyze(string filePath)
    {
        string source = File.ReadAllText(filePath);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);
        SyntaxNode root = syntaxTree.GetRoot();
        string displayPath = Path.GetRelativePath(Environment.CurrentDirectory, filePath);
        var warnings = new List<Warning>();

        foreach (ForStatementSyntax forStatement in root.DescendantNodes().OfType<ForStatementSyntax>())
        {
            foreach (string counterName in GetCounterNames(forStatement))
            {
                var walker = new CounterMutationWalker(counterName);
                walker.Visit(forStatement.Statement);

                foreach (ExpressionSyntax mutation in walker.Mutations)
                {
                    FileLinePositionSpan mutationLocation = syntaxTree.GetLineSpan(mutation.Span);
                    FileLinePositionSpan loopLocation = syntaxTree.GetLineSpan(forStatement.ForKeyword.Span);
                    int mutationLine = mutationLocation.StartLinePosition.Line + 1;
                    int mutationColumn = mutationLocation.StartLinePosition.Character + 1;
                    int loopLine = loopLocation.StartLinePosition.Line + 1;

                    warnings.Add(new Warning(
                        displayPath,
                        mutationLine,
                        mutationColumn,
                        Rule,
                        $"Счётчик '{counterName}' цикла for, объявленного в строке {loopLine}, повторно изменяется в теле цикла.",
                        "Удалите изменение счётчика из тела и оставьте управление шагом в секции итератора for."));
                }
            }
        }

        return warnings
            .OrderBy(warning => warning.LineNumber)
            .ThenBy(warning => warning.ColumnNumber)
            .ThenBy(warning => warning.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> GetCounterNames(ForStatementSyntax forStatement)
    {
        if (forStatement.Declaration is null)
        {
            yield break;
        }

        foreach (VariableDeclaratorSyntax variable in forStatement.Declaration.Variables)
        {
            string variableName = variable.Identifier.ValueText;

            if (forStatement.Incrementors.Any(
                    incrementor => ContainsDirectMutation(incrementor, variableName)))
            {
                yield return variableName;
            }
        }
    }

    private static bool ContainsDirectMutation(ExpressionSyntax expression, string identifier)
    {
        var walker = new CounterMutationWalker(identifier);
        walker.Visit(expression);
        return walker.Mutations.Count > 0;
    }

    private sealed class CounterMutationWalker(string counterName) : CSharpSyntaxWalker
    {
        public List<ExpressionSyntax> Mutations { get; } = [];

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (IsCounterIdentifier(node.Left))
            {
                Mutations.Add(node);
            }

            base.VisitAssignmentExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if ((node.IsKind(SyntaxKind.PostIncrementExpression)
                    || node.IsKind(SyntaxKind.PostDecrementExpression))
                && IsCounterIdentifier(node.Operand))
            {
                Mutations.Add(node);
            }

            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if ((node.IsKind(SyntaxKind.PreIncrementExpression)
                    || node.IsKind(SyntaxKind.PreDecrementExpression))
                && IsCounterIdentifier(node.Operand))
            {
                Mutations.Add(node);
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
        }

        private bool IsCounterIdentifier(ExpressionSyntax expression)
        {
            return expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, counterName, StringComparison.Ordinal);
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace CodeAnalyzer;

internal static class LivenessAnalysis
{
    private const string Rule = "DFA3001_DeadLocalValue";
    private const string Recommendation =
        "Удалите ненужное присваивание либо используйте вычисленное значение; "
        + "если правая часть имеет побочный эффект, сохраните сам вызов.";

    public static IReadOnlyList<Warning> Analyze(
        ControlFlowGraph graph,
        SyntaxTree syntaxTree,
        string displayPath)
    {
        BlockState[] states = graph.Blocks
            .Select(CreateBlockState)
            .ToArray();

        SolveToFixedPoint(states);

        var warnings = new List<Warning>();
        var reportedDefinitions = new HashSet<DefinitionKey>();

        foreach (BlockState state in states.Where(state => state.Block.IsReachable))
        {
            var live = new HashSet<ILocalSymbol>(state.LiveOut, SymbolEqualityComparer.Default);

            for (int index = state.Accesses.Count - 1; index >= 0; index--)
            {
                AccessEvent access = state.Accesses[index];

                if (access.Kind == AccessKind.Use)
                {
                    live.Add(access.Symbol);
                    continue;
                }

                if (!live.Contains(access.Symbol))
                {
                    var key = new DefinitionKey(access.Symbol, access.Syntax.SpanStart);

                    if (reportedDefinitions.Add(key))
                    {
                        warnings.Add(CreateWarning(
                            access,
                            syntaxTree,
                            displayPath));
                    }
                }

                live.Remove(access.Symbol);
            }
        }

        return warnings;
    }

    private static BlockState CreateBlockState(BasicBlock block)
    {
        var accesses = new List<AccessEvent>();

        foreach (IOperation operation in block.Operations)
        {
            AccessCollector.Collect(operation, accesses);
        }

        if (block.BranchValue is not null)
        {
            AccessCollector.Collect(block.BranchValue, accesses);
        }

        var use = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        var definition = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        foreach (AccessEvent access in accesses)
        {
            if (access.Kind == AccessKind.Use)
            {
                if (!definition.Contains(access.Symbol))
                {
                    use.Add(access.Symbol);
                }
            }
            else
            {
                definition.Add(access.Symbol);
            }
        }

        return new BlockState(block, accesses, use, definition);
    }

    private static void SolveToFixedPoint(IReadOnlyList<BlockState> states)
    {
        bool changed;

        do
        {
            changed = false;

            for (int index = states.Count - 1; index >= 0; index--)
            {
                BlockState state = states[index];

                if (!state.Block.IsReachable)
                {
                    continue;
                }

                var newLiveOut = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

                foreach (BasicBlock successor in GetSuccessors(state.Block))
                {
                    newLiveOut.UnionWith(states[successor.Ordinal].LiveIn);
                }

                var newLiveIn = new HashSet<ILocalSymbol>(
                    newLiveOut,
                    SymbolEqualityComparer.Default);
                newLiveIn.ExceptWith(state.Definition);
                newLiveIn.UnionWith(state.Use);

                if (!state.LiveOut.SetEquals(newLiveOut))
                {
                    state.LiveOut = newLiveOut;
                    changed = true;
                }

                if (!state.LiveIn.SetEquals(newLiveIn))
                {
                    state.LiveIn = newLiveIn;
                    changed = true;
                }
            }
        }
        while (changed);
    }

    private static IEnumerable<BasicBlock> GetSuccessors(BasicBlock block)
    {
        BasicBlock? fallThrough = block.FallThroughSuccessor?.Destination;
        BasicBlock? conditional = block.ConditionalSuccessor?.Destination;

        if (fallThrough is { IsReachable: true })
        {
            yield return fallThrough;
        }

        if (conditional is { IsReachable: true }
            && conditional.Ordinal != fallThrough?.Ordinal)
        {
            yield return conditional;
        }
    }

    private static Warning CreateWarning(
        AccessEvent definition,
        SyntaxTree syntaxTree,
        string displayPath)
    {
        FileLinePositionSpan location = syntaxTree.GetLineSpan(definition.Syntax.Span);
        int line = location.StartLinePosition.Line + 1;
        int column = location.StartLinePosition.Character + 1;

        return new Warning(
            displayPath,
            line,
            column,
            Rule,
            $"Значение локальной переменной '{definition.Symbol.Name}', присвоенное "
                + $"в строке {line}, не считывается до перезаписи или выхода из метода.",
            Recommendation);
    }

    private sealed class BlockState(
        BasicBlock block,
        IReadOnlyList<AccessEvent> accesses,
        HashSet<ILocalSymbol> use,
        HashSet<ILocalSymbol> definition)
    {
        public BasicBlock Block { get; } = block;

        public IReadOnlyList<AccessEvent> Accesses { get; } = accesses;

        public HashSet<ILocalSymbol> Use { get; } = use;

        public HashSet<ILocalSymbol> Definition { get; } = definition;

        public HashSet<ILocalSymbol> LiveIn { get; set; } =
            new(SymbolEqualityComparer.Default);

        public HashSet<ILocalSymbol> LiveOut { get; set; } =
            new(SymbolEqualityComparer.Default);
    }

    private sealed class AccessCollector
    {
        public static void Collect(IOperation operation, ICollection<AccessEvent> accesses)
        {
            new AccessCollector(accesses).Visit(operation);
        }

        private readonly ICollection<AccessEvent> _accesses;

        private AccessCollector(ICollection<AccessEvent> accesses)
        {
            _accesses = accesses;
        }

        private void Visit(IOperation operation)
        {
            switch (operation)
            {
                case IVariableDeclaratorOperation declarator:
                    VisitVariableDeclarator(declarator);
                    return;

                case ISimpleAssignmentOperation assignment:
                    VisitSimpleAssignment(assignment);
                    return;

                case ICompoundAssignmentOperation assignment:
                    VisitCompoundAssignment(assignment);
                    return;

                case IIncrementOrDecrementOperation increment:
                    VisitIncrement(increment);
                    return;

                case IArgumentOperation argument:
                    VisitArgument(argument);
                    return;

                case ILocalReferenceOperation localReference:
                    AddUse(localReference.Local, localReference.Syntax);
                    return;

                case IAnonymousFunctionOperation:
                case ILocalFunctionOperation:
                case IFlowAnonymousFunctionOperation:
                    return;

                default:
                    foreach (IOperation child in operation.ChildOperations)
                    {
                        Visit(child);
                    }

                    return;
            }
        }

        private void VisitVariableDeclarator(IVariableDeclaratorOperation declarator)
        {
            if (declarator.Initializer is null)
            {
                return;
            }

            Visit(declarator.Initializer.Value);
            AddDefinition(declarator.Symbol, declarator.Syntax);
        }

        private void VisitSimpleAssignment(ISimpleAssignmentOperation assignment)
        {
            if (assignment.Target is ILocalReferenceOperation localTarget)
            {
                Visit(assignment.Value);
                AddDefinition(localTarget.Local, assignment.Syntax);
                return;
            }

            Visit(assignment.Target);
            Visit(assignment.Value);
        }

        private void VisitCompoundAssignment(ICompoundAssignmentOperation assignment)
        {
            if (assignment.Target is ILocalReferenceOperation localTarget)
            {
                AddUse(localTarget.Local, localTarget.Syntax);
                Visit(assignment.Value);
                AddDefinition(localTarget.Local, assignment.Syntax);
                return;
            }

            Visit(assignment.Target);
            Visit(assignment.Value);
        }

        private void VisitIncrement(IIncrementOrDecrementOperation increment)
        {
            if (increment.Target is ILocalReferenceOperation localTarget)
            {
                AddUse(localTarget.Local, localTarget.Syntax);
                AddDefinition(localTarget.Local, increment.Syntax);
                return;
            }

            Visit(increment.Target);
        }

        private void VisitArgument(IArgumentOperation argument)
        {
            if (!TryGetLocalTarget(argument.Value, out ILocalSymbol local))
            {
                Visit(argument.Value);
                return;
            }

            switch (argument.Parameter?.RefKind)
            {
                case RefKind.Out:
                    AddDefinition(local, argument.Syntax);
                    break;

                case RefKind.Ref:
                    AddUse(local, argument.Value.Syntax);
                    AddDefinition(local, argument.Syntax);
                    break;

                default:
                    AddUse(local, argument.Value.Syntax);
                    break;
            }
        }

        private static bool TryGetLocalTarget(IOperation operation, out ILocalSymbol local)
        {
            switch (operation)
            {
                case ILocalReferenceOperation localReference:
                    local = localReference.Local;
                    return true;

                case IDeclarationExpressionOperation declaration:
                    return TryGetLocalTarget(declaration.Expression, out local);

                case IConversionOperation conversion:
                    return TryGetLocalTarget(conversion.Operand, out local);

                case IParenthesizedOperation parenthesized:
                    return TryGetLocalTarget(parenthesized.Operand, out local);

                default:
                    local = null!;
                    return false;
            }
        }

        private void AddUse(ILocalSymbol symbol, SyntaxNode syntax)
        {
            _accesses.Add(new AccessEvent(symbol, AccessKind.Use, syntax));
        }

        private void AddDefinition(ILocalSymbol symbol, SyntaxNode syntax)
        {
            _accesses.Add(new AccessEvent(symbol, AccessKind.Definition, syntax));
        }
    }

    private sealed record AccessEvent(
        ILocalSymbol Symbol,
        AccessKind Kind,
        SyntaxNode Syntax);

    private readonly record struct DefinitionKey(ISymbol Symbol, int SpanStart);

    private enum AccessKind
    {
        Use,
        Definition,
    }
}

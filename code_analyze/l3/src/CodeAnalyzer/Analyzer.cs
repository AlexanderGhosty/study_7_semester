using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace CodeAnalyzer;

internal static class Analyzer
{
    private static readonly IReadOnlyList<MetadataReference> MetadataReferences =
        CreateMetadataReferences();

    public static IReadOnlyList<Warning> Analyze(IReadOnlyList<string> filePaths)
    {
        SyntaxTree[] syntaxTrees = filePaths
            .Select(ParseFile)
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzedSources",
            syntaxTrees: syntaxTrees,
            references: MetadataReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var warnings = new List<Warning>();

        foreach (SyntaxTree syntaxTree in syntaxTrees)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            SyntaxNode root = syntaxTree.GetRoot();
            string displayPath = Path.GetRelativePath(
                Environment.CurrentDirectory,
                syntaxTree.FilePath);

            foreach (MethodDeclarationSyntax method in root
                         .DescendantNodes()
                         .OfType<MethodDeclarationSyntax>()
                         .Where(HasBody))
            {
                ControlFlowGraph? graph;

                try
                {
                    graph = ControlFlowGraph.Create(method, semanticModel);
                }
                catch (ArgumentException exception)
                {
                    FileLinePositionSpan location = syntaxTree.GetLineSpan(method.Identifier.Span);
                    int line = location.StartLinePosition.Line + 1;
                    throw new InvalidDataException(
                        $"не удалось построить граф потока управления для метода "
                        + $"'{method.Identifier.ValueText}' в файле '{displayPath}', строка {line}.",
                        exception);
                }

                if (graph is null)
                {
                    FileLinePositionSpan location = syntaxTree.GetLineSpan(method.Identifier.Span);
                    int line = location.StartLinePosition.Line + 1;
                    throw new InvalidDataException(
                        $"не удалось получить семантическую модель метода "
                        + $"'{method.Identifier.ValueText}' в файле '{displayPath}', строка {line}.");
                }

                warnings.AddRange(LivenessAnalysis.Analyze(graph, syntaxTree, displayPath));
            }
        }

        return warnings
            .OrderBy(warning => warning.FilePath, StringComparer.Ordinal)
            .ThenBy(warning => warning.LineNumber)
            .ThenBy(warning => warning.ColumnNumber)
            .ThenBy(warning => warning.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasBody(MethodDeclarationSyntax method)
    {
        return method.Body is not null || method.ExpressionBody is not null;
    }

    private static SyntaxTree ParseFile(string filePath)
    {
        string source = File.ReadAllText(filePath);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest),
            filePath);

        Diagnostic? error = syntaxTree
            .GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        if (error is not null)
        {
            FileLinePositionSpan location = error.Location.GetLineSpan();
            int line = location.StartLinePosition.Line + 1;
            int column = location.StartLinePosition.Character + 1;
            throw new InvalidDataException(
                $"синтаксическая ошибка в файле '{filePath}', строка {line}, "
                + $"столбец {column}: {error.GetMessage()}.");
        }

        return syntaxTree;
    }

    private static IReadOnlyList<MetadataReference> CreateMetadataReferences()
    {
        string? trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            throw new InvalidOperationException(
                "Не удалось получить список системных сборок .NET для семантического анализа.");
        }

        return trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}


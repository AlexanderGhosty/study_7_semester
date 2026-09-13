namespace CodeAnalyzer;

internal sealed record Warning(
    string FilePath,
    int LineNumber,
    string Rule,
    string Message,
    string Recommendation);

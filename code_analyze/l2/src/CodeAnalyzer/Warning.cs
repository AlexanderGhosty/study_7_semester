namespace CodeAnalyzer;

internal sealed record Warning(
    string FilePath,
    int LineNumber,
    int ColumnNumber,
    string Rule,
    string Message,
    string Recommendation);

using System.Text.RegularExpressions;

namespace CodeAnalyzer;

internal static class Analyzer
{
    private const int MaximumLineLength = 120;

    private static readonly Regex ClassDeclarationPattern = new(
        @"\bclass\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<Warning> Analyze(string filePath)
    {
        var warnings = new List<Warning>();
        string displayPath = Path.GetRelativePath(Environment.CurrentDirectory, filePath);
        int lineNumber = 0;

        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;

            if (line.Length > MaximumLineLength)
            {
                warnings.Add(new Warning(
                    displayPath,
                    lineNumber,
                    "STYLE001: длина строки",
                    $"Длина строки — {line.Length} символов, допустимо не более {MaximumLineLength}.",
                    "Разбить выражение или текст на несколько строк."));
            }

            if (line.Contains('\t'))
            {
                warnings.Add(new Warning(
                    displayPath,
                    lineNumber,
                    "STYLE002: табуляция",
                    "Обнаружен символ табуляции.",
                    "Заменить табуляцию пробелами."));
            }

            if (!string.IsNullOrWhiteSpace(line) && line.EndsWith(' '))
            {
                warnings.Add(new Warning(
                    displayPath,
                    lineNumber,
                    "STYLE003: пробелы в конце строки",
                    "После последнего значимого символа есть пробелы.",
                    "Удалить пробелы в конце строки."));
            }

            if (line.Length > 0 && line.All(character => character == ' '))
            {
                warnings.Add(new Warning(
                    displayPath,
                    lineNumber,
                    "STYLE004: пробелы в пустой строке",
                    "Пустая строка содержит пробелы.",
                    "Удалить все символы из пустой строки."));
            }

            foreach (Match match in ClassDeclarationPattern.Matches(line))
            {
                string className = match.Groups["name"].Value.TrimStart('@');

                if (className.Length == 0 || !char.IsUpper(className[0]))
                {
                    warnings.Add(new Warning(
                        displayPath,
                        lineNumber,
                        "STYLE005: имя класса",
                        $"Имя класса '{match.Groups["name"].Value}' начинается не с прописной буквы.",
                        "Переименовать класс так, чтобы его имя начиналось с прописной буквы."));
                }
            }
        }

        return warnings;
    }
}

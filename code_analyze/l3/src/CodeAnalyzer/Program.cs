namespace CodeAnalyzer;

internal static class Program
{
    private const int NoWarnings = 0;
    private const int WarningsFound = 1;
    private const int InputError = 2;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Использование: CodeAnalyzer <файл.cs|каталог>");
            return InputError;
        }

        IReadOnlyList<string> files;

        try
        {
            files = ResolveFiles(args[0]);
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return InputError;
        }

        if (files.Count == 0)
        {
            Console.Error.WriteLine("Ошибка: в указанном каталоге не найдены файлы .cs.");
            return InputError;
        }

        IReadOnlyList<Warning> warnings;

        try
        {
            warnings = Analyzer.Analyze(files);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            Console.Error.WriteLine($"Ошибка анализа: {exception.Message}");
            return InputError;
        }

        foreach (Warning warning in warnings)
        {
            PrintWarning(warning);
        }

        Console.WriteLine($"Всего предупреждений: {warnings.Count}");
        return warnings.Count == 0 ? NoWarnings : WarningsFound;
    }

    private static IReadOnlyList<string> ResolveFiles(string inputPath)
    {
        string fullPath = Path.GetFullPath(inputPath);

        if (File.Exists(fullPath))
        {
            if (!string.Equals(Path.GetExtension(fullPath), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("указанный файл должен иметь расширение .cs.");
            }

            return [fullPath];
        }

        if (Directory.Exists(fullPath))
        {
            return Directory
                .EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
                .Where(file => string.Equals(
                    Path.GetExtension(file),
                    ".cs",
                    StringComparison.OrdinalIgnoreCase))
                .Where(file => !IsBuildArtifact(file, fullPath))
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();
        }

        throw new FileNotFoundException("указанный путь не найден.");
    }

    private static bool IsBuildArtifact(string filePath, string rootPath)
    {
        string relativePath = Path.GetRelativePath(rootPath, filePath);
        return relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => string.Equals(part, "bin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(part, "obj", StringComparison.OrdinalIgnoreCase));
    }

    private static void PrintWarning(Warning warning)
    {
        Console.WriteLine($"Файл: {warning.FilePath}");
        Console.WriteLine($"Строка: {warning.LineNumber}");
        Console.WriteLine($"Правило: {warning.Rule}");
        Console.WriteLine($"Сообщение: {warning.Message}");
        Console.WriteLine($"Рекомендация: {warning.Recommendation}");
        Console.WriteLine();
    }
}


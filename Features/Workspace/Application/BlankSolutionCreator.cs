namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Создание пустого файла решения через <c>dotnet new sln</c> (без шаблонов из NuGet — только встроенный шаблон SDK).
/// </summary>
public static class BlankSolutionCreator
{
    /// <param name="solutionFilePath">Полный путь к будущему <c>.sln</c> (файл ещё не должен существовать).</param>
    public static async Task<BlankSolutionCreateResult> TryCreateAsync(
        string solutionFilePath,
        IDotnetCommandRunner dotnetRunner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dotnetRunner);

        if (string.IsNullOrWhiteSpace(solutionFilePath))
            return BlankSolutionCreateResult.Failure("Путь к решению пустой.");

        string fullPath;
        try
        {
            fullPath = CanonicalFilePath.Normalize(solutionFilePath.Trim());
        }
        catch (Exception ex)
        {
            return BlankSolutionCreateResult.Failure("Некорректный путь: " + ex.Message);
        }

        if (!fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return BlankSolutionCreateResult.Failure("Нужен путь к файлу с расширением .sln.");

        if (File.Exists(fullPath))
            return BlankSolutionCreateResult.Failure("Файл решения уже существует: " + fullPath);

        var solutionName = Path.GetFileNameWithoutExtension(fullPath);
        if (string.IsNullOrWhiteSpace(solutionName))
            return BlankSolutionCreateResult.Failure("Имя решения пустое.");

        var outputDir = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(outputDir))
            return BlankSolutionCreateResult.Failure("Не удалось определить каталог для решения.");

        try
        {
            Directory.CreateDirectory(outputDir);
        }
        catch (Exception ex)
        {
            return BlankSolutionCreateResult.Failure("Не удалось создать каталог: " + ex.Message);
        }

        IReadOnlyList<string> args = ["new", "sln", "-n", solutionName, "-o", outputDir];
        var (ok, exitCode, output) = await dotnetRunner
            .RunAsync(args, workingDirectory: outputDir, cancellationToken)
            .ConfigureAwait(false);

        if (!ok)
        {
            var tail = string.IsNullOrWhiteSpace(output) ? "" : "\r\n" + output.Trim();
            return BlankSolutionCreateResult.Failure($"dotnet new sln завершился с кодом {exitCode}.{tail}");
        }

        if (!File.Exists(fullPath))
            return BlankSolutionCreateResult.Failure(
                "Команда выполнилась, но файл решения не найден. Вывод dotnet:\r\n" + output.Trim());

        return BlankSolutionCreateResult.Success(fullPath);
    }
}

/// <summary>Исход <see cref="BlankSolutionCreator.TryCreateAsync"/>.</summary>
public readonly record struct BlankSolutionCreateResult(bool Ok, string? SolutionPath, string? ErrorMessage)
{
    public static BlankSolutionCreateResult Success(string solutionPath) => new(true, solutionPath, null);

    public static BlankSolutionCreateResult Failure(string message) => new(false, null, message);
}

using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Определяет путь к выходной сборке проекта для отладки через <c>dotnet msbuild -getProperty</c>.</summary>
public static class MsBuildDebugTargetResolver
{
    /// <summary>
    /// Возвращает полный путь к основной сборке (обычно .dll) и проверяет, что проект исполняемый (не Library).
    /// </summary>
    public static async Task<(string? TargetPath, string? Error)> TryResolveAsync(
        string csprojFullPath,
        IDotnetCommandRunner dotnet,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csprojFullPath) || !File.Exists(csprojFullPath))
            return (null, "Файл проекта не найден.");

        var projectDir = Path.GetDirectoryName(Path.GetFullPath(csprojFullPath));
        if (string.IsNullOrEmpty(projectDir))
            return (null, "Не удалось определить каталог проекта.");

        var args = new[]
        {
            "msbuild",
            csprojFullPath,
            "-nologo",
            "-p:Configuration=Debug",
            "-getProperty:OutputType",
            "-getProperty:TargetPath"
        };

        var (success, exitCode, output) = await dotnet.RunAsync(args, projectDir, cancellationToken).ConfigureAwait(false);
        if (!success)
            return (null, $"msbuild завершился с кодом {exitCode}: {output}");

        if (!TryParsePropertiesOutput(output, out var outputType, out var targetPath) || string.IsNullOrWhiteSpace(targetPath))
            return (null, "Не удалось разобрать вывод msbuild. Вывод: " + Truncate(output, 500));

        if (string.Equals(outputType, "Library", StringComparison.OrdinalIgnoreCase))
            return (null, "Проект с выходом Library не подходит как стартовый для отладки (нужен Exe / WinExe).");

        var full = Path.GetFullPath(targetPath);
        if (!File.Exists(full))
            return (null, $"Сборка ещё не найдена: {full}. Собери решение (Собрать) и повтори.");

        return (full, null);
    }

    /// <summary>Разбор JSON-вывода <c>dotnet msbuild -getProperty</c>.</summary>
    public static bool TryParsePropertiesOutput(string stdout, out string? outputType, out string? targetPath)
    {
        outputType = null;
        targetPath = null;
        var s = stdout.Trim();
        if (s.Length == 0)
            return false;

        var start = s.IndexOf('{');
        if (start < 0)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(s[start..]);
            if (!doc.RootElement.TryGetProperty("Properties", out var props))
                return false;
            if (props.TryGetProperty("OutputType", out var ot))
                outputType = ot.GetString();
            if (props.TryGetProperty("TargetPath", out var tp))
                targetPath = tp.GetString();
            return !string.IsNullOrEmpty(targetPath);
        }
        catch
        {
            return false;
        }
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max)
            return s;
        return s[..max] + "…";
    }
}

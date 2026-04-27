namespace CascadeIDE.Services;

/// <summary>
/// Каталог логов inlay: в репозитории рядом с <c>CascadeIDE.sln</c> (<c>.inlay-logs/</c>), не в <c>bin</c> — удобно читать из Cursor.
/// Пробрасывается в процесс как <see cref="EnvironmentVariableName"/> (см. <see cref="EnsureInlayLogEnvironment" /> в <c>Program.Main</c>),
/// чтобы вендорный AvaloniaEdit писал в тот же каталог без ссылки на CascadeIDE.
/// </summary>
internal static class InlayLogPaths
{
    /// <summary>Имя переменной окружения: абсолютный путь каталога для <c>inlay-*.log</c>.</summary>
    public const string EnvironmentVariableName = "CASCADE_INLAY_LOG_DIR";

    private static string? _cached;

    /// <summary>Каталог для append-логов (создаётся при записи).</summary>
    public static string GetLogDirectory()
    {
        // Env выставляется в Main после возможных ранних вызовов — всегда перекрывает кэш.
        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return Path.GetFullPath(fromEnv.Trim());

        if (_cached is not null)
            return _cached;

        if (TryFindDirectoryWithSolutionFile(out var slnDir))
        {
            _cached = Path.Combine(slnDir, ".inlay-logs");
            return _cached;
        }

        _cached = Path.Combine(AppContext.BaseDirectory, ".cascade-ide");
        return _cached;
    }

    /// <summary>
    /// Выставить <see cref="EnvironmentVariableName" />, если пользователь его не задал — до инициализации AvaloniaEdit.
    /// </summary>
    public static void EnsureInlayLogEnvironment()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariableName)))
            return;

        string dir;
        if (TryFindDirectoryWithSolutionFile(out var slnDir))
            dir = Path.Combine(slnDir, ".inlay-logs");
        else
            dir = Path.Combine(AppContext.BaseDirectory, ".cascade-ide");

        try
        {
            Environment.SetEnvironmentVariable(EnvironmentVariableName, dir);
        }
        catch
        {
            // ignore
        }
    }

    private static bool TryFindDirectoryWithSolutionFile([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? directory)
    {
        directory = null;
        var current = AppContext.BaseDirectory;
        for (var depth = 0; depth < 16 && !string.IsNullOrEmpty(current); depth++)
        {
            if (File.Exists(Path.Combine(current, "CascadeIDE.sln")))
            {
                directory = current;
                return true;
            }

            current = Path.GetDirectoryName(current)!;
        }

        return false;
    }
}

namespace CascadeIDE.Services;

/// <summary>
/// Точечный резолв строк settings.toml: <c>&lt;field&gt;_env</c> → литерал или sentinel (ADR 0149).
/// </summary>
public static class SettingsEnvResolver
{
    /// <summary>Явное намерение «искать команду в PATH», не читать переменную <c>PATH</c> (ADR 0149 §2.1).</summary>
    public const string PathLookupSentinel = "PATH";

    /// <summary>Значение для runtime: именованная env → литерал. Sentinel <see cref="PathLookupSentinel"/> здесь не обрабатывается.</summary>
    public static string Resolve(string? literal, string? environmentVariableName) =>
        TryResolve(literal, environmentVariableName, out var value, out _)
            ? value
            : literal ?? "";

    /// <summary>
    /// Путь/команда к процессу (LSP <c>executable</c>, ACP): <c>executable_env = "PATH"</c> → пустая строка (далее пресет + PATH);
    /// иначе именованная переменная → литерал.
    /// </summary>
    public static string ResolveLaunchPath(string? literal, string? environmentVariableName)
    {
        if (IsPathLookupSentinel(environmentVariableName))
            return "";

        return Resolve(literal, environmentVariableName);
    }

    /// <summary>Как <see cref="Resolve"/>, плюс флаг «взято из именованной переменной окружения».</summary>
    public static bool TryResolve(
        string? literal,
        string? environmentVariableName,
        out string value,
        out bool fromEnvironment)
    {
        if (IsPathLookupSentinel(environmentVariableName))
        {
            value = "";
            fromEnvironment = false;
            return false;
        }

        var fromEnv = TryGetEnvironmentValue(environmentVariableName);
        if (fromEnv is not null)
        {
            value = fromEnv;
            fromEnvironment = true;
            return true;
        }

        value = literal ?? "";
        fromEnvironment = false;
        return !string.IsNullOrEmpty(value);
    }

    public static bool IsPathLookupSentinel(string? environmentVariableName) =>
        string.Equals(environmentVariableName?.Trim(), PathLookupSentinel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Непустое значение **именованной** переменной или <see langword="null"/>.</summary>
    public static string? TryGetEnvironmentValue(string? environmentVariableName)
    {
        if (string.IsNullOrWhiteSpace(environmentVariableName) || IsPathLookupSentinel(environmentVariableName))
            return null;

        var raw = Environment.GetEnvironmentVariable(environmentVariableName.Trim());
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Trim();
    }
}

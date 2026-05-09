#nullable enable

namespace CascadeIDE.Services;

/// <summary>
/// Канонические пути к файлам для сравнения (брейкпоинты, DAP, MCP-редактор):
/// <see cref="Path.GetFullPath"/> и регистронезависимое сравнение на Windows.
/// </summary>
public static class CanonicalFilePath
{
    public static string Normalize(string path) => Path.GetFullPath(path);

    /// <summary>Оба пути не пустые; после нормализации сравниваются без учёта регистра.</summary>
    public static bool Equals(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Сравнение с уже нормализованным эталоном (один вызов <see cref="Normalize"/> на кандидата).
    /// Используй, когда эталон вычислен один раз в цикле по брейкпоинтам.
    /// </summary>
    public static bool EqualsNormalized(string normalizedReference, string candidate) =>
        string.Equals(normalizedReference, Normalize(candidate), StringComparison.OrdinalIgnoreCase);
}

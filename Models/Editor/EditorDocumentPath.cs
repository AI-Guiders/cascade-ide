#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Models.Editor;

/// <summary>Канонический путь активного документа редактора; строится через <see cref="CanonicalFilePath"/> для стыковки с деревом решения и DAP/MCP.</summary>
public readonly struct EditorDocumentPath : IEquatable<EditorDocumentPath>
{
    public string Value { get; }

    private EditorDocumentPath(string normalized) => Value = normalized;

    /// <summary>Отказ при пустой строке или при невалидном пути для <see cref="CanonicalFilePath.TryNormalize"/>.</summary>
    public static bool TryCreate(string? rawPath, out EditorDocumentPath path, out string error)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            path = default;
            error = "Пустой file_path.";
            return false;
        }

        var trimmed = rawPath.Trim();
        if (!CanonicalFilePath.TryNormalize(trimmed, out var full))
        {
            path = default;
            error = "Не удалось нормализовать file_path.";
            return false;
        }

        path = new EditorDocumentPath(full);
        error = "";
        return true;
    }

    public bool Equals(EditorDocumentPath other) =>
        string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is EditorDocumentPath other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public static bool operator ==(EditorDocumentPath left, EditorDocumentPath right) => left.Equals(right);

    public static bool operator !=(EditorDocumentPath left, EditorDocumentPath right) => !left.Equals(right);
}

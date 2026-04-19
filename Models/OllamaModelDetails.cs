namespace CascadeIDE.Models;

/// <summary>Детали модели Ollama из API (POST /api/show и GET /api/tags).</summary>
public sealed class OllamaModelDetails
{
    public string? ParameterSize { get; init; }
    public string? QuantizationLevel { get; init; }
    public string? Family { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = [];
    public long? ContextLength { get; init; }
    public string? License { get; init; }
    public long? SizeBytes { get; init; }
    public string? Format { get; init; }
    public string? ModifiedAt { get; init; }

    /// <summary>Краткая строка для отображения (размер, квантизация, контекст, возможности).</summary>
    public string ToShortString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(ParameterSize))
            parts.Add(ParameterSize);
        if (!string.IsNullOrEmpty(QuantizationLevel))
            parts.Add(QuantizationLevel);
        if (ContextLength is > 0)
            parts.Add($"контекст {ContextLength:N0}");
        if (Capabilities.Count > 0)
            parts.Add(string.Join(", ", Capabilities));
        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    /// <summary>Размер в человекочитаемом виде.</summary>
    public string? SizeFormatted =>
        SizeBytes is null or <= 0 ? null
        : SizeBytes < 1024 ? $"{SizeBytes} B"
        : SizeBytes < 1024 * 1024 ? $"{SizeBytes / 1024.0:F1} KB"
        : SizeBytes < 1024 * 1024 * 1024 ? $"{SizeBytes / (1024.0 * 1024):F1} MB"
        : $"{SizeBytes / (1024.0 * 1024 * 1024):F1} GB";
}

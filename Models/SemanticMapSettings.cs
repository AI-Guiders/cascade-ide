namespace CascadeIDE.Models;

/// <summary>Параметры Semantic Map в зоне PFD (<c>[semantic_map]</c> в <c>settings.toml</c>, ADR 0039).</summary>
public sealed class SemanticMapSettings
{
    /// <summary>
    /// <c>list</c> — только список связей; <c>graph</c> — мини-карта; <c>both</c> — оба.
    /// TOML: <c>presentation</c> в секции <c>[semantic_map]</c>.
    /// </summary>
    public string Presentation { get; set; } = "list";

    /// <summary>
    /// Уровень карты: <c>file</c> — файловые связи (baseline ADR 0039), <c>controlFlow</c> — подграф потока управления.
    /// TOML: <c>level</c> в секции <c>[semantic_map]</c>.
    /// </summary>
    public string Level { get; set; } = SemanticMapLevelKind.File;

    /// <summary>Нормализация значения для UI и сохранения (<c>list</c> | <c>graph</c> | <c>both</c>).</summary>
    public static string NormalizePresentation(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v is "graph" or "both" or "list" ? v : "list";
    }

    /// <summary>Нормализация уровня карты (<c>file</c> | <c>controlFlow</c>).</summary>
    public static string NormalizeLevel(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.Equals(v, SemanticMapLevelKind.ControlFlow, StringComparison.OrdinalIgnoreCase))
            return SemanticMapLevelKind.ControlFlow;
        return SemanticMapLevelKind.File;
    }
}

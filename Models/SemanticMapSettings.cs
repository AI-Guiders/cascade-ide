namespace CascadeIDE.Models;

/// <summary>Semantic Map в зоне PFD (ADR 0039). TOML: <c>[semantic_map]</c>.</summary>
public sealed class SemanticMapSettings
{
    /// <summary><c>list</c> | <c>graph</c> | <c>both</c>.</summary>
    public string View { get; set; } = "list";

    /// <summary><c>file</c> | <c>controlFlow</c>.</summary>
    public string Depth { get; set; } = SemanticMapLevelKind.File;

    public static string NormalizeView(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v is "graph" or "both" or "list" ? v : "list";
    }

    public static string NormalizeDepth(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.Equals(v, SemanticMapLevelKind.ControlFlow, StringComparison.OrdinalIgnoreCase))
            return SemanticMapLevelKind.ControlFlow;
        return SemanticMapLevelKind.File;
    }
}

namespace CascadeIDE.Models;

/// <summary>Semantic Map в зоне PFD (ADR 0039). TOML: <c>[semantic_map]</c>.</summary>
public sealed class SemanticMapSettings
{
    /// <summary><c>list</c> | <c>graph</c> | <c>both</c>.</summary>
    public string View { get; set; } = "list";

    /// <summary><c>file</c> | <c>controlFlow</c>.</summary>
    public string Depth { get; set; } = SemanticMapLevelKind.File;

    /// <summary>
    /// Режим Control Flow по <see cref="Depth"/> — тот же критерий, что ветка CF в
    /// <c>RunWorkspaceNavigationMapRefreshAsync</c> и обновление карты по курсору (<c>UpdateSemanticMapCaretOffset</c>).
    /// </summary>
    public bool IsControlFlowDepth =>
        string.Equals(NormalizeDepth(Depth), SemanticMapLevelKind.ControlFlow, StringComparison.Ordinal);

    /// <summary>Соответствует <c>wantList</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> (после <see cref="NormalizeView"/>).</summary>
    public bool WantsSemanticMapList =>
        NormalizeView(View) is "list" or "both";

    /// <summary>Соответствует <c>wantGraph</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c>.</summary>
    public bool WantsSemanticMapGraph =>
        NormalizeView(View) is "graph" or "both";

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

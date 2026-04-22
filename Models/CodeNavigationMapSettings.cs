namespace CascadeIDE.Models;

/// <summary>Карта PFD (вид/глубина/детализация; ADR 0039). TOML: <c>[code_navigation_map]</c>.</summary>
public sealed class CodeNavigationMapSettings
{
    /// <summary><c>list</c> | <c>graph</c> | <c>both</c>.</summary>
    public string View { get; set; } = "list";

    /// <summary><c>file</c> | <c>controlFlow</c>.</summary>
    public string Depth { get; set; } = CodeNavigationMapLevelKind.File;

    /// <summary><c>glance</c> | <c>normal</c> | <c>inspect</c> — детализация композиции (ADR 0055).</summary>
    public string DetailLevel { get; set; } = "normal";

    /// <summary>
    /// Режим Control Flow по <see cref="Depth"/> — тот же критерий, что ветка CF в
    /// <c>RunWorkspaceNavigationMapRefreshAsync</c> и обновление карты по курсору (<c>UpdateCodeNavigationMapCaretOffset</c>).
    /// </summary>
    public bool IsControlFlowDepth =>
        string.Equals(NormalizeDepth(Depth), CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal);

    /// <summary>Соответствует <c>wantList</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> (после <see cref="NormalizeView"/>).</summary>
    public bool WantsCodeNavigationMapList =>
        NormalizeView(View) is "list" or "both";

    /// <summary>Соответствует <c>wantGraph</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c>.</summary>
    public bool WantsCodeNavigationMapGraph =>
        NormalizeView(View) is "graph" or "both";

    public static string NormalizeView(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v is "graph" or "both" or "list" ? v : "list";
    }

    public static string NormalizeDepth(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.Equals(v, CodeNavigationMapLevelKind.ControlFlow, StringComparison.OrdinalIgnoreCase))
            return CodeNavigationMapLevelKind.ControlFlow;
        return CodeNavigationMapLevelKind.File;
    }

    /// <summary>Соответствует <see cref="DetailLevel"/> и вызову композитора карты намерений.</summary>
    public CodeNavigationMapDetailLevel NormalizedDetailLevel => NormalizeDetailLevel(DetailLevel);

    public static CodeNavigationMapDetailLevel NormalizeDetailLevel(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v switch
        {
            "glance" => CodeNavigationMapDetailLevel.Glance,
            "inspect" => CodeNavigationMapDetailLevel.Inspect,
            _ => CodeNavigationMapDetailLevel.Normal
        };
    }
}

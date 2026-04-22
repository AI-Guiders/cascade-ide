namespace CascadeIDE.Models;

/// <summary>
/// PFD-граф: связи файлов (якорь → соседи) и/или control-flow намерения по методу.
/// TOML: <c>[code_navigation.file_relations_and_control_flow_intent_graph]</c>.
/// </summary>
public sealed class FileRelationsAndControlFlowIntentGraphSettings
{
    public const double DefaultCaretIdleRefreshSeconds = 2.0;

    /// <summary><c>list</c> | <c>graph</c> | <c>both</c>.</summary>
    public string View { get; set; } = "list";

    /// <summary><c>file</c> | <c>controlFlow</c>.</summary>
    public string Depth { get; set; } = CodeNavigationMapLevelKind.File;

    /// <summary><c>glance</c> | <c>normal</c> | <c>inspect</c> — детализация композиции (ADR 0055).</summary>
    public string DetailLevel { get; set; } = "normal";

    /// <summary>
    /// Порог «покоя каретки» перед обновлением карты control-flow в секундах.
    /// TOML: <c>…graph].caret_idle_refresh_seconds</c>.
    /// </summary>
    public double CaretIdleRefreshSeconds { get; set; } = DefaultCaretIdleRefreshSeconds;

    /// <summary>
    /// Подавлять побочные эффекты редактора (HUD/карта/tooltip) во время drag-selection ЛКМ.
    /// TOML: <c>…graph].suspend_editor_side_effects_while_selecting</c>.
    /// </summary>
    public bool SuspendEditorSideEffectsWhileSelecting { get; set; }

    /// <summary>
    /// Режим Control Flow по <see cref="Depth"/> — тот же критерий, что ветка CF в
    /// <c>RunWorkspaceNavigationMapRefreshAsync</c> и обновление карты по курсору (<c>UpdateWorkspaceNavigationMapCaretOffset</c>).
    /// </summary>
    public bool IsControlFlowDepth =>
        string.Equals(NormalizeDepth(Depth), CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal);

    /// <summary>Соответствует <c>wantList</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c> (после <see cref="NormalizeView"/>).</summary>
    public bool WantsWorkspaceNavigationMapList =>
        NormalizeView(View) is "list" or "both";

    /// <summary>Соответствует <c>wantGraph</c> в <c>RunWorkspaceNavigationMapRefreshAsync</c>.</summary>
    public bool WantsWorkspaceNavigationMapGraph =>
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

    /// <summary>Соответствует <see cref="DetailLevel"/> и вызову композитора.</summary>
    public CodeNavigationMapDetailLevel NormalizedDetailLevel => NormalizeDetailLevel(DetailLevel);

    /// <summary>Нормализованный порог idle-перестройки: <c>0.15..10</c> секунд.</summary>
    public double NormalizedCaretIdleRefreshSeconds =>
        NormalizeCaretIdleRefreshSeconds(CaretIdleRefreshSeconds);

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

    public static double NormalizeCaretIdleRefreshSeconds(double? value)
    {
        var seconds = value ?? DefaultCaretIdleRefreshSeconds;
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
            seconds = DefaultCaretIdleRefreshSeconds;
        return Math.Clamp(seconds, 0.15, 10.0);
    }
}

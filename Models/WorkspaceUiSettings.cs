namespace CascadeIDE.Models;

/// <summary>Параметры панелей и режима UI workspace (<c>[workspace_ui]</c> в <c>settings.toml</c>).</summary>
public sealed class WorkspaceUiSettings
{
    /// <summary>Развёрнут ли регион Pfd в main grid (TOML: <c>pfd_region_expanded</c>).</summary>
    public bool PfdRegionExpanded { get; set; } = true;
    public bool ShowTerminal { get; set; }
    public bool ShowGit { get; set; }
    public bool ShowInstrumentation { get; set; } = true;

    /// <summary>Focus, Balanced, Power, Debug, AgentChat, Flight (TOML: <c>mode</c>).</summary>
    public string Mode { get; set; } = "Flight";

    /// <summary><c>ru-RU</c>, <c>en-US</c>; пусто — системная локаль (TOML: <c>culture</c>).</summary>
    public string Culture { get; set; } = "";

    /// <summary>
    /// Заблокировать сплиттеры рабочей области: колонки MainGrid (PFD | Forward | MFD), обозреватель решения, панель Git и т.д. TOML: <c>workspace_splitters_locked</c>.
    /// </summary>
    public bool WorkspaceSplittersLocked { get; set; }
}

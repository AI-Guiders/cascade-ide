namespace CascadeIDE.Models;

/// <summary>Параметры панелей и режима UI workspace (<c>[workspace_ui]</c> в <c>settings.toml</c>).</summary>
public sealed class WorkspaceUiSettings
{
    public bool ShowSolutionExplorer { get; set; } = true;
    public bool ShowTerminal { get; set; }
    public bool ShowGit { get; set; }
    public bool ShowInstrumentation { get; set; } = true;

    /// <summary>Focus, Balanced, Power, Debug, AgentChat, Flight (TOML: <c>mode</c>).</summary>
    public string Mode { get; set; } = "Balanced";

    /// <summary><c>ru-RU</c>, <c>en-US</c>; пусто — системная локаль (TOML: <c>culture</c>).</summary>
    public string Culture { get; set; } = "";
}

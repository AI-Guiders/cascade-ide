namespace CascadeIDE.Models;

/// <summary>Панели и режим UI рабочей области. TOML: <c>[workspace]</c>.</summary>
public sealed class WorkspaceSettings
{
    public bool PfdExpanded { get; set; } = true;

    public bool ShowTerminal { get; set; }

    public bool ShowGit { get; set; }

    public bool ShowInstrumentation { get; set; } = true;

    public string Mode { get; set; } = "Flight";

    public string Culture { get; set; } = "";

    public bool SplittersLocked { get; set; }
}

namespace CascadeIDE.Models;

/// <summary>Настройки debug-hints в редакторе.</summary>
public sealed class EditorDebugHintsSettings
{
    public bool Enabled { get; set; } = true;

    public bool ShowAssignments { get; set; } = true;

    public bool ShowConditions { get; set; } = true;
}

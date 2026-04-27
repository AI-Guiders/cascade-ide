namespace CascadeIDE.Models;

/// <summary>Тонкая настройка inlay hints в редакторе.</summary>
public sealed class EditorInlineHintsSettings
{
    public bool Enabled { get; set; } = true;

    public bool ParameterNames { get; set; } = true;

    public bool VariableTypes { get; set; } = true;
}

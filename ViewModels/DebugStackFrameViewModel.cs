namespace CascadeIDE.ViewModels;

/// <summary>Элемент стека вызовов для панели отладки.</summary>
public sealed class DebugStackFrameViewModel(string name, string? file, int line)
{
    public string Name { get; } = name;
    public string? File { get; } = file;
    public int Line { get; } = line;

    public string DisplayText => $"{Name} — {File ?? ""}:{Line}";
}

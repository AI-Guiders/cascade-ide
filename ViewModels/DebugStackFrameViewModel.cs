using System.IO;

namespace CascadeIDE.ViewModels;

/// <summary>Элемент стека вызовов для панели отладки.</summary>
public sealed class DebugStackFrameViewModel
{
    public DebugStackFrameViewModel(int frameIndex, string name, string? file, int line)
    {
        FrameIndex = frameIndex;
        Name = name;
        FilePath = file;
        Line = line;
        FileNameOnly = string.IsNullOrEmpty(file) ? "" : Path.GetFileName(file);
        HasFile = !string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(FileNameOnly);
        LocationText = HasFile ? $"{FileNameOnly}:{line}" : (line > 0 ? $"(строка {line})" : "—");
        DisplayText = $"{Name} — {FilePath ?? ""}:{Line}";
    }

    public int FrameIndex { get; }
    public string FrameIndexText => FrameIndex.ToString();
    public string Name { get; }
    public string? FilePath { get; }
    public int Line { get; }
    public string FileNameOnly { get; }
    public bool HasFile { get; }
    public string LocationText { get; }
    public string DisplayText { get; }
}

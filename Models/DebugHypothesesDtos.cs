namespace CascadeIDE.Models;

/// <summary>Корень JSON-файла гипотез отладки.</summary>
public sealed class DebugHypothesesFileRoot
{
    public int Version { get; set; } = 1;

    public List<DebugHypothesisRecord> Hypotheses { get; set; } = [];
}

/// <summary>Одна гипотеза на диске.</summary>
public sealed class DebugHypothesisRecord
{
    public string Id { get; set; } = "";

    public string Text { get; set; } = "";

    public DebugHypothesisStatus Status { get; set; }
}

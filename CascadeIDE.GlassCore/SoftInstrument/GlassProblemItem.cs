#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>Problems MFD row — Avalonia <c>ProblemListItem</c> parity (Glass full host).</summary>
public sealed record GlassProblemItem(
    string FilePath,
    int Line,
    int Column,
    string Severity,
    string Id,
    string Message)
{
    public string FileName => Path.GetFileName(FilePath);

    public string HeaderLine => $"{Severity} {FileName}({Line},{Column}) {Id}";

    public bool IsError => string.Equals(Severity, "error", StringComparison.OrdinalIgnoreCase);

    public bool IsWarning => !IsError;
}

namespace CascadeIDE.Services;

/// <summary>Inlay «var → тип»: якорь в буфере (0-based смещение сразу после <c>var</c>) и подпись.</summary>
public sealed record EditorTrailingInlayPart(int AnchorOffset, string Label);

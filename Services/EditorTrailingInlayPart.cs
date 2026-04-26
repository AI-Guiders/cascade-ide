namespace CascadeIDE.Services;

/// <summary>EOL inlay: метка (тип для <c>var</c>) в конце строки <see cref="Line1"/> (1-based).</summary>
public sealed record EditorTrailingInlayPart(int Line1, string Label);

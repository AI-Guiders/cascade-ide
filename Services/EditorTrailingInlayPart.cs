namespace CascadeIDE.Services;

/// <summary>
/// Intra-line inlay: <c>var → тип</c>, подписи аргументов <c>param:</c> и индексаторов — якорь 0-based (позиция вставки) и подпись.
/// </summary>
public sealed record EditorTrailingInlayPart(int AnchorOffset, string Label);

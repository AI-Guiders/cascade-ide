#nullable enable

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Строка всплывающего списка (slash autocomplete и др.).</summary>
public readonly record struct SkiaPopupListRow(string? Group, string Title, string Subtitle);

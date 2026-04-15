namespace CascadeIDE.Services.Presentation;

/// <summary>
/// DTO кадра геометрии рабочей строки <c>MainGrid</c> для зон P/F/M.
/// Используется как единый источник для Avalonia-bindings и будущего Skia preview-path.
/// </summary>
public readonly record struct PresentationMainGridLayoutFrame(
    string ColumnDefinitions,
    int ContentZoneCount,
    bool HasExplicitWeights,
    IReadOnlyList<double> NormalizedZoneWeights,
    IReadOnlyList<PresentationZoneBound> ZoneBounds);


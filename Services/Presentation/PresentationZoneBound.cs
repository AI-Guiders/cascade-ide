namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Нормализованные границы зоны на первом экране (0..1 по ширине контентной области).
/// </summary>
public readonly record struct PresentationZoneBound(
    PresentationAnchorKind Zone,
    double StartNormalized,
    double WidthNormalized);


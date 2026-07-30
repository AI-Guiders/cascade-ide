#nullable enable

namespace CascadeIDE.Primitives;

/// <summary>
/// Toolkit-agnostic 2D point for GlassCore graph layout (replaces Avalonia.Point on the layout side).
/// Host paint converts at the Skia/Avalonia boundary.
/// </summary>
public readonly record struct Point2D(double X, double Y);

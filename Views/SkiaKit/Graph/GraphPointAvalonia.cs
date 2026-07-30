#nullable enable
using Avalonia;
using CascadeIDE.Primitives;

namespace CascadeIDE.Views.SkiaKit.Graph;

/// <summary>Layout <see cref="Point2D"/> → Avalonia paint point (Skia/DrawingContext boundary).</summary>
internal static class GraphPointAvalonia
{
    public static Point ToAv(this Point2D p) => new(p.X, p.Y);
}

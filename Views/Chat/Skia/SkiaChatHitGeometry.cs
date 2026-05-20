#nullable enable

using Avalonia;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal static class SkiaChatHitGeometry
{
    public static Rect ToControlRect(SKRect rect) => new(rect.Left, rect.Top, rect.Width, rect.Height);
}

#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Базовая палитра IDE-Skia surfaces (чат, graph-backed cards, …).</summary>
internal readonly record struct SkiaKitPaintTheme(
    SKColor Surface,
    SKColor Content,
    SKColor Border,
    SKColor HoverBorder,
    SKColor SelectedBorder,
    SKColor Role,
    SKColor EmptyHint) : ISkiaKitPaintTheme
{
    public static SkiaKitPaintTheme DarkFallback => new(
        Surface: new SKColor(37, 37, 38),
        Content: new SKColor(223, 228, 236),
        Border: new SKColor(84, 92, 108),
        HoverBorder: new SKColor(126, 196, 255),
        SelectedBorder: new SKColor(196, 146, 255),
        Role: new SKColor(181, 196, 230),
        EmptyHint: new SKColor(160, 160, 160));
}

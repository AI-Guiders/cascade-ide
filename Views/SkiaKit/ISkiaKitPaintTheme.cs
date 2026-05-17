#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Минимальная палитра для Skia-примитивов IDE (карточки, чипы, подписи).</summary>
internal interface ISkiaKitPaintTheme
{
    SKColor Surface { get; }
    SKColor Content { get; }
    SKColor Border { get; }
    SKColor HoverBorder { get; }
    SKColor SelectedBorder { get; }
    SKColor Role { get; }
    SKColor EmptyHint { get; }
}

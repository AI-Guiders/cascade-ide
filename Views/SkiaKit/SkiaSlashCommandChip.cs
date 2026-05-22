#nullable enable

using CascadeIDE.Features.Chat;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Slash-команда в TCI: обёртка над <see cref="SkiaStatusChip"/>.</summary>
internal static class SkiaSlashCommandChip
{
    public static bool ShouldDraw(SlashCommandPreviewKind kind, string? text) =>
        SlashCommandPreviewVisualMapper.ShouldDrawChip(kind, text);

    public static float MeasureLabelWidth(string label, float fontSize, bool monoFont = true)
    {
        using var font = monoFont ? SkiaKitFonts.CreateMono(fontSize) : SkiaKitFonts.CreateUi(fontSize);
        return font.MeasureText(label);
    }

    public static SKRect ComputeChipRect(
        float textLeft,
        float textTop,
        float textLineHeight,
        float labelWidth) =>
        SkiaStatusChip.ComputeRectAroundTextStart(textLeft, textTop, textLineHeight, labelWidth);

    public static void Draw(
        SKCanvas canvas,
        SKRect chipRect,
        ISkiaKitPaintTheme theme,
        SlashCommandPreviewKind kind,
        float fontSize) =>
        SkiaStatusChip.DrawChrome(canvas, chipRect, theme, SkiaSlashPreviewChrome.ToChipSeverity(kind), fontSize);
}

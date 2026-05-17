#nullable enable
using Avalonia;
using Avalonia.Styling;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Мост CascadeTheme (Avalonia) → <see cref="SkiaKitPaintTheme"/>.</summary>
internal static class SkiaKitThemeBridge
{
    public static class Keys
    {
        public const string PanelBackground = "CascadeTheme.ChatPanelBackground";
        public const string BubbleBackground = "CascadeTheme.ChatMessageBubbleBackground";
        public const string LabelForeground = "CascadeTheme.ChatLabelForeground";
        public const string ContentForeground = "CascadeTheme.ChatMessageContentForeground";
        public const string ColumnBorder = "CascadeTheme.EditorColumnBorderBrush";
        public const string Accent = "CascadeTheme.PanelTitleAccentBrush";
    }

    public static SkiaKitPaintTheme ResolveIdeSurface(StyledElement element)
    {
        var theme = SkiaKitPaintTheme.DarkFallback;
        var hasBubble = SkiaKitColor.TrySkColor(element, Keys.BubbleBackground, out var bubble);
        if (hasBubble)
            theme = theme with { Surface = SkiaKitColor.Darken(bubble, 0.92f) };

        if (SkiaKitColor.TrySkColor(element, Keys.PanelBackground, out var panelBg))
            theme = theme with { Surface = panelBg };
        else if (hasBubble)
            theme = theme with { Surface = SkiaKitColor.Darken(bubble, 0.92f) };

        if (SkiaKitColor.TrySkColor(element, Keys.LabelForeground, out var label))
            theme = theme with { Role = label };
        if (SkiaKitColor.TrySkColor(element, Keys.ContentForeground, out var body))
            theme = theme with { Content = body };
        if (SkiaKitColor.TrySkColor(element, Keys.ColumnBorder, out var edge))
            theme = theme with { Border = edge };

        if (SkiaKitColor.TrySkColor(element, Keys.Accent, out var accent))
        {
            theme = theme with { HoverBorder = accent };
            theme = theme with
            {
                SelectedBorder = SkiaKitColor.Blend(accent, new SKColor(255, 255, 255), 0.35f)
            };
        }

        var emptyHint = SkiaKitColor.Blend(theme.Content, theme.Role, 0.58f);
        return theme with { EmptyHint = emptyHint };
    }
}

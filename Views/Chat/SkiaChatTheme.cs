#nullable enable
using Avalonia;
using Avalonia.Styling;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat;

internal readonly record struct SkiaChatTheme(
    SKColor Surface,
    SKColor BubbleAssistant,
    SKColor BubbleUser,
    SKColor Border,
    SKColor HoverBorder,
    SKColor SelectedBorder,
    SKColor Role,
    SKColor Content,
    SKColor EmptyHint,
    SKColor MutedContent,
    SKColor FooterMuted) : ISkiaKitPaintTheme
{
    public static SkiaChatTheme DarkFallback => new(
        Surface: new SKColor(37, 37, 38),
        BubbleAssistant: new SKColor(45, 47, 55),
        BubbleUser: new SKColor(53, 72, 112),
        Border: new SKColor(84, 92, 108),
        HoverBorder: new SKColor(126, 196, 255),
        SelectedBorder: new SKColor(196, 146, 255),
        Role: new SKColor(181, 196, 230),
        Content: new SKColor(223, 228, 236),
        EmptyHint: new SKColor(160, 160, 160),
        MutedContent: new SKColor(196, 202, 214),
        FooterMuted: new SKColor(181, 196, 230));

    public static SkiaChatTheme Resolve(StyledElement element)
    {
        var theme = DarkFallback;
        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.BubbleBackground, out var bubble))
            theme = theme with { BubbleAssistant = bubble };
        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.PanelBackground, out var panelBg))
            theme = theme with { Surface = panelBg };
        else if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.BubbleBackground, out bubble))
            theme = theme with { Surface = SkiaKitColor.Darken(bubble, 0.92f) };

        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.LabelForeground, out var label))
            theme = theme with { Role = label };
        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.ContentForeground, out var body))
            theme = theme with { Content = body };
        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.ColumnBorder, out var edge))
        {
            theme = theme with { Border = edge };
            theme = theme with { BubbleUser = SkiaKitColor.Blend(theme.BubbleAssistant, edge, 0.42f) };
        }

        if (SkiaKitColor.TrySkColor(element, SkiaKitThemeBridge.Keys.Accent, out var accent))
        {
            theme = theme with { HoverBorder = accent };
            theme = theme with
            {
                SelectedBorder = SkiaKitColor.Blend(accent, new SKColor(255, 255, 255), 0.35f)
            };
        }

        var muted = SkiaKitColor.Blend(theme.Content, theme.Role, 0.72f);
        var footer = SkiaKitColor.Blend(theme.Content, theme.Role, 0.58f);
        return theme with
        {
            MutedContent = muted,
            FooterMuted = footer,
            EmptyHint = footer
        };
    }
}

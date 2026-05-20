#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Spike branch feature/RichTextKit: optional RTK path for Intercom feed body (ADR 0123/0129).</summary>
internal static class SkiaRichTextKitFeature
{
    /// <summary>When true, <see cref="SkiaChatBubbleKind.Feed"/> message bodies use Topten.RichTextKit instead of manual wrap/draw.</summary>
    public static bool UseForIntercomFeedBody { get; set; } = true;
}

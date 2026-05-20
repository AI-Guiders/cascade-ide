#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Guards Intercom Skia measure/paint from unbounded line count (UI freeze on long messages).</summary>
internal static class SkiaChatRenderLimits
{
    /// <summary>Default when <see cref="SkiaChatBubbleSpec.MaxBodyLines"/> is unset (<c>int.MaxValue</c>).</summary>
    public const int MaxProseBodyLines = 128;

    /// <summary>Slash-command /help detail and block markdown document.</summary>
    public const int MaxDocumentRows = 256;
}

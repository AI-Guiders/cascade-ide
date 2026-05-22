#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Метрики пузырей: compact Forward feed (<see cref="SkiaChatSurfaceControl.ComfortableFeed"/> = false). Chrome spine/toolbar отдельно.</summary>
internal static class SkiaChatDensity
{
    public static SkiaChatBubbleSpec Apply(in SkiaChatBubbleSpec spec, bool forwardHost)
    {
        if (!forwardHost)
            return spec;

        return spec with
        {
            GapAfter = Math.Max(3f, spec.GapAfter * 0.65f),
            Padding = Math.Max(6f, spec.Padding - 3f),
            LineHeight = Math.Max(13f, spec.LineHeight - 2f),
            TitleHeight = spec.TitleHeight > 0 ? Math.Max(13f, spec.TitleHeight - 2f) : 0,
            FooterHeight = spec.FooterHeight > 0 ? Math.Max(12f, spec.FooterHeight - 2f) : 0,
            MinHeight = spec.MinHeight > 0 ? Math.Max(32f, spec.MinHeight - 8f) : 0,
            ForwardFeedMetrics = true,
        };
    }
}

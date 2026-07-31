#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Virtual History feed scroll policy — stay put when reading history;
/// stick to end only when already pinned near the bottom.
/// </summary>
public static class GlassIntercomFeedScroll
{
    public const double DefaultSlackPx = 24;

    /// <summary>
    /// True when the viewport is at/near the bottom (or content fits entirely).
    /// </summary>
    public static bool IsPinnedToEnd(
        double verticalOffset,
        double extentHeight,
        double viewportHeight,
        double slackPx = DefaultSlackPx)
    {
        if (viewportHeight <= 0)
            return true;
        if (extentHeight <= viewportHeight + slackPx)
            return true;
        if (slackPx < 0)
            slackPx = DefaultSlackPx;

        return verticalOffset + viewportHeight >= extentHeight - slackPx;
    }

    /// <summary>
    /// After rebuild: ScrollToEnd when stickEnd or was pinned; else keep prior offset.
    /// </summary>
    public static double ResolveOffsetAfterRebuild(
        bool stickEnd,
        bool wasPinnedToEnd,
        double priorOffset)
    {
        if (stickEnd || wasPinnedToEnd)
            return double.PositiveInfinity; // caller: ScrollToEnd
        return priorOffset < 0 ? 0 : priorOffset;
    }
}

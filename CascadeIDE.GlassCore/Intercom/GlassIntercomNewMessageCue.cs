#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Virtual History "new below" cue — show when feed is not pinned to end
/// and newer messages arrived (latch / rebuild while reading history).
/// </summary>
public static class GlassIntercomNewMessageCue
{
    public static bool ShouldShow(int pendingCount) => pendingCount > 0;

    public static int AfterArrival(int pendingCount, bool wasPinnedToEnd) =>
        wasPinnedToEnd ? 0 : pendingCount + 1;

    public static int AfterPinnedOrStickEnd(int pendingCount) => 0;

    public static string FormatLabel(int pendingCount)
    {
        if (pendingCount <= 0)
            return "";
        return pendingCount == 1 ? "↓ new" : $"↓ {pendingCount} new";
    }
}

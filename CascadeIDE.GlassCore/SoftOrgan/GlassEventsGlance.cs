#nullable enable
using System.Globalization;
using System.Text;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass MFD Events: habitat/catalog presence glance (no SoftOrgan invent).
/// Live EventTimeline SSOT stays Avalonia <c>EventsMfdPageView</c> / InstrumentationPanel (in-memory).
/// </summary>
public static class GlassEventsGlance
{
    /// <summary>Canonical DataBus event type names (Cockpit/DataBus/Events catalog).</summary>
    public static readonly string[] DataBusCatalog =
    [
        "BuildStateChanged",
        "TestsStateChanged",
        "DebugStateChanged",
        "GitStateChanged",
        "IdeHostStateChanged",
        "HybridIndexStateChanged",
        "SolutionWarmupStateChanged",
        "StartupProjectPathChanged",
        "AgentEnvironmentEvents",
    ];

    public readonly record struct EventsPresenceStatus(
        int LatchLatestCount,
        string? LatchRoot,
        IReadOnlyList<string> Catalog);

    public static EventsPresenceStatus ProbeCurrentHabitat()
    {
        var root = CdpHabitatPaths.StateRoot;
        var count = 0;
        try
        {
            if (Directory.Exists(root))
            {
                foreach (var _ in Directory.EnumerateFiles(root, "*-LATEST.json", SearchOption.TopDirectoryOnly))
                    count++;
            }
        }
        catch
        {
            // ignore
        }

        return new EventsPresenceStatus(count, root, DataBusCatalog);
    }

    public static string TryFormatCurrentHabitat() => Format(ProbeCurrentHabitat());

    /// <summary>Testable formatter (no I/O).</summary>
    public static string Format(EventsPresenceStatus status)
    {
        var level = status.LatchLatestCount > 0 ? "READY" : "IDLE";
        var sb = new StringBuilder();
        sb.Append("Events glance · ").AppendLine(level);
        sb.AppendLine("timeline · Avalonia in-memory (no Glass feed)");
        sb.Append("cdp latches · ")
            .Append(status.LatchLatestCount.ToString(CultureInfo.InvariantCulture))
            .AppendLine(" *-LATEST.json");

        if (!string.IsNullOrWhiteSpace(status.LatchRoot))
            sb.Append("root · ").AppendLine(ShortLeaf(status.LatchRoot));

        sb.Append("bus catalog · ")
            .Append(status.Catalog.Count.ToString(CultureInfo.InvariantCulture))
            .AppendLine(" types");
        foreach (var name in status.Catalog.Take(6))
            sb.Append("· ").AppendLine(name);
        if (status.Catalog.Count > 6)
            sb.Append("· …+")
                .AppendLine((status.Catalog.Count - 6).ToString(CultureInfo.InvariantCulture));

        sb.AppendLine();
        sb.AppendLine("┌ host ──────────────┐");
        sb.AppendLine("│ ■ Glass latch glance │");
        sb.AppendLine("│ □ Avalonia EventsMFD │");
        sb.AppendLine("└─────────────────────┘");
        return sb.ToString().TrimEnd();
    }

    static string ShortLeaf(string path)
    {
        try
        {
            return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch
        {
            return path;
        }
    }
}

#nullable enable
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>Pure policy: when OneOf <c>P/M</c> should auto-switch role (topology-oneof-slash-v0).</summary>
public static class PresentationPmOneOfPolicy
{
    /// <summary>MFD page / seats demand → show M.</summary>
    public static PresentationAnchorKind? FromMfdPage(string? page) =>
        string.IsNullOrWhiteSpace(page) ? null : PresentationAnchorKind.Mfd;

    /// <summary>
    /// Plan latch paints P readout only — never steals OneOf focus.
    /// Auto-Prefer P on every plan-LATEST rewrite thrashed Editor/MFD ↔ Plan.
    /// </summary>
    public static PresentationAnchorKind? FromPlanLatch() => null;

    /// <summary>
    /// SoftOrgan seats may drive MFD only when M pin changed or no sticky instrument page.
    /// P/F-only seat republish must not yank presentation/chord Editor off M.
    /// </summary>
    public static bool SeatsMaySelectMfd(
        string? stickyMfdPage,
        string? seatsMfdPage,
        bool seatsMOrganChanged)
    {
        if (string.IsNullOrWhiteSpace(seatsMfdPage))
            return false;
        if (seatsMOrganChanged)
            return true;
        if (string.IsNullOrWhiteSpace(stickyMfdPage))
            return true;
        return string.Equals(
            stickyMfdPage.Trim(),
            seatsMfdPage.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Toggle XOR: P↔M.</summary>
    public static PresentationAnchorKind Toggle(PresentationAnchorKind current) =>
        current == PresentationAnchorKind.Mfd
            ? PresentationAnchorKind.Pfd
            : PresentationAnchorKind.Mfd;
}

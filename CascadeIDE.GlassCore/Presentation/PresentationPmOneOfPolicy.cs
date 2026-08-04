#nullable enable
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// OneOf <c>P/M</c> role policy (topology-oneof-slash-v0).
/// «Auto» = intent (agent / operator / citizen), not latch side-effects.
/// </summary>
public static class PresentationPmOneOfPolicy
{
    /// <summary>
    /// Explicit MFD page command (presentation / chord / land / citizen) → show M.
    /// </summary>
    public static PresentationAnchorKind? FromMfdPage(string? page) =>
        string.IsNullOrWhiteSpace(page) ? null : PresentationAnchorKind.Mfd;

    /// <summary>
    /// Plan latch paints P readout only — never steals OneOf focus.
    /// </summary>
    public static PresentationAnchorKind? FromPlanLatch() => null;

    /// <summary>
    /// SoftOrgan seats never drive MFD page or OneOf role.
    /// Desk pin republish is not a show-page command; chrome tip only.
    /// Page / OneOf change only via agent|operator|citizen intent wire.
    /// </summary>
    public static bool SeatsMaySelectMfd(
        string? stickyMfdPage,
        string? seatsMfdPage,
        bool seatsMOrganChanged) => false;

    /// <summary>Toggle XOR: P↔M (operator/agent chord).</summary>
    public static PresentationAnchorKind Toggle(PresentationAnchorKind current) =>
        current == PresentationAnchorKind.Mfd
            ? PresentationAnchorKind.Pfd
            : PresentationAnchorKind.Mfd;
}

#nullable enable
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>Pure policy: when OneOf <c>P/M</c> should auto-switch role (topology-oneof-slash-v0).</summary>
public static class PresentationPmOneOfPolicy
{
    /// <summary>MFD page / seats demand → show M.</summary>
    public static PresentationAnchorKind? FromMfdPage(string? page) =>
        string.IsNullOrWhiteSpace(page) ? null : PresentationAnchorKind.Mfd;

    /// <summary>Plan latch / P-facing SoftOrgan → show P.</summary>
    public static PresentationAnchorKind FromPlanLatch() => PresentationAnchorKind.Pfd;

    /// <summary>Toggle XOR: P↔M.</summary>
    public static PresentationAnchorKind Toggle(PresentationAnchorKind current) =>
        current == PresentationAnchorKind.Mfd
            ? PresentationAnchorKind.Pfd
            : PresentationAnchorKind.Mfd;
}

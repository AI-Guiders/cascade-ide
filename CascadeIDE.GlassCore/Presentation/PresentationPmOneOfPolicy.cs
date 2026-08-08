#nullable enable
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// OneOf <c>P/M</c> role policy (topology-oneof-slash-v0 · surface stacks v1).
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
    /// Map show-page intent → preferred channel token for a surface OneOf stack.
    /// Null = no named channel; fall back to <see cref="FromMfdPage"/>.
    /// </summary>
    public static string? PreferSurfaceFromMfdPage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return null;
        return page.Trim().ToLowerInvariant() switch
        {
            "terminal" => "shell",
            "git" => "git",
            "webaiportal" or "browser" => "world",
            "domainboard" or "flightdatastorage" => "sit",
            "events" or "problems" => "alert",
            "hypotheses" or "debugstack" => "probe",
            "build" or "tests" or "hybridindex" or "workspacehealth" or "environmentreadiness"
                => "probe",
            "semanticmap" or "relatedfiles" or "correspondence" or "markdownpreview"
                => "world",
            "chat" => "intercom",
            // MFD Editor Face paints on M (AvalonEdit host) — not Forward "work" (that stuck OneOf on F·Intercom).
            "editor" => "m",
            _ => null,
        };
    }

    /// <summary>
    /// Pick a stack surface for an MFD page: exact prefer token, else any stack member
    /// that paints the same zone as the prefer token, else null.
    /// </summary>
    public static string? ResolveStackSurface(
        IReadOnlyList<string>? stack,
        string? page)
    {
        if (stack is null || stack.Count == 0)
            return null;
        var prefer = PreferSurfaceFromMfdPage(page);
        if (prefer is null)
            return null;
        foreach (var s in stack)
        {
            if (string.Equals(s, prefer, StringComparison.OrdinalIgnoreCase))
                return s.Trim().ToLowerInvariant();
        }

        var zone = GlassPresentationLayout.ZoneForSurface(prefer);
        if (zone is null)
            return null;
        foreach (var s in stack)
        {
            if (GlassPresentationLayout.ZoneForSurface(s) == zone)
                return s.Trim().ToLowerInvariant();
        }

        return null;
    }

    /// <summary>
    /// Plan latch paints P readout only — never steals OneOf focus.
    /// </summary>
    public static PresentationAnchorKind? FromPlanLatch() => null;

    /// <summary>
    /// SoftOrgan seats: quiet republish = chrome tip only.
    /// <paramref name="showFace"/> = PlaceOrgan human attention → SelectMfd when mfd_page set.
    /// </summary>
    public static bool SeatsMaySelectMfd(bool showFace, string? seatsMfdPage) =>
        showFace && !string.IsNullOrWhiteSpace(seatsMfdPage);

    /// <summary>Quiet layout pin / cold seats republish — never auto-switch MFD.</summary>
    public static bool SeatsMaySelectMfd(
        string? stickyMfdPage,
        string? seatsMfdPage,
        bool seatsMOrganChanged) =>
        false;

    /// <summary>Toggle XOR: P↔M (operator/agent chord).</summary>
    public static PresentationAnchorKind Toggle(PresentationAnchorKind current) =>
        current == PresentationAnchorKind.Mfd
            ? PresentationAnchorKind.Pfd
            : PresentationAnchorKind.Mfd;
}

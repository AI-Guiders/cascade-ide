#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Human Face handbook body for SoftOrgan QRH/ECL/alert — not EICAS tip alone.</summary>
public static class SoftOrganFaceHandbook
{
    public static string MarkdownFor(string organId, string label)
    {
        var id = (organId ?? "").Trim().ToLowerInvariant();
        return id switch
        {
            "qrh" => QrhMarkdown(label),
            "ecl" => EclMarkdown(label),
            "alert" or "eicas" or "sa" => AlertMarkdown(label),
            _ => $"# SoftOrgan · {label}\n\nHandbook page for `{organId}`.\n"
        };
    }

    static string QrhMarkdown(string label) =>
        $"""
        # {label} · Quick Reference Handbook

        Human Face page (not EICAS tip alone).

        ## How to fly

        - Ctrl+Q → Soft: QRH (or chord `qr`)
        - Agent desk: `go=qrh` / `@intent qrh open id=…`
        - Citizen peer: `@intent qrh` / `qrh open id=intake-brief`

        ## Dense entries

        | Id | Use |
        |----|-----|
        | intake-brief | cold start / remount |
        | path-mutate | buffer edit gate |
        | dig-before-ask | habitat dig before operator ask |

        Full index lives on MCP `go=qrh` (agent M seat). This Face is the human handbook peel.
        """;

    static string EclMarkdown(string label) =>
        $"""
        # {label} · Emergency Checklist

        Human Face page for densest recoveries.

        ## Common

        - Not connected + CdpMcp still up → `Recover-CdpSeatRemount.ps1 -Seat cdp|cdp-debug`
        - Hard deploy self → terminal_* + KillRunning (never in-proc shell)
        - PathMutate refuse → dig gate reason, not slap-slap Write

        Agent ECL index: `go=ecl` / `@intent ecl`.
        """;

    static string AlertMarkdown(string label) =>
        $"""
        # {label} · Alert / SA

        SoftBoard / SA desk Face peel.

        - `go=sa_desk` / `@intent sa` — situation pulse
        - PreferSurface(alert) keeps chrome; this page is the readable body
        """;
}

#nullable enable

using System.IO;
using System.Text;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Flight Data Storage (FDS) — MFD presence peel for shared plans/reports/notes.
/// ≠ FDR (<c>go=flight_data</c> black-box tape). Skeleton until full storage host.
/// </summary>
internal static class GlassFdsGlance
{
    public static string Format(string? workspaceRoot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FlightDataStorage · FDS");
        sb.AppendLine("┌ shared desk ─────────┐");
        sb.AppendLine("│ ■ Glass skeleton     │");
        sb.AppendLine("│ □ full shelf later   │");
        sb.AppendLine("│ ≠ FDR flight_data    │");

        Append(sb, "shared", CdpHabitatPaths.SharedLatchPath);
        Append(sb, "plan", Path.Combine(CdpHabitatPaths.StateRoot, "plan-LATEST.json"));
        Append(sb, "report", Path.Combine(CdpHabitatPaths.StateRoot, "report-LATEST.json"));
        Append(sb, "pressure", Path.Combine(CdpHabitatPaths.StateRoot, "cdp", "pressure-LATEST.md"));

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            Append(sb, "ws .cdp", Path.Combine(workspaceRoot, ".cdp"));
        }

        sb.AppendLine("└ plans · reports · notes ┘");
        sb.AppendLine("next: attach↔code + plan/report shelf");
        return sb.ToString().TrimEnd();
    }

    static void Append(StringBuilder sb, string label, string path)
    {
        var mark = Directory.Exists(path) || File.Exists(path) ? "READY" : "MISS ";
        sb.AppendLine($"│ {mark} {label,-12} │");
    }
}

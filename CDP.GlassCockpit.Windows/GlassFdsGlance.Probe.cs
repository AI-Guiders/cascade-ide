#nullable enable

using System.IO;
using System.Text.Json;
using CascadeIDE.Features.Cdp;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

internal static partial class GlassFdsGlance
{
    public static GlassGlanceCards.FdsShelfStatus Probe(string? workspaceRoot)
    {
        var plan = ProbePlan();
        var share = ProbeShared(workspaceRoot);
        var report = ProbeReport();
        var pressure = ProbePressure();
        var wake = ProbeWake();
        var cdp = !string.IsNullOrWhiteSpace(workspaceRoot)
                  && Directory.Exists(Path.Combine(workspaceRoot, ".cdp"));

        return new GlassGlanceCards.FdsShelfStatus(
            PlanReady: plan.ready,
            PlanPulse: plan.pulse,
            SharedOn: share.on,
            SharedFile: share.file,
            ReportReady: report.ready,
            ReportPulse: report.pulse,
            PressureReady: pressure.ready,
            PressureLine: pressure.line,
            WakeReady: wake.ready,
            WakeHint: wake.hint,
            WorkspaceCdp: cdp);
    }

    static (bool ready, string? pulse) ProbePlan()
    {
        var path = Path.Combine(CdpHabitatPaths.StateRoot, "plan-LATEST.json");
        if (!File.Exists(path))
            return (false, null);

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            var pulse = Prop(root, "pulse") ?? Prop(root, "feature") ?? "plan";
            return (true, active ? pulse : "(quiet)");
        }
        catch
        {
            return (true, "plan");
        }
    }

    static (bool on, string? file) ProbeShared(string? workspaceRoot = null)
    {
        // Prefer IdeShare operator delivery (share/v1) — what agent share with=operator writes.
        // Fall back to shared_file_latch co-presence (open-buffer ∩ human focus).
        var ide = GlassIdeShareGlance.TryReadOperatorLatest(workspaceRoot);
        if (ide is not null)
            return (true, ide.FileName);

        var latch = CdpHabitatPaths.SharedLatchPath;
        if (!File.Exists(latch))
            return (false, null);

        try
        {
            var view = LatchPaint.PaintShared(File.ReadAllText(latch));
            if (view is null)
                return (true, null);
            var file = view.Path is { Length: > 0 } p ? Path.GetFileName(p) : null;
            return (view.Shared, file);
        }
        catch
        {
            return (true, null);
        }
    }

    static (bool ready, string? pulse) ProbeReport()
    {
        var path = Path.Combine(CdpHabitatPaths.StateRoot, "report-LATEST.json");
        if (!File.Exists(path))
            return (false, null);

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var pulse = Prop(doc.RootElement, "pulse")
                        ?? Prop(doc.RootElement, "title")
                        ?? Prop(doc.RootElement, "chrome_hint")
                        ?? "report";
            return (true, pulse);
        }
        catch
        {
            return (true, "report");
        }
    }

    static (bool ready, string? line) ProbePressure()
    {
        var candidates = new[]
        {
            Path.Combine(CdpHabitatPaths.StateRoot, "cdp", "pressure-LATEST.md"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "cdp-mcp", "cdp", "pressure-LATEST.md")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;
            try
            {
                var head = File.ReadLines(path).Take(12).ToList();
                var plan = head.FirstOrDefault(l => l.StartsWith("- plan:", StringComparison.OrdinalIgnoreCase));
                var ignite = head.FirstOrDefault(l => l.StartsWith("- ignite:", StringComparison.OrdinalIgnoreCase));
                var line = plan ?? ignite ?? "pressure stashed";
                line = line.TrimStart('-', ' ').Trim();
                if (line.StartsWith("plan:", StringComparison.OrdinalIgnoreCase))
                    line = line[5..].Trim();
                if (line.StartsWith("ignite:", StringComparison.OrdinalIgnoreCase))
                    line = line[7..].Trim();
                return (true, line);
            }
            catch
            {
                /* try next */
            }
        }

        return (false, null);
    }

    static (bool ready, string? hint) ProbeWake()
    {
        var path = CdpHabitatPaths.IgniteWakeLatchPath;
        if (!File.Exists(path))
            return (false, null);

        try
        {
            var view = LatchPaint.PaintIgniteWake(File.ReadAllText(path));
            if (view is null)
                return (true, null);
            return (true, string.IsNullOrWhiteSpace(view.Task) ? view.ChromeHint : view.Task);
        }
        catch
        {
            return (true, null);
        }
    }
}

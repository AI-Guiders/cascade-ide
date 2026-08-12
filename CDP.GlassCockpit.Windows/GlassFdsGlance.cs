#nullable enable

using System.IO;
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Cdp;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Flight Data Storage (FDS) — MFD shelf peel for shared plans/reports/notes.
/// ≠ FDR (<c>go=flight_data</c> black-box tape).
/// </summary>
internal static partial class GlassFdsGlance
{
    public static string Format(string? workspaceRoot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FlightDataStorage · FDS");
        sb.AppendLine("┌ shelf ───────────────┐");
        sb.AppendLine("│ ■ Glass live peels   │");
        sb.AppendLine("│ ≠ FDR flight_data    │");

        AppendPlan(sb);
        AppendShared(sb, workspaceRoot);
        AppendReport(sb);
        AppendPressure(sb);
        AppendIgniteWake(sb);

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var cdp = Path.Combine(workspaceRoot, ".cdp");
            AppendMark(sb, "ws .cdp", Directory.Exists(cdp));
        }

        sb.AppendLine("└ plans · reports · notes ┘");
        sb.AppendLine("/fds · seats latch MFD · /open path:line");
        return sb.ToString().TrimEnd();
    }

    static void AppendPlan(StringBuilder sb)
    {
        var path = Path.Combine(CdpHabitatPaths.StateRoot, "plan-LATEST.json");
        if (!File.Exists(path))
        {
            AppendMark(sb, "plan", false);
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            var pulse = Prop(root, "pulse") ?? Prop(root, "feature") ?? "plan";
            var task = Prop(root, "task");
            var line = Truncate(active ? pulse : "(quiet)", 28);
            sb.AppendLine($"│ PLAN  {line}");
            if (!string.IsNullOrWhiteSpace(task))
                sb.AppendLine($"│   › {Truncate(task, 26)}");
        }
        catch
        {
            AppendMark(sb, "plan", true);
        }
    }

    static void AppendShared(StringBuilder sb, string? workspaceRoot)
    {
        var ide = GlassIdeShareGlance.TryReadOperatorLatest(workspaceRoot);
        if (ide is not null)
        {
            sb.AppendLine($"│ SHARE on · {Truncate(ide.FileName, 20)}");
            return;
        }

        var latch = CdpHabitatPaths.SharedLatchPath;
        if (!File.Exists(latch))
        {
            AppendMark(sb, "shared", false);
            return;
        }

        try
        {
            var raw = File.ReadAllText(latch);
            var view = LatchPaint.PaintShared(raw);
            if (view is null)
            {
                AppendMark(sb, "shared", true);
                return;
            }

            var file = view.Path is { Length: > 0 } p
                ? Path.GetFileName(p)
                : "—";
            sb.AppendLine($"│ SHARE {(view.Shared ? "on" : "off")} · {Truncate(file, 20)}");
        }
        catch
        {
            AppendMark(sb, "shared", true);
        }
    }

    static void AppendReport(StringBuilder sb)
    {
        var path = Path.Combine(CdpHabitatPaths.StateRoot, "report-LATEST.json");
        if (!File.Exists(path))
        {
            AppendMark(sb, "report", false);
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var pulse = Prop(doc.RootElement, "pulse")
                        ?? Prop(doc.RootElement, "title")
                        ?? Prop(doc.RootElement, "chrome_hint")
                        ?? "report";
            sb.AppendLine($"│ REPT  {Truncate(pulse, 28)}");
        }
        catch
        {
            AppendMark(sb, "report", true);
        }
    }

    static void AppendPressure(StringBuilder sb)
    {
        // Habitat + isolation roots: pressure lives under cdp/ or ws/*/cdp/
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
                sb.AppendLine($"│ NOTE  {Truncate(line, 28)}");
                return;
            }
            catch
            {
                /* try next */
            }
        }

        AppendMark(sb, "pressure", false);
    }

    static void AppendMark(StringBuilder sb, string label, bool ready) =>
        sb.AppendLine($"│ {(ready ? "READY" : "MISS ")} {label,-12} │");

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    static string Truncate(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (s.Length <= max)
            return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }
}

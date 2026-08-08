#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.SoftOrgan;

/// <summary>Parse dotnet test console output into fail rows + pass/fail summary (Glass MFD peel).</summary>
public static partial class GlassTestOutputParse
{
    public readonly record struct FailRow(string Display, string Name, string? Message);

    public readonly record struct Summary(int Total, int Passed, int Failed, int Skipped)
    {
        public string Label => $"{Passed} passed · {Failed} failed · {Skipped} skipped · {Total} total";
        public bool Success => Failed == 0;
    }

    [GeneratedRegex(
        @"^\s*(Passed|Failed|Skipped)\s+(.+?)(?:\s+\[(\d+)\s*ms\])?\s*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ResultLine();

    [GeneratedRegex(
        @"^\s*(?:Error Message|Message|Stack Trace):\s*(.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex MessageLine();

    [GeneratedRegex(
        @"(?:Failed|Passed)!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SummaryLine();

    public static IReadOnlyList<FailRow> ParseFails(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var fails = new List<FailRow>();
        string? lastName = null;
        var messageLines = new List<string>();

        void Flush()
        {
            if (lastName is null)
                return;

            var msg = messageLines.Count > 0 ? string.Join(" ", messageLines).Trim() : null;
            if (string.IsNullOrWhiteSpace(msg))
                msg = null;
            var display = msg is null ? $"✗ {lastName}" : $"✗ {lastName} — {msg}";
            fails.Add(new FailRow(display, lastName, msg));
            lastName = null;
            messageLines.Clear();
        }

        foreach (var raw in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var m = ResultLine().Match(line);
            if (m.Success)
            {
                Flush();
                if (string.Equals(m.Groups[1].Value, "Failed", StringComparison.OrdinalIgnoreCase))
                    lastName = m.Groups[2].Value.Trim();
                continue;
            }

            if (lastName is null)
                continue;

            var msgMatch = MessageLine().Match(line);
            if (msgMatch.Success)
            {
                if (!string.IsNullOrWhiteSpace(msgMatch.Groups[1].Value))
                    messageLines.Add(msgMatch.Groups[1].Value.Trim());
                continue;
            }

            // Stop before VSTest summary / Glass pump footers (keep Display jump-sized).
            if (SummaryLine().IsMatch(line)
                || line.StartsWith("Failed!", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Passed!", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("┌", StringComparison.Ordinal))
            {
                Flush();
                continue;
            }

            // Collect body/stack while inside a Failed block — empty "Error Message:"/
            // "Stack Trace:" headers alone must not gate jump paths (SoftFL Tests fail).
            messageLines.Add(line);
        }

        Flush();
        return fails;
    }

    public static Summary ParseSummary(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return new Summary(0, 0, 0, 0);

        var passed = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var raw in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var m = ResultLine().Match(raw.Trim());
            if (!m.Success)
                continue;

            switch (m.Groups[1].Value)
            {
                case "Passed": passed++; break;
                case "Failed": failed++; break;
                case "Skipped": skipped++; break;
            }
        }

        var summary = SummaryLine().Match(output);
        if (summary.Success)
        {
            failed = int.Parse(summary.Groups[1].ValueSpan);
            passed = int.Parse(summary.Groups[2].ValueSpan);
            skipped = int.Parse(summary.Groups[3].ValueSpan);
        }
        else if (passed + failed + skipped == 0)
        {
            var fails = ParseFails(output);
            if (fails.Count > 0)
                failed = fails.Count;
        }

        return new Summary(passed + failed + skipped, passed, failed, skipped);
    }

    /// <summary>Best-effort path:line from fail message/stack (dotnet test). SoftFL denser — no invent.</summary>
    public static bool TryResolveFailJump(FailRow row, string? workspaceRoot, out string path, out int? line)
    {
        path = "";
        line = null;
        var hay = string.IsNullOrWhiteSpace(row.Message) ? row.Display : $"{row.Display}\n{row.Message}";
        var m = StackPathLine().Match(hay);
        if (!m.Success)
            return false;

        var raw = m.Groups[1].Value.Trim();
        if (raw.Length == 0)
            return false;

        if (int.TryParse(m.Groups[2].ValueSpan, out var ln) && ln > 0)
            line = ln;

        if (Path.IsPathRooted(raw) && File.Exists(raw))
        {
            path = raw;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var combined = Path.GetFullPath(Path.Combine(workspaceRoot, raw));
            if (File.Exists(combined))
            {
                path = combined;
                return true;
            }
        }

        path = raw;
        return File.Exists(raw);
    }

    [GeneratedRegex(
        @"((?:[A-Za-z]:)?[^:\r\n]+?\.(?:cs|fs|vb))\s*(?::\s*(?:line\s+)?|[\(,])\s*(\d+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex StackPathLine();
}

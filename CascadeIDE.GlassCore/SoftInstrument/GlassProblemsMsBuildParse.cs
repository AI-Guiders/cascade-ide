#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.SoftInstrument;

/// <summary>Parse MSBuild / dotnet CLI diagnostic lines into Problems rows.</summary>
public static partial class GlassProblemsMsBuildParse
{
    [GeneratedRegex(
        @"^(?<path>.+?)\((?<line>\d+)(?:,(?<col>\d+))?\):\s+(?<sev>error|warning)\s+(?<id>[A-Za-z]+\d+)?:?\s*(?<msg>.*)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiagLine();

    public static IReadOnlyList<GlassProblemItem> Parse(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var rows = new List<GlassProblemItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var m = DiagLine().Match(line);
            if (!m.Success)
                continue;

            var path = m.Groups["path"].Value.Trim();
            if (!int.TryParse(m.Groups["line"].Value, out var ln))
                continue;
            var col = 1;
            if (m.Groups["col"].Success && int.TryParse(m.Groups["col"].Value, out var parsedCol))
                col = Math.Max(1, parsedCol);

            var sev = m.Groups["sev"].Value.ToLowerInvariant();
            var id = m.Groups["id"].Success ? m.Groups["id"].Value : "";
            var msg = m.Groups["msg"].Value.Trim();
            var key = $"{path}|{ln}|{col}|{sev}|{id}|{msg}";
            if (!seen.Add(key))
                continue;

            rows.Add(new GlassProblemItem(path, ln, col, sev, id, msg));
        }

        return rows;
    }
}

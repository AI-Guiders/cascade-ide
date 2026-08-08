#nullable enable

using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>Thin Glass workspace text search via <c>rg --json</c> (no Avalonia Ripgrep peel).</summary>
internal static class GlassWorkspaceTextSearch
{
    public readonly record struct Hit(string FullPath, int LineNumber, string PreviewText);

    public static IReadOnlyList<Hit> Search(string workspaceRoot, string pattern, int maxMatches = 200)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(workspaceRoot)
            || !System.IO.Directory.Exists(workspaceRoot))
            return [];

        maxMatches = Math.Clamp(maxMatches, 1, 5_000);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "rg",
                WorkingDirectory = workspaceRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--json");
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add(pattern.Trim());
            psi.ArgumentList.Add(".");

            using var process = Process.Start(psi);
            if (process is null)
                return FallbackEnumerate(workspaceRoot, pattern, maxMatches);

            var hits = new List<Hit>(Math.Min(maxMatches, 256));
            while (hits.Count < maxMatches)
            {
                var line = process.StandardOutput.ReadLine();
                if (line is null)
                    break;
                if (TryParseRgMatch(line, workspaceRoot, out var hit))
                    hits.Add(hit);
            }

            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                /* ignore */
            }

            return hits;
        }
        catch
        {
            return FallbackEnumerate(workspaceRoot, pattern, maxMatches);
        }
    }

    static bool TryParseRgMatch(string line, string workspaceRoot, out Hit hit)
    {
        hit = default;
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "match")
                return false;
            if (!root.TryGetProperty("data", out var data))
                return false;

            string? pathText = null;
            if (data.TryGetProperty("path", out var pathObj) && pathObj.TryGetProperty("text", out var pathInner))
                pathText = pathInner.GetString();
            var lineNumber = 0;
            if (data.TryGetProperty("line_number", out var ln) && ln.TryGetInt32(out var lnv))
                lineNumber = lnv;
            string? lineText = null;
            if (data.TryGetProperty("lines", out var linesObj) && linesObj.TryGetProperty("text", out var linesInner))
                lineText = linesInner.GetString()?.TrimEnd('\r', '\n');

            if (string.IsNullOrEmpty(pathText) || lineNumber <= 0)
                return false;

            var full = System.IO.Path.IsPathRooted(pathText)
                ? pathText
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(
                    workspaceRoot,
                    pathText.Replace('/', System.IO.Path.DirectorySeparatorChar)));
            hit = new Hit(full, lineNumber, lineText ?? "");
            return true;
        }
        catch
        {
            return false;
        }
    }

    static IReadOnlyList<Hit> FallbackEnumerate(string workspaceRoot, string pattern, int maxMatches)
    {
        var needle = pattern.Trim();
        if (needle.Length == 0)
            return [];

        var hits = new List<Hit>();
        string[] skip = [".git", "bin", "obj", "node_modules", ".vs"];
        try
        {
            foreach (var file in System.IO.Directory.EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories))
            {
                if (skip.Any(s => file.Contains(
                        $"{System.IO.Path.DirectorySeparatorChar}{s}{System.IO.Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase)))
                    continue;
                var ext = System.IO.Path.GetExtension(file);
                if (ext is not (".cs" or ".xaml" or ".md" or ".toml" or ".json" or ".ps1" or ".py" or ".txt" or ".csproj"))
                    continue;

                string[] lines;
                try
                {
                    lines = System.IO.File.ReadAllLines(file);
                }
                catch
                {
                    continue;
                }

                for (var i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].Contains(needle, StringComparison.OrdinalIgnoreCase))
                        continue;
                    hits.Add(new Hit(file, i + 1, lines[i].Trim()));
                    if (hits.Count >= maxMatches)
                        return hits;
                }
            }
        }
        catch
        {
            /* best-effort */
        }

        return hits;
    }
}

#nullable enable

using System.Diagnostics;
using System.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// CIDE GitPanel parity: drop porcelain rows that match <c>.gitignore</c>
/// even when wrongly tracked (<c>check-ignore --no-index</c>).
/// </summary>
public static class GlassGitIgnoreFilter
{
    public static IReadOnlyList<GlassGitPorcelainParse.Row> DropIgnored(
        string? workspaceRoot,
        IReadOnlyList<GlassGitPorcelainParse.Row> rows)
    {
        if (rows.Count == 0)
            return rows;

        var cwd = ResolveToplevel(workspaceRoot);
        var ignored = QueryIgnored(cwd, rows.Select(r => r.Path));
        if (ignored.Count == 0)
            return rows;

        return rows.Where(r => !ignored.Contains(Normalize(r.Path))).ToList();
    }

    public static string ResolveToplevel(string? workspaceRoot)
    {
        var cwd = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Environment.CurrentDirectory
            : workspaceRoot.Trim();

        var top = GlassGitProcess.Run(cwd, "rev-parse", "--show-toplevel");
        if (top.Ok && !string.IsNullOrWhiteSpace(top.Output))
        {
            var line = top.Output.Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (line.Length > 0 && Directory.Exists(line[0]))
                return line[0];
        }

        return cwd;
    }

    static HashSet<string> QueryIgnored(string cwd, IEnumerable<string> paths)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (list.Count == 0)
            return set;

        // NUL-delimited stdin — paths may contain spaces.
        var payload = string.Join('\0', list) + '\0';
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(cwd);
        psi.ArgumentList.Add("check-ignore");
        psi.ArgumentList.Add("--no-index");
        psi.ArgumentList.Add("-z");
        psi.ArgumentList.Add("--stdin");

        try
        {
            using var p = Process.Start(psi);
            if (p is null)
                return set;

            p.StandardInput.Write(payload);
            p.StandardInput.Close();
            var stdout = p.StandardOutput.ReadToEnd();
            _ = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(30_000))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return set;
            }

            // Exit 0 = ≥1 match, 1 = none — both ok.
            foreach (var path in stdout.Split('\0', StringSplitOptions.RemoveEmptyEntries))
                set.Add(Normalize(path));
        }
        catch
        {
            /* best-effort: show raw porcelain */
        }

        return set;
    }

    static string Normalize(string path) =>
        path.Replace('\\', '/').Trim().Trim('"');
}

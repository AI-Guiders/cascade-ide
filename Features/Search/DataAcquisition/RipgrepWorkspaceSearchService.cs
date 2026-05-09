using System.Diagnostics;
using System.Text.Json;

namespace CascadeIDE.Features.Search.DataAcquisition;

/// <summary>Одно совпадение <c>rg --json</c> (полный путь к файлу, 1-based строка).</summary>
public readonly record struct RipgrepWorkspaceMatch(string Path, int LineNumber, string LineText);

/// <summary>
/// DAL: поиск по файлам в каталоге workspace через внешний <see href="https://github.com/BurntSushi/ripgrep">ripgrep</see> (<c>rg</c>).
/// </summary>
/// <remarks>
/// По умолчанию запускается имя <c>rg</c> (разрешение через PATH ОС). Отдельный бинарь в составе IDE не поставляется —
/// на любой платформе достаточно установить ripgrep и убедиться, что <c>rg</c> находится в PATH; иначе передай полный путь в <c>rg_path</c>.
/// </remarks>
public static class RipgrepWorkspaceSearchService
{
    private const int DefaultMaxMatches = 200;
    private const int AbsoluteMaxMatches = 50_000;

    /// <summary>
    /// Запускает <c>rg --json</c> с рабочим каталогом <paramref name="workspaceRoot"/>; возвращает JSON с массивом совпадений или полем <c>error</c>.
    /// </summary>
    public static async Task<string> SearchAsync(
        string workspaceRoot,
        string pattern,
        string? subPath,
        bool fixedString,
        string? glob,
        int maxMatches,
        string? rgExecutable,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return JsonSerializer.Serialize(new { error = "Missing pattern." });

        workspaceRoot = CanonicalFilePath.Normalize(workspaceRoot.Trim());
        if (!Directory.Exists(workspaceRoot))
            return JsonSerializer.Serialize(new { error = "Workspace root is not a directory: " + workspaceRoot });

        var relSearch = ResolveSafeRelativeSearchPath(workspaceRoot, subPath);
        if (relSearch is null)
            return JsonSerializer.Serialize(new { error = "subpath escapes workspace root." });

        maxMatches = Math.Clamp(maxMatches <= 0 ? DefaultMaxMatches : maxMatches, 1, AbsoluteMaxMatches);

        var rg = string.IsNullOrWhiteSpace(rgExecutable) ? "rg" : rgExecutable.Trim();
        try
        {
            var (matches, error, truncated, stderr) = await RunRipgrepCoreAsync(
                    workspaceRoot, rg, pattern, relSearch, fixedString, glob, maxMatches, cancellationToken)
                .ConfigureAwait(false);
            if (error is not null)
                return JsonSerializer.Serialize(new { error = error });

            return JsonSerializer.Serialize(new
            {
                workspace_root = workspaceRoot,
                pattern,
                matches = matches.Select(x => new { path = x.path, line_number = x.line_number, line_text = x.line_text }).ToList(),
                match_count = matches.Count,
                truncated,
                stderr = string.IsNullOrEmpty(stderr) ? null : stderr
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>Совпадения без сериализации в JSON (палитра «Перейти к…»).</summary>
    public static async Task<(IReadOnlyList<RipgrepWorkspaceMatch> Matches, string? Error)> SearchMatchesAsync(
        string workspaceRoot,
        string pattern,
        string? subPath,
        bool fixedString,
        string? glob,
        int maxMatches,
        string? rgExecutable,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return (Array.Empty<RipgrepWorkspaceMatch>(), "Missing pattern.");

        workspaceRoot = CanonicalFilePath.Normalize(workspaceRoot.Trim());
        if (!Directory.Exists(workspaceRoot))
            return (Array.Empty<RipgrepWorkspaceMatch>(), "Workspace root is not a directory: " + workspaceRoot);

        var relSearch = ResolveSafeRelativeSearchPath(workspaceRoot, subPath);
        if (relSearch is null)
            return (Array.Empty<RipgrepWorkspaceMatch>(), "subpath escapes workspace root.");

        maxMatches = Math.Clamp(maxMatches <= 0 ? DefaultMaxMatches : maxMatches, 1, AbsoluteMaxMatches);

        var rg = string.IsNullOrWhiteSpace(rgExecutable) ? "rg" : rgExecutable.Trim();
        try
        {
            var (raw, error, _, _) = await RunRipgrepCoreAsync(
                    workspaceRoot, rg, pattern, relSearch, fixedString, glob, maxMatches, cancellationToken)
                .ConfigureAwait(false);
            if (error is not null)
                return (Array.Empty<RipgrepWorkspaceMatch>(), error);

            var list = new RipgrepWorkspaceMatch[raw.Count];
            for (var i = 0; i < raw.Count; i++)
            {
                var m = raw[i];
                list[i] = new RipgrepWorkspaceMatch(m.path, m.line_number, m.line_text);
            }

            return (list, null);
        }
        catch (Exception ex)
        {
            return (Array.Empty<RipgrepWorkspaceMatch>(), ex.Message);
        }
    }

    /// <summary>Возвращает относительный путь поиска от <paramref name="workspaceRoot"/> или null при обходе вверх.</summary>
    private static string? ResolveSafeRelativeSearchPath(string workspaceRoot, string? subPath)
    {
        if (string.IsNullOrWhiteSpace(subPath))
            return ".";

        var combined = CanonicalFilePath.Normalize(Path.Combine(workspaceRoot, subPath.Trim()));
        var root = CanonicalFilePath.Normalize(workspaceRoot);
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;
        if (string.Equals(combined, root, StringComparison.OrdinalIgnoreCase))
            return ".";

        var rel = Path.GetRelativePath(root, combined);
        if (rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
            return null;
        return rel;
    }

    private static async Task<(List<(string path, int line_number, string line_text)> matches, string? error, bool truncated, string stderr)> RunRipgrepCoreAsync(
        string workspaceRoot,
        string rgExecutable,
        string pattern,
        string relativeSearchPath,
        bool fixedString,
        string? glob,
        int maxMatches,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = rgExecutable,
            WorkingDirectory = workspaceRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("--json");
        if (fixedString)
            psi.ArgumentList.Add("-F");
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add(pattern);
        if (!string.IsNullOrWhiteSpace(glob))
        {
            psi.ArgumentList.Add("--glob");
            psi.ArgumentList.Add(glob.Trim());
        }

        psi.ArgumentList.Add(relativeSearchPath);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!process.Start())
            return ([], "Failed to start rg process.", false, "");

        var matches = new List<(string path, int line_number, string line_text)>(Math.Min(maxMatches, 1024));
        var truncated = false;

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            var reader = process.StandardOutput;
            while (matches.Count < maxMatches)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                    break;
                if (line.Length == 0)
                    continue;
                if (TryParseMatchLine(line, workspaceRoot, out var m))
                    matches.Add(m);
            }

            if (matches.Count >= maxMatches)
            {
                truncated = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            throw;
        }

        var errText = (await stderrTask.ConfigureAwait(false)).Trim();

        var exit = process.ExitCode;
        if (exit == 2 && matches.Count == 0)
        {
            return ([], string.IsNullOrEmpty(errText)
                ? "rg exited with code 2. Is ripgrep installed and on PATH? See https://github.com/BurntSushi/ripgrep/releases"
                : errText, false, errText);
        }

        return (matches, null, truncated, errText);
    }

    private static bool TryParseMatchLine(string line, string workspaceRoot, out (string path, int line_number, string line_text) match)
    {
        match = default;
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

            if (string.IsNullOrEmpty(pathText))
                return false;

            var fullPath = CanonicalFilePath.Normalize(Path.Combine(workspaceRoot, pathText.Replace('/', Path.DirectorySeparatorChar)));
            match = (fullPath, lineNumber, lineText ?? "");
            return true;
        }
        catch
        {
            return false;
        }
    }
}

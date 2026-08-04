#nullable enable
using System.Text;
using System.Text.Json;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Human → agent IdeShare operator inbox (.cdp/share + habitat LocalAppData/cdp-mcp/share).
/// Shape matches cdp-mcp <c>IdeShare.SharePut</c> with=operator so <c>share from=operator</c> works.
/// </summary>
[IoBoundary]
public static class GlassOperatorShareShelf
{
    public const string SchemaVersion = "share/v1";

    /// <summary>
    /// Write body onto operator share shelves. Returns primary inbox path or null on failure.
    /// Always mirrors habitat LocalAppData; also project <c>.cdp/share</c> when workspaceRoot set.
    /// </summary>
    public static string? TryPut(string body, string? workspaceRoot = null, string? shareId = null, string what = "intercom")
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var id = string.IsNullOrWhiteSpace(shareId)
            ? Guid.NewGuid().ToString("N")[..12]
            : shareId.Trim();
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var title = what;
        var fileName = $"{Slug(what)}-{stamp}-{Slug(title)}.md";
        string? primary = null;

        foreach (var dir in ResolveInboxes(workspaceRoot))
        {
            try
            {
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, fileName);
                File.WriteAllText(path, body, Encoding.UTF8);
                var latest = Path.Combine(dir, "LATEST.md");
                File.Copy(path, latest, overwrite: true);
                var meta = new
                {
                    schema = SchemaVersion,
                    share_id = id,
                    with = "operator",
                    what,
                    ask = "none",
                    status = "shared",
                    path,
                    title,
                    lines = CountLines(body),
                    chars = body.Length,
                    shared_utc = DateTime.UtcNow,
                    origin = "glass_intercom"
                };
                File.WriteAllText(
                    Path.Combine(dir, "LATEST.json"),
                    JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }),
                    Encoding.UTF8);
                primary ??= dir;
            }
            catch
            {
                /* best-effort per inbox */
            }
        }

        return primary;
    }

    public static IEnumerable<string> ResolveInboxes(string? workspaceRoot)
    {
        var habitat = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CdpHabitatPaths.FolderName,
            "share");
        yield return habitat;

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var project = Path.GetFullPath(Path.Combine(workspaceRoot.Trim(), ".cdp", "share"));
            if (!string.Equals(project, habitat, StringComparison.OrdinalIgnoreCase))
                yield return project;
        }
    }

    static string Slug(string title)
    {
        var sb = new StringBuilder(title.Length);
        foreach (var ch in title.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is ' ' or '-' or '_' && sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }

        var s = sb.ToString().Trim('-');
        return s.Length == 0 ? "share" : s.Length <= 32 ? s : s[..32];
    }

    static int CountLines(string text)
    {
        if (text.Length == 0) return 1;
        var n = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') n++;
        }

        return n;
    }
}

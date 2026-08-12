#nullable enable
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass MFD Hypotheses: JSON status glance (no SoftInstrument invent).
/// Live list SSOT stays Avalonia <c>HypothesesMfdPageView</c> / <c>DebugHypothesesStorage</c> (ADR 0001).
/// </summary>
public static class GlassHypothesesGlance
{
    public const string RelativePath = ".cascade-ide/debug-hypotheses.json";

    public readonly record struct HypothesesFsStatus(
        string FilePath,
        bool FileExists,
        int Total,
        int Open,
        int Rejected,
        int Confirmed,
        DateTimeOffset? ModifiedUtc);

    public static string? TryResolveFilePath(string? workspaceRoot)
    {
        var root = GlassWorkspaceClimb.ResolveRoot(workspaceRoot);
        if (root is null)
            return null;

        try
        {
            return Path.Combine(root, ".cascade-ide", "debug-hypotheses.json");
        }
        catch
        {
            return null;
        }
    }

    public static HypothesesFsStatus? TryProbe(string? workspaceRoot)
    {
        var path = TryResolveFilePath(workspaceRoot);
        if (path is null)
            return new HypothesesFsStatus(RelativePath, false, 0, 0, 0, 0, null);

        try
        {
            if (!File.Exists(path))
                return new HypothesesFsStatus(path, false, 0, 0, 0, 0, null);

            var info = new FileInfo(path);
            var (total, open, rejected, confirmed) = CountFromJson(File.ReadAllText(path));
            return new HypothesesFsStatus(
                path,
                true,
                total,
                open,
                rejected,
                confirmed,
                info.LastWriteTimeUtc);
        }
        catch
        {
            return new HypothesesFsStatus(path, false, 0, 0, 0, 0, null);
        }
    }

    public static string? TryFormatFromWorkspaceRoot(string? workspaceRoot)
    {
        var probe = TryProbe(workspaceRoot);
        return probe is null ? null : Format(probe.Value, workspaceRoot);
    }

    /// <summary>Testable formatter (no I/O).</summary>
    public static string Format(HypothesesFsStatus status, string? workspaceRoot = null)
    {
        var level = !status.FileExists
            ? "MISSING"
            : status.Total == 0
                ? "EMPTY"
                : "READY";

        var sb = new StringBuilder();
        sb.Append("Hypotheses glance · ").AppendLine(level);
        sb.Append("file · ").AppendLine(ShortPath(status.FilePath, workspaceRoot));

        if (status.FileExists)
        {
            sb.Append("count · ").Append(status.Total.ToString(CultureInfo.InvariantCulture))
                .Append(" · open=").Append(status.Open.ToString(CultureInfo.InvariantCulture))
                .Append(" rejected=").Append(status.Rejected.ToString(CultureInfo.InvariantCulture))
                .Append(" confirmed=").AppendLine(status.Confirmed.ToString(CultureInfo.InvariantCulture));
            if (status.ModifiedUtc is { } mtime)
                sb.Append("mtime · ").AppendLine(mtime.ToString("u", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.AppendLine("· no debug-hypotheses.json yet");
            sb.AppendLine("· Avalonia Hypotheses panel creates on edit");
        }

        sb.AppendLine();
        sb.AppendLine("┌ host ──────────────┐");
        sb.AppendLine("│ ■ Glass JSON status  │");
        sb.AppendLine("│ □ Avalonia Hypotheses│");
        sb.AppendLine("└─────────────────────┘");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Parse ADR 0001 root without Models dependency.</summary>
    public static (int Total, int Open, int Rejected, int Confirmed) CountFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (0, 0, 0, 0);

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hypotheses", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return (0, 0, 0, 0);

            var open = 0;
            var rejected = 0;
            var confirmed = 0;
            var total = 0;
            foreach (var item in arr.EnumerateArray())
            {
                total++;
                var status = item.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String
                    ? s.GetString()
                    : null;
                switch (status?.Trim().ToLowerInvariant())
                {
                    case "rejected":
                        rejected++;
                        break;
                    case "confirmed":
                        confirmed++;
                        break;
                    default:
                        open++;
                        break;
                }
            }

            return (total, open, rejected, confirmed);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    static string ShortPath(string filePath, string? workspaceRoot)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
            {
                var root = Path.GetFullPath(workspaceRoot.Trim());
                var full = Path.GetFullPath(filePath);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = full[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (rel.Length > 0)
                        return rel.Replace('\\', '/');
                }
            }
        }
        catch
        {
            // fall through
        }

        return Path.GetFileName(filePath);
    }
}

#nullable enable
using System.Globalization;
using System.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass MFD HybridIndex: filesystem status glance (no SoftOrgan invent).
/// Live HCI SSOT stays Avalonia <c>HybridIndexMfdPageView</c> / orchestrator.
/// </summary>
public static class GlassHybridIndexGlance
{
    public const string DefaultIndexDir = ".hybrid-codebase-index";
    public const string DefaultDbFileName = "codebase-index-v2.sqlite";

    public readonly record struct IndexFsStatus(
        string DatabasePath,
        bool DatabaseExists,
        long? ByteLength,
        DateTimeOffset? ModifiedUtc);

    /// <summary>Resolve default SQLite path under workspace (always returns a path when root is valid).</summary>
    public static string? TryResolveDatabasePath(string? workspaceRoot, string? indexDir = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var root = Path.GetFullPath(workspaceRoot.Trim());
            var dir = string.IsNullOrWhiteSpace(indexDir) ? DefaultIndexDir : indexDir.Trim();
            return Path.Combine(root, dir, DefaultDbFileName);
        }
        catch
        {
            return null;
        }
    }

    public static IndexFsStatus? TryProbe(string? workspaceRoot, string? indexDir = null)
    {
        var db = TryResolveDatabasePath(workspaceRoot, indexDir);
        if (db is null)
            return null;

        try
        {
            if (!File.Exists(db))
                return new IndexFsStatus(db, false, null, null);

            var info = new FileInfo(db);
            return new IndexFsStatus(
                db,
                true,
                info.Length,
                info.LastWriteTimeUtc);
        }
        catch
        {
            return new IndexFsStatus(db, false, null, null);
        }
    }

    /// <summary>Format MFD body from workspace probe (null only when root unusable).</summary>
    public static string? TryFormatFromWorkspaceRoot(string? workspaceRoot, string? indexDir = null)
    {
        var probe = TryProbe(workspaceRoot, indexDir);
        return probe is null ? null : Format(probe.Value, workspaceRoot);
    }

    /// <summary>Testable formatter (no I/O).</summary>
    public static string Format(IndexFsStatus status, string? workspaceRoot = null)
    {
        var sb = new StringBuilder();
        sb.Append("HybridIndex glance · ");
        sb.AppendLine(status.DatabaseExists ? "READY" : "MISSING");

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            try
            {
                var rootName = Path.GetFileName(Path.GetFullPath(workspaceRoot.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (!string.IsNullOrWhiteSpace(rootName))
                    sb.Append("ws · ").AppendLine(rootName);
            }
            catch
            {
                // ignore
            }
        }

        sb.Append("db · ").AppendLine(ShortPath(status.DatabasePath, workspaceRoot));

        if (status.DatabaseExists)
        {
            if (status.ByteLength is { } bytes)
                sb.Append("size · ").AppendLine(FormatBytes(bytes));
            if (status.ModifiedUtc is { } mtime)
                sb.Append("mtime · ").AppendLine(mtime.ToString("u", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.AppendLine("· index not built");
            sb.AppendLine("· reindex: codebase_index_reindex / Avalonia HybridIndex");
        }

        sb.AppendLine();
        sb.AppendLine("┌ host ────────────────┐");
        sb.AppendLine("│ ■ Glass FS status    │");
        sb.AppendLine("│ □ Avalonia HCI SSOT  │");
        sb.AppendLine("└──────────────────────┘");
        return sb.ToString().TrimEnd();
    }

  /// <summary>FS glance + live <c>codebase_index_status</c> JSON when probe succeeds.</summary>
    public static string? TryFormatLiveFromWorkspaceRoot(string? workspaceRoot, string? indexDir = null)
    {
        var fs = TryFormatFromWorkspaceRoot(workspaceRoot, indexDir);
        if (fs is null)
            return null;

        var json = GlassHybridIndexStatusProbe.TryFetchStatusJson(workspaceRoot);
        if (string.IsNullOrWhiteSpace(json))
            return fs;

        return fs + Environment.NewLine + Environment.NewLine
               + "status json ·" + Environment.NewLine
               + json;
    }

    static string ShortPath(string databasePath, string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            return "?";

        try
        {
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
            {
                var root = Path.GetFullPath(workspaceRoot.Trim());
                var full = Path.GetFullPath(databasePath);
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

        return Path.GetFileName(databasePath);
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
        return (bytes / (1024.0 * 1024.0)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
    }
}

#nullable enable
using System.Globalization;
using System.Text;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass MFD HybridIndex: filesystem + live status for instrument cards / scope map.
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

    /// <summary>Live HCI status for instrument cards (Avalonia HybridIndexMfdPageView parity).</summary>
    public readonly record struct LiveInstrumentStatus(
        bool DatabaseExists,
        int DocumentCount,
        bool DocumentCountMayBeStale,
        string? IndexedAtIso,
        string? ReindexState,
        string? LastReindexError,
        string? DatabasePath,
        string? WorkspaceRoot,
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

    /// <summary>FS + in-proc <c>codebase_index_status</c> for human instrument cards.</summary>
    public static LiveInstrumentStatus? TryProbeLive(string? workspaceRoot, string? indexDir = null)
    {
        var fs = TryProbe(workspaceRoot, indexDir);
        if (fs is null)
            return null;

        var live = GlassHybridIndexStatusProbe.TryFetchStatus(workspaceRoot);
        if (live is null)
        {
            return new LiveInstrumentStatus(
                fs.Value.DatabaseExists,
                DocumentCount: 0,
                DocumentCountMayBeStale: false,
                IndexedAtIso: null,
                ReindexState: fs.Value.DatabaseExists ? "fs-only" : "missing",
                LastReindexError: null,
                DatabasePath: fs.Value.DatabasePath,
                WorkspaceRoot: workspaceRoot,
                ByteLength: fs.Value.ByteLength,
                ModifiedUtc: fs.Value.ModifiedUtc);
        }

        var body = live.Value;
        return body with
        {
            ByteLength = fs.Value.ByteLength ?? body.ByteLength,
            ModifiedUtc = fs.Value.ModifiedUtc ?? body.ModifiedUtc,
            DatabasePath = string.IsNullOrWhiteSpace(body.DatabasePath) ? fs.Value.DatabasePath : body.DatabasePath,
        };
    }

    public static IReadOnlyList<GlassGlanceChip> BuildInstrument(LiveInstrumentStatus status)
    {
        var err = !string.IsNullOrWhiteSpace(status.LastReindexError);
        var ready = status.DatabaseExists && status.DocumentCount > 0 && !err;
        var level = err ? "FAULT" : status.DatabaseExists ? (ready ? "READY" : "THIN") : "MISSING";
        var levelTone = level switch { "READY" => "ok", "THIN" => "warn", "FAULT" => "bad", _ => "idle" };
        var docsTone = status.DocumentCountMayBeStale ? "warn" : status.DocumentCount > 0 ? "ok" : "idle";
        var fresh = FormatFresh(status.IndexedAtIso ?? status.ModifiedUtc?.ToString("u", CultureInfo.InvariantCulture));
        var state = string.IsNullOrWhiteSpace(status.ReindexState) ? "—" : Trunc(status.ReindexState!, 22);
        var dbShort = ShortPath(status.DatabasePath ?? "?", status.WorkspaceRoot);

        return
        [
            new("HCI", level, levelTone),
            new("DOCS", status.DocumentCount.ToString(CultureInfo.InvariantCulture)
                + (status.DocumentCountMayBeStale ? " · stale" : ""), docsTone),
            new("FRESH", fresh.Value, fresh.Tone),
            new("STATE", state, err ? "bad" : status.DatabaseExists ? "ok" : "idle"),
            new("DB", Trunc(dbShort, 28), status.DatabaseExists ? "ok" : "idle"),
            new("ERR", err ? Trunc(status.LastReindexError!, 28) : "—", err ? "bad" : "idle"),
        ];
    }

    /// <summary>Workspace scope map: root hub + top-level folders (index presence via DB ready).</summary>
    public static GlassSemanticMapGraph.Graph BuildScopeMap(string? workspaceRoot, bool indexReady, int maxNodes = 24)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || maxNodes < 1)
            return new GlassSemanticMapGraph.Graph(null, [], []);

        string root;
        try
        {
            root = Path.GetFullPath(workspaceRoot.Trim());
        }
        catch
        {
            return new GlassSemanticMapGraph.Graph(null, [], []);
        }

        if (!Directory.Exists(root))
            return new GlassSemanticMapGraph.Graph(null, [], []);

        var nodes = new List<GlassSemanticMapGraph.Node>
        {
            new(root, indexReady ? "index-root" : "ws-root", indexReady ? "hci ready" : "ws", 0),
        };
        var edges = new List<GlassSemanticMapGraph.Edge>();

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(root)
                         .Where(d =>
                         {
                             var name = Path.GetFileName(d);
                             return name is not ("." or "..")
                                    && !name.StartsWith('.')
                                    && !string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase)
                                    && !string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase);
                         })
                         .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
                         .Take(Math.Max(0, maxNodes - 1)))
            {
                nodes.Add(new GlassSemanticMapGraph.Node(dir, "folder", "scope", 1));
                edges.Add(new GlassSemanticMapGraph.Edge(root, dir, "child"));
            }
        }
        catch
        {
            // keep hub-only
        }

        return new GlassSemanticMapGraph.Graph(root, nodes, edges);
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

    static (string Value, string Tone) FormatFresh(string? isoOrUtc)
    {
        if (string.IsNullOrWhiteSpace(isoOrUtc))
            return ("—", "idle");

        if (!DateTimeOffset.TryParse(isoOrUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at))
            return (Trunc(isoOrUtc, 22), "meta");

        var mins = (DateTimeOffset.UtcNow - at.ToUniversalTime()).TotalMinutes;
        if (mins < 0)
            mins = 0;
        if (mins < 60)
            return ($"{(int)mins}m", mins < 30 ? "ok" : "warn");
        if (mins < 60 * 24)
            return ($"{(int)(mins / 60)}h", "warn");
        return ($"{(int)(mins / (60 * 24))}d", "bad");
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

    static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        if (bytes < 1024 * 1024)
            return (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
        return (bytes / (1024.0 * 1024.0)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
    }
}

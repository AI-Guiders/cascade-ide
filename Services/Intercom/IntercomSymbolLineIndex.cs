#nullable enable

using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models.Editor;
using CascadeIDE.Models.Intercom;
using Microsoft.Data.Sqlite;

namespace CascadeIDE.Services.Intercom;

/// <summary>SQLite sidecar: member/doc id → line range per file (ADR 0135 L2, colocated with HCI index_dir).</summary>
public static class IntercomSymbolLineIndex
{
    private const string LookupKindDocId = "docid";
    private const string LookupKindSimple = "simple";

    public static bool TryResolveMemberLines(
        in IntercomAttachResolveCacheContext cache,
        string absoluteFilePath,
        string? memberKey,
        out LineRange lines,
        out string detail)
    {
        lines = default;
        detail = "";

        if (string.IsNullOrWhiteSpace(cache.WorkspaceRoot)
            || string.IsNullOrWhiteSpace(cache.RelativePath)
            || string.IsNullOrWhiteSpace(memberKey))
        {
            return false;
        }

        if (!File.Exists(absoluteFilePath))
        {
            detail = "file_not_found";
            return false;
        }

        long mtimeTicks;
        try
        {
            mtimeTicks = File.GetLastWriteTimeUtc(absoluteFilePath).Ticks;
        }
        catch (Exception ex)
        {
            detail = "mtime_error: " + ex.Message;
            return false;
        }

        var rel = cache.RelativePath.Replace('\\', '/');
        var scopeKey = cache.ScopeKey;
        var dbPath = resolveDatabasePath(cache.WorkspaceRoot, cache.IndexDirectoryRelative);

        try
        {
            using var conn = openConnection(dbPath);
            ensureSchema(conn);

            if (tryQuery(conn, scopeKey, rel, LookupKindDocId, memberKey.Trim(), mtimeTicks, out lines))
            {
                detail = "symbol_cache_docid";
                return true;
            }

            var simple = memberKey.Contains('.') ? memberKey.Split('.')[^1] : memberKey.Trim();
            if (tryQuery(conn, scopeKey, rel, LookupKindSimple, simple, mtimeTicks, out lines))
            {
                detail = "symbol_cache_simple";
                return true;
            }

            detail = "symbol_cache_miss";
            return false;
        }
        catch (Exception ex)
        {
            detail = "symbol_cache_error: " + ex.Message;
            return false;
        }
    }

    public static void UpsertMemberLines(
        in IntercomAttachResolveCacheContext cache,
        string absoluteFilePath,
        string? memberKey,
        LineRange lines)
    {
        if (string.IsNullOrWhiteSpace(cache.WorkspaceRoot)
            || string.IsNullOrWhiteSpace(cache.RelativePath)
            || string.IsNullOrWhiteSpace(memberKey))
        {
            return;
        }

        if (!File.Exists(absoluteFilePath))
            return;

        long mtimeTicks;
        try
        {
            mtimeTicks = File.GetLastWriteTimeUtc(absoluteFilePath).Ticks;
        }
        catch
        {
            return;
        }

        var rel = cache.RelativePath.Replace('\\', '/');
        var scopeKey = cache.ScopeKey;
        var dbPath = resolveDatabasePath(cache.WorkspaceRoot, cache.IndexDirectoryRelative);

        try
        {
            using var conn = openConnection(dbPath);
            ensureSchema(conn);
            upsertRow(conn, scopeKey, rel, LookupKindDocId, memberKey.Trim(), lines, mtimeTicks);

            var simple = memberKey.Contains('.') ? memberKey.Split('.')[^1] : memberKey.Trim();
            upsertRow(conn, scopeKey, rel, LookupKindSimple, simple, lines, mtimeTicks);
        }
        catch
        {
            // best-effort sidecar
        }
    }

    public static void ReplaceFileSymbols(
        in IntercomAttachResolveCacheContext cache,
        string relativePath,
        long fileMtimeTicks,
        IReadOnlyList<IntercomSymbolLineEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(cache.WorkspaceRoot) || string.IsNullOrWhiteSpace(relativePath))
            return;

        var rel = relativePath.Replace('\\', '/');
        var scopeKey = cache.ScopeKey;
        var dbPath = resolveDatabasePath(cache.WorkspaceRoot, cache.IndexDirectoryRelative);

        try
        {
            using var conn = openConnection(dbPath);
            ensureSchema(conn);

            using var tx = conn.BeginTransaction();
            using (var del = conn.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText =
                    "DELETE FROM symbol_line WHERE scope_key = $scope AND relative_path = $path;";
                del.Parameters.AddWithValue("$scope", scopeKey);
                del.Parameters.AddWithValue("$path", rel);
                del.ExecuteNonQuery();
            }

            foreach (var e in entries)
            {
                if (!LineNumber.TryCreate(e.LineStart, out var lineStart)
                    || !LineNumber.TryCreate(e.LineEnd, out var lineEnd)
                    || !LineRange.TryCreate(lineStart, lineEnd, out var range))
                {
                    continue;
                }
                upsertRow(conn, scopeKey, rel, e.LookupKind, e.LookupKey, range, fileMtimeTicks, tx);
            }

            tx.Commit();
        }
        catch
        {
            // best-effort background index
        }
    }

    public static void RemoveScope(string? workspaceRoot, string indexDirectoryRelative, string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        var dbPath = resolveDatabasePath(workspaceRoot, indexDirectoryRelative);
        if (!File.Exists(dbPath))
            return;

        try
        {
            using var conn = openConnection(dbPath);
            ensureSchema(conn);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM symbol_line WHERE scope_key = $scope;";
            cmd.Parameters.AddWithValue("$scope", scopeKey);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // ignore
        }
    }

    private static bool tryQuery(
        SqliteConnection conn,
        string scopeKey,
        string relativePath,
        string lookupKind,
        string lookupKey,
        long fileMtimeTicks,
        out LineRange lines)
    {
        lines = default;
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT line_start, line_end FROM symbol_line
            WHERE scope_key = $scope AND relative_path = $path
              AND lookup_kind = $kind AND lookup_key = $key
              AND file_mtime_ticks = $mtime
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$scope", scopeKey);
        cmd.Parameters.AddWithValue("$path", relativePath);
        cmd.Parameters.AddWithValue("$kind", lookupKind);
        cmd.Parameters.AddWithValue("$key", lookupKey);
        cmd.Parameters.AddWithValue("$mtime", fileMtimeTicks);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return false;

        if (!LineNumber.TryCreate(reader.GetInt32(0), out var lineStart)
            || !LineNumber.TryCreate(reader.GetInt32(1), out var lineEnd))
        {
            return false;
        }

        return LineRange.TryCreate(lineStart, lineEnd, out lines);
    }

    private static void upsertRow(
        SqliteConnection conn,
        string scopeKey,
        string relativePath,
        string lookupKind,
        string lookupKey,
        LineRange lines,
        long fileMtimeTicks,
        SqliteTransaction? tx = null)
    {
        using var cmd = conn.CreateCommand();
        if (tx is not null)
            cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO symbol_line(scope_key, relative_path, lookup_kind, lookup_key, line_start, line_end, file_mtime_ticks)
            VALUES ($scope, $path, $kind, $key, $start, $end, $mtime)
            ON CONFLICT(scope_key, relative_path, lookup_kind, lookup_key) DO UPDATE SET
              line_start = excluded.line_start,
              line_end = excluded.line_end,
              file_mtime_ticks = excluded.file_mtime_ticks;
            """;
        cmd.Parameters.AddWithValue("$scope", scopeKey);
        cmd.Parameters.AddWithValue("$path", relativePath);
        cmd.Parameters.AddWithValue("$kind", lookupKind);
        cmd.Parameters.AddWithValue("$key", lookupKey);
        cmd.Parameters.AddWithValue("$start", lines.Start.Value);
        cmd.Parameters.AddWithValue("$end", lines.End.Value);
        cmd.Parameters.AddWithValue("$mtime", fileMtimeTicks);
        cmd.ExecuteNonQuery();
    }

    private static string resolveDatabasePath(string workspaceRoot, string indexDirectoryRelative)
    {
        var dir = HybridIndexIndexDirectoryRelative.ResolveOrDefault(indexDirectoryRelative);
        var fullDir = Path.Combine(workspaceRoot, dir);
        Directory.CreateDirectory(fullDir);
        return Path.Combine(fullDir, "intercom-symbol-lines.sqlite");
    }

    private static SqliteConnection openConnection(string dbPath)
    {
        var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString());
        conn.Open();
        return conn;
    }

    private static void ensureSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS symbol_line (
              scope_key TEXT NOT NULL,
              relative_path TEXT NOT NULL,
              lookup_kind TEXT NOT NULL,
              lookup_key TEXT NOT NULL,
              line_start INTEGER NOT NULL,
              line_end INTEGER NOT NULL,
              file_mtime_ticks INTEGER NOT NULL,
              PRIMARY KEY (scope_key, relative_path, lookup_kind, lookup_key)
            );
            CREATE INDEX IF NOT EXISTS idx_symbol_line_lookup
              ON symbol_line(scope_key, relative_path, lookup_kind, lookup_key);
            CREATE INDEX IF NOT EXISTS idx_symbol_line_member_lookup
              ON symbol_line(scope_key, lookup_kind, lookup_key);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Все workspace-relative файлы, где sidecar знает <paramref name="memberKey"/>.</summary>
    public static bool TryFindRelativePathsForMember(
        in IntercomAttachResolveCacheContext cache,
        string? memberKey,
        out IReadOnlyList<string> relativePaths)
    {
        relativePaths = [];
        if (string.IsNullOrWhiteSpace(cache.WorkspaceRoot) || string.IsNullOrWhiteSpace(memberKey))
            return false;

        var dbPath = resolveDatabasePath(cache.WorkspaceRoot, cache.IndexDirectoryRelative);
        if (!File.Exists(dbPath))
            return false;

        var keys = lookupKeysForMember(memberKey);
        if (keys.Count == 0)
            return false;

        try
        {
            using var conn = openConnection(dbPath);
            ensureSchema(conn);
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (kind, key) in keys)
            {
                foreach (var path in queryPaths(conn, cache.ScopeKey, kind, key))
                    set.Add(path);
            }

            if (set.Count == 0)
                return false;

            relativePaths = set.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<(string Kind, string Key)> lookupKeysForMember(string memberKey)
    {
        var trimmed = memberKey.Trim();
        if (trimmed.Length == 0)
            return [];

        var list = new List<(string, string)> { (LookupKindDocId, trimmed) };
        var simple = trimmed.Contains('.') ? trimmed.Split('.')[^1] : trimmed;
        if (!string.Equals(simple, trimmed, StringComparison.Ordinal))
            list.Add((LookupKindSimple, simple));

        return list;
    }

    private static IEnumerable<string> queryPaths(
        SqliteConnection conn,
        string scopeKey,
        string lookupKind,
        string lookupKey)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT DISTINCT relative_path FROM symbol_line
            WHERE scope_key = $scope AND lookup_kind = $kind AND lookup_key = $key
            ORDER BY relative_path;
            """;
        cmd.Parameters.AddWithValue("$scope", scopeKey);
        cmd.Parameters.AddWithValue("$kind", lookupKind);
        cmd.Parameters.AddWithValue("$key", lookupKey);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            yield return reader.GetString(0).Replace('\\', '/');
    }
}

/// <summary>Строка symbol sidecar при индексации файла.</summary>
public readonly record struct IntercomSymbolLineEntry(string LookupKind, string LookupKey, int LineStart, int LineEnd);

#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Documents;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent Instant Save → reload open Monaco tabs (shared dirty glass).
/// Watches %LocalAppData%/cdp-mcp/disk-LATEST.json; applies origin=agent only.
/// </summary>
internal sealed class CdpDiskSyncProjector : IDisposable
{
    public const string Schema = "document_disk_sync_latch/v1";
    public const string OriginAgent = "agent";
    public const string OriginHuman = "human";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    readonly DocumentsWorkspaceViewModel _documents;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastPath;
    DateTimeOffset _suppressPublishUntil = DateTimeOffset.MinValue;
    bool _disposed;

    public static CdpDiskSyncProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "disk-LATEST.json");

    CdpDiskSyncProjector(DocumentsWorkspaceViewModel documents, string stateRoot)
    {
        _documents = documents;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "disk-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        TryApplyFromDisk(force: true);
    }

    public static CdpDiskSyncProjector Start(DocumentsWorkspaceViewModel documents)
    {
        Instance?.Dispose();
        Instance = new CdpDiskSyncProjector(documents, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        DiskSyncDoc? doc;
        try
        {
            if (!File.Exists(LatchPath))
                return;
            var raw = File.ReadAllText(LatchPath);
            doc = JsonSerializer.Deserialize<DiskSyncDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null || string.IsNullOrWhiteSpace(doc.Path))
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(doc.Origin, OriginAgent, StringComparison.OrdinalIgnoreCase))
            return;
        if (!File.Exists(doc.Path))
            return;

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(doc.Path, _lastPath, StringComparison.OrdinalIgnoreCase))
                return;
            _lastStamp = doc.StampedUtc;
            _lastPath = doc.Path;
        }

        try
        {
            // Applying agent flush — do not echo a human Save latch.
            _suppressPublishUntil = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(400);
            _documents.ForceReloadOpenDocumentFromDisk(doc.Path);
        }
        catch
        {
            /* best-effort */
        }
    }

    /// <summary>Human Save → agent buffer reload (peer of agent Instant Save).</summary>
    public void PublishHumanSave(string path)
    {
        if (_disposed || string.IsNullOrWhiteSpace(path))
            return;
        lock (_gate)
        {
            if (DateTimeOffset.UtcNow < _suppressPublishUntil)
                return;
        }

        try
        {
            Directory.CreateDirectory(StateRoot);
            var body = new DiskSyncDoc
            {
                Schema = Schema,
                Path = Path.GetFullPath(path),
                Origin = OriginHuman,
                StampedUtc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(body, JsonOpts);
            var tmp = LatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, LatchPath, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFsEvent;
        _watcher.Created -= OnFsEvent;
        _watcher.Renamed -= OnFsEvent;
        _watcher.Dispose();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    sealed class DiskSyncDoc
    {
        public string? Schema { get; set; }
        public string? Path { get; set; }
        public string? Origin { get; set; }
        public DateTimeOffset StampedUtc { get; set; }
    }
}

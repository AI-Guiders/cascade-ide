#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Documents;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Dual-cockpit co-presence chrome: watches shared-LATEST.json (CDP desk latch)
/// and paints matching open tab with <c> · shared</c>.
/// </summary>
internal sealed class CdpSharedFileProjector : IDisposable
{
    public const string Schema = "shared_file_latch/v1";

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
    bool _lastShared;
    bool _disposed;

    public static CdpSharedFileProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "shared-LATEST.json");

    CdpSharedFileProjector(DocumentsWorkspaceViewModel documents, string stateRoot)
    {
        _documents = documents;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "shared-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        TryApplyFromDisk(force: true);
    }

    public static CdpSharedFileProjector Start(DocumentsWorkspaceViewModel documents)
    {
        Instance?.Dispose();
        Instance = new CdpSharedFileProjector(documents, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        SharedFileDoc? doc;
        try
        {
            if (!File.Exists(LatchPath))
            {
                _documents.ApplySharedFileChrome(path: null, shared: false);
                return;
            }

            var raw = File.ReadAllText(LatchPath);
            doc = JsonSerializer.Deserialize<SharedFileDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null)
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && doc.Shared == _lastShared
                && string.Equals(doc.Path, _lastPath, StringComparison.OrdinalIgnoreCase))
                return;
            _lastStamp = doc.StampedUtc;
            _lastPath = doc.Path;
            _lastShared = doc.Shared;
        }

        try
        {
            _documents.ApplySharedFileChrome(doc.Path, doc.Shared);
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
        _watcher.Dispose();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    sealed class SharedFileDoc
    {
        public string? Schema { get; set; }
        public string? Path { get; set; }
        public bool Shared { get; set; }

        [JsonPropertyName("stamped_utc")]
        public DateTimeOffset StampedUtc { get; set; }
    }
}

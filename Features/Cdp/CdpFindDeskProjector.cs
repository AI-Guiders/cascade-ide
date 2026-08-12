#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent Find desk pulse → quiet chrome band.
/// Watches find_desk-LATEST.json; paints <see cref="MainWindowViewModel.AgentFindDeskChromeHint"/>.
/// Idle (no chrome_hint) stays silent — not EICAS. SoftInstrumentMfdGlance RelatedFiles stays ←refactor.
/// </summary>
internal sealed class CdpFindDeskProjector : IDisposable
{
    public const string Schema = "cide_find_desk_latch/v1";
    public const string OriginAgent = "agent";

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    readonly MainWindowViewModel _vm;
    readonly FileSystemWatcher _watcher;
    readonly object _gate = new();
    DateTimeOffset _lastStamp = DateTimeOffset.MinValue;
    string? _lastHint;
    bool _disposed;

    public static CdpFindDeskProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "find_desk-LATEST.json");

    CdpFindDeskProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "find_desk-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpFindDeskProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpFindDeskProjector(vm, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        FindDeskLatchDoc? doc;
        try
        {
            var raw = CdpLatchIo.TryReadAllTextIfExists(LatchPath);
            if (raw is null)
            {
                _vm.ApplyFindDeskChromeHint(null);
                return;
            }
            doc = JsonSerializer.Deserialize<FindDeskLatchDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null)
            return;
        if (!string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.Equals(doc.Origin, OriginAgent, StringComparison.OrdinalIgnoreCase))
            return;

        var hint = string.IsNullOrWhiteSpace(doc.ChromeHint) ? null : doc.ChromeHint.Trim();

        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(hint, _lastHint, StringComparison.Ordinal))
                return;
            _lastStamp = doc.StampedUtc;
            _lastHint = hint;
        }

        try
        {
            _vm.ApplyFindDeskChromeHint(hint);
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

    public sealed class FindDeskLatchDoc
    {
        public string? Schema { get; set; }
        public string? Origin { get; set; }
        public bool Active { get; set; }
        public string? Pulse { get; set; }
        public string? Op { get; set; }
        public string? Where { get; set; }
        public string? Query { get; set; }
        public int HitCount { get; set; }
        public string? ChromeHint { get; set; }

        [JsonPropertyName("stamped_utc")]
        public DateTimeOffset StampedUtc { get; set; }
    }
}

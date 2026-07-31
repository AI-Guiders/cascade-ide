#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// SoftOrgan sa_desk (cdp_sa) pulse → quiet chrome band.
/// Watches sa-desk-LATEST.json; paints <see cref="MainWindowViewModel.AgentSaDeskChromeHint"/>.
/// Idle (no chrome_hint) stays silent — not EICAS go=sa.
/// </summary>
internal sealed class CdpSaDeskProjector : IDisposable
{
    public const string Schema = "cide_sa_desk_latch/v1";
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

    public static CdpSaDeskProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "sa-desk-LATEST.json");

    CdpSaDeskProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "sa-desk-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpSaDeskProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpSaDeskProjector(vm, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        SaDeskLatchDoc? doc;
        try
        {
            var raw = CdpLatchIo.TryReadAllTextIfExists(LatchPath);
            if (raw is null)
            {
                _vm.ApplySaDeskChromeHint(null);
                return;
            }
            doc = JsonSerializer.Deserialize<SaDeskLatchDoc>(raw, ReadOpts);
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
            _vm.ApplySaDeskChromeHint(hint);
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

    public sealed class SaDeskLatchDoc
    {
        public string? Schema { get; set; }
        public string? Origin { get; set; }
        public bool Active { get; set; }
        public string? Pulse { get; set; }
        public string? Verdict { get; set; }
        public string? ChromeHint { get; set; }

        [JsonPropertyName("stamped_utc")]
        public DateTimeOffset StampedUtc { get; set; }
    }
}

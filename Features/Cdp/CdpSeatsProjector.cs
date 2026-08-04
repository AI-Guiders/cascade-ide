#nullable enable
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent desk seats → cabin SoftOrgan chrome tip on glass.
/// Watches seats-LATEST.json; does <b>not</b> flip MFD page (intent wire only).
/// </summary>
internal sealed class CdpSeatsProjector : IDisposable
{
    public const string Schema = "cide_seats_latch/v1";
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
    string? _lastFingerprint;
    bool _disposed;

    public static CdpSeatsProjector? Instance { get; private set; }

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => Path.Combine(StateRoot, "seats-LATEST.json");

    CdpSeatsProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "seats-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpSeatsProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpSeatsProjector(vm, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        var raw = CdpLatchIo.TryReadAllTextIfExists(LatchPath);
        if (raw is null)
            return;

        SeatsLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<SeatsLatchDoc>(raw, ReadOpts);
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

        var fingerprint = doc.Fingerprint();
        lock (_gate)
        {
            if (!force
                && doc.StampedUtc <= _lastStamp
                && string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
                return;

            _lastStamp = doc.StampedUtc;
            _lastFingerprint = fingerprint;
        }

        // SoftOrgan seats → chrome only. MFD page is intent wire (presentation/chord/land/citizen), not desk pin.
        _vm.ApplyCabinOrganChromeHint(doc.ChromeHint);
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

    sealed class SeatsLatchDoc
    {
        public string Schema { get; set; } = CdpSeatsProjector.Schema;
        public Dictionary<string, string?>? Seats { get; set; }
        public string? MfdPage { get; set; }
        public string? ChromeHint { get; set; }
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }

        public string Fingerprint()
        {
            var seats = Seats is null
                ? ""
                : string.Join(';', Seats.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => kv.Key + '=' + (kv.Value ?? "")));
            return string.Join('|', seats, MfdPage ?? "", ChromeHint ?? "");
        }
    }
}

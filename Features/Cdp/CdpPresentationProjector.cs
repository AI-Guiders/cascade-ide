#nullable enable
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Agent desk → live CIDE operator glass.
/// Watches %LocalAppData%/cdp-mcp/presentation-LATEST.json; applies origin=agent via
/// <see cref="MainWindowViewModel.ApplyPresentationGlassPatch"/>.
/// </summary>
internal sealed class CdpPresentationProjector : IDisposable
{
    public const string Schema = "cide_presentation_latch/v1";
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

    public static CdpPresentationProjector? Instance { get; private set; }

    public static string StateRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cdp-mcp");

    public static string LatchPath => Path.Combine(StateRoot, "presentation-LATEST.json");

    CdpPresentationProjector(MainWindowViewModel vm, string stateRoot)
    {
        _vm = vm;
        Directory.CreateDirectory(stateRoot);
        _watcher = new FileSystemWatcher(stateRoot)
        {
            Filter = "presentation-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
        // Defer cold apply: MainWindow is not Visible yet during App setup — Sync apply
        // of mfd_page crashed with "Cannot show window with non-visible owner".
        Dispatcher.UIThread.Post(() => TryApplyFromDisk(force: true), DispatcherPriority.Loaded);
    }

    public static CdpPresentationProjector Start(MainWindowViewModel vm)
    {
        Instance?.Dispose();
        Instance = new CdpPresentationProjector(vm, StateRoot);
        return Instance;
    }

    void OnFsEvent(object sender, FileSystemEventArgs e) =>
        CdpLatchFs.PostApply(() => TryApplyFromDisk(force: false));

    void TryApplyFromDisk(bool force)
    {
        if (_disposed)
            return;

        string raw;
        try
        {
            if (!File.Exists(LatchPath))
                return;
            raw = File.ReadAllText(LatchPath);
        }
        catch
        {
            return;
        }

        PresentationLatchDoc? doc;
        try
        {
            doc = JsonSerializer.Deserialize<PresentationLatchDoc>(raw, ReadOpts);
        }
        catch
        {
            return;
        }

        if (doc is null || !doc.HasAny)
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

        _vm.ApplyPresentationGlassPatch(
            topology: doc.Topology,
            tier: doc.Tier,
            instruments: doc.Instruments,
            mfdPage: doc.MfdPage);
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

    sealed class PresentationLatchDoc
    {
        public string Schema { get; set; } = CdpPresentationProjector.Schema;
        public string? Topology { get; set; }
        public string? Tier { get; set; }
        public Dictionary<string, string>? Instruments { get; set; }
        public string? MfdPage { get; set; }
        public string Origin { get; set; } = OriginAgent;
        public DateTimeOffset StampedUtc { get; set; }

        public bool HasAny =>
            !string.IsNullOrWhiteSpace(Topology)
            || !string.IsNullOrWhiteSpace(Tier)
            || (Instruments is { Count: > 0 })
            || !string.IsNullOrWhiteSpace(MfdPage);

        public string Fingerprint()
        {
            var instruments = Instruments is null
                ? ""
                : string.Join(';', Instruments.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => kv.Key + '=' + kv.Value));
            return string.Join('|',
                Topology ?? "",
                Tier ?? "",
                instruments,
                MfdPage ?? "");
        }
    }
}

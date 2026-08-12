#nullable enable
using System.IO;
using CascadeIDE.Features.Cdp;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// File-latch IPC with CDP habitat (%LocalAppData%/cdp-mcp/*-LATEST.json).
/// Same contract as Avalonia CIDE projectors — toolkit-agnostic.
/// </summary>
internal sealed class LatchHub : IDisposable
{
    FileSystemWatcher? _watcher;

    public string StateRoot { get; } = CdpHabitatPaths.StateRoot;

    public event Action<string>? IntercomChanged;
    public event Action<string>? PresenceChanged;
    public event Action<string>? PresentationChanged;

    /// <summary>plan-LATEST.json path (TM → P Plan readout).</summary>
    public event Action<string>? PlanChanged;

    /// <summary>citizen-dialog-request-LATEST.json path (Glass /citizen → habitat bridge status).</summary>
    public event Action<string>? CitizenDialogRequestChanged;

    /// <summary>alert-LATEST.json path (EICAS).</summary>
    public event Action<string>? AlertChanged;

    /// <summary>qrh-LATEST.json path (EICAS advisory).</summary>
    public event Action<string>? QrhChanged;

    /// <summary>ecl-LATEST.json path (EICAS checklist advisory).</summary>
    public event Action<string>? EclChanged;

    /// <summary>seats-LATEST.json path (MFD select + cabin SoftInstrument chrome).</summary>
    public event Action<string>? SeatsChanged;

    /// <summary>land-LATEST.json path (agent cdp_land → AvalonEdit open/goto).</summary>
    public event Action<string>? LandChanged;

    /// <summary>shared-LATEST.json path (dual-cockpit co-presence chrome).</summary>
    public event Action<string>? SharedChanged;

    /// <summary>disk-LATEST.json path (agent Instant Save → AvalonEdit reload).</summary>
    public event Action<string>? DiskChanged;

    /// <summary>ignite-wake-LATEST.json path (AutoI wake charge — Autoi consumer).</summary>
    public event Action<string>? IgniteWakeChanged;

    /// <summary>ignite-LATEST.json path (AutoI/HILD/course → Intercom HUD).</summary>
    public event Action<string>? IgniteChanged;

    /// <summary>organId, chrome_hint (null/blank = clear).</summary>
    public event Action<string, string?>? SoftInstrumentChanged;

    public void Start()
    {
        CdpHabitatPaths.EnsureStateRoot();
        _watcher = new FileSystemWatcher(StateRoot)
        {
            Filter = "*-LATEST.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFs;
        _watcher.Created += OnFs;
        _watcher.Renamed += (_, e) => OnFs(_watcher, new FileSystemEventArgs(WatcherChangeTypes.Changed, StateRoot, e.Name));

        TryFireExisting(CdpHabitatPaths.IntercomLatchFileName, IntercomChanged);
        TryFireExisting(CdpHabitatPaths.IntercomPresenceLatchFileName, PresenceChanged);
        TryFireExisting(CdpHabitatPaths.PresentationLatchFileName, PresentationChanged);
        TryFireExisting(CdpHabitatPaths.PlanLatchFileName, PlanChanged);
        TryFireExisting(CdpHabitatPaths.CitizenDialogRequestLatchFileName, CitizenDialogRequestChanged);
        TryFireExisting("alert-LATEST.json", AlertChanged);
        TryFireExisting("qrh-LATEST.json", QrhChanged);
        TryFireExisting("ecl-LATEST.json", EclChanged);
        TryFireExisting(CdpHabitatPaths.SeatsLatchFileName, SeatsChanged);
        TryFireExisting(CdpHabitatPaths.LandLatchFileName, LandChanged);
        TryFireExisting(CdpHabitatPaths.SharedLatchFileName, SharedChanged);
        TryFireExisting(CdpHabitatPaths.DiskLatchFileName, DiskChanged);
        TryFireExisting(CdpHabitatPaths.IgniteWakeLatchFileName, IgniteWakeChanged);
        TryFireExisting(CdpHabitatPaths.IgniteLatchFileName, IgniteChanged);
        foreach (var id in SoftInstrumentLatchCatalog.Ids)
            TryFireSoftInstrument(id + "-LATEST.json");
    }

    void OnFs(object sender, FileSystemEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Name))
            return;

        var name = e.Name;
        if (name.Equals(CdpHabitatPaths.IntercomLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => IntercomChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.IntercomPresenceLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => PresenceChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.PresentationLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => PresentationChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.PlanLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => PlanChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.CitizenDialogRequestLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => CitizenDialogRequestChanged?.Invoke(p));
        else if (name.Equals("alert-LATEST.json", StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => AlertChanged?.Invoke(p));
        else if (name.Equals("qrh-LATEST.json", StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => QrhChanged?.Invoke(p));
        else if (name.Equals("ecl-LATEST.json", StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => EclChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.SeatsLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => SeatsChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.LandLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => LandChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.SharedLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => SharedChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.DiskLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => DiskChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.IgniteWakeLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => IgniteWakeChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.IgniteLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => IgniteChanged?.Invoke(p));
        else if (SoftInstrumentLatchCatalog.TryParseFileName(name, out var organId))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), _ => ApplySoftInstrumentFromDisk(organId, name));
    }

    void TryFireExisting(string fileName, Action<string>? sink)
    {
        var path = CdpHabitatPaths.GetLatchPath(fileName);
        if (File.Exists(path))
            sink?.Invoke(path);
    }

    void TryFireSoftInstrument(string fileName)
    {
        if (!SoftInstrumentLatchCatalog.TryParseFileName(fileName, out var organId))
            return;
        if (!File.Exists(CdpHabitatPaths.GetLatchPath(fileName)))
            return;
        ApplySoftInstrumentFromDisk(organId, fileName);
    }

    void ApplySoftInstrumentFromDisk(string organId, string fileName)
    {
        var path = CdpHabitatPaths.GetLatchPath(fileName);
        var hint = LatchPaint.TryReadChromeHint(path);
        SoftInstrumentChanged?.Invoke(organId, hint);
    }

    public void Dispose()
    {
        if (_watcher is null)
            return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
    }
}

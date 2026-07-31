#nullable enable
using System.IO;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// File-latch IPC with CDP habitat (%LocalAppData%/cdp-mcp/*-LATEST.json).
/// Same contract as Avalonia CIDE projectors — toolkit-agnostic.
/// </summary>
internal sealed class LatchHub : IDisposable
{
    static readonly HashSet<string> SoftOrganIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "pressure", "ignite", "plan", "cabin", "scope", "review", "refactor", "plugins",
        "toolchain", "crm", "report", "webcam", "sys", "onboard", "arch", "mcp", "learn", "domain",
        "sa-desk"
    };

    FileSystemWatcher? _watcher;

    public string StateRoot { get; } = CdpHabitatPaths.StateRoot;

    public event Action<string>? IntercomChanged;
    public event Action<string>? PresentationChanged;

    /// <summary>organId, chrome_hint (null/blank = clear).</summary>
    public event Action<string, string?>? SoftOrganChanged;

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
        TryFireExisting(CdpHabitatPaths.PresentationLatchFileName, PresentationChanged);
        foreach (var id in SoftOrganIds)
            TryFireSoftOrgan(id + "-LATEST.json");
    }

    void OnFs(object sender, FileSystemEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Name))
            return;

        var name = e.Name;
        if (name.Equals(CdpHabitatPaths.IntercomLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => IntercomChanged?.Invoke(p));
        else if (name.Equals(CdpHabitatPaths.PresentationLatchFileName, StringComparison.OrdinalIgnoreCase))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), p => PresentationChanged?.Invoke(p));
        else if (TryParseSoftOrganFileName(name, out var organId))
            CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.GetLatchPath(name), _ => ApplySoftOrganFromDisk(organId, name));
    }

    void TryFireExisting(string fileName, Action<string>? sink)
    {
        var path = CdpHabitatPaths.GetLatchPath(fileName);
        if (File.Exists(path))
            sink?.Invoke(path);
    }

    void TryFireSoftOrgan(string fileName)
    {
        if (!TryParseSoftOrganFileName(fileName, out var organId))
            return;
        if (!File.Exists(CdpHabitatPaths.GetLatchPath(fileName)))
            return;
        ApplySoftOrganFromDisk(organId, fileName);
    }

    void ApplySoftOrganFromDisk(string organId, string fileName)
    {
        var path = CdpHabitatPaths.GetLatchPath(fileName);
        var hint = LatchPaint.TryReadChromeHint(path);
        SoftOrganChanged?.Invoke(organId, hint);
    }

    static bool TryParseSoftOrganFileName(string fileName, out string organId)
    {
        organId = "";
        const string suffix = "-LATEST.json";
        if (!fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;
        organId = fileName[..^suffix.Length];
        return SoftOrganIds.Contains(organId);
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

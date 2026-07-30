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
    FileSystemWatcher? _watcher;

    public string StateRoot { get; } = CdpHabitatPaths.StateRoot;

    public event Action<string>? IntercomChanged;
    public event Action<string>? PresentationChanged;

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
    }

    void OnFs(object sender, FileSystemEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Name))
            return;

        var name = e.Name;
        if (name.Equals(CdpHabitatPaths.IntercomLatchFileName, StringComparison.OrdinalIgnoreCase))
            DebounceFire(CdpHabitatPaths.GetLatchPath(name), IntercomChanged);
        else if (name.Equals(CdpHabitatPaths.PresentationLatchFileName, StringComparison.OrdinalIgnoreCase))
            DebounceFire(CdpHabitatPaths.GetLatchPath(name), PresentationChanged);
    }

    void TryFireExisting(string fileName, Action<string>? sink)
    {
        var path = CdpHabitatPaths.GetLatchPath(fileName);
        if (File.Exists(path))
            sink?.Invoke(path);
    }

    static void DebounceFire(string path, Action<string>? sink)
    {
        // Tiny settle — writers often replace via temp+rename / multi-write.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                Thread.Sleep(40);
                if (File.Exists(path))
                    sink?.Invoke(path);
            }
            catch
            {
                /* best-effort */
            }
        });
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

#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Bidirectional surface RPC: watch surface-cmd-LATEST → UI-thread dispatch → surface-reply-LATEST.
/// </summary>
internal sealed class GlassSurfaceCommandHub : IDisposable
{
    readonly MainWindow _main;
    FileSystemWatcher? _watcher;
    string? _lastHandledId;

    public GlassSurfaceCommandHub(MainWindow main) => _main = main;

    public void Start()
    {
        CdpHabitatPaths.EnsureStateRoot();
        _watcher = new FileSystemWatcher(CdpHabitatPaths.StateRoot)
        {
            Filter = CdpHabitatPaths.SurfaceCmdLatchFileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFs;
        _watcher.Created += OnFs;
        _watcher.Renamed += (_, e) =>
        {
            if (string.Equals(e.Name, CdpHabitatPaths.SurfaceCmdLatchFileName, StringComparison.OrdinalIgnoreCase))
                OnFs(_watcher, new FileSystemEventArgs(WatcherChangeTypes.Changed, CdpHabitatPaths.StateRoot, e.Name));
        };

        TryHandleExisting();
    }

    void TryHandleExisting()
    {
        var path = CdpHabitatPaths.SurfaceCmdLatchPath;
        if (File.Exists(path))
            CdpLatchIo.PostSettledIfExists(path, _ => DispatchCmd());
    }

    void OnFs(object sender, FileSystemEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Name))
            return;
        if (!e.Name.Equals(CdpHabitatPaths.SurfaceCmdLatchFileName, StringComparison.OrdinalIgnoreCase))
            return;
        CdpLatchIo.PostSettledIfExists(CdpHabitatPaths.SurfaceCmdLatchPath, _ => DispatchCmd());
    }

    void DispatchCmd()
    {
        void Work()
        {
            try
            {
                HandleCmdOnUi();
            }
            catch (Exception ex)
            {
                WriteReply(new JsonObject
                {
                    ["schema"] = "agent_surface/v0",
                    ["id"] = _lastHandledId ?? "",
                    ["ok"] = false,
                    ["op"] = "?",
                    ["error"] = "surface_host_exception",
                    ["detail"] = ex.Message
                });
            }
        }

        if (_main.Dispatcher.CheckAccess())
            Work();
        else
            _main.Dispatcher.BeginInvoke(DispatcherPriority.Normal, Work);
    }

    void HandleCmdOnUi()
    {
        var path = CdpHabitatPaths.SurfaceCmdLatchPath;
        if (!File.Exists(path))
            return;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        if (id.Length == 0 || string.Equals(id, _lastHandledId, StringComparison.Ordinal))
            return;

        _lastHandledId = id;
        var op = (root.TryGetProperty("op", out var opEl) ? opEl.GetString() : null)?.Trim().ToLowerInvariant()
                 ?? "layout";

        if (op is "layout")
        {
            var windows = _main.EnumerateSurfaceWindows();
            var json = GlassUiLayoutSnapshot.BuildJsonAllWindows(windows);
            using var layoutDoc = JsonDocument.Parse(json);
            var resultNode = JsonNode.Parse(layoutDoc.RootElement.GetRawText());
            WriteReply(new JsonObject
            {
                ["schema"] = "agent_surface/v0",
                ["id"] = id,
                ["ok"] = true,
                ["op"] = "layout",
                ["result"] = resultNode,
                ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
            });
            return;
        }

        WriteReply(new JsonObject
        {
            ["schema"] = "agent_surface/v0",
            ["id"] = id,
            ["ok"] = false,
            ["op"] = op,
            ["error"] = "not_implemented",
            ["detail"] = "Glass host v0: layout only",
            ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
        });
    }

    static void WriteReply(JsonObject body)
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var path = CdpHabitatPaths.SurfaceReplyLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, body.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
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

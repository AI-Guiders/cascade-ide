#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        JsonElement args = default;
        var hasArgs = root.TryGetProperty("args", out args);
        string? Arg(string key)
        {
            if (!hasArgs || args.ValueKind != JsonValueKind.Object)
                return null;
            if (!args.TryGetProperty(key, out var el))
                return null;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        }

        var windows = _main.EnumerateSurfaceWindows();

        if (op is "layout")
        {
            var json = GlassUiLayoutSnapshot.BuildJsonAllWindows(windows);
            using var layoutDoc = JsonDocument.Parse(json);
            WriteOk(id, op, JsonNode.Parse(layoutDoc.RootElement.GetRawText()));
            return;
        }

        string detail;
        JsonNode? result = null;
        switch (op)
        {
            case "highlight":
                detail = GlassSurfaceActions.Highlight(windows, Arg("name"));
                break;
            case "focus":
                detail = GlassSurfaceActions.Focus(windows, Arg("name"));
                break;
            case "click":
                detail = GlassSurfaceActions.Click(windows, Arg("name"));
                break;
            case "set_text":
                detail = GlassSurfaceActions.SetText(windows, Arg("name"), Arg("text"));
                break;
            case "send_keys":
                detail = GlassSurfaceActions.SendKeys(windows, Arg("name"), Arg("keys"));
                break;
            case "palette":
            {
                var q = Arg("query") ?? Arg("text");
                var ex = string.Equals(Arg("execute"), "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Arg("execute"), "1", StringComparison.OrdinalIgnoreCase);
                detail = _main.AgentSurfacePalette(q, ex);
                if (detail.StartsWith('{'))
                {
                    result = JsonNode.Parse(detail);
                    var okNode = result?["ok"]?.GetValue<bool?>();
                    detail = okNode == false
                        ? (result?["error"]?.GetValue<string>() ?? "palette_failed")
                        : "OK";
                }

                break;
            }
            case "appearance":
                detail = GlassSurfaceActions.Appearance(windows, Arg("name"));
                if (detail.StartsWith('{'))
                {
                    result = JsonNode.Parse(detail);
                    detail = "OK";
                }

                break;
            case "colors" or "colors_under_cursor":
                detail = GlassSurfaceActions.ColorsUnderCursor(windows);
                if (detail.StartsWith('{'))
                {
                    result = JsonNode.Parse(detail);
                    detail = result?["error"]?.GetValue<string>() is { } e ? e : "OK";
                }

                break;
            case "set_control_layout":
                detail = GlassSurfaceActions.SetControlLayout(windows, Arg("name"), Arg("layout"));
                break;
            case "set_panel_size":
                detail = GlassSurfaceActions.SetPanelSize(windows, Arg("panel"), Arg("width"), Arg("height"));
                break;
            case "request_confirmation":
            {
                // Modal MessageBox — RPC ok when answered; result is ok|cancel.
                var answer = GlassSurfaceActions.RequestConfirmation(windows, Arg("message"));
                WriteReply(new JsonObject
                {
                    ["schema"] = "agent_surface/v0",
                    ["id"] = id,
                    ["ok"] = answer is "ok" or "cancel",
                    ["op"] = op,
                    ["result"] = answer,
                    ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
                });
                return;
            }
            default:
                WriteReply(new JsonObject
                {
                    ["schema"] = "agent_surface/v0",
                    ["id"] = id,
                    ["ok"] = false,
                    ["op"] = op,
                    ["error"] = "not_implemented",
                    ["detail"] =
                        "Glass host: layout|highlight|focus|click|set_text|send_keys|palette|appearance|colors|set_control_layout|set_panel_size|request_confirmation",
                    ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
                });
                return;
        }

        var ok = detail == "OK";
        var body = new JsonObject
        {
            ["schema"] = "agent_surface/v0",
            ["id"] = id,
            ["ok"] = ok,
            ["op"] = op,
            ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
        };
        if (ok)
        {
            if (result is not null)
                body["result"] = result;
            else
                body["result"] = "OK";
        }
        else
        {
            body["error"] = "surface_action_failed";
            body["detail"] = detail;
            if (result is not null)
                body["result"] = result;
        }

        WriteReply(body);
    }

    void WriteOk(string id, string op, JsonNode? result) =>
        WriteReply(new JsonObject
        {
            ["schema"] = "agent_surface/v0",
            ["id"] = id,
            ["ok"] = true,
            ["op"] = op,
            ["result"] = result,
            ["stamped_utc"] = DateTimeOffset.UtcNow.ToString("o")
        });

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

#nullable enable

using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD DebugStack — live spectator from debug_desk latch (DAP stopped → SoftOrgan FSW).</summary>
public partial class MainWindow
{
    void RefreshMfdDebugVisibility()
    {
        if (MfdDebugStackHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "DebugStack", StringComparison.OrdinalIgnoreCase);
        MfdDebugStackHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            RefreshDebugSpectator();
    }

    bool IsDebugHostActive()
    {
        if (MfdDebugStackHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "DebugStack", StringComparison.OrdinalIgnoreCase)
               && MfdDebugStackHost.Visibility == Visibility.Visible;
    }

    internal void DebugRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshDebugSpectator();

    internal void DebugStack_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DebugStackList?.SelectedItem is not string line)
            return;
        // "name · file:line"
        var at = line.LastIndexOf('·');
        if (at < 0)
            return;
        var loc = line[(at + 1)..].Trim();
        var colon = loc.LastIndexOf(':');
        if (colon <= 0)
            return;
        var file = loc[..colon].Trim();
        if (!int.TryParse(loc[(colon + 1)..].Trim(), out var lineNo))
            return;
        if (File.Exists(file))
            OpenCodeFile(file, lineNo);
    }

    void OnDebugDeskLatchChanged()
    {
        if (IsDebugHostActive())
            RefreshDebugSpectator();
    }

    void RefreshDebugSpectator()
    {
        if (DebugStackList is null || DebugLocalsList is null)
            return;

        DebugStackList.Items.Clear();
        DebugLocalsList.Items.Clear();

        var path = CdpHabitatPaths.GetLatchPath("debug_desk-LATEST.json");
        var raw = CdpLatchIo.TryReadAllTextIfExists(path);
        if (raw is null)
        {
            if (DebugStatusLabel is not null)
                DebugStatusLabel.Text = "debug · live · no latch";
            DebugStackList.Items.Add("(no DAP session · live latch)");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var frames = 0;
            if (root.TryGetProperty("stack", out var stack) && stack.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in stack.EnumerateArray())
                {
                    var name = f.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
                    var file = f.TryGetProperty("file", out var fl) ? fl.GetString() : null;
                    var line = f.TryGetProperty("line", out var ln) && ln.TryGetInt32(out var li) ? li : 0;
                    DebugStackList.Items.Add(file is null ? name : $"{name} · {file}:{line}");
                    frames++;
                }
            }

            if (root.TryGetProperty("locals", out var locals) && locals.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in locals.EnumerateArray())
                {
                    var name = v.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
                    var val = v.TryGetProperty("value", out var vv) ? vv.GetString() ?? "" : "";
                    DebugLocalsList.Items.Add($"{name} = {val}");
                }
            }

            var pulse = root.TryGetProperty("pulse", out var pulseEl) ? pulseEl.GetString() : null;
            var verdict = root.TryGetProperty("verdict", out var verdEl) ? verdEl.GetString() : null;
            var stopped = root.TryGetProperty("stopped", out var stEl) && stEl.ValueKind == JsonValueKind.True;
            var activeDap = root.TryGetProperty("active_dap", out var ad) && ad.ValueKind == JsonValueKind.True;
            var bp = root.TryGetProperty("bp_count", out var bpEl) && bpEl.TryGetInt32(out var bpi) ? bpi : 0;

            if (frames == 0)
            {
                if (pulse is { Length: > 0 })
                    DebugStackList.Items.Add(pulse);
                else
                    DebugStackList.Items.Add(stopped
                        ? "(stopped · frames pending enrich)"
                        : "(latch idle · no frames)");
            }

            if (DebugLocalsList.Items.Count == 0 && verdict is { Length: > 0 })
                DebugLocalsList.Items.Add($"verdict = {verdict}");

            if (DebugStatusLabel is not null)
            {
                var mode = frames > 0 ? "live" : "latch";
                var stopBit = stopped ? "stopped" : "run";
                var dapBit = activeDap ? "dap" : "idle";
                DebugStatusLabel.Text = $"debug · {mode} · {stopBit} · {dapBit} · frames {frames} · bp={bp}";
            }
        }
        catch (Exception ex)
        {
            DebugStackList.Items.Add(ex.Message);
            if (DebugStatusLabel is not null)
                DebugStatusLabel.Text = "debug · latch parse fail";
        }
    }
}

#nullable enable

using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD DebugStack — spectator ListBoxes from debug_desk latch (live DAP later).</summary>
public partial class MainWindow
{
    void RefreshMfdDebugVisibility()
    {
        if (MfdDebugStackHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "DebugStack", StringComparison.OrdinalIgnoreCase);
        MfdDebugStackHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show
            && DebugStackList is not null
            && DebugStackList.Items.Count == 0)
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
                DebugStatusLabel.Text = "debug · spectator · no latch";
            DebugStackList.Items.Add("(no DAP session · spectator)");
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

            if (frames == 0)
                DebugStackList.Items.Add("(latch idle · no frames)");

            if (DebugStatusLabel is not null)
            {
                var active = root.TryGetProperty("active_dap", out var ad) && ad.ValueKind == JsonValueKind.True;
                DebugStatusLabel.Text = active
                    ? $"debug · spectator · frames {frames}"
                    : $"debug · latch · frames {frames}";
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

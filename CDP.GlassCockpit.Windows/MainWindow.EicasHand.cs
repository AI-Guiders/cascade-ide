#nullable enable

using System.IO;
using System.Text.Json;
using System.Windows;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>EICAS SoftKey hand — clr (local suppress) · ack (eicas-cmd → CDP ECL) · list (open items).</summary>
public partial class MainWindow
{
    string? _eicasClrSuppressPulse;

    void WireEicasSoftKeys()
    {
        if (EicasSoftKeys is null)
            return;
        EicasSoftKeys.Key1Click += EicasClr_OnClick;
        EicasSoftKeys.Key2Click += EicasAck_OnClick;
        EicasSoftKeys.Key3Click += EicasList_OnClick;
    }

    void EicasClr_OnClick(object sender, RoutedEventArgs e)
    {
        var pulse = ReadEclPulse() ?? ReadAlertPulse() ?? "";
        _eicasClrSuppressPulse = pulse;
        RefreshEicasHealth();
        StatusText.Text = string.IsNullOrWhiteSpace(pulse)
            ? "glass · eicas · clr · idle"
            : $"glass · eicas · clr suppress · {DateTime.Now:HH:mm:ss}";
    }

    void EicasAck_OnClick(object sender, RoutedEventArgs e)
    {
        var hot = TryReadEclHot();
        if (hot is null || string.IsNullOrWhiteSpace(hot.Value.Checklist) || string.IsNullOrWhiteSpace(hot.Value.Item))
        {
            StatusText.Text = "glass · eicas · ack · no open ecl item";
            return;
        }

        PublishEicasCmd("ack_ecl", hot.Value.Checklist, hot.Value.Item);
    }

    void EicasList_OnClick(object sender, RoutedEventArgs e)
    {
        var lines = TryReadEclOpenLines();
        StatusText.Text = lines.Count == 0
            ? "glass · eicas · list · no open"
            : $"glass · eicas · {string.Join(" · ", lines.Take(4))}";
    }

    void PublishEicasCmd(string op, string? checklist, string? item)
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var id = Guid.NewGuid().ToString("N")[..12];
            var doc = new Dictionary<string, object?>
            {
                ["schema"] = "glass_eicas_cmd/v0",
                ["origin"] = "glass",
                ["id"] = id,
                ["op"] = op,
                ["checklist"] = checklist,
                ["item"] = item,
                ["stamped_utc"] = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            var path = CdpHabitatPaths.EicasCmdLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
            StatusText.Text = $"glass · eicas · {op} {checklist}/{item} · pending";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · eicas cmd fail · {ex.Message}";
        }
    }

    (string Checklist, string Item)? TryReadEclHot()
    {
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "ecl-LATEST.json");
            if (!File.Exists(path))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var checklist = root.TryGetProperty("hot_id", out var h) ? h.GetString() : null;
            if (string.IsNullOrWhiteSpace(checklist))
                return null;
            if (!root.TryGetProperty("open_items", out var items) || items.ValueKind != JsonValueKind.Array)
                return null;
            foreach (var el in items.EnumerateArray())
            {
                var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(id))
                    return (checklist!, id!);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    List<string> TryReadEclOpenLines()
    {
        var list = new List<string>();
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "ecl-LATEST.json");
            if (!File.Exists(path))
                return list;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var hot = root.TryGetProperty("hot_id", out var h) ? h.GetString() : null;
            if (!string.IsNullOrWhiteSpace(hot))
                list.Add(hot!);
            if (root.TryGetProperty("open_items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in items.EnumerateArray())
                {
                    var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    var text = el.TryGetProperty("text", out var tEl) ? tEl.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(id))
                        list.Add(string.IsNullOrWhiteSpace(text) ? id! : $"{id}:{text}");
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return list;
    }

    string? ReadEclPulse()
    {
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "ecl-LATEST.json");
            if (!File.Exists(path))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("pulse", out var p) ? p.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    string? ReadAlertPulse()
    {
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "alert-LATEST.json");
            if (!File.Exists(path))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("pulse", out var p) ? p.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}

#nullable enable

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Intercom HUD: flat Korry AUTOI/HILD/VAD + HDG/CRS + model picker.</summary>
public partial class MainWindow
{
    static readonly Brush KorryOnBg = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x2F));
    static readonly Brush KorryOnBorder = new SolidColorBrush(Color.FromRgb(0x3D, 0x8F, 0x6A));
    static readonly Brush KorryOnFg = new SolidColorBrush(Color.FromRgb(0xB8, 0xF0, 0xD0));
    static readonly Brush KorryOffBg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
    static readonly Brush KorryOffBorder = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46));
    static readonly Brush KorryOffFg = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

    GlassIntercomHud.Snapshot _hud = GlassIntercomHud.Empty;
    string? _lastIgniteStamp;

    static readonly string[] ModelChoices =
    [
        "Citizen · default",
        "Composer · host",
        "PF · habitat"
    ];

    void InitIntercomHud()
    {
        ModelPicker.ItemsSource = ModelChoices;
        var saved = TryLoadModelChoice();
        ModelPicker.SelectedItem = ModelChoices.Contains(saved) ? saved : ModelChoices[0];
        ModelPicker.SelectionChanged += ModelPicker_OnSelectionChanged;
        PaintIntercomHud(_hud);
        TryApplyIgniteHudFromDisk();
    }

    void OnIgniteChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                ApplyIgniteHudRaw(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · hud fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void TryApplyIgniteHudFromDisk()
    {
        try
        {
            var path = CdpHabitatPaths.IgniteLatchPath;
            if (File.Exists(path))
                ApplyIgniteHudRaw(File.ReadAllText(path));
        }
        catch
        {
            /* best-effort */
        }
    }

    void ApplyIgniteHudRaw(string raw)
    {
        var snap = GlassIntercomHud.ParseIgniteJson(raw);
        // stamp guard when present
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("stamped_utc", out var st))
            {
                var stamp = st.ToString();
                if (string.Equals(stamp, _lastIgniteStamp, StringComparison.Ordinal))
                {
                    PaintIntercomHud(snap);
                    return;
                }
                _lastIgniteStamp = stamp;
            }
        }
        catch
        {
            /* ignore */
        }

        _hud = snap;
        PaintIntercomHud(snap);
    }

    void PaintIntercomHud(GlassIntercomHud.Snapshot snap)
    {
        PaintKorry(AutoiKorryBtn, snap.Autoi);
        PaintKorry(HildKorryBtn, snap.Hild);
        PaintKorry(VadKorryBtn, snap.Vad, enabled: false);
        HdgCrsText.Text = snap.HdgCrs;
        HdgCrsText.ToolTip = snap.Pulse ?? snap.HdgCrs;
    }

    static void PaintKorry(Button btn, bool on, bool enabled = true)
    {
        btn.IsEnabled = enabled;
        btn.Tag = on ? "on" : "off";
        btn.Background = on ? KorryOnBg : KorryOffBg;
        btn.BorderBrush = on ? KorryOnBorder : KorryOffBorder;
        btn.Foreground = on ? KorryOnFg : KorryOffFg;
        btn.Opacity = enabled ? 1.0 : 0.55;
    }

    void AutoiKorryBtn_OnClick(object sender, RoutedEventArgs e) =>
        PublishIgniteCmd(GlassIntercomHud.ToggleOp("autoi", _hud.Autoi));

    void HildKorryBtn_OnClick(object sender, RoutedEventArgs e) =>
        PublishIgniteCmd(GlassIntercomHud.ToggleOp("hild", _hud.Hild));

    void VadKorryBtn_OnClick(object sender, RoutedEventArgs e) =>
        StatusText.Text = "glass · VAD · not wired yet";

    void PublishIgniteCmd(string op)
    {
        if (string.IsNullOrWhiteSpace(op))
            return;

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var id = Guid.NewGuid().ToString("N")[..12];
            var doc = new
            {
                schema = "glass_ignite_cmd/v0",
                origin = "glass",
                id,
                op,
                stamped_utc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            var path = CdpHabitatPaths.IgniteCmdLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
            StatusText.Text = $"glass · hud · {op} · pending";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · hud cmd fail · {ex.Message}";
        }
    }

    void ModelPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelPicker.SelectedItem is string s)
            TrySaveModelChoice(s);
    }

    static string ModelChoicePath =>
        Path.Combine(CdpHabitatPaths.StateRoot, "glass-intercom-model.json");

    static string? TryLoadModelChoice()
    {
        try
        {
            if (!File.Exists(ModelChoicePath))
                return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(ModelChoicePath));
            return doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    static void TrySaveModelChoice(string model)
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var json = JsonSerializer.Serialize(new { model, stamped_utc = DateTimeOffset.UtcNow });
            File.WriteAllText(ModelChoicePath, json);
        }
        catch
        {
            /* best-effort */
        }
    }
}

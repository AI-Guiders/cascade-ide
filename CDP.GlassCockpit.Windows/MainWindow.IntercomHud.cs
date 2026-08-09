#nullable enable

using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Intercom HUD: AUTOI/HILD/VAD + HDG/CRS + CIT-lit model; composer lane XOR Korry.</summary>
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
    GlassIntercomLane.Kind _lane = GlassIntercomLane.DefaultLane;
    GlassIntercomChannel.Kind _channel = GlassIntercomChannel.DefaultKind;
    string? _modelId;
    bool _hudModelSuppress;
    string? _dmPeerId;
    IReadOnlyList<GlassIntercomContacts.Contact> _dmRoster = GlassIntercomContacts.DefaultRoster();
    bool _dmContactSuppress;
    IReadOnlyList<GlassIntercomModels.Entry> _modelDirectory = GlassIntercomModels.SealedDirectory;
    bool _citModelSuppress;

    void InitIntercomHud()
    {
        var snap = TryLoadLaneLatch();
        _lane = snap.Lane;
        _modelId = snap.ModelId;
        _channel = TryLoadChannelLatch().Channel;
        LoadDmContactsLatch();
        HudModelPicker.SelectionChanged += HudModelPicker_OnSelectionChanged;
        PaintLaneStrip();
        PaintChannelRail();
        PaintHudModelAxis();
        ApplyComposerHintForLane(force: true);
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
        AutoiKorryBtn.Content = snap.AutoiLabel;
        AutoiKorryBtn.ToolTip = snap.Mode is "talk" or "halt"
            ? $"Partner dialog · Autoi OFF ({snap.Mode})"
            : "AutoIgnition · 2-state";
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

    void LaneCitBtn_OnClick(object sender, RoutedEventArgs e) => SetLane(GlassIntercomLane.Kind.Cit);
    void LaneHostBtn_OnClick(object sender, RoutedEventArgs e) => SetLane(GlassIntercomLane.Kind.Host);
    void LanePfBtn_OnClick(object sender, RoutedEventArgs e) => SetLane(GlassIntercomLane.Kind.Pf);

    void ChannelCrewBtn_OnClick(object sender, RoutedEventArgs e) => SetChannel(GlassIntercomChannel.Kind.Crew);
    void ChannelRadioBtn_OnClick(object sender, RoutedEventArgs e) => SetChannel(GlassIntercomChannel.Kind.Radio);
    void ChannelDmBtn_OnClick(object sender, RoutedEventArgs e) => SetChannel(GlassIntercomChannel.Kind.Dm);

    void SetLane(GlassIntercomLane.Kind lane)
    {
        if (_lane == lane)
            return;

        _lane = lane;
        PaintLaneStrip();
        PaintHudModelAxis();
        ApplyComposerHintForLane(force: false);
        TrySaveLaneLatch();
        StatusText.Text = $"glass · lane · {GlassIntercomLane.Label(lane)} · ch {GlassIntercomChannel.Label(_channel)}";
    }

    void SetChannel(GlassIntercomChannel.Kind channel)
    {
        if (_channel == channel)
            return;

        _channel = channel;
        PaintChannelRail();
        TrySaveChannelLatch();
        RebuildIntercomFeedFromJournal(stickEnd: true);
        if (channel == GlassIntercomChannel.Kind.Dm
            && GlassIntercomContacts.Find(_dmRoster, _dmPeerId) is { } peer)
            StatusText.Text = $"glass · channel · DM · @{peer.Display}";
        else
            StatusText.Text = $"glass · channel · {GlassIntercomChannel.Label(channel)}";
    }

    void PaintLaneStrip()
    {
        PaintKorry(LaneCitBtn, _lane == GlassIntercomLane.Kind.Cit);
        PaintKorry(LaneHostBtn, _lane == GlassIntercomLane.Kind.Host);
        PaintKorry(LanePfBtn, _lane == GlassIntercomLane.Kind.Pf);
        SendBtn.ToolTip =
            $"Send to @{GlassIntercomLane.Label(_lane)} · {GlassIntercomChannel.Label(_channel)} (Enter / Ctrl+Enter; Shift+Enter = newline; / = slash)";
    }

    void PaintChannelRail()
    {
        PaintKorry(ChannelCrewBtn, _channel == GlassIntercomChannel.Kind.Crew);
        PaintKorry(ChannelRadioBtn, _channel == GlassIntercomChannel.Kind.Radio);
        PaintKorry(ChannelDmBtn, _channel == GlassIntercomChannel.Kind.Dm);
        PaintDmContactsPanel();
        PaintLaneStrip();
    }

    void PaintDmContactsPanel()
    {
        var show = _channel == GlassIntercomChannel.Kind.Dm;
        DmContactsPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
            return;

        _dmContactSuppress = true;
        try
        {
            DmContactList.ItemsSource = _dmRoster;
            var sel = GlassIntercomContacts.Find(_dmRoster, _dmPeerId);
            DmContactList.SelectedItem = sel is { } c ? c : (_dmRoster.Count > 0 ? _dmRoster[0] : null);
            if (DmContactList.SelectedItem is GlassIntercomContacts.Contact picked)
                _dmPeerId = picked.Id;
        }
        finally
        {
            _dmContactSuppress = false;
        }
    }

    void DmContactList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dmContactSuppress)
            return;
        if (_channel != GlassIntercomChannel.Kind.Dm)
            return;
        if (DmContactList.SelectedItem is not GlassIntercomContacts.Contact c)
            return;

        if (string.Equals(_dmPeerId, c.Id, StringComparison.OrdinalIgnoreCase))
            return;

        _dmPeerId = c.Id;
        TrySaveDmContactsLatch();
        StatusText.Text = $"glass · channel · DM · @{c.Display}";
    }

    void LoadDmContactsLatch()
    {
        // DM book = operator + Face Who (profiles). Tip Кир/guest = PF lane, not a second DM row.
        var (op, _) = LatchPaint.ResolveIntercomIdentity("pm", "human", null, null);
        var (faceWho, _) = GlassIntercomIdentity.TryCitizenFace("pf");
        var roster = GlassIntercomContacts.DefaultRoster(op, citizenDisplay: faceWho);
        string? selected = null;
        try
        {
            if (File.Exists(DmContactsLatchPath))
                selected = GlassIntercomContacts.ParseLatchJson(File.ReadAllText(DmContactsLatchPath), roster).SelectedId;
        }
        catch
        {
            /* best-effort */
        }

        _dmRoster = roster;
        _dmPeerId = GlassIntercomContacts.ResolveSelectedId(roster, selected ?? "citizen");
    }

    void TrySaveDmContactsLatch()
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var json = GlassIntercomContacts.FormatLatchJson(_dmPeerId, _dmRoster);
            var tmp = DmContactsLatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, DmContactsLatchPath, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }

    static string DmContactsLatchPath =>
        Path.Combine(CdpHabitatPaths.StateRoot, "glass-intercom-dm.json");

    void PaintHudModelAxis()
    {
        var lit = GlassIntercomLane.ModelAxisLit(_lane);
        PaintCitModelsPanel(lit);
        _hudModelSuppress = true;
        try
        {
            if (!lit)
            {
                HudModelPicker.DisplayMemberPath = string.Empty;
                HudModelPicker.SelectedValuePath = string.Empty;
                HudModelPicker.ItemsSource = new[] { "—" };
                HudModelPicker.SelectedItem = "—";
                HudModelPicker.IsEnabled = false;
                HudModelPicker.Opacity = 0.55;
                HudModelPicker.ToolTip = "FM model · dim when lane ≠ CIT";
                return;
            }

            var cfg = GlassCitizenAiKeys.TryReadOpenAiModel();
            _modelDirectory = GlassIntercomModels.BuildDirectory(_modelId, cfg);
            HudModelPicker.DisplayMemberPath = nameof(GlassIntercomModels.Entry.Display);
            HudModelPicker.SelectedValuePath = nameof(GlassIntercomModels.Entry.Id);
            HudModelPicker.ItemsSource = _modelDirectory;
            var pick = GlassIntercomModels.ResolveSelectedId(_modelDirectory, _modelId, cfg)
                ?? _modelDirectory[0].Id;
            HudModelPicker.SelectedValue = pick;
            HudModelPicker.IsEnabled = true;
            HudModelPicker.Opacity = 1.0;
            HudModelPicker.ToolTip =
                $"FM Face directory · Cloud.ru internal · tariff {GlassIntercomModels.TariffRev} · pick writes CFG open_ai_model";
        }
        finally
        {
            _hudModelSuppress = false;
        }
    }

    void PaintCitModelsPanel(bool lit)
    {
        CitModelsPanel.Visibility = lit ? Visibility.Visible : Visibility.Collapsed;
        if (!lit)
            return;

        _citModelSuppress = true;
        try
        {
            var cfg = GlassCitizenAiKeys.TryReadOpenAiModel();
            _modelDirectory = GlassIntercomModels.BuildDirectory(_modelId, cfg);
            CitModelList.ItemsSource = _modelDirectory;
            var pickId = GlassIntercomModels.ResolveSelectedId(_modelDirectory, _modelId, cfg);
            CitModelList.SelectedItem = GlassIntercomModels.Find(_modelDirectory, pickId);
        }
        finally
        {
            _citModelSuppress = false;
        }
    }

    void ApplyModelSelection(string? selectedId)
    {
        var next = GlassIntercomModels.ToLatchModelId(selectedId);
        if (next is null)
        {
            if (_modelId is null)
                return;
            _modelId = null;
            TrySaveLaneLatch();
            var cfg = GlassCitizenAiKeys.TryReadOpenAiModel();
            StatusText.Text = cfg is null
                ? "glass · model · CFG"
                : $"glass · model · {GlassIntercomModels.ShortLabel(cfg)} · CFG";
            PaintHudModelAxis();
            return;
        }

        var cfgNow = GlassCitizenAiKeys.TryReadOpenAiModel();
        var alreadyCfg = string.Equals(cfgNow, next, StringComparison.OrdinalIgnoreCase);
        if (alreadyCfg && _modelId is null)
            return;

        if (!alreadyCfg)
        {
            if (!GlassCitizenAiKeys.TryWriteOpenAiModel(next, out var err))
            {
                StatusText.Text = $"glass · model · CFG write failed · {err}";
                return;
            }
        }

        // CFG is SSOT after pick — clear sticky override so Face Autoi + dialog agree.
        _modelId = null;
        TrySaveLaneLatch();
        var entry = GlassIntercomModels.Find(_modelDirectory, next)
            ?? new GlassIntercomModels.Entry(next, GlassIntercomModels.ShortLabel(next));
        StatusText.Text = GlassIntercomModels.FormatStatusLine(entry, wroteCfg: true);
        PaintHudModelAxis();
    }

    void CitModelList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_citModelSuppress)
            return;
        if (!GlassIntercomLane.ModelAxisLit(_lane))
            return;
        if (CitModelList.SelectedItem is not GlassIntercomModels.Entry entry)
            return;

        ApplyModelSelection(entry.Id);
    }

    void HudModelPicker_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_hudModelSuppress)
            return;
        if (!GlassIntercomLane.ModelAxisLit(_lane))
            return;
        if (HudModelPicker.SelectedItem is GlassIntercomModels.Entry entry)
        {
            ApplyModelSelection(entry.Id);
            return;
        }

        if (HudModelPicker.SelectedValue is string id)
        {
            ApplyModelSelection(id);
            return;
        }

        if (HudModelPicker.SelectedItem is not string s)
            return;
        if (string.Equals(s, "—", StringComparison.Ordinal))
            return;

        ApplyModelSelection(s);
    }

    void ApplyComposerHintForLane(bool force)
    {
        var hint = GlassIntercomLane.ComposerHint(_lane);
        var cur = ComposerBox.Text ?? "";
        if (force || GlassIntercomLane.IsComposerPlaceholder(cur) || string.IsNullOrWhiteSpace(cur))
            ComposerBox.Text = hint;
    }

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

    static string LaneLatchPath =>
        Path.Combine(CdpHabitatPaths.StateRoot, "glass-intercom-lane.json");

    static string LegacyModelLatchPath =>
        Path.Combine(CdpHabitatPaths.StateRoot, "glass-intercom-model.json");

    static GlassIntercomLane.Snapshot TryLoadLaneLatch()
    {
        try
        {
            if (File.Exists(LaneLatchPath))
                return GlassIntercomLane.ParseLatchJson(File.ReadAllText(LaneLatchPath));

            if (File.Exists(LegacyModelLatchPath))
                return GlassIntercomLane.ParseLatchJson(File.ReadAllText(LegacyModelLatchPath));
        }
        catch
        {
            /* best-effort */
        }

        return new GlassIntercomLane.Snapshot(GlassIntercomLane.DefaultLane, null);
    }

    void TrySaveLaneLatch()
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var json = GlassIntercomLane.FormatLatchJson(_lane, _modelId);
            var tmp = LaneLatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, LaneLatchPath, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }

    static string ChannelLatchPath =>
        Path.Combine(CdpHabitatPaths.StateRoot, "glass-intercom-channel.json");

    static GlassIntercomChannel.Snapshot TryLoadChannelLatch()
    {
        try
        {
            if (File.Exists(ChannelLatchPath))
                return GlassIntercomChannel.ParseLatchJson(File.ReadAllText(ChannelLatchPath));
        }
        catch
        {
            /* best-effort */
        }

        return new GlassIntercomChannel.Snapshot(GlassIntercomChannel.DefaultKind);
    }

    void TrySaveChannelLatch()
    {
        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var json = GlassIntercomChannel.FormatLatchJson(_channel);
            var tmp = ChannelLatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, ChannelLatchPath, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }
}

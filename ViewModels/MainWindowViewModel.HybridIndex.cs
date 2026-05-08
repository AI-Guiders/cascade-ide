using System.Diagnostics;
using System.Globalization;
using System.IO;
using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Hybrid Codebase Index (HCI): status projection and UI commands for the HIS (MFD) page.
/// </summary>
public partial class MainWindowViewModel
{
    private IDisposable? _hybridIndexStateSubscription;
    private HybridIndexStateChanged? _hybridIndexLast;

    public string HybridIndexLampText => _hybridIndexLast is null
        ? "NO DATA"
        : string.IsNullOrWhiteSpace(_hybridIndexLast.LastError)
            ? "OK"
            : "CAUTION";

    public string HybridIndexStateShort => _hybridIndexLast is null
        ? "—"
        : string.IsNullOrWhiteSpace(_hybridIndexLast.LastError)
            ? "IDLE"
            : "ERROR";

    public string HybridIndexDocumentCountText =>
        _hybridIndexLast?.DocumentCount.ToString(CultureInfo.InvariantCulture) ?? "—";

    public double HybridIndexDocsValue => (double)(_hybridIndexLast?.DocumentCount ?? 0);

    public double HybridIndexDocsGauge01
    {
        get
        {
            // ECAM-like: simple 0..1 gauge. Scale is a UX choice; start with a stable max.
            const double max = 3000.0;
            var v = (double)(_hybridIndexLast?.DocumentCount ?? 0);
            if (v <= 0)
                return 0;
            return Math.Clamp(v / max, 0, 1);
        }
    }

    public double HybridIndexFreshnessMinutes
    {
        get
        {
            var iso = _hybridIndexLast?.IndexedAtIso;
            if (string.IsNullOrWhiteSpace(iso))
                return 0;
            if (!DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
                return 0;
            var age = DateTimeOffset.UtcNow - ts;
            if (age < TimeSpan.Zero)
                age = TimeSpan.Zero;
            return age.TotalMinutes;
        }
    }

    public string HybridIndexFreshnessMinutesText
    {
        get
        {
            var m = HybridIndexFreshnessMinutes;
            if (m <= 0.5)
                return "0";
            if (m >= 10_000)
                return "9999";
            return Math.Floor(m).ToString(CultureInfo.InvariantCulture);
        }
    }

    public string HybridIndexFreshnessEcamText
    {
        get
        {
            var m = HybridIndexFreshnessMinutes;
            if (m <= 0.5)
                return "0m";

            if (m < 60)
                return $"{Math.Floor(m).ToString(CultureInfo.InvariantCulture)}m";

            var h = m / 60.0;
            if (h < 24)
                return $"{Math.Floor(h).ToString(CultureInfo.InvariantCulture)}h";

            var d = h / 24.0;
            if (d >= 100)
                return "99d";
            return $"{Math.Floor(d).ToString(CultureInfo.InvariantCulture)}d";
        }
    }

    public string HybridIndexIndexedAtText
    {
        get
        {
            var iso = _hybridIndexLast?.IndexedAtIso;
            if (string.IsNullOrWhiteSpace(iso))
                return "—";
            return iso;
        }
    }

    public string HybridIndexFreshnessText
    {
        get
        {
            var iso = _hybridIndexLast?.IndexedAtIso;
            if (string.IsNullOrWhiteSpace(iso))
                return "freshness: —";
            if (!DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
                return "freshness: ?";
            var age = DateTimeOffset.UtcNow - ts;
            if (age < TimeSpan.Zero)
                age = TimeSpan.Zero;
            if (age.TotalHours >= 24)
                return $"freshness: {Math.Floor(age.TotalDays)}d";
            if (age.TotalMinutes >= 60)
                return $"freshness: {Math.Floor(age.TotalHours)}h";
            return $"freshness: {Math.Floor(age.TotalMinutes)}m";
        }
    }

    public string HybridIndexLastErrorText =>
        string.IsNullOrWhiteSpace(_hybridIndexLast?.LastError) ? "—" : _hybridIndexLast!.LastError!;

    public string HybridIndexWorkspaceRootText => _hybridIndexLast?.WorkspaceRoot ?? "—";
    public string HybridIndexSolutionPathText => _hybridIndexLast?.SolutionPath ?? "—";
    public string HybridIndexDatabasePathText => _hybridIndexLast?.DatabasePath ?? "—";

    public string HybridIndexWorkspaceShortText => ShortenPathLikeEcam(HybridIndexWorkspaceRootText);
    public string HybridIndexSolutionShortText => ShortenPathLikeEcam(HybridIndexSolutionPathText);
    public string HybridIndexDatabaseShortText => ShortenPathLikeEcam(HybridIndexDatabasePathText);

    public AnnunciatorLampItem HybridIndexLampItem
    {
        get
        {
            if (_hybridIndexLast is null)
                return new AnnunciatorLampItem(
                    Id: "hci",
                    Title: "HCI",
                    Detail: "No data yet.",
                    Level: AnnunciatorLampLevel.Advisory,
                    LampShortLabel: "HCI");

            var level = string.IsNullOrWhiteSpace(_hybridIndexLast.LastError)
                ? AnnunciatorLampLevel.Ok
                : AnnunciatorLampLevel.Caution;

            var detail = string.IsNullOrWhiteSpace(_hybridIndexLast.LastError)
                ? "OK"
                : _hybridIndexLast.LastError!;

            return new AnnunciatorLampItem(
                Id: "hci",
                Title: "HCI",
                Detail: detail,
                Level: level,
                LampShortLabel: "HCI");
        }
    }

    public string HybridIndexMsgLine1 =>
        $"HCI {HybridIndexLampText}  DOCS {HybridIndexDocumentCountText}  FRESH {HybridIndexFreshnessEcamText}";

    public string HybridIndexMsgLine2
    {
        get
        {
            var err = HybridIndexLastErrorText;
            if (string.IsNullOrWhiteSpace(err) || err == "—")
                return "NO FAILURES";
            return err;
        }
    }

    [RelayCommand]
    private void HybridIndexReindexNow()
    {
        var sln = Workspace.SolutionPath;
        var ws = GetWorkspacePath(sln);
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var (hciWs, hciSln) = ResolveHybridIndexScope(ws, sln);
        if (string.IsNullOrWhiteSpace(hciWs))
            return;

        var enableWatcher = _settings.HybridIndex.Enabled
            && _settings.HybridIndex.WatchFiles
            && !(ChatMcpOnly && _settings.HybridIndex.PauseWhenMcpStdioHost);
        _hybridIndex.SetEnabled(hciWs, hciSln, enabled: enableWatcher, debounceMs: ResolveHybridIndexDebounceMs());
        if (enableWatcher)
            _hybridIndex.Poke(hciWs, hciSln);
        else
            _ = _hybridIndex.RunFullReindexAndPublishStatusAsync(hciWs, hciSln, CancellationToken.None);

        NotifyHybridIndexSnapshotChanged();
    }

    [RelayCommand]
    private void HybridIndexOpenIndexDir()
    {
        var sln = Workspace.SolutionPath;
        var ws = GetWorkspacePath(sln);
        if (string.IsNullOrWhiteSpace(ws))
            return;
        var dir = Path.Combine(ws, ResolveHybridIndexDirRelative());
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignore
        }
    }


    private void EnsureHybridIndexSubscription()
    {
        if (_hybridIndexStateSubscription is not null)
            return;
        _hybridIndexStateSubscription = _ideDataBus.Subscribe<HybridIndexStateChanged>(evt =>
        {
            // DataBus is sync on UI thread in main VM, but keep this UI-safe anyway.
            UiScheduler.Default.Post(() =>
            {
                _hybridIndexLast = evt;
                NotifyHybridIndexSnapshotChanged();
            }, DispatcherPriority.Background);
        });
    }

    private void NotifyHybridIndexSnapshotChanged()
    {
        OnPropertyChanged(nameof(HybridIndexLampText));
        OnPropertyChanged(nameof(HybridIndexStateShort));
        OnPropertyChanged(nameof(HybridIndexDocumentCountText));
        OnPropertyChanged(nameof(HybridIndexDocsValue));
        OnPropertyChanged(nameof(HybridIndexDocsGauge01));
        OnPropertyChanged(nameof(HybridIndexIndexedAtText));
        OnPropertyChanged(nameof(HybridIndexFreshnessText));
        OnPropertyChanged(nameof(HybridIndexFreshnessMinutes));
        OnPropertyChanged(nameof(HybridIndexFreshnessMinutesText));
        OnPropertyChanged(nameof(HybridIndexFreshnessEcamText));
        OnPropertyChanged(nameof(HybridIndexLastErrorText));
        OnPropertyChanged(nameof(HybridIndexWorkspaceRootText));
        OnPropertyChanged(nameof(HybridIndexSolutionPathText));
        OnPropertyChanged(nameof(HybridIndexDatabasePathText));
        OnPropertyChanged(nameof(HybridIndexWorkspaceShortText));
        OnPropertyChanged(nameof(HybridIndexSolutionShortText));
        OnPropertyChanged(nameof(HybridIndexDatabaseShortText));
        OnPropertyChanged(nameof(HybridIndexLampItem));
        OnPropertyChanged(nameof(HybridIndexMsgLine1));
        OnPropertyChanged(nameof(HybridIndexMsgLine2));
    }

    private static string ShortenPathLikeEcam(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "—")
            return "—";

        var s = text.Trim();
        try
        {
            if (s.IndexOf(Path.DirectorySeparatorChar) >= 0 || s.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            {
                var trimmed = s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var name = Path.GetFileName(trimmed);
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
        }
        catch
        {
            // ignore
        }

        const int max = 34;
        if (s.Length <= max)
            return s;
        return s[..(max - 1)] + "…";
    }
}


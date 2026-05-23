using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Hybrid Codebase Index (HCI): status projection and UI commands for the HIS (MFD) page.
/// </summary>
public partial class MainWindowViewModel
{
    private IDisposable? _hybridIndexStateSubscription;

    /// <summary>
    /// Имена свойств HIS, которые завязаны на <see cref="HybridIndexLast"/> или должны пересчитываться без нового <c>HybridIndexStateChanged</c> (UTC/открытие вкладки).
    /// При добавлении свойства расширяй массив и список в <c>[NotifyPropertyChangedFor(...)]</c> над полем снимка индекса.
    /// </summary>
    private static readonly string[] HybridIndexDependentPresentationNames =
    [
        nameof(HybridIndexLampText),
        nameof(HybridIndexStateShort),
        nameof(HybridIndexDocumentCountText),
        nameof(HybridIndexDocsValue),
        nameof(HybridIndexDocsGauge01),
        nameof(HybridIndexIndexedAtText),
        nameof(HybridIndexFreshnessText),
        nameof(HybridIndexFreshnessMinutes),
        nameof(HybridIndexFreshnessMinutesText),
        nameof(HybridIndexFreshnessEcamText),
        nameof(HybridIndexLastErrorText),
        nameof(HybridIndexWorkspaceRootText),
        nameof(HybridIndexSolutionPathText),
        nameof(HybridIndexDatabasePathText),
        nameof(HybridIndexWorkspaceShortText),
        nameof(HybridIndexSolutionShortText),
        nameof(HybridIndexDatabaseShortText),
        nameof(HybridIndexLampItem),
        nameof(HybridIndexMsgLine1),
        nameof(HybridIndexMsgLine2),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(HybridIndexLampText),
        nameof(HybridIndexStateShort),
        nameof(HybridIndexDocumentCountText),
        nameof(HybridIndexDocsValue),
        nameof(HybridIndexDocsGauge01),
        nameof(HybridIndexIndexedAtText),
        nameof(HybridIndexFreshnessText),
        nameof(HybridIndexFreshnessMinutes),
        nameof(HybridIndexFreshnessMinutesText),
        nameof(HybridIndexFreshnessEcamText),
        nameof(HybridIndexLastErrorText),
        nameof(HybridIndexWorkspaceRootText),
        nameof(HybridIndexSolutionPathText),
        nameof(HybridIndexDatabasePathText),
        nameof(HybridIndexWorkspaceShortText),
        nameof(HybridIndexSolutionShortText),
        nameof(HybridIndexDatabaseShortText),
        nameof(HybridIndexLampItem),
        nameof(HybridIndexMsgLine1),
        nameof(HybridIndexMsgLine2),
        nameof(ShowPfdBackgroundStatusBar))]
    private HybridIndexStateChanged? _hybridIndexLast;

    public string HybridIndexLampText => HybridIndexHisPresentationProjection.LampText(HybridIndexLast);

    public string HybridIndexStateShort =>
        HybridIndexHisPresentationProjection.StateShort(HybridIndexLast);

    public string HybridIndexDocumentCountText =>
        HybridIndexLast is null ? "—" : HybridIndexLast.DocumentCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public double HybridIndexDocsValue => (double)(HybridIndexLast?.DocumentCount ?? 0);

    public double HybridIndexDocsGauge01 =>
        HybridIndexHisPresentationProjection.DocsGauge01(HybridIndexLast?.DocumentCount ?? 0);

    public double HybridIndexFreshnessMinutes =>
        HybridIndexHisPresentationProjection.FreshnessTotalMinutes(
            HybridIndexLast?.IndexedAtIso,
            DateTimeOffset.UtcNow);

    public string HybridIndexFreshnessMinutesText =>
        HybridIndexHisPresentationProjection.FreshnessMinutesRoundedText(HybridIndexFreshnessMinutes);

    public string HybridIndexFreshnessEcamText =>
        HybridIndexHisPresentationProjection.FreshnessEcamText(HybridIndexFreshnessMinutes);

    public string HybridIndexIndexedAtText =>
        HybridIndexHisPresentationProjection.IndexedAtOrDash(HybridIndexLast?.IndexedAtIso);

    public string HybridIndexFreshnessText =>
        HybridIndexHisPresentationProjection.FreshnessColonLine(
            HybridIndexLast?.IndexedAtIso,
            DateTimeOffset.UtcNow);

    public string HybridIndexLastErrorText =>
        HybridIndexHisPresentationProjection.LastErrorOrDash(HybridIndexLast?.LastError);

    public string HybridIndexWorkspaceRootText =>
        HybridIndexHisPresentationProjection.OptionalFieldOrDash(HybridIndexLast?.WorkspaceRoot);

    public string HybridIndexSolutionPathText =>
        HybridIndexHisPresentationProjection.OptionalFieldOrDash(HybridIndexLast?.SolutionPath);

    public string HybridIndexDatabasePathText =>
        HybridIndexHisPresentationProjection.OptionalFieldOrDash(HybridIndexLast?.DatabasePath);

    public string HybridIndexWorkspaceShortText => HybridIndexHisPathDisplayShortener.ShortenLikeEcam(HybridIndexWorkspaceRootText);
    public string HybridIndexSolutionShortText => HybridIndexHisPathDisplayShortener.ShortenLikeEcam(HybridIndexSolutionPathText);
    public string HybridIndexDatabaseShortText => HybridIndexHisPathDisplayShortener.ShortenLikeEcam(HybridIndexDatabasePathText);

    public AnnunciatorLampItem HybridIndexLampItem =>
        HybridIndexHisPresentationProjection.LampItem(HybridIndexLast);

    public string HybridIndexMsgLine1 =>
        $"HCI {HybridIndexLampText}  DOCS {HybridIndexDocumentCountText}  FRESH {HybridIndexFreshnessEcamText}";

    public string HybridIndexMsgLine2 =>
        $"{SolutionWarmupStatusText}  |  {HybridIndexHisPresentationProjection.SecondMessageLine(HybridIndexLastErrorText)}";

    [RelayCommand]
    private void HybridIndexReindexNow()
    {
        var sln = Workspace.SolutionPath;
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(sln);
        if (string.IsNullOrWhiteSpace(ws))
            return;

        _ = HybridIndexOrchestrationPolicy.TriggerReindexNowAsync(
            _hybridIndex,
            _settings.HybridIndex,
            ChatMcpOnly,
            ws,
            sln,
            CancellationToken.None);

        RaiseHybridIndexPresentationProperties();
    }

    [RelayCommand]
    private void HybridIndexOpenIndexDir()
    {
        var sln = Workspace.SolutionPath;
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(sln);
        if (string.IsNullOrWhiteSpace(ws))
            return;
        var dir = Path.Combine(ws, HybridIndexIndexDirectoryRelative.ResolveOrDefault(_settings.HybridIndex.IndexDir));
        _osShell.TryOpenDirectory(dir);
    }


    private void EnsureHybridIndexSubscription()
    {
        if (_hybridIndexStateSubscription is not null)
            return;
        _hybridIndexStateSubscription = HybridIndexHisStateBusSubscription.Subscribe(
            _ideDataBus,
            UiScheduler.Default,
            evt => HybridIndexLast = evt);
    }

    /// <summary>Перерисовать вычисляемые поля HIS без смены последнего события DataBus (свежесть от часов, открытие вкладки).</summary>
    private void RaiseHybridIndexPresentationProperties()
    {
        foreach (var name in HybridIndexDependentPresentationNames)
            OnPropertyChanged(name);
    }

}


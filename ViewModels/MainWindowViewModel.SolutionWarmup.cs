using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using CascadeIDE.Features.SolutionWarmup.Application;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Solution warm-up: оркестратор фаз загрузки решения, лампа статуса и подписка на DataBus (ADR 0141).
/// </summary>
public partial class MainWindowViewModel
{
    private SolutionWarmupOrchestrator? _solutionWarmup;
    private IDisposable? _solutionWarmupStateSubscription;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolutionWarmupStatusText))]
    [NotifyPropertyChangedFor(nameof(SolutionWarmupLampItem))]
    [NotifyPropertyChangedFor(nameof(HybridIndexMsgLine2), nameof(SolutionWarmupStatusText), nameof(ShowPfdBackgroundStatusBar))]
    private SolutionWarmupStateChanged? _solutionWarmupLast;

    public string SolutionWarmupStatusText =>
        SolutionWarmupHisPresentationProjection.StatusLine(SolutionWarmupLast);

    public AnnunciatorLampItem SolutionWarmupLampItem =>
        SolutionWarmupHisPresentationProjection.LampItem(SolutionWarmupLast);

    private void EnsureSolutionWarmupOrchestrator()
    {
        if (_solutionWarmup is not null)
            return;

        _solutionWarmup = new SolutionWarmupOrchestrator(
            _ideDataBus,
            new SolutionWarmupHostCallbacks
            {
                GetActiveCsFilePath = () => CurrentFilePath,
                GetOpenCsFilePaths = () => Documents.OpenDocuments
                    .Select(d => d.FilePath)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList(),
                RunFeedAnchorsOnUi = () => UiScheduler.Default.Post(
                    () => ChatPanel.RefreshAttachmentAnchorsForCurrentScope(),
                    Avalonia.Threading.DispatcherPriority.Background),
                GetWarmupSettings = () => _settings.SolutionWarmup,
                GetHybridIndexSettings = () => _settings.HybridIndex,
                GetLatestHybridIndexState = () => HybridIndexLast,
            });
    }

    private void EnsureSolutionWarmupSubscription()
    {
        if (_solutionWarmupStateSubscription is not null)
            return;

        _solutionWarmupStateSubscription = SolutionWarmupStateBusSubscription.Subscribe(
            _ideDataBus,
            UiScheduler.Default,
            evt => SolutionWarmupLast = evt);
    }

    private void ApplySolutionWarmupForCurrentSolution()
    {
        EnsureSolutionWarmupOrchestrator();
        EnsureSolutionWarmupSubscription();

        var value = Workspace.SolutionPath ?? "";
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(value);
        _solutionWarmup!.OnSolutionScopeChanged(ws, string.IsNullOrWhiteSpace(value) ? null : value);
    }
}

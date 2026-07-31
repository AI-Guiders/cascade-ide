#nullable enable

using Avalonia.Threading;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.SolutionWarmup.Application;
using CascadeIDE.Features.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Компактная полоса статуса над PFD/Forward: warmup/HCI refresh (ADR 0141).
/// Verify Epoch → <c>PfdBackgroundStatus.VerifyEpoch</c>.
/// </summary>
public partial class MainWindowViewModel
{
    private const int PfdStatusMinVisibleMs = 400;

    private IDisposable? _pfdBackgroundStatusWarmupSubscription;
    private IDisposable? _pfdBackgroundStatusHciSubscription;
    private bool _hciReindexPending;
    private DateTimeOffset _pfdStatusVisibleSinceUtc;
    private IDisposable? _pfdStatusHideTimer;
    private IDisposable? _pfdAgentEnvironmentTaskSubscription;
    private DispatcherTimer? _verifyEpochActiveTicker;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPfdBackgroundStatusBar))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBackgroundStatusOnPfd))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBackgroundStatusOnForward))]
    private string? _pfdBackgroundStatusText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPfdAgentEnvironmentCancel))]
    [NotifyPropertyChangedFor(nameof(ShowPfdVerifyEpochRetry))]
    private bool _isPfdBackgroundStatusCaution;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPfdAgentEnvironmentCancel))]
    private bool _pfdAgentEnvironmentCancelVisible;

    public bool ShowPfdAgentEnvironmentCancel =>
        ShowPfdBackgroundStatusBar && PfdAgentEnvironmentCancelVisible;

    public bool ShowPfdVerifyEpochRetry =>
        ShowPfdBackgroundStatusBar && _verifyEpochInstrument.ShowRetry;

    public bool ShowPfdVerifyEpochExpandToggle => _verifyEpochInstrument.IsVisible;

    public bool ShowPfdBackgroundStatusBar =>
        _settings.SolutionWarmup.ShowBackgroundStatusOnPfd
        && (!string.IsNullOrWhiteSpace(PfdBackgroundStatusText)
            || ShowPfdVerifyEpochExpandedPanel);

    /// <summary>Полоса на PFD: master + <c>pfd_status_strip</c> в <c>[display.instruments]</c>.</summary>
    public bool ShowWorkspaceBackgroundStatusOnPfd =>
        ShowPfdBackgroundStatusBar
        && InstrumentStatusStripPlacement.IsVisibleOnPfd(_settings.Display, masterEnabled: true);

    /// <summary>Полоса на Forward: master + <c>forward_status_strip</c>.</summary>
    public bool ShowWorkspaceBackgroundStatusOnForward =>
        ShowPfdBackgroundStatusBar
        && InstrumentStatusStripPlacement.IsVisibleOnForward(_settings.Display, masterEnabled: true);

    internal void NotifyWorkspaceBackgroundStatusStripPlacement()
    {
        OnPropertyChanged(nameof(ShowWorkspaceBackgroundStatusOnPfd));
        OnPropertyChanged(nameof(ShowWorkspaceBackgroundStatusOnForward));
        OnPropertyChanged(nameof(ShowPfdBackgroundStatusBar));
    }

    private void EnsurePfdBackgroundStatusSubscription()
    {
        if (_pfdBackgroundStatusWarmupSubscription is not null)
            return;

        _pfdBackgroundStatusWarmupSubscription = _ideDataBus.Subscribe<SolutionWarmupStateChanged>(_ =>
            UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background));

        _pfdBackgroundStatusHciSubscription = _ideDataBus.Subscribe<HybridIndexStateChanged>(_ =>
            UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background));
    }

    internal void MarkHciReindexPendingForPfdStatus()
    {
        if (!_settings.HybridIndex.Enabled || !_settings.HybridIndex.AutoReindexOnSolutionOpen)
        {
            _hciReindexPending = false;
            return;
        }

        _hciReindexPending = true;
        RefreshPfdBackgroundStatusBar();
    }

    internal void RefreshPfdBackgroundStatusBar()
    {
        if (!_settings.SolutionWarmup.ShowBackgroundStatusOnPfd)
        {
            applyPfdStatusHidden(immediate: true);
            return;
        }

        if (_settings.Agent.Environment.TimeAccounting.PfdInstrumentEnabled
            && TryApplyVerifyEpochPfdStatus())
        {
            NotifyWorkspaceBackgroundStatusStripPlacement();
            return;
        }

        var workspaceRoot = WorkspaceDirectoryFromSolutionPath.Resolve(Workspace.SolutionPath ?? "");
        var solutionPath = Workspace.SolutionPath;

        var snap = PfdBackgroundStatusPresentation.Compute(
            workspaceRoot,
            solutionPath,
            SolutionWarmupLast,
            HybridIndexLast,
            _hciReindexPending,
            _settings.HybridIndex);

        if (PfdBackgroundStatusPresentation.MatchesScope(
                HybridIndexLast?.WorkspaceRoot,
                HybridIndexLast?.SolutionPath,
                workspaceRoot,
                solutionPath)
            && string.IsNullOrWhiteSpace(HybridIndexLast?.LastError))
            _hciReindexPending = false;

        if (snap.Show)
        {
            StopPfdStatusHideTimer();
            _pfdStatusVisibleSinceUtc = DateTimeOffset.UtcNow;
            PfdBackgroundStatusText = snap.Text;
            IsPfdBackgroundStatusCaution = snap.IsCaution;
            PfdAgentEnvironmentCancelVisible = false;
            NotifyWorkspaceBackgroundStatusStripPlacement();
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - _pfdStatusVisibleSinceUtc;
        if (elapsed.TotalMilliseconds < PfdStatusMinVisibleMs && !string.IsNullOrWhiteSpace(PfdBackgroundStatusText))
        {
            schedulePfdStatusHide(TimeSpan.FromMilliseconds(PfdStatusMinVisibleMs) - elapsed);
            return;
        }

        applyPfdStatusHidden(immediate: true);
    }
}

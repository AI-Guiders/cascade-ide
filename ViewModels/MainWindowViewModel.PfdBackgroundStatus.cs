#nullable enable

using Avalonia.Threading;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.SolutionWarmup.Application;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Компактная полоса статуса над PFD/Forward: индексация HCI и solution warm-up (ADR 0141).
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
    private string? _pfdAgentEnvironmentTaskDetail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPfdBackgroundStatusBar))]
    private string? _pfdBackgroundStatusText;

    [ObservableProperty]
    private bool _isPfdBackgroundStatusCaution;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPfdAgentEnvironmentCancel))]
    private bool _pfdAgentEnvironmentCancelVisible;

    public bool ShowPfdAgentEnvironmentCancel =>
        ShowPfdBackgroundStatusBar && PfdAgentEnvironmentCancelVisible;

    public bool ShowPfdBackgroundStatusBar =>
        _settings.SolutionWarmup.ShowBackgroundStatusOnPfd
        && !string.IsNullOrWhiteSpace(PfdBackgroundStatusText);

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

        if (_settings.Agent.Environment.TimeAccounting.PfdInstrumentEnabled)
        {
            var agentStatus = _agentEnvironment.GetStatus();
            var epochStale = _agentEnvironment.EpochTracker.IsUiStale
                || agentStatus.WritesInvalidatedVerifyEpoch;
            PfdAgentEnvironmentCancelVisible = false;

            if (epochStale)
            {
                StopPfdStatusHideTimer();
                _pfdStatusVisibleSinceUtc = DateTimeOffset.UtcNow;
                PfdBackgroundStatusText = "⚠ AEE verify устарел — перезапусти /agent verify";
                IsPfdBackgroundStatusCaution = true;
                NotifyWorkspaceBackgroundStatusStripPlacement();
                return;
            }

            if (agentStatus.IsActive)
            {
                StopPfdStatusHideTimer();
                _pfdStatusVisibleSinceUtc = DateTimeOffset.UtcNow;
                var detail = string.IsNullOrWhiteSpace(_pfdAgentEnvironmentTaskDetail)
                    ? ""
                    : $" · {_pfdAgentEnvironmentTaskDetail}";
                PfdBackgroundStatusText =
                    $"AEE verify {agentStatus.RunId![..8]}… · {agentStatus.Policy}{detail}";
                IsPfdBackgroundStatusCaution = false;
                PfdAgentEnvironmentCancelVisible = true;
                NotifyWorkspaceBackgroundStatusStripPlacement();
                return;
            }

            _pfdAgentEnvironmentTaskDetail = null;
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

    internal void EnsurePfdAgentEnvironmentTaskSubscription()
    {
        if (_pfdAgentEnvironmentTaskSubscription is not null)
            return;

        _pfdAgentEnvironmentTaskSubscription = _ideDataBus.Subscribe<AgentEnvironmentTaskChanged>(evt =>
        {
            if (evt.State is not (AgentEnvironmentTaskState.Running or AgentEnvironmentTaskState.Queued))
                return;

            UiScheduler.Default.Post(() =>
            {
                _pfdAgentEnvironmentTaskDetail = string.IsNullOrWhiteSpace(evt.ProgressMessage)
                    ? evt.Kind
                    : $"{evt.Kind}: {evt.ProgressMessage}";
                RefreshPfdBackgroundStatusBar();
            }, DispatcherPriority.Background);
        });
    }

    [RelayCommand]
    private void CancelPfdAgentEnvironmentVerify()
    {
        if (_agentEnvironment.CancelActive())
            RefreshPfdBackgroundStatusBar();
    }

    [RelayCommand]
    private void OpenPfdBackgroundStatusDetails()
    {
        if (!string.IsNullOrWhiteSpace(HybridIndexLast?.LastError)
            || _hciReindexPending
            || HybridIndexLast is null)
        {
            TryNavigateToMfdShellPage(MfdShellPage.HybridIndex);
            return;
        }

        if (SolutionWarmupLast?.Lifecycle == SolutionWarmupLifecycle.Partial)
            TryNavigateToMfdShellPage(MfdShellPage.HybridIndex);
    }

    private void schedulePfdStatusHide(TimeSpan delay)
    {
        StopPfdStatusHideTimer();
        _pfdStatusHideTimer = DispatcherTimer.RunOnce(() =>
        {
            _pfdStatusHideTimer = null;
            applyPfdStatusHidden(immediate: true);
        }, delay);
    }

    private void applyPfdStatusHidden(bool immediate)
    {
        if (!immediate && IsPfdBackgroundStatusCaution)
            return;

        StopPfdStatusHideTimer();
        PfdBackgroundStatusText = null;
        IsPfdBackgroundStatusCaution = false;
        NotifyWorkspaceBackgroundStatusStripPlacement();
    }

    private void StopPfdStatusHideTimer()
    {
        _pfdStatusHideTimer?.Dispose();
        _pfdStatusHideTimer = null;
    }
}

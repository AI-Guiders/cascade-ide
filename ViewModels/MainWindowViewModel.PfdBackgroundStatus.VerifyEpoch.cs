#nullable enable

using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Verify Epoch (AEE W3) on PFD status strip: apply, commands, ticker, hide timers.</summary>
public partial class MainWindowViewModel
{
    private bool TryApplyVerifyEpochPfdStatus()
    {
        var snap = _verifyEpochInstrument.Snapshot();
        if (!snap.IsVisible && string.IsNullOrWhiteSpace(snap.CompactLine))
            return false;

        StopPfdStatusHideTimer();
        _pfdStatusVisibleSinceUtc = DateTimeOffset.UtcNow;
        PfdBackgroundStatusText = snap.CompactLine;
        IsPfdBackgroundStatusCaution = snap.IsCaution;
        PfdAgentEnvironmentCancelVisible = snap.ShowCancel;
        EnsureVerifyEpochActiveTicker();

        if (!snap.IsVisible && !snap.IsCaution)
        {
            schedulePfdStatusHide(TimeSpan.FromMilliseconds(PfdStatusMinVisibleMs));
            return true;
        }

        return true;
    }

    internal void EnsurePfdAgentEnvironmentTaskSubscription()
    {
        if (_pfdAgentEnvironmentTaskSubscription is not null)
            return;

        _pfdAgentEnvironmentTaskSubscription = _ideDataBus.Subscribe<AgentEnvironmentTaskChanged>(_ =>
        {
            UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background);
        });
    }

    [RelayCommand]
    private void CancelPfdAgentEnvironmentVerify()
    {
        if (_agentEnvironment.CancelActive())
            RefreshPfdBackgroundStatusBar();
    }

    [RelayCommand]
    private void RetryPfdAgentEnvironmentVerify()
    {
        var solutionPath = Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath))
            return;

        _agentEnvironment.StartVerify(solutionPath, AgentVerifyPolicy.Standard);
        RefreshPfdBackgroundStatusBar();
    }

    private void EnsureVerifyEpochActiveTicker()
    {
        if (!_verifyEpochInstrument.IsActive)
        {
            StopVerifyEpochActiveTicker();
            return;
        }

        if (_verifyEpochActiveTicker is not null)
            return;

        _verifyEpochActiveTicker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _verifyEpochActiveTicker.Tick += (_, _) =>
            UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background);
        _verifyEpochActiveTicker.Start();
    }

    private void StopVerifyEpochActiveTicker()
    {
        if (_verifyEpochActiveTicker is null)
            return;

        _verifyEpochActiveTicker.Stop();
        _verifyEpochActiveTicker = null;
    }

    [RelayCommand]
    private void OpenPfdBackgroundStatusDetails()
    {
        if (_settings.Agent.Environment.TimeAccounting.PfdInstrumentEnabled
            && _verifyEpochInstrument.IsVisible)
        {
            TogglePfdVerifyEpochExpandedCommand.Execute(null);
            return;
        }

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
            _verifyEpochInstrument.HideAfterIdle();
            applyPfdStatusHidden(immediate: true);
        }, delay);
    }

    private void applyPfdStatusHidden(bool immediate)
    {
        if (!immediate && IsPfdBackgroundStatusCaution)
            return;

        StopPfdStatusHideTimer();
        StopVerifyEpochActiveTicker();
        PfdBackgroundStatusText = null;
        IsPfdBackgroundStatusCaution = false;
        PfdAgentEnvironmentCancelVisible = false;
        IsPfdVerifyEpochExpanded = false;
        NotifyWorkspaceBackgroundStatusStripPlacement();
    }

    private void StopPfdStatusHideTimer()
    {
        _pfdStatusHideTimer?.Dispose();
        _pfdStatusHideTimer = null;
    }
}

using Avalonia.Threading;
using CascadeIDE.Features.UiChrome;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.AutonomousAgent;

/// <summary>
/// Состояние и команды автономного агента (Power): цель, шаги, start/pause/resume и быстрые сценарии.
/// </summary>
public sealed partial class AutonomousAgentSessionViewModel : ObservableObject
{
    private readonly IAutonomousAgentSessionHost _host;

    private AutonomousAgentService _agentService;
    private CancellationTokenSource? _cts;
    private Task? _task;
    private AutonomousRunState? _runState;

    public AutonomousAgentSessionViewModel(AutonomousAgentService agentService, IAutonomousAgentSessionHost host)
    {
        _agentService = agentService;
        _host = host;
    }

    /// <summary>Автономный цикл разрешён только в семействе Power (кокпит); иначе сценарии уходят в чат.</summary>
    private bool AutonomousCockpitActive => _host.UiModeFamily.IsPowerFamily();

    private bool HasResumableAutonomousRun =>
        _runState?.HasResumableSteps == true;

    public bool IsAutonomousPaused =>
        AutonomousCockpitActive && !IsAutonomousRunning && HasResumableAutonomousRun;

    public void NotifyHostPowerContextChanged() =>
        OnPropertyChanged(nameof(IsAutonomousPaused));

    public void ReplaceAgentService(AutonomousAgentService agentService) =>
        _agentService = agentService;

    /// <summary>Отмена из-за смены внешних MCP: только CTS, UI добьёт async.</summary>
    public void CancelForHostReconfiguration() =>
        _cts?.Cancel();

    /// <summary>Emergency stop: сбросить сессию и полосу задач.</summary>
    public void CancelAutonomousRunCompletely()
    {
        _cts?.Cancel();
        IsAutonomousRunning = false;
        _host.SetActiveTaskStrip("Autonomous Agent", "Paused", 0);
        _host.ResultSummary = "Autonomous flow paused by operator.";
        _runState = null;
        OnPropertyChanged(nameof(IsAutonomousPaused));
        StartAutonomousCommand.NotifyCanExecuteChanged();
        ResumeAutonomousCommand.NotifyCanExecuteChanged();
    }

    private bool CanStartAutonomous() =>
        AutonomousCockpitActive
        && !IsAutonomousRunning
        && !HasResumableAutonomousRun
        && !string.IsNullOrWhiteSpace(AutonomousObjective)
        && AutonomousMaxSteps > 0;

    [RelayCommand(CanExecute = nameof(CanStartAutonomous))]
    private void StartAutonomous() =>
        StartAutonomousFlow(AutonomousObjective, AutonomousMaxSteps);

    private bool CanPauseAutonomous() => IsAutonomousRunning;

    [RelayCommand(CanExecute = nameof(CanPauseAutonomous))]
    private void PauseAutonomous()
    {
        _cts?.Cancel();
        IsAutonomousRunning = false;
        _host.SetActiveTaskStrip("Autonomous Agent", "Paused", 0);
        var state = _runState;
        _host.ResultSummary = state is null
            ? "Autonomous flow paused."
            : $"Autonomous paused. Next step: {state.NextStep + 1}/{state.MaxSteps}.";
        OnPropertyChanged(nameof(IsAutonomousPaused));
        StartAutonomousCommand.NotifyCanExecuteChanged();
        ResumeAutonomousCommand.NotifyCanExecuteChanged();
    }

    private bool CanResumeAutonomous() =>
        AutonomousCockpitActive && !IsAutonomousRunning && HasResumableAutonomousRun;

    [RelayCommand(CanExecute = nameof(CanResumeAutonomous))]
    private void ResumeAutonomous() =>
        ResumeAutonomousFlow();

    private void StartAutonomousFlow(string objective, int maxSteps)
    {
        if (!AutonomousCockpitActive)
            return;

        AutonomousObjective = objective;
        AutonomousMaxSteps = maxSteps;

        _runState = new AutonomousRunState
        {
            Objective = objective,
            SafetyLevel = _host.SafetyLevel,
            MaxSteps = maxSteps,
            NextStep = 0
        };

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsAutonomousRunning = true;
        _host.SetActiveTaskStrip("Autonomous Agent", "Running", 0);
        _host.ResultSummary = "Autonomous flow started…";
        OnPropertyChanged(nameof(IsAutonomousPaused));

        _task = Task.Run(async () =>
        {
            try
            {
                var state = _runState;
                var result = await _agentService.RunAutonomousAsync(
                        objective,
                        _host.SafetyLevel,
                        maxSteps,
                        ct,
                        state)
                    .ConfigureAwait(false);
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Done", 100);
                    _host.ResultSummary = result;
                    _runState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (OperationCanceledException)
            {
                var state = _runState;
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Paused", 0);
                    _host.ResultSummary = state is null
                        ? "Autonomous flow cancelled."
                        : $"Autonomous paused. Next step: {state.NextStep + 1}/{state.MaxSteps}.";
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Error", 0);
                    _host.ResultSummary = "Autonomous error: " + ex.Message;
                    _runState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
        }, ct);
    }

    private void ResumeAutonomousFlow()
    {
        if (!CanResumeAutonomous())
            return;

        var state = _runState;
        if (state is null)
            return;

        _host.SafetyLevel = state.SafetyLevel;
        AutonomousObjective = state.Objective;
        AutonomousMaxSteps = state.MaxSteps;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsAutonomousRunning = true;
        _host.SetActiveTaskStrip("Autonomous Agent", "Running", 0);
        _host.ResultSummary = $"Autonomous resumed from step {state.NextStep + 1}/{state.MaxSteps}…";
        OnPropertyChanged(nameof(IsAutonomousPaused));

        var capturedState = state;

        _task = Task.Run(async () =>
        {
            try
            {
                var result = await _agentService.RunAutonomousAsync(
                        capturedState.Objective,
                        capturedState.SafetyLevel,
                        capturedState.MaxSteps,
                        ct,
                        capturedState)
                    .ConfigureAwait(false);
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Done", 100);
                    _host.ResultSummary = result;
                    _runState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (OperationCanceledException)
            {
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Paused", 0);
                    _host.ResultSummary =
                        $"Autonomous paused. Next step: {capturedState.NextStep + 1}/{capturedState.MaxSteps}.";
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                UiScheduler.Default.Post(() =>
                {
                    IsAutonomousRunning = false;
                    _host.SetActiveTaskStrip("Autonomous Agent", "Error", 0);
                    _host.ResultSummary = "Autonomous error: " + ex.Message;
                    _runState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
        }, ct);
    }

    [RelayCommand]
    private void FixFailingTests()
    {
        var objective = "Fix failing tests using minimal-risk changes. Use get_workspace_state / run_affected_tests, then propose safe fixes.";
        if (AutonomousCockpitActive)
        {
            StartAutonomousFlow(objective, maxSteps: 10);
            return;
        }

        _host.OpenChatForAgentFallback(
            "Fix failing tests using minimal-risk changes. Start with ide_run_affected_tests and explain each step.");
    }

    [RelayCommand]
    private void InvestigateNullref()
    {
        var objective = "Investigate possible null reference in current context. Show the shortest safe fix plan with evidence from diagnostics/tests.";
        if (AutonomousCockpitActive)
        {
            StartAutonomousFlow(objective, maxSteps: 8);
            return;
        }

        _host.OpenChatForAgentFallback(
            "Investigate possible null reference in current context. Show the shortest safe fix plan.");
    }

    [RelayCommand]
    private void PrepareCommit()
    {
        var objective = "Prepare a clean commit plan grouped by logical changes and include verification steps.";
        if (AutonomousCockpitActive)
        {
            StartAutonomousFlow(objective, maxSteps: 6);
            return;
        }

        _host.OpenChatForAgentFallback(
            "Prepare a clean commit plan grouped by logical changes and include verification steps.");
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    private string _autonomousObjective = "Autonomous objective: fix issues in the current workspace.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    private int _autonomousMaxSteps = 10;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseAutonomousCommand))]
    [NotifyPropertyChangedFor(nameof(IsAutonomousPaused))]
    private bool _isAutonomousRunning;
}

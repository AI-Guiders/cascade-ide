using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Instrumentation;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    private bool HasResumableAutonomousRun =>
        _autonomousRunState?.HasResumableSteps == true;

    public bool IsAutonomousPaused =>
        IsPowerMode && !IsAutonomousRunning && HasResumableAutonomousRun;

    private bool CanStartAutonomous() =>
        IsPowerMode
        && !IsAutonomousRunning
        && !HasResumableAutonomousRun
        && !string.IsNullOrWhiteSpace(AutonomousObjective)
        && AutonomousMaxSteps > 0;

    [RelayCommand(CanExecute = nameof(CanStartAutonomous))]
    private void StartAutonomous()
    {
        StartAutonomousFlow(AutonomousObjective, AutonomousMaxSteps);
    }

    private bool CanPauseAutonomous() => IsAutonomousRunning;

    [RelayCommand(CanExecute = nameof(CanPauseAutonomous))]
    private void PauseAutonomous()
    {
        _autonomousCts?.Cancel();
        IsAutonomousRunning = false;
        ActiveTaskStatus = "Paused";
        var state = _autonomousRunState;
        ResultSummary = state is null
            ? "Autonomous flow paused."
            : $"Autonomous paused. Next step: {state.NextStep + 1}/{state.MaxSteps}.";
        OnPropertyChanged(nameof(IsAutonomousPaused));
        StartAutonomousCommand.NotifyCanExecuteChanged();
        ResumeAutonomousCommand.NotifyCanExecuteChanged();
    }

    private bool CanResumeAutonomous() =>
        IsPowerMode && !IsAutonomousRunning && HasResumableAutonomousRun;

    [RelayCommand(CanExecute = nameof(CanResumeAutonomous))]
    private void ResumeAutonomous()
    {
        ResumeAutonomousFlow();
    }

    private void StartAutonomousFlow(string objective, int maxSteps)
    {
        if (!IsPowerMode)
            return;

        AutonomousObjective = objective;
        AutonomousMaxSteps = maxSteps;

        _autonomousRunState = new AutonomousRunState
        {
            Objective = objective,
            SafetyLevel = SafetyLevel,
            MaxSteps = maxSteps,
            NextStep = 0
        };

        // Avoid overlapping runs: new run cancels the previous one.
        _autonomousCts?.Cancel();
        _autonomousCts = new CancellationTokenSource();
        var ct = _autonomousCts.Token;

        IsAutonomousRunning = true;
        ActiveTaskTitle = "Autonomous Agent";
        ActiveTaskStatus = "Running";
        ActiveTaskProgress = 0;
        ResultSummary = "Autonomous flow started…";
        OnPropertyChanged(nameof(IsAutonomousPaused));

        _autonomousTask = Task.Run(async () =>
        {
            try
            {
                var state = _autonomousRunState;
                var result = await _autonomousAgentService.RunAutonomousAsync(
                        objective,
                        SafetyLevel,
                        maxSteps,
                        ct,
                        state)
                    .ConfigureAwait(false);
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Done";
                    ActiveTaskProgress = 100;
                    ResultSummary = result;
                    _autonomousRunState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (OperationCanceledException)
            {
                var state = _autonomousRunState;
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Paused";
                    ActiveTaskProgress = 0;
                    ResultSummary = state is null
                        ? "Autonomous flow cancelled."
                        : $"Autonomous paused. Next step: {state.NextStep + 1}/{state.MaxSteps}.";
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Error";
                    ActiveTaskProgress = 0;
                    ResultSummary = "Autonomous error: " + ex.Message;
                    _autonomousRunState = null;
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

        var state = _autonomousRunState;
        if (state is null)
            return;

        // Align UI with the captured run settings.
        SafetyLevel = state.SafetyLevel;
        AutonomousObjective = state.Objective;
        AutonomousMaxSteps = state.MaxSteps;

        _autonomousCts?.Cancel();
        _autonomousCts = new CancellationTokenSource();
        var ct = _autonomousCts.Token;

        IsAutonomousRunning = true;
        ActiveTaskTitle = "Autonomous Agent";
        ActiveTaskStatus = "Running";
        ActiveTaskProgress = 0;
        ResultSummary = $"Autonomous resumed from step {state.NextStep + 1}/{state.MaxSteps}…";
        OnPropertyChanged(nameof(IsAutonomousPaused));

        var capturedState = state;

        _autonomousTask = Task.Run(async () =>
        {
            try
            {
                var result = await _autonomousAgentService.RunAutonomousAsync(
                        capturedState.Objective,
                        capturedState.SafetyLevel,
                        capturedState.MaxSteps,
                        ct,
                        capturedState)
                    .ConfigureAwait(false);
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Done";
                    ActiveTaskProgress = 100;
                    ResultSummary = result;
                    _autonomousRunState = null;
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (OperationCanceledException)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Paused";
                    ActiveTaskProgress = 0;
                    ResultSummary =
                        $"Autonomous paused. Next step: {capturedState.NextStep + 1}/{capturedState.MaxSteps}.";
                    OnPropertyChanged(nameof(IsAutonomousPaused));
                    StartAutonomousCommand.NotifyCanExecuteChanged();
                    ResumeAutonomousCommand.NotifyCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsAutonomousRunning = false;
                    ActiveTaskStatus = "Error";
                    ActiveTaskProgress = 0;
                    ResultSummary = "Autonomous error: " + ex.Message;
                    _autonomousRunState = null;
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
        if (IsPowerMode)
        {
            StartAutonomousFlow(objective, maxSteps: 10);
            return;
        }

        IsChatPanelExpanded = true;
        ChatPanel.ChatInput = "Fix failing tests using minimal-risk changes. Start with ide_run_affected_tests and explain each step.";
    }

    [RelayCommand]
    private void InvestigateNullref()
    {
        var objective = "Investigate possible null reference in current context. Show the shortest safe fix plan with evidence from diagnostics/tests.";
        if (IsPowerMode)
        {
            StartAutonomousFlow(objective, maxSteps: 8);
            return;
        }

        IsChatPanelExpanded = true;
        ChatPanel.ChatInput = "Investigate possible null reference in current context. Show the shortest safe fix plan.";
    }

    [RelayCommand]
    private void PrepareCommit()
    {
        var objective = "Prepare a clean commit plan grouped by logical changes and include verification steps.";
        if (IsPowerMode)
        {
            StartAutonomousFlow(objective, maxSteps: 6);
            return;
        }

        IsChatPanelExpanded = true;
        ChatPanel.ChatInput = "Prepare a clean commit plan grouped by logical changes and include verification steps.";
    }

    [RelayCommand]
    private void ExplainCurrentStep()
    {
        IsChatPanelExpanded = true;
        ChatPanel.ChatInput = "Explain the current autonomous step in plain language: intent, tool call, risk, and rollback.";
    }

    [RelayCommand]
    private void ExplainTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        IsChatPanelExpanded = true;
        ChatPanel.ChatInput =
            $"Объясни шаг трассы [{step.Kind} / {step.Status}] ({step.TimestampText}): {step.Text}. Укажи намерение, риск и откат.";
    }

    [RelayCommand]
    private void RollbackTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Запрошен откат для шага [{step.Kind}]");
        IsChatPanelExpanded = true;
        ChatPanel.ChatInput =
            $"Предложи минимальный откат для шага [{step.Kind}] ({step.TimestampText}): {step.Text}. Проверь состояние workspace.";
    }

    /// <summary>Добавить шаг в Agent Trace Timeline (Power); потокобезопасно.</summary>
    public void AppendAgentTraceStep(string kind, string text, string status, DateTimeOffset? at = null) =>
        InstrumentationPanel.AppendAgentTraceStep(kind, text, status, at);

    private void RefreshWorkspaceSnapshotCore()
    {
        try
        {
            var json = GetUiLayoutProvider?.Invoke() ?? "{}";
            if (json.Length > 4000)
                json = json[..4000] + "\n…";
            WorkspaceSnapshotJson = json;
        }
        catch (Exception ex)
        {
            WorkspaceSnapshotJson = JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [RelayCommand]
    private void RefreshWorkspaceSnapshot() => RefreshWorkspaceSnapshotCore();

    [RelayCommand]
    private void EmergencyStop()
    {
        IsBuilding = false;
        _autonomousCts?.Cancel();
        IsAutonomousRunning = false;
        ActiveTaskStatus = "Paused";
        ResultSummary = "Autonomous flow paused by operator.";
        _autonomousRunState = null;
        OnPropertyChanged(nameof(IsAutonomousPaused));
        StartAutonomousCommand.NotifyCanExecuteChanged();
        ResumeAutonomousCommand.NotifyCanExecuteChanged();
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Emergency stop engaged");
    }

    /// <summary>Focus: зафиксировать контрольную точку в таймлайне и кратком результате.</summary>
    [RelayCommand]
    private void FocusCheckpoint()
    {
        var stamp = DateTime.Now;
        InstrumentationPanel.EventTimeline.Insert(0, $"{stamp:HH:mm:ss} — Контрольная точка");
        ResultSummary = $"Контрольная точка: {stamp:yyyy-MM-dd HH:mm}";
    }

    /// <summary>Focus: запрос на откат — подсказка в чат и событие в таймлайне.</summary>
    [RelayCommand]
    private void FocusRollback()
    {
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Запрошен откат");
        IsChatPanelExpanded = true;
        ChatPanel.ChatInput = "Помоги безопасно откатить последние изменения (git или патчи). Оцени риск и предложи минимальный набор команд.";
    }

    /// <summary>Focus: подтвердить текущий шаг в гейте.</summary>
    [RelayCommand]
    private void ConfirmFocusStep()
    {
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Шаг подтверждён");
        ActiveTaskStatus = "В работе";
    }

    /// <summary>Focus: отменить предложенный шаг.</summary>
    [RelayCommand]
    private void CancelFocusStep()
    {
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Шаг отменён");
        NextActionSummary = "Ожидание следующего шага.";
        ActiveTaskStatus = "Ожидание";
    }
}
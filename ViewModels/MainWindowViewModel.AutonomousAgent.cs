using System.Text.Json;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.UiChrome;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Автономный агент (Power).</summary>
public partial class MainWindowViewModel
{
    UiModeFamily IAutonomousAgentSessionHost.UiModeFamily => UiModeFamily;

    string IAutonomousAgentSessionHost.SafetyLevel
    {
        get => SafetyLevel;
        set => SafetyLevel = value;
    }

    string IAutonomousAgentSessionHost.ResultSummary
    {
        get => ResultSummary;
        set => ResultSummary = value;
    }

    void IAutonomousAgentSessionHost.SetActiveTaskStrip(string title, string status, int progress)
    {
        ActiveTaskTitle = title;
        ActiveTaskStatus = status;
        ActiveTaskProgress = progress;
    }

    void IAutonomousAgentSessionHost.OpenChatForAgentFallback(string chatInput)
    {
        ApplyMfdRegionExpanded(true);
        ChatPanel.ChatInput = chatInput;
    }

    [RelayCommand]
    private void ExplainCurrentStep()
    {
        ApplyMfdRegionExpanded(true);
        ChatPanel.ChatInput = "Explain the current autonomous step in plain language: intent, tool call, risk, and rollback.";
    }

    [RelayCommand]
    private void ExplainTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        ApplyMfdRegionExpanded(true);
        ChatPanel.ChatInput =
            $"Объясни шаг трассы [{step.Kind} / {step.Status}] ({step.TimestampText}): {step.Text}. Укажи намерение, риск и откат.";
    }

    [RelayCommand]
    private void RollbackTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        InstrumentationPanel.EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Запрошен откат для шага [{step.Kind}]");
        ApplyMfdRegionExpanded(true);
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
        Autonomous.CancelAutonomousRunCompletely();
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
        ApplyMfdRegionExpanded(true);
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

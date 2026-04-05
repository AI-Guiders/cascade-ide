using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Features.AutonomousAgent;

/// <summary>
/// Доступ сессии автономного агента к полосе задач, чату и уровню безопасности на главном окне.
/// </summary>
public interface IAutonomousAgentSessionHost
{
    UiModeFamily UiModeFamily { get; }

    string SafetyLevel { get; set; }

    string ResultSummary { get; set; }

    void SetActiveTaskStrip(string title, string status, int progress);

    /// <summary>Режим не Power: открыть чат с подсказкой вместо автономного цикла.</summary>
    void OpenChatForAgentFallback(string chatInput);
}

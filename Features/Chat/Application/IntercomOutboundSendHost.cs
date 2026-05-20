#nullable enable

namespace CascadeIDE.Features.Chat.Application;

/// <summary>Порты UI/сессии для <see cref="IntercomOutboundSendOrchestrator"/> (ADR 0119 / 0128).</summary>
public sealed class IntercomOutboundSendHost
{
    public required Func<string> GetTrimmedInput { get; init; }

    public required Func<string?> GetWorkspaceRoot { get; init; }

    public required Func<int> GetPendingAttachCount { get; init; }

    /// <summary>Обработать строку-слэш; <c>true</c> — сценарий завершён, дальше не идти.</summary>
    public required Func<string, Task<bool>> TryHandleSlashLineAsync { get; init; }

    public required Func<string, CancellationToken, Task<(bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error)>> TryBuildOutboundAsync { get; init; }

    public required Func<Task> BeginPrepareOutboundAsync { get; init; }

    public required Func<Task> EndPrepareOutboundAsync { get; init; }

    public required Func<string, string?> ApplyProductSpine { get; init; }

    public required Func<string, IntercomAttachmentMessageBuilder.Outbound, string> FormatAgentInput { get; init; }

    public required Func<string, IntercomAttachmentMessageBuilder.Outbound, bool, Task> CommitUserMessageAsync { get; init; }

    public required Func<bool> GetChatMcpOnly { get; init; }

    public required Func<string> GetActiveAiProvider { get; init; }

    public required Func<string, Task> SendCursorAcpAsync { get; init; }

    public required Func<string, string, Task> SendStreamingAsync { get; init; }

    public required Func<string, Task> SetClarificationStatusAsync { get; init; }

    public required Func<Task> EndProviderTurnAsync { get; init; }
}

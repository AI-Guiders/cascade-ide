#nullable enable
using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>External MCP <c>send_chat</c> append (bracket prepare + UI commit).</summary>
public partial class ChatPanelViewModel
{
    /// <summary>Добавить сообщение из внешнего MCP (<c>send_chat</c> с <c>role=assistant</c>).
    /// Dual-cockpit voice: never drop the bubble if bracket prepare fails — keep prose <c>[F:…]</c> for click-time resolve.</summary>
    public async Task<string> AppendMessageFromMcpAsync(string role, string content, CancellationToken cancellationToken = default)
    {
        var r = string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
        var trimmed = content.Trim();
        if (trimmed.Length == 0)
            return "Empty content";

        var body = trimmed;
        IReadOnlyList<AttachmentAnchor> attachments = [];
        SenderWorkspaceContext? senderContext = null;
        string? statusHint = null;
        if (IntercomAttachSyntax.HasWireOrBracketSyntax(trimmed))
        {
            var (editor, workspace, solution, pending) = await UiScheduler.Default.InvokeAsync(() =>
            {
                var p = new Dictionary<string, AttachmentAnchor>(_pendingAttachDrafts, StringComparer.OrdinalIgnoreCase);
                return (
                    IntercomAttachmentResolveAtSend.EditorSnapshot.ForMcpBracketResolve(_getCurrentFilePath?.Invoke()),
                    ResolveAttachWorkspaceRoot(),
                    ResolveAttachSolutionPath(),
                    p);
            }).ConfigureAwait(false);

            var prepared = await IntercomSendTrace.RunAsync(
                workspace,
                "AppendMessageFromMcp.Prepare",
                async phase =>
                {
                    var result = await IntercomOutboundMessagePreparer.PrepareForMcpAsync(
                        trimmed,
                        pending,
                        editor,
                        workspace,
                        solution,
                        cancellationToken).ConfigureAwait(false);
                    phase.Detail(result.Status.ToString());
                    return result;
                }).ConfigureAwait(false);

            if (prepared.IsCommittable)
            {
                body = prepared.Outbound.Content;
                attachments = prepared.Outbound.Attachments;
                senderContext = prepared.Outbound.SenderWorkspaceContext;
                statusHint = IntercomPreparedMessageCommit.FormatStatusHint(prepared);
            }
            else
            {
                // Keep original prose (incl. `[F:…]`) so the bubble still lands; reveal resolves on click.
                var err = prepared.Error ?? "Не удалось собрать вложения.";
                statusHint = $"MCP: вложения deferred — {err}";
            }
        }

        // Commit только на UI-потоке; prepare — с ConfigureAwait(false), иначе дедлок с MCP на UI.
        return await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(statusHint))
                ClarificationStatusText = statusHint;

            var vm = new ChatMessageViewModel(
                r,
                body,
                threadId: _activeThreadId,
                attachments: attachments,
                senderWorkspaceContext: senderContext);
            ChatMessages.Add(vm);
            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(vm));
            if (string.Equals(r, "assistant", StringComparison.Ordinal))
                _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(vm));
            return "OK";
        }).ConfigureAwait(false);
    }
}

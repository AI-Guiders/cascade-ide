#nullable enable

using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Features.Chat.DataAcquisition;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private IntercomOutboundSendHost CreateIntercomOutboundSendHost() =>
        new()
        {
            GetTrimmedInput = () => ChatInput.Trim(),
            GetWorkspaceRoot = ResolveAttachWorkspaceRoot,
            GetPendingAttachCount = () => _pendingAttachDrafts.Count,
            TryHandleSlashLineAsync = tryHandleIntercomSlashLineAsync,
            TryBuildOutboundAsync = buildOutboundOnBackgroundAsync,
            BeginPrepareOutboundAsync = beginPrepareOutboundAsync,
            EndPrepareOutboundAsync = endPrepareOutboundAsync,
            ApplyProductSpine = ApplyProductSpineToOutboundMessage,
            FormatAgentInput = (display, outbound) => IntercomAttachmentPromptFormatter.AppendToUserMessage(
                display,
                outbound.Attachments,
                outbound.SenderWorkspaceContext),
            CommitUserMessageAsync = commitIntercomUserMessageAsync,
            GetChatMcpOnly = _getChatMcpOnly,
            GetActiveAiProvider = _getActiveAiProvider,
            SendCursorAcpAsync = SendChatWithCursorAcpAsync,
            SendStreamingAsync = SendChatWithStreamingProviderAsync,
            SetClarificationStatusAsync = text => UiScheduler.Default.InvokeAsync(() => ClarificationStatusText = text),
            EndProviderTurnAsync = endIntercomProviderTurnAsync,
        };

    private async Task<(bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error)> buildOutboundOnBackgroundAsync(
        string rawInput,
        CancellationToken cancellationToken)
    {
        var pending = new Dictionary<string, AttachmentAnchor>(_pendingAttachDrafts, StringComparer.OrdinalIgnoreCase);
        var prepared = await IntercomOutboundMessagePreparer.PrepareAsync(
            rawInput,
            pending,
            BuildAttachEditorSnapshot(),
            ResolveAttachWorkspaceRoot(),
            _getSolutionPath?.Invoke(),
            cancellationToken).ConfigureAwait(false);

        if (prepared.IsCommittable)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                _pendingAttachDrafts.Clear();
                ComposerAttachHint = "";
                var hint = IntercomPreparedMessageCommit.FormatStatusHint(prepared);
                if (!string.IsNullOrWhiteSpace(hint))
                    ClarificationStatusText = hint;
            });

            return (true, prepared.Outbound, "");
        }

        return (false, prepared.Outbound, prepared.Error ?? "Не удалось собрать сообщение.");
    }

    private Task beginPrepareOutboundAsync() =>
        UiScheduler.Default.InvokeAsync(() => ChatLoadingStatusText = "Готовлю вложения…");

    private Task endPrepareOutboundAsync() =>
        UiScheduler.Default.InvokeAsync(() =>
        {
            if (!IsChatLoading)
                ChatLoadingStatusText = "";
        });

    private async Task<bool> tryHandleIntercomSlashLineAsync(string rawInput)
    {
        var parse = ChatSlashCommandParser.TryParse(rawInput);
        if (!parse.IsSlashLine)
            return false;

        var slashPath = ChatSlashCommandPresentation.FormatDisplayPath(parse, rawInput);
        if (IsComposerAttachSlash(slashPath))
        {
            var attachOnly = await _slashCommandRunner.TryRunAsync(rawInput).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() =>
            {
                ChatInput = "";
                if (!attachOnly.Success && !string.IsNullOrWhiteSpace(attachOnly.DetailText))
                    ClarificationStatusText = attachOnly.DetailText;
            });
            return true;
        }

        var slashArgs = ChatSlashCommandPresentation.NormalizeArgsTail(parse.ArgsTail);
        var slashAudience = ChatSlashCommandCatalog.TryResolve(parse, out var slashDescriptor)
            ? slashDescriptor.MessageAudience
            : IntercomMessageAudience.Channel;

        var slash = await _slashCommandRunner.TryRunAsync(rawInput).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            var threadAtSlash = _activeThreadId;
            var cmdMsg = ChatMessageViewModel.CreateSlashCommand(slashPath, slashArgs, threadAtSlash, slashAudience);
            ChatInput = "";
            ChatMessages.Add(cmdMsg);
            if (_activeThreadId != threadAtSlash && _activeThreadId != Guid.Empty)
                cmdMsg.AssignThread(_activeThreadId);
            cmdMsg.ApplySlashCommandResult(slash);
            var payload = ChatHistoryPayloadMapping.ToMessagePayload(cmdMsg);
            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, payload);
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, payload);
            RefreshChatSurfaceSnapshot();
        });
        return true;
    }

    private async Task commitIntercomUserMessageAsync(
        string displayInput,
        IntercomAttachmentMessageBuilder.Outbound outbound,
        bool startProviderLoading)
    {
        await UiScheduler.Default.InvokeAsync(() =>
        {
            ChatInput = "";
            var parent = _pendingParentForNextMessage;
            _pendingParentForNextMessage = null;
            var userMsg = new ChatMessageViewModel(
                "user",
                displayInput,
                threadId: _activeThreadId,
                parentMessageId: parent,
                attachments: outbound.Attachments,
                senderWorkspaceContext: outbound.SenderWorkspaceContext);
            ChatMessages.Add(userMsg);
            SelectedChatThreadId = _activeThreadId;
            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(userMsg));
            RefreshChatSurfaceSnapshot();
            _ = PersistSessionSolutionPathIfChangedAsync(CancellationToken.None);
            if (startProviderLoading)
            {
                IsChatLoading = true;
                ChatLoadingStatusText = "Модель отвечает…";
            }
        });
    }

    private Task endIntercomProviderTurnAsync() =>
        UiScheduler.Default.InvokeAsync(() =>
        {
            StopAcpWaitWatchdog();
            IsChatLoading = false;
            ChatLoadingStatusText = "";
        });
}

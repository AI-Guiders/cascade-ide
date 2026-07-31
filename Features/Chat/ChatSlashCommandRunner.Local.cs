#nullable enable

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Features.Chat;

public sealed partial class ChatSlashCommandRunner
{
    private ChatSlashCommandRunResult RunLocalHelp(string displayPath, string? argsTail)
    {
        var helpText = string.IsNullOrWhiteSpace(argsTail)
            ? IntercomHelpGuide.FormatFull()
            : string.Join(Environment.NewLine, ChatSlashCommandCatalog.ListHelpLines(argsTail));
        return new ChatSlashCommandRunResult(true, true, displayPath, argsTail, helpText);
    }

    private ChatSlashCommandRunResult RunLocalReport(
        ChatSlashCommandDescriptor descriptor,
        string displayPath,
        string? argsTail)
    {
        var snapshot = _getChatSurfaceSnapshot?.Invoke() ?? ChatSurfaceSnapshot.Empty;
        var report = ChatSlashSessionReports.TryFormat(descriptor.SlashPath, snapshot)
            ?? "Отчёт недоступен.";
        return new ChatSlashCommandRunResult(true, true, displayPath, argsTail, report);
    }

    private ChatSlashCommandRunResult RunLocalIntercom(
        ChatSlashCommandDescriptor descriptor,
        string displayPath,
        string? argsTail)
    {
        if (_selectChatThread is null || _setChatOverviewMode is null)
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Intercom navigation недоступна.");
        }

        var snapshot = _getChatSurfaceSnapshot?.Invoke() ?? ChatSurfaceSnapshot.Empty;
        var selectedId = _getSelectedChatThreadId?.Invoke() ?? Guid.Empty;
        if (!ChatSlashIntercomActions.TryExecute(
                descriptor.SlashPath,
                argsTail,
                selectedId,
                _selectChatThread,
                _setChatOverviewMode,
                snapshot,
                out var intercom,
                _setTopicPicker,
                _createTopicWithTitle,
                _renameTopicWithTitle,
                _tryAttachSlash,
                _selectMessageByOrdinalRangeInDetailLane,
                _selectMessagesByOrdinalRangesInDetailLane,
                _clearMessageSelectionInDetailLane,
                _findMessagesForCodeRef,
                _relateMessageRangeToCodeRef,
                _listMessageAnchors,
                _peekAnchorById,
                _runIntercomAdmin))
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Действие недоступно.");
        }

        return new ChatSlashCommandRunResult(
            true,
            intercom.Success,
            displayPath,
            argsTail,
            intercom.Message);
    }

    private async Task<ChatSlashCommandRunResult> RunLocalAgentAsync(
        ChatSlashCommandDescriptor descriptor,
        string displayPath,
        string? argsTail,
        CancellationToken cancellationToken)
    {
        if (_agentEnvironment is null || _getSolutionPathForAgent is null)
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Agent Execution Environment недоступен.");
        }

        var agent = await Task.Run(
            () =>
            {
                ChatSlashAgentActions.TryExecute(
                    descriptor.SlashPath,
                    argsTail,
                    _agentEnvironment,
                    _getSolutionPathForAgent,
                    out var result);
                return result;
            },
            cancellationToken).ConfigureAwait(false);

        return new ChatSlashCommandRunResult(
            true,
            agent.Success,
            displayPath,
            argsTail,
            agent.Message);
    }
}

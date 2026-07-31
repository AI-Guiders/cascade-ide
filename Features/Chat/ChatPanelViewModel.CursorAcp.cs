#nullable enable
using AgentClientProtocol;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services.CursorAcp;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Cursor ACP dispose, model-pick reaction, and PromptAsync send path.</summary>
public partial class ChatPanelViewModel
{
    /// <summary>Сброс stdio-сессии Cursor ACP (смена провайдера, пути к агенту или корня workspace).</summary>
    public void DisposeCursorAcpSession()
    {
        _cursorAcp?.Dispose();
        _cursorAcp = null;
        void clearPicks()
        {
            _suppressCursorAcpModelPickChanged = true;
            try
            {
                CursorAcpModelPicks.Clear();
                SelectedCursorAcpModelPick = null;
            }
            finally
            {
                _suppressCursorAcpModelPickChanged = false;
            }
        }

        if (UiScheduler.Default.CheckAccess())
            clearPicks();
        else
            UiScheduler.Default.Post(clearPicks);
    }

    partial void OnSelectedCursorAcpModelPickChanged(CursorAcpModelPick? value)
    {
        if (_suppressCursorAcpModelPickChanged || value is null || _cursorAcp is null)
            return;
        _ = ApplyUserSelectedCursorAcpModelAsync(value);
    }

    private async Task SendChatWithCursorAcpAsync(string input)
    {
        var assistantMsg = new ChatMessageViewModel("assistant", "", threadId: _activeThreadId);
        ChatMessages.Add(assistantMsg);
        ChatMessageViewModel? thoughtMsg = null;
        ChatMessageViewModel? toolMsg = null;
        try
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                SetChatLoadingStage("Подключение к Cursor ACP…");
                MarkAcpActivity();
                RestartAcpWaitWatchdog();
            });
            _cursorAcp ??= new CursorAcpChatConnection();
            _cursorAcp.SetIdeTerminalCallbacks(
                text =>
                {
                    _appendAcpTerminal?.Invoke(text);
                    UiScheduler.Default.Post(() =>
                    {
                        SetChatLoadingStage("Выполняю инструмент…");
                        MarkAcpActivity();
                    });
                },
                _showAcpTerminal);
            var workspace = _getWorkspaceRoot().Trim();
            if (string.IsNullOrEmpty(workspace))
                workspace = Environment.CurrentDirectory;
            await _cursorAcp.PromptAsync(
                workspace,
                _getCursorAcpAgentPath(),
                _getExternalMcpServersJson(),
                _getAcpAutoInjectIdeMcp(),
                _getCursorAcpPreferredModelId(),
                input,
                appendMessageChunk: t => UiScheduler.Default.Post(() =>
                {
                    assistantMsg.Content += t;
                    SetChatLoadingStage("Формирую ответ…");
                    MarkAcpActivity();
                }),
                appendThoughtChunk: t => UiScheduler.Default.Post(() =>
                {
                    thoughtMsg ??= CreateThoughtMessage();
                    thoughtMsg.Content += t;
                    SetChatLoadingStage("Модель думает…");
                    MarkAcpActivity();
                }),
                onStage: stage => UiScheduler.Default.Post(() =>
                {
                    if (stage == CursorAcpStreamStage.ToolCall)
                        toolMsg ??= CreateToolMessage();
                    SetChatLoadingStage(stage switch
                    {
                        CursorAcpStreamStage.ThoughtChunk => "Модель думает…",
                        CursorAcpStreamStage.ToolCall => "Выполняю инструмент…",
                        _ => "Формирую ответ…"
                    });
                    MarkAcpActivity();
                }),
                onSessionModels: state => UiScheduler.Default.Post(() => ApplyCursorAcpSessionModels(state)),
                BeginAgentTurnCancellation()).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() =>
            {
                FinalizeThinkingMessage(thoughtMsg);
                FinalizeToolMessage(toolMsg, isError: false);
            });
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                var mapped = MapCursorAcpError(ex);
                assistantMsg.Content = mapped.UserMessage;
                FinalizeThinkingMessage(thoughtMsg);
                FinalizeToolMessage(toolMsg, isError: true);
                SetChatLoadingStage(mapped.StageText);
            });
        }
    }
}

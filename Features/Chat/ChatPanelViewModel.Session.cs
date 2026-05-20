#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private async Task InitializeSessionAsync()
    {
        try
        {
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            if (meta.MainThreadId == Guid.Empty)
            {
                meta = meta with { MainThreadId = Guid.NewGuid() };
                await _sessionStore.SaveMetadataAsync(meta, CancellationToken.None).ConfigureAwait(false);
            }

            _mainThreadId = meta.MainThreadId;
            _activeThreadId = meta.MainThreadId;
            ThreadBranchHint = $"Ветка {_activeThreadId:N}";

            var events = await _sessionStore.ReadEventsAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var rows = ChatHistoryMessageProjector.Project(events, _mainThreadId);
            var forks = ChatThreadForkProjector.Project(events);
            UiScheduler.Default.Post(() =>
            {
                ApplyProductSpineFromMetadata(meta);
                ApplyThreadTitlesFromMetadata(meta);
                _threadForks.Clear();
                _threadForks.AddRange(forks);
                foreach (var row in rows)
                    ChatMessages.Add(new ChatMessageViewModel(
                        row.Role,
                        row.Content,
                        row.MessageId,
                        row.ThreadId,
                        row.ParentMessageId,
                        slashCommandPath: row.SlashCommandPath,
                        slashCommandArgs: row.SlashCommandArgs,
                        slashCommandStatus: row.SlashCommandStatus,
                        attachments: row.Attachments,
                        senderWorkspaceContext: row.SenderWorkspaceContext,
                        audience: row.Audience));
                if (rows.Count > 0)
                    ClarificationStatusText = $"Восстановлено сообщений: {rows.Count}";
                RefreshChatSurfaceSnapshot();
            });
        }
        catch
        {
            // v1 persistence best-effort: не роняем чат при ошибке диска/JSON.
        }
    }

    private async Task PersistEventAsync<T>(string kind, T payload, Guid? envelopeThreadId = null)
    {
        try
        {
            var tid = envelopeThreadId ?? _activeThreadId;
            if (tid == Guid.Empty)
                tid = _mainThreadId;
            var ev = new ChatHistoryEvent(
                Guid.NewGuid(),
                _sessionId,
                DateTimeOffset.UtcNow,
                kind,
                ChatHistoryJson.Serialize(payload),
                ThreadId: tid == Guid.Empty ? null : tid.ToString("N"));
            await _sessionStore.AppendEventAsync(ev, CancellationToken.None).ConfigureAwait(false);
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            if (meta.UpdatedAtUtc < ev.AtUtc)
                await _sessionStore.SaveMetadataAsync(meta with { UpdatedAtUtc = ev.AtUtc }, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // intentionally ignored (best-effort persistence).
        }
    }
}

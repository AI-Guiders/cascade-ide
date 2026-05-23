#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private readonly SemaphoreSlim _intercomSessionReloadGate = new(1, 1);

    /// <summary>Перечитать workspace, session id и историю с диска (после загрузки решения или смены корня).</summary>
    public Task ReloadIntercomSessionFromDiskAsync() => ReloadIntercomSessionCoreAsync();

    private Task ReloadIntercomSessionCoreAsync() =>
        RunIntercomSessionReloadAsync(InitializeSessionCoreAsync);

    private async Task RunIntercomSessionReloadAsync(Func<CancellationToken, Task> reloadBody)
    {
        await _intercomSessionReloadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await reloadBody(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _intercomSessionReloadGate.Release();
        }
    }

    private async Task InitializeSessionCoreAsync(CancellationToken ct)
    {
        try
        {
            var workspaceRoot = ResolveAttachWorkspaceRoot();
            _sessionStore.SetWorkspaceRoot(string.IsNullOrWhiteSpace(workspaceRoot) ? null : workspaceRoot);
            _sessionId = await _sessionStore.ResolveOrCreateCurrentSessionIdAsync(ct).ConfigureAwait(false);
            await _sessionStore.BindCurrentSessionAsync(_sessionId, ct).ConfigureAwait(false);

            var events = await _sessionStore.ReadEventsAsync(_sessionId, ct).ConfigureAwait(false);
            rebuildExplicitRelatesFromEvents(events);

            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, ct).ConfigureAwait(false);
            var inferredMain = ChatHistoryMessageProjector.InferMainThreadId(events);
            if (meta.MainThreadId == Guid.Empty)
            {
                meta = meta with { MainThreadId = inferredMain };
                await _sessionStore.SaveMetadataAsync(meta, ct).ConfigureAwait(false);
            }
            else if (events.Count > 0)
            {
                var rowsProbe = ChatHistoryMessageProjector.Project(events, meta.MainThreadId);
                if (rowsProbe.Count > 0 && rowsProbe.All(row => row.ThreadId != meta.MainThreadId))
                {
                    meta = meta with { MainThreadId = inferredMain };
                    await _sessionStore.SaveMetadataAsync(meta, ct).ConfigureAwait(false);
                }
            }

            _mainThreadId = meta.MainThreadId;
            _activeThreadId = meta.MainThreadId;
            _sessionSolutionPathRelative = meta.SolutionPath;
            ThreadBranchHint = $"Ветка {_activeThreadId:N}";

            await PersistSessionSolutionPathIfChangedAsync(ct).ConfigureAwait(false);

            var rows = ChatHistoryMessageProjector.Project(events, _mainThreadId);
            var forks = ChatThreadForkProjector.Project(events);
            await UiScheduler.Default.InvokeAsync(() =>
            {
                ChatMessages.Clear();
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
                ClarificationStatusText = rows.Count > 0
                    ? $"Восстановлено сообщений: {rows.Count}"
                    : "История Intercom пуста (новая сессия или нет событий на диске).";
                RefreshAttachmentAnchorsForCurrentScope();
                RefreshChatSurfaceSnapshot();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
                ClarificationStatusText = "Не удалось загрузить историю Intercom: " + ex.Message).ConfigureAwait(false);
        }
    }

    private async Task PersistEventAsync<T>(string kind, T payload, Guid? envelopeThreadId = null)
    {
        try
        {
            var workspaceRoot = ResolveAttachWorkspaceRoot();
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
                _sessionStore.SetWorkspaceRoot(workspaceRoot);

            if (_sessionId == Guid.Empty)
                _sessionId = await _sessionStore.ResolveOrCreateCurrentSessionIdAsync(CancellationToken.None).ConfigureAwait(false);

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
            await _sessionStore.BindCurrentSessionAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            if (meta.UpdatedAtUtc < ev.AtUtc)
                await _sessionStore.SaveMetadataAsync(meta with { UpdatedAtUtc = ev.AtUtc }, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // intentionally ignored (best-effort persistence).
        }
    }

    private async Task PersistSessionSolutionPathIfChangedAsync(CancellationToken ct)
    {
        try
        {
            var workspace = ResolveAttachWorkspaceRoot();
            if (string.IsNullOrWhiteSpace(workspace))
                return;

            var rel = IntercomAttachScope.ToSessionRelativeSolutionPath(_getSolutionPath?.Invoke(), workspace)
                ?? IntercomAttachScope.TryDiscoverSolutionPathRelative(workspace)
                ?? _sessionSolutionPathRelative;

            if (string.IsNullOrWhiteSpace(rel))
                return;

            _sessionSolutionPathRelative = rel;
            if (_sessionId == Guid.Empty)
                return;

            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, ct).ConfigureAwait(false);
            if (string.Equals(meta.SolutionPath, rel, StringComparison.OrdinalIgnoreCase))
                return;

            var schema = meta.SchemaVersion < 2 ? 2 : meta.SchemaVersion;
            await _sessionStore.SaveMetadataAsync(meta with { SolutionPath = rel, SchemaVersion = schema }, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            // best-effort scope metadata
        }
    }
}

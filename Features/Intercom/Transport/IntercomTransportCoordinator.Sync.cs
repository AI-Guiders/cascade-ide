#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

public sealed partial class IntercomTransportCoordinator
{
    private async Task FlushOutboxAsync(IntercomTransportSettings settings, string teamId, CancellationToken ct)
    {
        var pending = await _outbox.ReadAllAsync(ct).ConfigureAwait(false);
        if (pending.Count == 0)
            return;

        var remaining = new List<IntercomOutboundQueueEntry>();
        foreach (var entry in pending)
        {
            ct.ThrowIfCancellationRequested();
            var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(bearer))
            {
                remaining.Add(entry);
                continue;
            }

            var topicId = entry.TopicId;
            if (string.Equals(topicId, "pending", StringComparison.Ordinal))
            {
                topicId = await ResolveTopicIdForThreadWithBearerAsync(settings, teamId, "general", bearer, ct)
                    .ConfigureAwait(false) ?? "";
            }

            if (string.IsNullOrWhiteSpace(topicId))
            {
                remaining.Add(entry with { Attempts = entry.Attempts + 1 });
                continue;
            }

            try
            {
                var envelope = await _api.AppendEventAsync(topicId, entry.Request, bearer, ct).ConfigureAwait(false);
                if (envelope is not null)
                    _state.UpdateLastSeq(envelope.Seq);
                else
                    remaining.Add(entry with { Attempts = entry.Attempts + 1 });
            }
            catch
            {
                remaining.Add(entry with { Attempts = entry.Attempts + 1 });
            }
        }

        await _outbox.ReplaceAllAsync(remaining, ct).ConfigureAwait(false);
        if (remaining.Count == 0)
            _deliveryStatus = "Intercom: очередь доставлена.";
    }

    private async Task CatchUpAsync(IntercomTransportSettings settings, string teamId, CancellationToken ct)
    {
        var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(bearer))
            return;

        IReadOnlyList<IntercomTransportEventEnvelopeDto> batch;
        try
        {
            batch = await _api.ListTeamEventsAsync(teamId, _state.LastSeq, bearer, ct).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        if (batch.Count == 0)
            return;

        var ingested = await IngestBatchAsync(batch, ct).ConfigureAwait(false);
        if (ingested && _onRemoteEventsIngested is not null)
            await _onRemoteEventsIngested().ConfigureAwait(false);
    }

    private async Task<bool> IngestBatchAsync(
        IReadOnlyList<IntercomTransportEventEnvelopeDto> batch,
        CancellationToken ct)
    {
        var store = _getSessionStore?.Invoke();
        var sessionId = _getSessionId?.Invoke() ?? Guid.Empty;
        if (store is null || sessionId == Guid.Empty)
            return false;

        var existing = await store.ReadEventsAsync(sessionId, ct).ConfigureAwait(false);
        var knownIds = new HashSet<Guid>(existing.Select(e => e.EventId));

        var appended = false;
        foreach (var envelope in batch.OrderBy(e => e.Seq))
        {
            if (!Guid.TryParse(envelope.ClientEventId, out var eventId) || knownIds.Contains(eventId))
            {
                _state.UpdateLastSeq(envelope.Seq);
                continue;
            }

            if (!IntercomTransportIngest.TryMapToLocalEvent(envelope, sessionId, out var local) || local is null)
            {
                _state.UpdateLastSeq(envelope.Seq);
                continue;
            }

            if (ShouldSkipInboundDuplicate(local, existing))
            {
                _state.UpdateLastSeq(envelope.Seq);
                continue;
            }

            await store.AppendEventAsync(local, ct).ConfigureAwait(false);
            knownIds.Add(local.EventId);
            existing = await store.ReadEventsAsync(sessionId, ct).ConfigureAwait(false);
            _state.UpdateLastSeq(envelope.Seq);
            appended = true;
        }

        return appended;
    }

    private async Task<string?> ResolveTopicIdForThreadAsync(
        IntercomTransportSettings settings,
        string teamId,
        string threadId,
        CancellationToken ct)
    {
        var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(bearer))
            return null;
        return await ResolveTopicIdForThreadWithBearerAsync(settings, teamId, threadId, bearer, ct).ConfigureAwait(false);
    }

    private async Task<string?> ResolveTopicIdForThreadWithBearerAsync(
        IntercomTransportSettings settings,
        string teamId,
        string threadId,
        string bearer,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.DefaultTopicId))
            return settings.DefaultTopicId.Trim();

        var spineKey = string.IsNullOrWhiteSpace(threadId) ? "general" : threadId;
        if (_state.TryGetTopicForThread(spineKey, out var cached))
            return cached;

        try
        {
            var topic = await _api.EnsureTopicAsync(
                teamId,
                spineKey,
                IntercomTransportPublishRules.TopicTitleForThread(spineKey),
                bearer,
                ct).ConfigureAwait(false);
            if (topic is null)
                return null;
            _state.SetTopicForThread(spineKey, topic.TopicId);
            return topic.TopicId;
        }
        catch
        {
            return null;
        }
    }

    private void StartSse(IntercomTransportSettings settings, string teamId)
    {
        lock (_gate)
        {
            StopSseCore();
            _sseCts = new CancellationTokenSource();
            var ct = _sseCts.Token;
            _sseTask = Task.Run(() => SseLoopAsync(settings, teamId, ct), ct);
        }
    }

    private void StopSse()
    {
        lock (_gate)
            StopSseCore();
    }

    private void StopSseCore()
    {
        try
        {
            _sseCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        _sseCts = null;
        _sseTask = null;
    }

    private async Task SseLoopAsync(IntercomTransportSettings settings, string teamId, CancellationToken ct)
    {
        var backoff = Math.Max(500, settings.SseReconnectBackoffMs);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!await EnsureBearerAsync(settings, ct).ConfigureAwait(false))
                {
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
                    continue;
                }

                var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(bearer))
                {
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
                    continue;
                }

                using var req = _api.CreateSseRequest(teamId, topicId: null, bearer);
                using var res = await _api.SendSseAsync(req, ct).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                {
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
                    continue;
                }

                await foreach (var envelope in IntercomSseParser.ReadEnvelopesAsync(res, ct).ConfigureAwait(false))
                {
                    var ingested = await IngestBatchAsync([envelope], ct).ConfigureAwait(false);
                    if (ingested && _onRemoteEventsIngested is not null)
                        await _onRemoteEventsIngested().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // reconnect
            }

            try
            {
                await Task.Delay(backoff, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<bool> EnsureBearerAsync(IntercomTransportSettings settings, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.DevTeamToken))
            return true;

        var secrets = IntercomTransportSecretsStorage.Load();
        if (secrets.HasAccessToken)
        {
            var exp = secrets.TryGetAccessExpiresAtUtc();
            if (exp is null || exp > DateTimeOffset.UtcNow.AddMinutes(1))
                return true;
        }

        if (!secrets.HasRefreshToken)
            return false;

        var refreshed = await _api.RefreshTokenAsync(secrets.RefreshToken, ct).ConfigureAwait(false);
        if (refreshed is null)
            return false;

        IntercomTransportApiClient.ApplyTokenResponse(secrets, refreshed);
        return true;
    }

    private async Task<string?> ResolveBearerAsync(IntercomTransportSettings settings, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.DevTeamToken))
            return settings.DevTeamToken.Trim();

        await EnsureBearerAsync(settings, ct).ConfigureAwait(false);
        var secrets = IntercomTransportSecretsStorage.Load();
        return secrets.HasAccessToken ? secrets.AccessToken : null;
    }

    private bool TryGetEffectiveConfig(
        out IntercomTransportSettings settings,
        out string teamId,
        out string error)
    {
        settings = new IntercomTransportSettings();
        teamId = "";
        error = "";

        var s = _getSettings?.Invoke();
        if (s is null)
        {
            error = "Настройки недоступны.";
            return false;
        }

        settings = s.Intercom.Transport;
        if (!settings.IsConfigured)
        {
            error = "Transport выключен или не задан base_url.";
            return false;
        }

        teamId = ResolveTeamIdSync(settings, _getWorkspaceRoot?.Invoke());
        if (string.IsNullOrWhiteSpace(teamId))
        {
            error = "team_id не задан (settings, workspace hint или Connect).";
            return false;
        }

        return true;
    }

    private async Task<string?> TryResolveTeamIdAsync(
        IntercomTransportSettings settings,
        CancellationToken ct)
    {
        var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
        var resolved = await IntercomWorkspaceContextResolver.ResolveAsync(
            settings,
            _getWorkspaceRoot?.Invoke(),
            bearer,
            _api,
            ct).ConfigureAwait(false);

        if (resolved.Found && !string.IsNullOrWhiteSpace(resolved.TeamId))
            return resolved.TeamId;

        return ResolveTeamIdSync(settings, _getWorkspaceRoot?.Invoke());
    }

    private static string ResolveTeamIdSync(IntercomTransportSettings settings, string? workspaceRoot)
    {
        if (!string.IsNullOrWhiteSpace(settings.TeamId))
            return settings.TeamId.Trim();

        var repoKey = IntercomWorkspaceGitRemoteResolver.TryGetNormalizedOrigin(workspaceRoot);
        if (!string.IsNullOrWhiteSpace(repoKey)
            && settings.WorkspaceHints.TryGetValue(repoKey, out var hint)
            && !string.IsNullOrWhiteSpace(hint.TeamId))
            return hint.TeamId.Trim();

        var manifest = IntercomTeamManifestResolver.TryResolve(workspaceRoot);
        return manifest?.TeamId.Trim() ?? "";
    }

    private bool TryBuildAppendPayload(ChatHistoryEvent ev, out IntercomAppendEventRequestDto request)
    {
        request = default!;
        JsonElement payloadElement;
        try
        {
            payloadElement = JsonSerializer.Deserialize<JsonElement>(ev.PayloadJson, IntercomTransportJson.Web);
        }
        catch (JsonException)
        {
            return false;
        }

        var senderRole = IntercomTransportPublishRules.ResolveWireSenderRole(ev.PayloadJson, ev.Kind);
        var transport = _getSettings?.Invoke().Intercom.Transport;
        string memberId;
        string displayName;

        if (string.Equals(senderRole, "agent", StringComparison.Ordinal))
        {
            memberId = transport?.SelectedAgentMemberId.Trim() ?? "";
            displayName = transport?.SelectedAgentDisplayName.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(memberId))
                return false;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = memberId;

            payloadElement = IntercomTransportPayloadEnricher.EnrichForWire(
                ev.Kind,
                payloadElement,
                _operatorMemberId);
        }
        else
        {
            memberId = _operatorMemberId;
            displayName = _operatorDisplayName;
            payloadElement = IntercomTransportPayloadEnricher.EnrichForWire(ev.Kind, payloadElement);
        }

        request = new IntercomAppendEventRequestDto(
            SchemaVersion: 1,
            ClientEventId: ev.EventId.ToString("N"),
            OccurredAtUtc: ev.AtUtc.ToString("O"),
            EventKind: IntercomTransportPublishRules.ToWireEventKind(ev.Kind),
            Sender: new IntercomSenderWireDto(memberId, displayName, senderRole, "cide"),
            Payload: payloadElement);
        return true;
    }

    private static bool ShouldSkipInboundDuplicate(
        ChatHistoryEvent local,
        IReadOnlyList<ChatHistoryEvent> existing)
    {
        if (!string.Equals(local.Kind, ChatHistoryEventKind.MessageCompleted, StringComparison.Ordinal))
            return false;

        ChatHistoryMessagePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ChatHistoryMessagePayload>(local.PayloadJson, IntercomTransportJson.Web);
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload is null || !Guid.TryParse(payload.MessageId, out var messageId))
            return false;

        foreach (var ev in existing)
        {
            if (!string.Equals(ev.Kind, ChatHistoryEventKind.MessageAdded, StringComparison.Ordinal)
                && !string.Equals(ev.Kind, ChatHistoryEventKind.MessageCompleted, StringComparison.Ordinal))
                continue;

            try
            {
                var p = JsonSerializer.Deserialize<ChatHistoryMessagePayload>(ev.PayloadJson, IntercomTransportJson.Web);
                if (p is not null && Guid.TryParse(p.MessageId, out var mid) && mid == messageId)
                    return true;
            }
            catch (JsonException)
            {
                // skip
            }
        }

        return false;
    }
}

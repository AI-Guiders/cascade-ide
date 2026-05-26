#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>FederatedSync: publish, SSE, OAuth, offline outbox (ADR 0144).</summary>
public sealed class IntercomTransportCoordinator : IDisposable
{
    private readonly IntercomTransportApiClient _api = new();
    private readonly IntercomOAuthConnectService _oauth;
    private readonly IntercomTransportStateStore _state = new();
    private readonly IntercomTransportOutboundQueue _outbox = new();
    private readonly object _gate = new();

    private Func<CascadeIdeSettings>? _getSettings;
    private Func<string>? _getWorkspaceRoot;
    private Func<Guid>? _getSessionId;
    private Func<ChatSessionStore>? _getSessionStore;
    private Func<Task>? _onRemoteEventsIngested;

    private CancellationTokenSource? _sseCts;
    private Task? _sseTask;
    private string _deliveryStatus = "";
    private string _connectionStatus = "";
    private string _operatorMemberId = "";
    private string _operatorDisplayName = "";

    public IntercomTransportCoordinator() => _oauth = new IntercomOAuthConnectService(_api);

    public IntercomTransportApiClient ApiClient => _api;

    public string? GetWorkspaceRootForAdmin() => _getWorkspaceRoot?.Invoke();

    public Task<bool> EnsureBearerForAdminAsync(IntercomTransportSettings settings, CancellationToken ct) =>
        EnsureBearerAsync(settings, ct);

    public Task<string?> ResolveBearerForAdminAsync(IntercomTransportSettings settings, CancellationToken ct) =>
        ResolveBearerAsync(settings, ct);

    public string DeliveryStatus => _deliveryStatus;

    public string ConnectionStatus => _connectionStatus;

    public bool IsConnected
    {
        get
        {
            var secrets = IntercomTransportSecretsStorage.Load();
            var settings = _getSettings?.Invoke().Intercom.Transport;
            if (settings is not null && !string.IsNullOrWhiteSpace(settings.DevTeamToken))
                return true;
            return secrets.HasAccessToken || secrets.HasRefreshToken;
        }
    }

    public void Configure(
        Func<CascadeIdeSettings> getSettings,
        Func<string> getWorkspaceRoot,
        Func<Guid> getSessionId,
        Func<ChatSessionStore> getSessionStore,
        Func<Task> onRemoteEventsIngested)
    {
        _getSettings = getSettings;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getSessionId = getSessionId;
        _getSessionStore = getSessionStore;
        _onRemoteEventsIngested = onRemoteEventsIngested;
    }

    public void OnWorkspaceChanged()
    {
        var root = _getWorkspaceRoot?.Invoke();
        _state.SetWorkspaceRoot(string.IsNullOrWhiteSpace(root) ? null : root);
        _outbox.SetWorkspaceRoot(string.IsNullOrWhiteSpace(root) ? null : root);
        _state.Load();
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!TryGetEffectiveConfig(out var settings, out var teamId, out _))
        {
            StopSse();
            _connectionStatus = "";
            return;
        }

        _api.ConfigureBaseUrl(settings.ResolveBaseUrl());
        OnWorkspaceChanged();

        if (!await EnsureBearerAsync(settings, ct).ConfigureAwait(false))
        {
            _connectionStatus = "Intercom: нужен Connect (OAuth) или dev_team_token.";
            StopSse();
            return;
        }

        var resolvedTeam = await TryResolveTeamIdAsync(settings, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(resolvedTeam))
            teamId = resolvedTeam;

        var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            var me = await _api.GetMeAsync(bearer, ct).ConfigureAwait(false);
            if (me is not null)
            {
                _operatorMemberId = me.MemberId;
                _operatorDisplayName = me.DisplayName;
                IntercomWorkspaceContextResolver.InvalidateStaleHints(settings, me);
            }
        }

        if (string.IsNullOrWhiteSpace(teamId))
        {
            _connectionStatus = "Intercom: team_id не определён.";
            StopSse();
            return;
        }

        _connectionStatus = "Intercom: подключено";
        await FlushOutboxAsync(settings, teamId, ct).ConfigureAwait(false);
        await CatchUpAsync(settings, teamId, ct).ConfigureAwait(false);
        StartSse(settings, teamId);
    }

    public void Stop() => StopSse();

    public async Task<(bool Ok, string Message)> ConnectAsync(CancellationToken ct = default)
    {
        if (!TryGetEffectiveConfig(out var settings, out var teamId, out var error))
            return (false, error);

        if (!string.IsNullOrWhiteSpace(settings.DevTeamToken))
        {
            _connectionStatus = "Intercom: dev token";
            await StartAsync(ct).ConfigureAwait(false);
            return (true, "DEV bearer активен.");
        }

        var provider = string.IsNullOrWhiteSpace(settings.OAuthProvider) ? "github" : settings.OAuthProvider.Trim();
        var (ok, oauthError) = await _oauth.ConnectAsync(
            settings.ResolveBaseUrl(),
            teamId,
            provider,
            string.IsNullOrWhiteSpace(settings.InviteToken) ? null : settings.InviteToken.Trim(),
            ct).ConfigureAwait(false);
        if (!ok)
            return (false, oauthError);

        await StartAsync(ct).ConfigureAwait(false);
        return (true, "Intercom подключён.");
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        StopSse();
        var secrets = IntercomTransportSecretsStorage.Load();
        if (secrets.HasRefreshToken && secrets.HasAccessToken)
        {
            try
            {
                await _api.LogoutAsync(secrets.RefreshToken, secrets.AccessToken, ct).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        secrets.ClearTokens();
        IntercomTransportSecretsStorage.Save(secrets);
        _connectionStatus = "Intercom: отключено";
        _deliveryStatus = "";
    }

    public void PublishLocalEventFireAndForget(ChatHistoryEvent ev) => _ = PublishLocalEventAsync(ev);

    public async Task PublishLocalEventAsync(ChatHistoryEvent ev, CancellationToken ct = default)
    {
        var settings = _getSettings?.Invoke().Intercom.Transport;
        if (settings is null)
            return;

        if (!IntercomTransportPublishRules.ShouldPublish(
                ev.Kind,
                ev.PayloadJson,
                settings.SyncAgentChannelMessages))
            return;

        if (!TryGetEffectiveConfig(out settings, out var teamId, out _))
            return;

        if (!await EnsureBearerAsync(settings, ct).ConfigureAwait(false))
        {
            await EnqueueForLaterAsync(ev, settings, teamId, ct).ConfigureAwait(false);
            _deliveryStatus = "Intercom: в очереди (нет авторизации).";
            return;
        }

        var sent = await TrySendEventAsync(ev, settings, teamId, ct).ConfigureAwait(false);
        if (!sent)
            await EnqueueForLaterAsync(ev, settings, teamId, ct).ConfigureAwait(false);
    }

    public async Task TryAutoConnectOnSendAsync(CancellationToken ct = default)
    {
        var settings = _getSettings?.Invoke().Intercom.Transport;
        if (settings is null || !settings.Enabled || !settings.AutoConnectOnSend || IsConnected)
            return;

        if (!TryGetEffectiveConfig(out _, out _, out _))
            return;

        _ = await ConnectAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> TrySendEventAsync(
        ChatHistoryEvent ev,
        IntercomTransportSettings settings,
        string teamId,
        CancellationToken ct)
    {
        var bearer = await ResolveBearerAsync(settings, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(bearer))
            return false;

        var threadId = IntercomTransportPublishRules.TryExtractThreadId(ev.Kind, ev.PayloadJson) ?? "general";
        var topicId = await ResolveTopicIdForThreadWithBearerAsync(settings, teamId, threadId, bearer, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(topicId))
        {
            _deliveryStatus = "Intercom: topic не найден.";
            return false;
        }

        if (!TryBuildAppendPayload(ev, out var request))
            return false;

        try
        {
            var envelope = await _api.AppendEventAsync(topicId, request, bearer, ct).ConfigureAwait(false);
            if (envelope is null)
            {
                _deliveryStatus = "Intercom: ошибка доставки.";
                return false;
            }

            _deliveryStatus = $"Intercom: доставлено (seq {envelope.Seq}).";
            _state.UpdateLastSeq(envelope.Seq);
            return true;
        }
        catch (Exception ex)
        {
            _deliveryStatus = "Intercom: " + ex.Message;
            return false;
        }
    }

    private async Task EnqueueForLaterAsync(
        ChatHistoryEvent ev,
        IntercomTransportSettings settings,
        string teamId,
        CancellationToken ct)
    {
        var threadId = IntercomTransportPublishRules.TryExtractThreadId(ev.Kind, ev.PayloadJson) ?? "general";
        var topicId = await ResolveTopicIdForThreadAsync(settings, teamId, threadId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(topicId))
            topicId = "pending";

        if (!TryBuildAppendPayload(ev, out var request))
            return;

        await _outbox.EnqueueAsync(new IntercomOutboundQueueEntry(topicId, request), ct).ConfigureAwait(false);
    }

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

    public void Dispose()
    {
        StopSse();
        _api.Dispose();
    }
}

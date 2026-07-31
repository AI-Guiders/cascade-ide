#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>FederatedSync: publish, SSE, OAuth, offline outbox (ADR 0144).</summary>
public sealed partial class IntercomTransportCoordinator : IDisposable
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


    public void Dispose()
    {
        StopSse();
        _api.Dispose();
    }
}

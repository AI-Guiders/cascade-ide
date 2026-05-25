using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Admin;

/// <summary>Team/server admin из CIDE (ADR 0147 фазы 1–4).</summary>
public sealed class IntercomAdminService
{
    private readonly IntercomTransportCoordinator _transport;
    private readonly IntercomServerHostService _serverHost = new();
    private readonly Func<CascadeIdeSettings> _getSettings;
    private readonly Action _saveSettings;

    public IntercomAdminService(
        IntercomTransportCoordinator transport,
        Func<CascadeIdeSettings> getSettings,
        Action saveSettings)
    {
        _transport = transport;
        _getSettings = getSettings;
        _saveSettings = saveSettings;
    }

    public IntercomServerHostService ServerHost => _serverHost;

    public async Task<ChatSlashIntercomResult> ExecuteAsync(
        string handlerId,
        string? argsTail,
        CancellationToken ct)
    {
        return handlerId switch
        {
            ChatSlashIntercomHandlers.Ids.ServerStatus => await ServerStatusAsync(ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.ServerStart => ServerStart(argsTail),
            ChatSlashIntercomHandlers.Ids.ServerStop => ServerStop(),
            ChatSlashIntercomHandlers.Ids.TeamMembers => await TeamMembersAsync(ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.TeamInvite => await TeamInviteAsync(argsTail, ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.TeamSeedProject => await TeamSeedProjectAsync(argsTail, ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.AgentList => await AgentListAsync(ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.AgentProvision => await AgentProvisionAsync(argsTail, ct).ConfigureAwait(false),
            ChatSlashIntercomHandlers.Ids.AgentSelect => AgentSelect(argsTail),
            _ => ChatSlashIntercomResult.Fail($"Неизвестная admin-команда: {handlerId}"),
        };
    }

    public async Task<(bool Ok, string Message)> RefreshWorkspaceContextAsync(CancellationToken ct = default)
    {
        var settings = _getSettings();
        var transport = settings.Intercom.Transport;
        if (!transport.IsConfigured)
            return (false, "Transport не настроен.");

        _transport.OnWorkspaceChanged();

        if (!await _transport.EnsureBearerForAdminAsync(transport, ct).ConfigureAwait(false))
            return (false, "Нужен Connect или dev_team_token.");

        var bearer = await _transport.ResolveBearerForAdminAsync(transport, ct).ConfigureAwait(false);
        var resolved = await IntercomWorkspaceContextResolver.ResolveAsync(
            transport,
            _transport.GetWorkspaceRootForAdmin(),
            bearer,
            _transport.ApiClient,
            ct).ConfigureAwait(false);

        if (!resolved.Found)
            return (false, $"team не найден (repo={resolved.RepoKey ?? "—"}).");

        if (string.IsNullOrWhiteSpace(transport.TeamId))
            transport.TeamId = resolved.TeamId;

        _saveSettings();
        return (true, $"workspace → team {resolved.TeamId} ({resolved.Source}).");
    }

    private async Task<ChatSlashIntercomResult> ServerStatusAsync(CancellationToken ct)
    {
        var settings = _getSettings().Intercom.Transport;
        var host = _serverHost.DescribeStatus();
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            return ChatSlashIntercomResult.Ok($"{host}\nbase_url не задан.");

        _transport.ApiClient.ConfigureBaseUrl(settings.BaseUrl);
        var ping = await _transport.ApiClient.PingHealthAsync(ct).ConfigureAwait(false);
        var health = ping ? "HTTP /health OK" : "HTTP /health недоступен";

        if (!await _transport.EnsureBearerForAdminAsync(settings, ct).ConfigureAwait(false))
            return ChatSlashIntercomResult.Ok($"{host}\n{settings.BaseUrl}: {health}. Bearer: нет.");

        var bearer = await _transport.ResolveBearerForAdminAsync(settings, ct).ConfigureAwait(false);
        var teamId = settings.TeamId.Trim();
        if (string.IsNullOrWhiteSpace(teamId))
            return ChatSlashIntercomResult.Ok($"{host}\n{settings.BaseUrl}: {health}. team_id не задан.");

        var admin = await _transport.ApiClient.GetTeamAdminHealthAsync(teamId, bearer!, ct).ConfigureAwait(false);
        var adminText = admin is null
            ? "admin/health: нет доступа"
            : $"admin/health: {admin.Status}, SSE={admin.SseSubscribers}";
        return ChatSlashIntercomResult.Ok($"{host}\n{settings.BaseUrl}: {health}. {adminText}. Transport: {_transport.ConnectionStatus}");
    }

    private ChatSlashIntercomResult ServerStart(string? argsTail)
    {
        var settings = _getSettings().Intercom.Transport;
        var url = string.IsNullOrWhiteSpace(argsTail) ? settings.BaseUrl : argsTail.Trim();
        if (string.IsNullOrWhiteSpace(url))
            url = _serverHost.DefaultBaseUrl;

        var (ok, message) = _serverHost.Start(url);
        if (ok && string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            settings.BaseUrl = url;
            settings.Enabled = true;
            _saveSettings();
        }

        return ok ? ChatSlashIntercomResult.Ok(message) : ChatSlashIntercomResult.Fail(message);
    }

    private ChatSlashIntercomResult ServerStop()
    {
        var (ok, message) = _serverHost.Stop();
        return ok ? ChatSlashIntercomResult.Ok(message) : ChatSlashIntercomResult.Fail(message);
    }

    private async Task<ChatSlashIntercomResult> TeamMembersAsync(CancellationToken ct)
    {
        var (bearer, teamId, error) = await RequireTeamBearerAsync(ct).ConfigureAwait(false);
        if (error is not null)
            return ChatSlashIntercomResult.Fail(error);

        var members = await _transport.ApiClient.ListTeamMembersAsync(teamId!, bearer!, ct).ConfigureAwait(false);
        if (members.Count == 0)
            return ChatSlashIntercomResult.Ok("Участников нет.");

        var lines = members.Select(m =>
            $"{m.MemberId} · {m.MemberKind}/{m.TeamRole} · {m.DisplayName}");
        return ChatSlashIntercomResult.Ok(string.Join(Environment.NewLine, lines));
    }

    private async Task<ChatSlashIntercomResult> TeamInviteAsync(string? argsTail, CancellationToken ct)
    {
        var (bearer, teamId, error) = await RequireTeamBearerAsync(ct).ConfigureAwait(false);
        if (error is not null)
            return ChatSlashIntercomResult.Fail(error);

        var role = string.IsNullOrWhiteSpace(argsTail) ? "member" : argsTail.Trim();
        var invite = await _transport.ApiClient.CreateInviteAsync(teamId!, role, bearer!, ct).ConfigureAwait(false);
        if (invite is null)
            return ChatSlashIntercomResult.Fail("Не удалось создать invite (нужна роль admin).");

        return ChatSlashIntercomResult.Ok(
            $"invite {invite.InviteId}: role={invite.TeamRole}, token={invite.Token}, expires={invite.ExpiresAtUtc}");
    }

    private async Task<ChatSlashIntercomResult> TeamSeedProjectAsync(string? argsTail, CancellationToken ct)
    {
        var (bearer, teamId, error) = await RequireTeamBearerAsync(ct).ConfigureAwait(false);
        if (error is not null)
            return ChatSlashIntercomResult.Fail(error);

        var repoRoot = _transport.GetWorkspaceRootForAdmin();
        var repoUrl = IntercomWorkspaceGitRemoteResolver.TryGetNormalizedOrigin(repoRoot);
        if (string.IsNullOrWhiteSpace(repoUrl))
            return ChatSlashIntercomResult.Fail("Не удалось нормализовать origin репозитория.");

        var projectId = string.IsNullOrWhiteSpace(argsTail)
            ? SanitizeProjectId(teamId!)
            : argsTail.Trim();

        var created = await _transport.ApiClient.CreateProjectAsync(projectId, projectId, bearer!, ct)
            .ConfigureAwait(false);
        if (created is null)
            return ChatSlashIntercomResult.Fail("POST /projects не удался.");

        if (!await _transport.ApiClient.PutProjectReposAsync(projectId, [repoUrl], bearer!, ct).ConfigureAwait(false))
            return ChatSlashIntercomResult.Fail("PUT project repos не удался.");

        if (!await _transport.ApiClient.PutTeamProjectsAsync(teamId!, [projectId], bearer!, ct).ConfigureAwait(false))
            return ChatSlashIntercomResult.Fail("PUT team projects не удался.");

        _ = await RefreshWorkspaceContextAsync(ct).ConfigureAwait(false);
        return ChatSlashIntercomResult.Ok($"project {projectId} ← {repoUrl}, team {teamId} привязан.");
    }

    private async Task<ChatSlashIntercomResult> AgentListAsync(CancellationToken ct)
    {
        var (bearer, teamId, error) = await RequireTeamBearerAsync(ct).ConfigureAwait(false);
        if (error is not null)
            return ChatSlashIntercomResult.Fail(error);

        var members = await _transport.ApiClient.ListTeamMembersAsync(teamId!, bearer!, ct).ConfigureAwait(false);
        var agents = members.Where(m => string.Equals(m.MemberKind, "agent", StringComparison.Ordinal)).ToList();
        if (agents.Count == 0)
            return ChatSlashIntercomResult.Ok("Агентов нет. /intercom agent provision <имя>");

        var selected = _getSettings().Intercom.Transport.SelectedAgentMemberId;
        var lines = agents.Select(a =>
        {
            var mark = string.Equals(a.MemberId, selected, StringComparison.Ordinal) ? " *" : "";
            return $"{a.MemberId}{mark} · {a.DisplayName}";
        });
        return ChatSlashIntercomResult.Ok(string.Join(Environment.NewLine, lines));
    }

    private async Task<ChatSlashIntercomResult> AgentProvisionAsync(string? argsTail, CancellationToken ct)
    {
        var name = argsTail?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return ChatSlashIntercomResult.Fail("Укажи имя: /intercom agent provision <display name>");

        var (bearer, teamId, error) = await RequireTeamBearerAsync(ct).ConfigureAwait(false);
        if (error is not null)
            return ChatSlashIntercomResult.Fail(error);

        var agent = await _transport.ApiClient.ProvisionAgentAsync(teamId!, name, null, bearer!, ct)
            .ConfigureAwait(false);
        if (agent is null)
            return ChatSlashIntercomResult.Fail("Provision не удался (нужна роль admin).");

        var secrets = IntercomAgentSecretsStorage.Load();
        secrets.CredentialsByTeamAgent[IntercomAgentSecrets.MakeKey(teamId!, agent.MemberId)] =
            agent.CredentialToken;
        IntercomAgentSecretsStorage.Save(secrets);

        var transport = _getSettings().Intercom.Transport;
        transport.SelectedAgentMemberId = agent.MemberId;
        transport.SelectedAgentDisplayName = agent.DisplayName;
        _saveSettings();

        return ChatSlashIntercomResult.Ok(
            $"Агент {agent.DisplayName} ({agent.MemberId}). Credential сохранён в LocalAppData (не в git).");
    }

    private ChatSlashIntercomResult AgentSelect(string? argsTail)
    {
        var memberId = argsTail?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(memberId))
            return ChatSlashIntercomResult.Fail("Укажи member_id: /intercom agent select <id>");

        var transport = _getSettings().Intercom.Transport;
        transport.SelectedAgentMemberId = memberId;
        transport.SelectedAgentDisplayName = "";
        _saveSettings();
        return ChatSlashIntercomResult.Ok($"Выбран агент {memberId} для transport fan-out.");
    }

    private async Task<(string? Bearer, string? TeamId, string? Error)> RequireTeamBearerAsync(CancellationToken ct)
    {
        var settings = _getSettings().Intercom.Transport;
        if (!settings.IsConfigured)
            return (null, null, "Transport не настроен (base_url).");

        _transport.ApiClient.ConfigureBaseUrl(settings.BaseUrl);
        if (!await _transport.EnsureBearerForAdminAsync(settings, ct).ConfigureAwait(false))
            return (null, null, "Нужен Connect или dev_team_token.");

        var teamId = settings.TeamId.Trim();
        if (string.IsNullOrWhiteSpace(teamId))
            return (null, null, "team_id не задан.");

        var bearer = await _transport.ResolveBearerForAdminAsync(settings, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(bearer)
            ? (null, null, "Bearer недоступен.")
            : (bearer, teamId, null);
    }

    private static string SanitizeProjectId(string teamId) =>
        new string(teamId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
}

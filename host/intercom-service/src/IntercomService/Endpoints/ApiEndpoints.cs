using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IntercomService.Contracts;
using IntercomService.Data;
using IntercomService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Endpoints;

public static partial class ApiEndpoints
{
    public static void MapIntercomApi(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var api = app.MapGroup("/api/v1");

        MapAuth(api, app);
        MapIdentity(api);
        MapTeams(api);
        MapTopics(api);
    }

    private static void MapAuth(RouteGroupBuilder api, WebApplication app)
    {
        api.MapGet("/auth/login", async (
            string provider,
            string team_id,
            string redirect_uri,
            string? code_challenge,
            string? code_challenge_method,
            string? invite_token,
            GitHubAuthService github,
            OidcAuthService oidc,
            TeamMembershipService teams,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(team_id) || string.IsNullOrWhiteSpace(redirect_uri))
                return Results.BadRequest(new { error = "team_id and redirect_uri required" });

            if (!OAuthRedirectAllowlist.IsAllowed(redirect_uri))
                return Results.BadRequest(new { error = "redirect_uri not allowed" });

            await teams.EnsureTeamAsync(team_id, team_id, ct).ConfigureAwait(false);

            if (string.Equals(provider, "github", StringComparison.OrdinalIgnoreCase))
            {
                if (!github.IsConfigured)
                    return Results.Problem("GitHub OAuth is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);

                var ghState = await github.CreateStateAsync(
                        team_id,
                        redirect_uri,
                        code_challenge,
                        code_challenge_method,
                        invite_token,
                        ct)
                    .ConfigureAwait(false);
                return Results.Redirect(github.BuildAuthorizeUrl(ghState));
            }

            if (string.Equals(provider, "oidc", StringComparison.OrdinalIgnoreCase))
            {
                if (!oidc.IsConfigured)
                    return Results.Problem("OIDC is not configured", statusCode: StatusCodes.Status503ServiceUnavailable);

                var oidcState = await oidc.CreateStateAsync(
                        team_id,
                        redirect_uri,
                        code_challenge,
                        code_challenge_method,
                        invite_token,
                        ct)
                    .ConfigureAwait(false);
                return Results.Redirect(oidc.BuildAuthorizeUrl(oidcState));
            }

            return Results.BadRequest(new { error = "unsupported_provider" });
        });

        api.MapGet("/auth/callback/github", async (
            string? code,
            string? state,
            GitHubAuthService github,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return Results.BadRequest(new { error = "code and state required" });

            var oauthState = await github.GetValidStateAsync(state, ct).ConfigureAwait(false);
            if (oauthState is null)
                return Results.BadRequest(new { error = "invalid_state" });

            return Results.Redirect(AppendAuthorizationCode(oauthState.RedirectUri, code, state));
        });

        api.MapGet("/auth/callback/oidc", async (
            string? code,
            string? state,
            OidcAuthService oidc,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return Results.BadRequest(new { error = "code and state required" });

            var oauthState = await oidc.GetValidStateAsync(state, ct).ConfigureAwait(false);
            if (oauthState is null)
                return Results.BadRequest(new { error = "invalid_state" });

            return Results.Redirect(AppendAuthorizationCode(oauthState.RedirectUri, code, state));
        });

        api.MapPost("/auth/token", async (
            OAuthTokenRequest body,
            JwtTokenService jwt,
            GitHubAuthService github,
            OidcAuthService oidc,
            TeamMembershipService teams,
            CancellationToken ct) =>
        {
            if (string.Equals(body.GrantType, "authorization_code", StringComparison.OrdinalIgnoreCase))
            {
                return await exchangeAuthorizationCodeAsync(body, github, oidc, teams, jwt, ct);
            }

            if (!string.Equals(body.GrantType, "refresh_token", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                return Results.BadRequest(new { error = "grant_type must be refresh_token or authorization_code" });
            }

            var tokens = await jwt.RefreshAsync(body.RefreshToken, ct).ConfigureAwait(false);
            return tokens is null ? Results.Unauthorized() : Results.Json(tokens);
        });

        api.MapPost("/auth/logout", async (
            RefreshTokenRequest body,
            JwtTokenService jwt,
            CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(body.RefreshToken))
                await jwt.RevokeRefreshAsync(body.RefreshToken, ct).ConfigureAwait(false);
            return Results.Ok();
        }).RequireAuthorization();

        api.MapPatch("/auth/me", async (
            PatchMeRequest body,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(memberId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(body.DisplayName))
                return Results.BadRequest(new { error = "display_name required" });

            var ok = await teams.PatchMemberDefaultDisplayNameAsync(memberId, body.DisplayName, ct)
                .ConfigureAwait(false);
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();

        api.MapGet("/auth/me", async (
            ClaimsPrincipal user,
            TeamMembershipService teams,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(memberId))
                return Results.Unauthorized();

            var member = await teams.GetMemberAsync(memberId, ct).ConfigureAwait(false);
            var displayName = member?.DisplayName ?? user.FindFirstValue("display_name") ?? memberId;
            var memberKind = member?.MemberKind ?? MemberKinds.Human;
            var teamList = await teams.ListMeTeamsAsync(memberId, ct).ConfigureAwait(false);
            return Results.Json(new MeResponse(memberId, displayName, memberKind, teamList));
        }).RequireAuthorization();
    }

    private static void MapTeams(RouteGroupBuilder api)
    {
        api.MapGet("/teams/{teamId}", async (
            string teamId,
            IntercomDbContext db,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(x => x.TeamId == teamId, ct)
                .ConfigureAwait(false);
            return team is null
                ? Results.NotFound()
                : Results.Json(new { team_id = team.TeamId, display_name = team.DisplayName });
        }).RequireAuthorization();

        api.MapGet("/teams/{teamId}/topics", async (
            string teamId,
            IntercomDbContext db,
            TransportEventService events,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            await events.EnsureGeneralTopicAsync(teamId, ct).ConfigureAwait(false);

            var topics = await db.Topics.AsNoTracking()
                .Where(x => x.TeamId == teamId)
                .OrderBy(x => x.Title)
                .Select(x => new TopicDto(x.TopicId, x.TeamId, x.Title, x.SpineKey))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return Results.Json(topics);
        }).RequireAuthorization();

        api.MapPost("/teams/{teamId}/topics", async (
            string teamId,
            CreateTopicRequest body,
            IntercomDbContext db,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(body.Title))
                return Results.BadRequest(new { error = "title required" });

            var topic = new TopicEntity
            {
                TopicId = Guid.NewGuid().ToString("N"),
                TeamId = teamId,
                Title = body.Title.Trim(),
                SpineKey = body.SpineKey,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.Topics.Add(topic);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Json(new TopicDto(topic.TopicId, topic.TeamId, topic.Title, topic.SpineKey));
        }).RequireAuthorization();

        api.MapPost("/teams/{teamId}/topics/ensure", async (
            string teamId,
            EnsureTopicRequest body,
            TransportEventService events,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(body.SpineKey))
                return Results.BadRequest(new { error = "spine_key required" });

            var topic = await events.EnsureTopicBySpineAsync(teamId, body.SpineKey, body.Title ?? body.SpineKey, ct)
                .ConfigureAwait(false);
            return Results.Json(new TopicDto(topic.TopicId, topic.TeamId, topic.Title, topic.SpineKey));
        }).RequireAuthorization();

        api.MapGet("/teams/{teamId}/events", async (
            string teamId,
            long? after_seq,
            int? limit,
            TransportEventService events,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            var list = await events.ListTeamAsync(teamId, after_seq, limit ?? 200, ct).ConfigureAwait(false);
            return Results.Json(list);
        }).RequireAuthorization();

        api.MapGet("/teams/{teamId}/stream", async (
            string teamId,
            string? topic_id,
            TeamMembershipService teams,
            SseEventHub sse,
            ClaimsPrincipal user,
            HttpContext http,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.EnsureAccessAsync(teamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            http.Response.Headers.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";

            await foreach (var envelope in sse.SubscribeAsync(teamId, ct).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(topic_id)
                    && !string.Equals(envelope.TopicId, topic_id, StringComparison.Ordinal))
                    continue;

                var json = JsonSerializer.Serialize(envelope, IntercomJson.Web);
                await http.Response.WriteAsync($"event: transport_event\ndata: {json}\n\n", ct).ConfigureAwait(false);
                await http.Response.Body.FlushAsync(ct).ConfigureAwait(false);
            }
        }).RequireAuthorization();
    }

    private static void MapTopics(RouteGroupBuilder api)
    {
        api.MapGet("/topics/{topicId}/events", async (
            string topicId,
            long? after_seq,
            int? limit,
            IntercomDbContext db,
            TransportEventService events,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var topic = await db.Topics.AsNoTracking().FirstOrDefaultAsync(x => x.TopicId == topicId, ct)
                .ConfigureAwait(false);
            if (topic is null)
                return Results.NotFound();

            if (!await teams.EnsureAccessAsync(topic.TeamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            var list = await events.ListAsync(topic.TeamId, topicId, after_seq, limit ?? 100, ct)
                .ConfigureAwait(false);
            return Results.Json(list);
        }).RequireAuthorization();

        api.MapPost("/topics/{topicId}/events", async (
            string topicId,
            AppendEventRequest body,
            IntercomDbContext db,
            TransportEventService events,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var display = user.FindFirstValue("display_name") ?? memberId;

            var topic = await db.Topics.AsNoTracking().FirstOrDefaultAsync(x => x.TopicId == topicId, ct)
                .ConfigureAwait(false);
            if (topic is null)
                return Results.NotFound();

            if (!await teams.EnsureAccessAsync(topic.TeamId, memberId, IsDevAuth(user), user.FindFirstValue("display_name"), ct).ConfigureAwait(false))
                return Results.Forbid();

            var (envelope, error) = await events.AppendAsync(
                topic.TeamId,
                topicId,
                body,
                memberId,
                display,
                body.Sender?.ClientKind ?? "cide",
                ct).ConfigureAwait(false);

            return error is not null
                ? Results.BadRequest(new { error })
                : Results.Json(envelope);
        }).RequireAuthorization();
    }

    private static bool IsDevAuth(ClaimsPrincipal user) =>
        string.Equals(user.Identity?.AuthenticationType, IntercomService.Auth.DevBearerAuthenticationHandler.SchemeName, StringComparison.Ordinal);

    private static string AppendAuthorizationCode(string redirectUri, string code, string state)
    {
        var sep = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{redirectUri}{sep}code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}";
    }

    private static async Task<IResult> exchangeAuthorizationCodeAsync(
        OAuthTokenRequest body,
        GitHubAuthService github,
        OidcAuthService oidc,
        TeamMembershipService teams,
        JwtTokenService jwt,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Code)
            || string.IsNullOrWhiteSpace(body.State)
            || string.IsNullOrWhiteSpace(body.CodeVerifier)
            || string.IsNullOrWhiteSpace(body.RedirectUri))
        {
            return Results.BadRequest(new { error = "code, state, code_verifier, redirect_uri required" });
        }

        if (!OAuthRedirectAllowlist.IsAllowed(body.RedirectUri))
            return Results.BadRequest(new { error = "redirect_uri not allowed" });

        OAuthStateEntity? oauthState = await github.ConsumeStateAsync(body.State, ct).ConfigureAwait(false);
        oauthState ??= await oidc.ConsumeStateAsync(body.State, ct).ConfigureAwait(false);
        if (oauthState is null)
            return Results.BadRequest(new { error = "invalid_state" });

        if (!string.Equals(oauthState.RedirectUri, body.RedirectUri, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "redirect_uri mismatch" });

        if (string.Equals(oauthState.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            var exchange = await github.ExchangeCodeAsync(body.Code, oauthState, body.CodeVerifier, ct).ConfigureAwait(false);
            if (exchange is null)
                return Results.BadRequest(new { error = "token_exchange_failed" });

            await teams.EnsureTeamAsync(oauthState.TeamId, oauthState.TeamId, ct).ConfigureAwait(false);
            var member = await teams.EnsureGitHubMemberAsync(exchange.User.Id, exchange.User.Login, ct).ConfigureAwait(false);
            var (joined, joinError) = await teams.TryJoinTeamAsync(
                oauthState.TeamId,
                member,
                oauthState.InviteToken,
                "github",
                exchange.AccessToken,
                ct).ConfigureAwait(false);
            if (!joined)
                return Results.Forbid();

            var tokens = await jwt.IssueTokensAsync(member, ct).ConfigureAwait(false);
            return Results.Json(tokens);
        }

        if (string.Equals(oauthState.Provider, "oidc", StringComparison.OrdinalIgnoreCase))
        {
            var user = await oidc.ExchangeCodeAsync(body.Code, oauthState, body.CodeVerifier, ct).ConfigureAwait(false);
            if (user is null)
                return Results.BadRequest(new { error = "token_exchange_failed" });

            await teams.EnsureTeamAsync(oauthState.TeamId, oauthState.TeamId, ct).ConfigureAwait(false);
            var member = await teams.EnsureOidcMemberAsync(user.Issuer, user.Subject, user.DisplayName, ct).ConfigureAwait(false);
            var (joined, joinError) = await teams.TryJoinTeamAsync(
                oauthState.TeamId,
                member,
                oauthState.InviteToken,
                "oidc",
                null,
                ct).ConfigureAwait(false);
            if (!joined)
                return Results.Forbid();

            var tokens = await jwt.IssueTokensAsync(member, ct).ConfigureAwait(false);
            return Results.Json(tokens);
        }

        return Results.BadRequest(new { error = "unsupported_provider" });
    }
}

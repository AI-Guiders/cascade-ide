using System.Security.Claims;
using IntercomService.Contracts;
using IntercomService.Data;
using IntercomService.Services;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Endpoints;

public static partial class ApiEndpoints
{
    private static void MapIdentity(RouteGroupBuilder api)
    {
        api.MapGet("/auth/providers", (AuthProviderRegistry registry) =>
            Results.Json(registry.ListProviders()));

        api.MapPost("/auth/redeem-invite", async (
            RedeemInviteRequest body,
            TeamInviteService invites,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.InviteToken) || string.IsNullOrWhiteSpace(body.TeamId))
                return Results.BadRequest(new { error = "invite_token and team_id required" });

            var valid = await invites.IsValidInviteAsync(body.TeamId, body.InviteToken, ct).ConfigureAwait(false);
            return valid ? Results.Ok(new { status = "valid" }) : Results.BadRequest(new { error = "invalid_invite" });
        });

        api.MapPost("/teams", async (
            CreateTeamRequest body,
            TeamMembershipService teams,
            IntercomDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(memberId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(body.TeamId))
                return Results.BadRequest(new { error = "team_id required" });

            var teamId = body.TeamId.Trim();
            if (await db.Teams.AnyAsync(x => x.TeamId == teamId, ct).ConfigureAwait(false))
                return Results.Conflict(new { error = "team_exists" });

            var team = await teams.CreateTeamAsync(teamId, body.DisplayName, memberId, ct)
                .ConfigureAwait(false);
            return Results.Json(new TeamDto(team.TeamId, team.DisplayName, team.JoinPolicy, team.DefaultTeamRole));
        }).RequireAuthorization();

        api.MapPatch("/teams/{teamId}", async (
            string teamId,
            PatchTeamRequest body,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            if (role is null || !TeamRoleAuthorization.CanPatchTeam(role))
                return Results.Forbid();

            if (!string.IsNullOrWhiteSpace(body.JoinPolicy)
                && !TeamRoleAuthorization.CanChangeJoinPolicy(role))
                return Results.Forbid();

            var ok = await teams.PatchTeamAsync(teamId, body, ct).ConfigureAwait(false);
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();

        api.MapGet("/teams/{teamId}/members", async (
            string teamId,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            if (role is null || !TeamRoleAuthorization.MeetsMinimum(role, TeamRoles.Member))
                return Results.Forbid();

            var list = await teams.ListTeamMembersAsync(teamId, ct).ConfigureAwait(false);
            return Results.Json(list);
        }).RequireAuthorization();

        api.MapPatch("/teams/{teamId}/members/me", async (
            string teamId,
            PatchSelfTeamMemberRequest body,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await teams.IsMemberOfTeamAsync(teamId, memberId, ct).ConfigureAwait(false))
                return Results.Forbid();

            var ok = await teams.PatchSelfTeamDisplayNameAsync(teamId, memberId, body, ct).ConfigureAwait(false);
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();

        api.MapPatch("/teams/{teamId}/members/{targetMemberId}", async (
            string teamId,
            string targetMemberId,
            PatchTeamMemberRequest body,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (ok, error) = await teams
                .PatchTeamMemberRoleAsync(teamId, targetMemberId, memberId, body, ct)
                .ConfigureAwait(false);

            return error switch
            {
                "forbidden" => Results.Forbid(),
                "member_not_found" => Results.NotFound(),
                "last_owner" => Results.Conflict(new { error }),
                "owner_promotion_forbidden" => Results.Forbid(),
                "invalid_role" => Results.BadRequest(new { error }),
                _ when ok => Results.Ok(),
                _ => Results.BadRequest(new { error = error ?? "failed" }),
            };
        }).RequireAuthorization();

        api.MapPost("/teams/{teamId}/invites", async (
            string teamId,
            CreateInviteRequest body,
            TeamInviteService invites,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            if (role is null || !TeamRoleAuthorization.CanManageInvites(role))
                return Results.Forbid();

            var teamRole = string.IsNullOrWhiteSpace(body.TeamRole) ? TeamRoles.Member : body.TeamRole.Trim();
            var ttl = TimeSpan.FromHours(Math.Clamp(body.TtlHours ?? 168, 1, 8760));
            var maxUses = Math.Clamp(body.MaxUses ?? 1, 1, 1000);

            var created = await invites
                .CreateInviteAsync(teamId, memberId, teamRole, ttl, maxUses, ct)
                .ConfigureAwait(false);
            if (created is null)
                return Results.BadRequest(new { error = "invalid_role" });

            var (invite, token) = created.Value;
            return Results.Json(new InviteDto(
                invite.InviteId,
                token,
                invite.TeamRole,
                invite.ExpiresAtUtc.ToString("O"),
                invite.MaxUses));
        }).RequireAuthorization();

        api.MapPost("/teams/{teamId}/agents", async (
            string teamId,
            CreateAgentRequest body,
            AgentAccountService agents,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            if (role is null || !TeamRoleAuthorization.CanManageAgents(role))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(body.DisplayName))
                return Results.BadRequest(new { error = "display_name required" });

            var agent = await agents
                .ProvisionAsync(teamId, body.DisplayName, body.AvatarGlyph, ct)
                .ConfigureAwait(false);
            return agent is null ? Results.BadRequest() : Results.Json(agent);
        }).RequireAuthorization();

        api.MapPatch("/teams/{teamId}/agents/{agentMemberId}", async (
            string teamId,
            string agentMemberId,
            PatchAgentRequest body,
            AgentAccountService agents,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            var isAdmin = role is not null && TeamRoleAuthorization.CanManageAgents(role);
            var isSelf = string.Equals(memberId, agentMemberId, StringComparison.Ordinal);

            if (!isAdmin && !isSelf)
                return Results.Forbid();

            var ok = await agents.PatchAsync(teamId, agentMemberId, body, isAdmin, ct).ConfigureAwait(false);
            return ok ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization();

        api.MapGet("/resolve/workspace-context", async (
            HttpRequest request,
            WorkspaceResolveService resolve,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(memberId))
                return Results.Unauthorized();

            var repoUrls = request.Query["repo_url"].ToArray().Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray();
            if (repoUrls.Length == 0)
                return Results.BadRequest(new { error = "repo_url required" });

            var response = await resolve.ResolveAsync(memberId, repoUrls, ct).ConfigureAwait(false);
            return Results.Json(response);
        }).RequireAuthorization();

        api.MapPost("/projects", async (
            CreateProjectRequest body,
            IntercomDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.ProjectId))
                return Results.BadRequest(new { error = "project_id required" });

            var exists = await db.Projects.AnyAsync(x => x.ProjectId == body.ProjectId, ct).ConfigureAwait(false);
            if (exists)
                return Results.Conflict(new { error = "exists" });

            var project = new ProjectEntity
            {
                ProjectId = body.ProjectId.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(body.DisplayName) ? body.ProjectId.Trim() : body.DisplayName.Trim(),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Json(new ProjectDto(project.ProjectId, project.DisplayName));
        }).RequireAuthorization();

        api.MapPut("/projects/{projectId}/repos", async (
            string projectId,
            PutProjectReposRequest body,
            IntercomDbContext db,
            CancellationToken ct) =>
        {
            var project = await db.Projects.FindAsync([projectId], ct).ConfigureAwait(false);
            if (project is null)
                return Results.NotFound();

            var normalized = GitRepoUrlNormalizer.NormalizeMany(body.RepoUrls ?? []);
            var existing = await db.ProjectRepos.Where(x => x.ProjectId == projectId).ToListAsync(ct)
                .ConfigureAwait(false);
            db.ProjectRepos.RemoveRange(existing);

            foreach (var url in normalized)
            {
                db.ProjectRepos.Add(new ProjectRepoEntity
                {
                    ProjectId = projectId,
                    NormalizedRepoUrl = url,
                    LinkedAtUtc = DateTimeOffset.UtcNow,
                });
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Ok(new { repo_urls = normalized });
        }).RequireAuthorization();

        api.MapPut("/teams/{teamId}/projects", async (
            string teamId,
            PutTeamProjectsRequest body,
            IntercomDbContext db,
            TeamMembershipService teams,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var memberId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var role = await teams.GetTeamRoleAsync(teamId, memberId, ct).ConfigureAwait(false);
            if (role is null || !TeamRoleAuthorization.CanPatchTeam(role))
                return Results.Forbid();

            var team = await db.Teams.FindAsync([teamId], ct).ConfigureAwait(false);
            if (team is null)
                return Results.NotFound();

            var projectIds = (body.ProjectIds ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var existing = await db.TeamProjects.Where(x => x.TeamId == teamId).ToListAsync(ct)
                .ConfigureAwait(false);
            db.TeamProjects.RemoveRange(existing);

            foreach (var pid in projectIds)
            {
                if (!await db.Projects.AnyAsync(x => x.ProjectId == pid, ct).ConfigureAwait(false))
                    continue;

                db.TeamProjects.Add(new TeamProjectEntity
                {
                    TeamId = teamId,
                    ProjectId = pid,
                    LinkedAtUtc = DateTimeOffset.UtcNow,
                });
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Results.Ok(new { project_ids = projectIds });
        }).RequireAuthorization();
    }
}

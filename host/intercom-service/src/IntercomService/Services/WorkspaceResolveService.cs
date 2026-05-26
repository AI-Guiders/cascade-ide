using IntercomService.Contracts;
using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class WorkspaceResolveService(IntercomDbContext db, TeamMembershipService teams)
{
    public async Task<WorkspaceContextResponse> ResolveAsync(
        string memberId,
        IReadOnlyList<string> repoUrls,
        CancellationToken ct)
    {
        var normalized = GitRepoUrlNormalizer.NormalizeMany(repoUrls);
        if (normalized.Count == 0)
        {
            return new WorkspaceContextResponse([], [], [], null);
        }

        var memberTeamIds = await teams.ListTeamIdsForMemberAsync(memberId, ct).ConfigureAwait(false);
        var memberTeamSet = memberTeamIds.ToHashSet(StringComparer.Ordinal);

        var projectIds = await db.ProjectRepos.AsNoTracking()
            .Where(x => normalized.Contains(x.NormalizedRepoUrl))
            .Select(x => x.ProjectId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (projectIds.Count == 0)
        {
            return new WorkspaceContextResponse(normalized, [], [], null);
        }

        var projects = await db.Projects.AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectId))
            .OrderBy(x => x.DisplayName)
            .Select(x => new WorkspaceContextProjectDto(x.ProjectId, x.DisplayName))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var teamLinks = await db.TeamProjects.AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectId) && memberTeamSet.Contains(x.TeamId))
            .Join(
                db.Teams.AsNoTracking(),
                tp => tp.TeamId,
                t => t.TeamId,
                (tp, t) => new { tp.TeamId, tp.ProjectId, t.DisplayName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var teamDtos = new List<WorkspaceContextTeamDto>();
        foreach (var link in teamLinks)
        {
            var role = await teams.GetTeamRoleAsync(link.TeamId, memberId, ct).ConfigureAwait(false);
            if (role is null)
                continue;

            teamDtos.Add(new WorkspaceContextTeamDto(
                link.TeamId,
                link.DisplayName,
                role,
                link.ProjectId));
        }

        string? suggested = null;
        if (teamDtos.Count == 1)
            suggested = teamDtos[0].TeamId;

        return new WorkspaceContextResponse(normalized, projects, teamDtos, suggested);
    }
}

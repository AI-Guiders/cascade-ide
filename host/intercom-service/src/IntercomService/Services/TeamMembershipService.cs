using IntercomService.Contracts;
using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class TeamMembershipService(
    IntercomDbContext db,
    TeamInviteService invites,
    GitHubAuthService github,
    IHostEnvironment environment)
{
    public async Task<TeamEntity> CreateTeamAsync(
        string teamId,
        string? displayName,
        string creatorMemberId,
        CancellationToken ct)
    {
        var team = new TeamEntity
        {
            TeamId = teamId.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? teamId.Trim() : displayName.Trim(),
            JoinPolicy = JoinPolicies.FirstOwner,
            DefaultTeamRole = TeamRoles.Member,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Teams.Add(team);
        db.TeamMembers.Add(new TeamMemberEntity
        {
            TeamId = team.TeamId,
            MemberId = creatorMemberId,
            TeamRole = TeamRoles.Owner,
            JoinedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return team;
    }

    public async Task EnsureTeamAsync(string teamId, string? displayName, CancellationToken ct)
    {
        if (await db.Teams.AnyAsync(x => x.TeamId == teamId, ct).ConfigureAwait(false))
            return;

        db.Teams.Add(new TeamEntity
        {
            TeamId = teamId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? teamId : displayName.Trim(),
            JoinPolicy = environment.IsDevelopment() ? JoinPolicies.Open : JoinPolicies.FirstOwner,
            DefaultTeamRole = TeamRoles.Member,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task<MemberEntity> EnsureGitHubMemberAsync(long githubUserId, string login, CancellationToken ct)
    {
        var issuer = "https://github.com";
        var subject = githubUserId.ToString();
        var memberId = $"github:{githubUserId}";

        var member = await db.Members.FirstOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject, ct)
            .ConfigureAwait(false);

        if (member is not null)
        {
            if (string.IsNullOrWhiteSpace(member.DisplayName))
            {
                member.DisplayName = login;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return member;
        }

        member = new MemberEntity
        {
            MemberId = memberId,
            Issuer = issuer,
            Subject = subject,
            DisplayName = login,
            MemberKind = MemberKinds.Human,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return member;
    }

    public async Task<MemberEntity> EnsureOidcMemberAsync(
        string issuer,
        string subject,
        string displayName,
        CancellationToken ct)
    {
        var memberId = $"oidc:{Uri.EscapeDataString(issuer)}:{subject}";
        var member = await db.Members.FirstOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject, ct)
            .ConfigureAwait(false);

        if (member is not null)
        {
            if (string.IsNullOrWhiteSpace(member.DisplayName) && !string.IsNullOrWhiteSpace(displayName))
            {
                member.DisplayName = displayName;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return member;
        }

        member = new MemberEntity
        {
            MemberId = memberId,
            Issuer = issuer,
            Subject = subject,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? memberId : displayName,
            MemberKind = MemberKinds.Human,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return member;
    }

    public async Task JoinTeamAsync(string teamId, string memberId, string teamRole, CancellationToken ct)
    {
        if (await db.TeamMembers.AnyAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct).ConfigureAwait(false))
            return;

        db.TeamMembers.Add(new TeamMemberEntity
        {
            TeamId = teamId,
            MemberId = memberId,
            TeamRole = teamRole,
            JoinedAtUtc = DateTimeOffset.UtcNow,
        });

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task<(bool Joined, string? Error)> TryJoinTeamAsync(
        string teamId,
        MemberEntity member,
        string? inviteToken,
        string provider,
        string? githubAccessToken,
        CancellationToken ct)
    {
        if (await IsMemberOfTeamAsync(teamId, member.MemberId, ct).ConfigureAwait(false))
            return (true, null);

        var team = await db.Teams.FindAsync([teamId], ct).ConfigureAwait(false);
        if (team is null)
            return (false, "team_not_found");

        var policy = team.JoinPolicy;
        if (string.Equals(policy, JoinPolicies.Open, StringComparison.Ordinal))
        {
            if (!environment.IsDevelopment())
                return (false, "open_join_forbidden");

            await JoinTeamAsync(teamId, member.MemberId, team.DefaultTeamRole, ct).ConfigureAwait(false);
            return (true, null);
        }

        if (string.Equals(policy, JoinPolicies.FirstOwner, StringComparison.Ordinal))
        {
            var hasHuman = await db.TeamMembers.AsNoTracking()
                .Join(
                    db.Members.AsNoTracking(),
                    tm => tm.MemberId,
                    m => m.MemberId,
                    (tm, m) => new { tm.TeamId, m.MemberKind })
                .AnyAsync(x => x.TeamId == teamId && x.MemberKind == MemberKinds.Human, ct)
                .ConfigureAwait(false);

            if (!hasHuman)
            {
                await JoinTeamAsync(teamId, member.MemberId, TeamRoles.Owner, ct).ConfigureAwait(false);
                return (true, null);
            }

            policy = JoinPolicies.InviteRequired;
        }

        if (string.Equals(policy, JoinPolicies.InviteRequired, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(inviteToken))
                return (false, "invite_required");

            var consumed = await invites.TryConsumeInviteAsync(teamId, inviteToken, ct).ConfigureAwait(false);
            if (consumed is null)
                return (false, "invalid_invite");

            await JoinTeamAsync(teamId, member.MemberId, consumed.Value.TeamRole, ct).ConfigureAwait(false);
            return (true, null);
        }

        if (string.Equals(policy, JoinPolicies.GitHubOrg, StringComparison.Ordinal))
        {
            if (!string.Equals(provider, "github", StringComparison.OrdinalIgnoreCase))
                return (false, "github_org_requires_github");

            if (string.IsNullOrWhiteSpace(githubAccessToken))
                return (false, "github_token_required");

            var config = JoinPolicyConfig.Parse(team.JoinPolicyJson);
            if (config.GitHubOrgs.Count == 0)
                return (false, "github_org_not_configured");

            var ok = await github.IsMemberOfAnyOrgAsync(githubAccessToken, config.GitHubOrgs, ct)
                .ConfigureAwait(false);
            if (!ok)
                return (false, "github_org_denied");

            await JoinTeamAsync(teamId, member.MemberId, team.DefaultTeamRole, ct).ConfigureAwait(false);
            return (true, null);
        }

        return (false, "unsupported_join_policy");
    }

    public async Task<IReadOnlyList<string>> ListTeamIdsForMemberAsync(string memberId, CancellationToken ct) =>
        await db.TeamMembers.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .Select(x => x.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MeTeamDto>> ListMeTeamsAsync(string memberId, CancellationToken ct)
    {
        var rows = await db.TeamMembers.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .Join(
                db.Teams.AsNoTracking(),
                tm => tm.TeamId,
                t => t.TeamId,
                (tm, t) => new { tm, t })
            .Join(
                db.Members.AsNoTracking(),
                x => x.tm.MemberId,
                m => m.MemberId,
                (x, m) => new { x.tm, x.t, m })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .Select(x => new MeTeamDto(
                x.tm.TeamId,
                x.tm.TeamRole,
                EffectiveDisplayName(x.m.DisplayName, x.tm.TeamDisplayName),
                x.tm.TeamDisplayName))
            .ToList();
    }

    public async Task<bool> IsMemberOfTeamAsync(string teamId, string memberId, CancellationToken ct) =>
        await db.TeamMembers.AnyAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct).ConfigureAwait(false);

    public async Task<string?> GetTeamRoleAsync(string teamId, string memberId, CancellationToken ct) =>
        await db.TeamMembers.AsNoTracking()
            .Where(x => x.TeamId == teamId && x.MemberId == memberId)
            .Select(x => x.TeamRole)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task<bool> EnsureAccessAsync(
        string teamId,
        string memberId,
        bool allowDevAutoJoin,
        string? displayName,
        CancellationToken ct)
    {
        if (await IsMemberOfTeamAsync(teamId, memberId, ct).ConfigureAwait(false))
            return true;

        if (!allowDevAutoJoin)
            return false;

        await EnsureMemberAsync(memberId, "dev", memberId, displayName ?? memberId, ct).ConfigureAwait(false);
        await EnsureTeamAsync(teamId, teamId, ct).ConfigureAwait(false);
        await JoinTeamAsync(teamId, memberId, TeamRoles.Admin, ct).ConfigureAwait(false);
        return true;
    }

    public async Task EnsureMemberAsync(
        string memberId,
        string issuer,
        string subject,
        string displayName,
        CancellationToken ct)
    {
        if (await db.Members.FindAsync([memberId], ct).ConfigureAwait(false) is not null)
            return;

        db.Members.Add(new MemberEntity
        {
            MemberId = memberId,
            Issuer = issuer,
            Subject = subject,
            DisplayName = displayName,
            MemberKind = MemberKinds.Human,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<MemberEntity?> GetMemberAsync(string memberId, CancellationToken ct) =>
        await db.Members.FindAsync([memberId], ct).ConfigureAwait(false);

    public async Task<bool> PatchMemberDefaultDisplayNameAsync(string memberId, string displayName, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([memberId], ct).ConfigureAwait(false);
        if (member is null || string.IsNullOrWhiteSpace(displayName))
            return false;

        member.DisplayName = displayName.Trim();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> PatchSelfTeamDisplayNameAsync(
        string teamId,
        string memberId,
        PatchSelfTeamMemberRequest body,
        CancellationToken ct)
    {
        var row = await db.TeamMembers
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct)
            .ConfigureAwait(false);
        if (row is null)
            return false;

        if (body.ClearTeamDisplayName)
            row.TeamDisplayName = null;
        else if (body.TeamDisplayName is not null)
            row.TeamDisplayName = string.IsNullOrWhiteSpace(body.TeamDisplayName) ? null : body.TeamDisplayName.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<TeamMemberDto>> ListTeamMembersAsync(string teamId, CancellationToken ct)
    {
        var rows = await db.TeamMembers.AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .Join(db.Members.AsNoTracking(), tm => tm.MemberId, m => m.MemberId, (tm, m) => new { tm, m })
            .OrderBy(x => x.m.DisplayName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(x => new TeamMemberDto(
            x.m.MemberId,
            x.m.MemberKind,
            x.tm.TeamRole,
            EffectiveDisplayName(x.m.DisplayName, x.tm.TeamDisplayName),
            x.tm.TeamDisplayName,
            x.tm.JoinedAtUtc.ToString("O"))).ToList();
    }

    public async Task<(bool Ok, string? Error)> PatchTeamMemberRoleAsync(
        string teamId,
        string targetMemberId,
        string callerMemberId,
        PatchTeamMemberRequest body,
        CancellationToken ct)
    {
        var callerRole = await GetTeamRoleAsync(teamId, callerMemberId, ct).ConfigureAwait(false);
        if (callerRole is null || !TeamRoleAuthorization.CanManageMembers(callerRole))
            return (false, "forbidden");

        var target = await db.TeamMembers
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.MemberId == targetMemberId, ct)
            .ConfigureAwait(false);
        if (target is null)
            return (false, "member_not_found");

        if (!string.IsNullOrWhiteSpace(body.TeamRole))
        {
            var newRole = body.TeamRole.Trim();
            if (!TeamRoles.All.Contains(newRole))
                return (false, "invalid_role");

            if (string.Equals(newRole, TeamRoles.Owner, StringComparison.Ordinal)
                && !TeamRoleAuthorization.CanPromoteToOwner(callerRole))
                return (false, "owner_promotion_forbidden");

            if (string.Equals(target.TeamRole, TeamRoles.Owner, StringComparison.Ordinal)
                && !string.Equals(newRole, TeamRoles.Owner, StringComparison.Ordinal))
            {
                var ownerCount = await db.TeamMembers.CountAsync(
                    x => x.TeamId == teamId && x.TeamRole == TeamRoles.Owner,
                    ct).ConfigureAwait(false);
                if (ownerCount <= 1)
                    return (false, "last_owner");
            }

            target.TeamRole = newRole;
        }

        if (body.ClearTeamDisplayName)
            target.TeamDisplayName = null;
        else if (body.TeamDisplayName is not null)
            target.TeamDisplayName = string.IsNullOrWhiteSpace(body.TeamDisplayName)
                ? null
                : body.TeamDisplayName.Trim();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (true, null);
    }

    public async Task<bool> PatchTeamAsync(string teamId, PatchTeamRequest body, CancellationToken ct)
    {
        var team = await db.Teams.FindAsync([teamId], ct).ConfigureAwait(false);
        if (team is null)
            return false;

        if (!string.IsNullOrWhiteSpace(body.DisplayName))
            team.DisplayName = body.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(body.JoinPolicy))
            team.JoinPolicy = body.JoinPolicy.Trim();

        if (!string.IsNullOrWhiteSpace(body.DefaultTeamRole))
            team.DefaultTeamRole = body.DefaultTeamRole.Trim();

        if (body.JoinPolicyJson is not null)
            team.JoinPolicyJson = body.JoinPolicyJson;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public static string EffectiveDisplayName(string memberDisplayName, string? teamDisplayName) =>
        string.IsNullOrWhiteSpace(teamDisplayName) ? memberDisplayName : teamDisplayName;
}

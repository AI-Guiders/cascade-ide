using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class TeamMembershipService(IntercomDbContext db)
{
    public async Task EnsureTeamAsync(string teamId, string? displayName, CancellationToken ct)
    {
        var team = await db.Teams.FindAsync([teamId], ct).ConfigureAwait(false);
        if (team is not null)
            return;

        db.Teams.Add(new TeamEntity
        {
            TeamId = teamId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? teamId : displayName.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
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
            if (!string.Equals(member.DisplayName, login, StringComparison.Ordinal))
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
            if (!string.Equals(member.DisplayName, displayName, StringComparison.Ordinal))
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
            DisplayName = displayName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Members.Add(member);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return member;
    }

    public async Task JoinTeamAsync(string teamId, string memberId, CancellationToken ct)
    {
        var exists = await db.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct)
            .ConfigureAwait(false);
        if (exists)
            return;

        db.TeamMembers.Add(new TeamMemberEntity
        {
            TeamId = teamId,
            MemberId = memberId,
            JoinedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> ListTeamIdsForMemberAsync(string memberId, CancellationToken ct) =>
        await db.TeamMembers.AsNoTracking()
            .Where(x => x.MemberId == memberId)
            .Select(x => x.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<bool> IsMemberOfTeamAsync(string teamId, string memberId, CancellationToken ct) =>
        await db.TeamMembers.AnyAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct).ConfigureAwait(false);

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
        await JoinTeamAsync(teamId, memberId, ct).ConfigureAwait(false);
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
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class TeamInviteService(IntercomDbContext db)
{
    public async Task<(TeamInviteEntity Invite, string PlainToken)?> CreateInviteAsync(
        string teamId,
        string createdByMemberId,
        string teamRole,
        TimeSpan ttl,
        int maxUses,
        CancellationToken ct)
    {
        if (!TeamRoles.All.Contains(teamRole) || string.Equals(teamRole, TeamRoles.Agent, StringComparison.Ordinal))
            return null;

        var plain = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var invite = new TeamInviteEntity
        {
            InviteId = Guid.NewGuid().ToString("N"),
            TeamId = teamId,
            TokenHash = JwtTokenService.HashToken(plain),
            TeamRole = teamRole,
            MaxUses = Math.Max(1, maxUses),
            UseCount = 0,
            CreatedByMemberId = createdByMemberId,
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(ttl),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.TeamInvites.Add(invite);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (invite, plain);
    }

    public async Task<(string TeamId, string TeamRole)?> TryConsumeInviteAsync(
        string teamId,
        string plainToken,
        CancellationToken ct)
    {
        var hash = JwtTokenService.HashToken(plainToken);
        var invite = await db.TeamInvites
            .FirstOrDefaultAsync(
                x => x.TeamId == teamId && x.TokenHash == hash && x.ExpiresAtUtc >= DateTimeOffset.UtcNow,
                ct)
            .ConfigureAwait(false);

        if (invite is null || invite.UseCount >= invite.MaxUses)
            return null;

        invite.UseCount++;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (invite.TeamId, invite.TeamRole);
    }

    public async Task<bool> IsValidInviteAsync(string teamId, string plainToken, CancellationToken ct)
    {
        var hash = JwtTokenService.HashToken(plainToken);
        return await db.TeamInvites.AsNoTracking()
            .AnyAsync(
                x => x.TeamId == teamId
                    && x.TokenHash == hash
                    && x.ExpiresAtUtc >= DateTimeOffset.UtcNow
                    && x.UseCount < x.MaxUses,
                ct)
            .ConfigureAwait(false);
    }
}

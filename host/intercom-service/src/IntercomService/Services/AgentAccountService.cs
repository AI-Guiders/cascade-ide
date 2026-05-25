using IntercomService.Contracts;
using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class AgentAccountService(IntercomDbContext db)
{
    public async Task<AgentDto?> ProvisionAsync(
        string teamId,
        string displayName,
        string? avatarGlyph,
        CancellationToken ct)
    {
        var memberId = $"agent:{Guid.NewGuid():N}";
        var member = new MemberEntity
        {
            MemberId = memberId,
            Issuer = "agent",
            Subject = memberId,
            DisplayName = displayName.Trim(),
            MemberKind = MemberKinds.Agent,
            AvatarGlyph = avatarGlyph,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Members.Add(member);
        db.TeamMembers.Add(new TeamMemberEntity
        {
            TeamId = teamId,
            MemberId = memberId,
            TeamRole = TeamRoles.Agent,
            JoinedAtUtc = DateTimeOffset.UtcNow,
        });

        var plain = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        db.AgentCredentials.Add(new AgentCredentialEntity
        {
            CredentialId = Guid.NewGuid().ToString("N"),
            MemberId = memberId,
            TokenHash = JwtTokenService.HashToken(plain),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AgentDto(memberId, member.DisplayName, avatarGlyph, plain);
    }

    public async Task<bool> PatchAsync(
        string teamId,
        string memberId,
        PatchAgentRequest body,
        bool isAdmin,
        CancellationToken ct)
    {
        var membership = await db.TeamMembers
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.MemberId == memberId, ct)
            .ConfigureAwait(false);

        if (membership?.Member is null || !string.Equals(membership.Member.MemberKind, MemberKinds.Agent, StringComparison.Ordinal))
            return false;

        if (!isAdmin && !string.Equals(membership.MemberId, memberId, StringComparison.Ordinal))
            return false;

        if (!string.IsNullOrWhiteSpace(body.DisplayName))
            membership.Member.DisplayName = body.DisplayName.Trim();

        if (body.AvatarGlyph is not null)
            membership.Member.AvatarGlyph = string.IsNullOrWhiteSpace(body.AvatarGlyph) ? null : body.AvatarGlyph.Trim();

        if (body.RevokeCredentials)
        {
            var creds = await db.AgentCredentials
                .Where(x => x.MemberId == memberId && x.RevokedAtUtc == null)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            foreach (var c in creds)
                c.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<MemberEntity?> AuthenticateAgentCredentialAsync(string plainToken, CancellationToken ct)
    {
        var hash = JwtTokenService.HashToken(plainToken);
        var cred = await db.AgentCredentials
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAtUtc == null, ct)
            .ConfigureAwait(false);

        if (cred?.Member is null)
            return null;

        if (cred.ExpiresAtUtc is not null && cred.ExpiresAtUtc < DateTimeOffset.UtcNow)
            return null;

        return cred.Member;
    }
}

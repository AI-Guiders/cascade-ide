using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IntercomService.Contracts;
using IntercomService.Data;
using IntercomService.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IntercomService.Services;

public sealed class JwtTokenService(
    IntercomDbContext db,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<TokenResponse> IssueTokensAsync(MemberEntity member, CancellationToken ct)
    {
        var access = CreateAccessToken(member);
        var refreshPlain = GenerateRefreshToken();
        var refreshHash = HashToken(refreshPlain);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            MemberId = member.MemberId,
            TokenHash = refreshHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TokenResponse(access, refreshPlain, _jwt.AccessTokenMinutes * 60);
    }

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var row = await db.RefreshTokens
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAtUtc == null, ct)
            .ConfigureAwait(false);

        if (row?.Member is null || row.ExpiresAtUtc < DateTimeOffset.UtcNow)
            return null;

        row.RevokedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await IssueTokensAsync(row.Member, ct).ConfigureAwait(false);
    }

    public async Task RevokeRefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var row = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct).ConfigureAwait(false);
        if (row is null)
            return;
        row.RevokedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public string CreateAccessToken(MemberEntity member)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, member.MemberId),
            new Claim("display_name", member.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

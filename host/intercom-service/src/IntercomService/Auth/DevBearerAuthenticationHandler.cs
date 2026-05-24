using System.Security.Claims;
using System.Text.Encodings.Web;
using IntercomService.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IntercomService.Auth;

public sealed class DevBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<DevAuthOptions> devOptions,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment())
            return Task.FromResult(AuthenticateResult.NoResult());

        var dev = devOptions.Value;
        if (string.IsNullOrWhiteSpace(dev.TeamToken))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Request.Headers.TryGetValue("Authorization", out var header))
            return Task.FromResult(AuthenticateResult.NoResult());

        var value = header.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = value["Bearer ".Length..].Trim();
        if (!string.Equals(token, dev.TeamToken, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid dev team token"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, dev.MemberId),
            new Claim("display_name", dev.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

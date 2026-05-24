namespace IntercomService.Options;

public sealed class IntercomOptions
{
    public const string SectionName = "Intercom";

    public string DataDirectory { get; set; } = "data";

    public string DatabaseFileName { get; set; } = "intercom.witdb";
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "intercom-service";

    public string Audience { get; set; } = "intercom-api";

    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 30;
}

public sealed class GitHubAuthOptions
{
    public const string SectionName = "GitHub";

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";
}

public sealed class DevAuthOptions
{
    public const string SectionName = "DevAuth";

    public string? TeamToken { get; set; }

    public string MemberId { get; set; } = "dev-member";

    public string DisplayName { get; set; } = "Dev Operator";
}

public sealed class OidcAuthOptions
{
    public const string SectionName = "Oidc";

    public string Authority { get; set; } = "";

    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";

    public string Scopes { get; set; } = "openid profile email";
}

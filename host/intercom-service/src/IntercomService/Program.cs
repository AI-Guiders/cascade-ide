using System.Text;
using IntercomService.Auth;
using IntercomService.Data;
using IntercomService.Endpoints;
using IntercomService.Options;
using IntercomService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OutWit.Database.EntityFramework.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IntercomOptions>(builder.Configuration.GetSection(IntercomOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GitHubAuthOptions>(builder.Configuration.GetSection(GitHubAuthOptions.SectionName));
builder.Services.Configure<OidcAuthOptions>(builder.Configuration.GetSection(OidcAuthOptions.SectionName));
builder.Services.Configure<DevAuthOptions>(builder.Configuration.GetSection(DevAuthOptions.SectionName));

var intercom = builder.Configuration.GetSection(IntercomOptions.SectionName).Get<IntercomOptions>() ?? new IntercomOptions();
var dataDir = Path.IsPathRooted(intercom.DataDirectory)
    ? intercom.DataDirectory
    : Path.Combine(builder.Environment.ContentRootPath, intercom.DataDirectory);
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, intercom.DatabaseFileName);

builder.Services.AddDbContext<IntercomDbContext>(options =>
    options.UseWitDb($"Data Source={dbPath}"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<SseEventHub>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<TransportEventService>();
builder.Services.AddScoped<TeamInviteService>();
builder.Services.AddScoped<AgentAccountService>();
builder.Services.AddScoped<WorkspaceResolveService>();
builder.Services.AddSingleton<AuthProviderRegistry>();
builder.Services.AddScoped<TeamMembershipService>();
builder.Services.AddScoped<GitHubAuthService>();
builder.Services.AddScoped<OidcAuthService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
        jwt.SigningKey = "dev-signing-key-change-me-32chars-min!!";
    else
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "Smart", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            if (builder.Environment.IsDevelopment()
                && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = header["Bearer ".Length..].Trim();
                var dev = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<DevAuthOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(dev.TeamToken)
                    && string.Equals(token, dev.TeamToken, StringComparison.Ordinal))
                    return DevBearerAuthenticationHandler.SchemeName;
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevBearerAuthenticationHandler>(
        DevBearerAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IntercomDbContext>();
    var intercomOpts = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntercomOptions>>().Value;
    if (intercomOpts.RecreateDatabaseOnStart)
        db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapIntercomApi();

app.Run();

public partial class Program;

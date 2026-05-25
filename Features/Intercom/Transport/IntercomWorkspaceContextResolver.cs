using CascadeIDE.Models;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Resolve team_id: hint → API → manifest strangler (ADR 0144 §2.3.1).</summary>
public static class IntercomWorkspaceContextResolver
{
    public static async Task<IntercomWorkspaceResolveResult> ResolveAsync(
        IntercomTransportSettings transport,
        string? workspaceRoot,
        string? bearerToken,
        IntercomTransportApiClient api,
        CancellationToken ct)
    {
        var repoKey = IntercomWorkspaceGitRemoteResolver.TryGetNormalizedOrigin(workspaceRoot);
        if (string.IsNullOrWhiteSpace(transport.TeamId))
        {
            if (!string.IsNullOrWhiteSpace(repoKey)
                && transport.WorkspaceHints.TryGetValue(repoKey, out var hint)
                && !string.IsNullOrWhiteSpace(hint.TeamId))
            {
                return IntercomWorkspaceResolveResult.FromHint(hint.TeamId, repoKey, hint.Source);
            }
        }

        if (!string.IsNullOrWhiteSpace(bearerToken) && !string.IsNullOrWhiteSpace(repoKey))
        {
            var ctx = await api.ResolveWorkspaceContextAsync(repoKey, bearerToken, ct).ConfigureAwait(false);
            if (ctx is not null)
            {
                var teamId = !string.IsNullOrWhiteSpace(transport.TeamId)
                    ? transport.TeamId.Trim()
                    : ctx.SuggestedTeamId ?? ctx.Teams.FirstOrDefault()?.TeamId ?? "";

                if (!string.IsNullOrWhiteSpace(teamId))
                {
                    transport.WorkspaceHints[repoKey] = new IntercomWorkspaceHintEntry
                    {
                        TeamId = teamId,
                        ProjectId = ctx.Teams.FirstOrDefault(x => x.TeamId == teamId)?.ProjectId ?? "",
                        UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                        Source = "resolve",
                    };
                    return IntercomWorkspaceResolveResult.FromResolve(teamId, repoKey);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(transport.TeamId))
        {
            var manifest = IntercomTeamManifestResolver.TryResolve(workspaceRoot);
            if (manifest is not null && !string.IsNullOrWhiteSpace(manifest.TeamId))
            {
                if (!string.IsNullOrWhiteSpace(repoKey))
                {
                    transport.WorkspaceHints[repoKey] = new IntercomWorkspaceHintEntry
                    {
                        TeamId = manifest.TeamId,
                        UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                        Source = "manifest_strangler",
                    };
                }

                return IntercomWorkspaceResolveResult.FromManifest(manifest.TeamId, repoKey);
            }
        }

        var explicitTeam = transport.TeamId.Trim();
        if (!string.IsNullOrWhiteSpace(explicitTeam))
            return IntercomWorkspaceResolveResult.FromSettings(explicitTeam, repoKey);

        return IntercomWorkspaceResolveResult.NotFound(repoKey);
    }

    public static void InvalidateStaleHints(IntercomTransportSettings transport, IntercomMeResponseDto me)
    {
        var valid = me.Teams.Select(x => x.TeamId).ToHashSet(StringComparer.Ordinal);
        var stale = transport.WorkspaceHints
            .Where(kv => !valid.Contains(kv.Value.TeamId))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in stale)
            transport.WorkspaceHints.Remove(key);
    }
}

public sealed record IntercomWorkspaceResolveResult(
    bool Found,
    string TeamId,
    string? RepoKey,
    string Source)
{
    public static IntercomWorkspaceResolveResult FromHint(string teamId, string? repoKey, string source) =>
        new(true, teamId, repoKey, source);

    public static IntercomWorkspaceResolveResult FromResolve(string teamId, string repoKey) =>
        new(true, teamId, repoKey, "resolve");

    public static IntercomWorkspaceResolveResult FromManifest(string teamId, string? repoKey) =>
        new(true, teamId, repoKey, "manifest_strangler");

    public static IntercomWorkspaceResolveResult FromSettings(string teamId, string? repoKey) =>
        new(true, teamId, repoKey, "settings");

    public static IntercomWorkspaceResolveResult NotFound(string? repoKey) =>
        new(false, "", repoKey, "");
}

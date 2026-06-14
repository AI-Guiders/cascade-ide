#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Features.Forge.Lens;

/// <summary>Forge Lens write: create issue / MR via HTTP API (ADR 0158, release gate B).</summary>
public static class ForgeLensWriteClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<(bool Ok, string Message)> CreateIssueAsync(
        string baseUrl,
        string repo,
        string? apiToken,
        string title,
        string? body,
        IReadOnlyList<ForgeLensAnchorPayload>? anchors,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?> { ["title"] = title };
        if (!string.IsNullOrWhiteSpace(body))
            payload["body"] = body;
        if (anchors is { Count: > 0 })
            payload["anchors"] = anchors;

        return await PostAsync(baseUrl, repo, apiToken, "issues", payload, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<(bool Ok, string Message)> CreateMergeRequestAsync(
        string baseUrl,
        string repo,
        string? apiToken,
        string title,
        string sourceBranch,
        string? targetBranch,
        IReadOnlyList<ForgeLensAnchorPayload>? anchors,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["sourceBranch"] = sourceBranch,
        };
        if (!string.IsNullOrWhiteSpace(targetBranch))
            payload["targetBranch"] = targetBranch;
        if (anchors is { Count: > 0 })
            payload["anchors"] = anchors;

        return await PostAsync(baseUrl, repo, apiToken, "merge-requests", payload, cancellationToken).ConfigureAwait(false);
    }

    public static ForgeLensWriteContext? TryResolveContext(string? workspaceRoot, string? baseUrlArg, string? repoArg)
    {
        var toml = RepositoryWorkspaceTomlLoader.TryLoad(workspaceRoot);
        var forge = toml?.Workspace?.Forge;
        var baseUrl = (baseUrlArg ?? forge?.BaseUrl ?? "").Trim().TrimEnd('/');
        var repo = (repoArg ?? forge?.Repo ?? "").Trim();
        if (baseUrl.Length == 0 || repo.Length == 0)
            return null;

        var token = ForgeLensCredentialResolver.ResolveApiToken(baseUrl, forge?.ApiTokenEnv);
        return new ForgeLensWriteContext(baseUrl, repo, token);
    }

    private static async Task<(bool Ok, string Message)> PostAsync(
        string baseUrl,
        string repo,
        string? apiToken,
        string resource,
        Dictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
        if (!string.IsNullOrEmpty(apiToken))
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

        var path = $"/api/v1/repos/{Uri.EscapeDataString(repo)}/{resource}";
        using var response = await http.PostAsJsonAsync(path, payload, JsonOptions, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return (true, FormatSuccess(resource, body));

        return (false, FormatError(response.StatusCode, body));
    }

    private static string FormatSuccess(string resource, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (resource == "issues" && root.TryGetProperty("number", out var num) && root.TryGetProperty("issueUrl", out var url))
                return $"Issue #{num.GetInt32()} → {url.GetString()}";
            if (resource == "merge-requests" && root.TryGetProperty("number", out var mrNum) && root.TryGetProperty("mrUrl", out var mrUrl))
                return $"MR !{mrNum.GetInt32()} → {mrUrl.GetString()}";
        }
        catch
        {
            // fall through
        }

        return json;
    }

    private static string FormatError(System.Net.HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return $"{(int)status}: {detail.GetString()}";
        }
        catch
        {
            // fall through
        }

        return $"{(int)status}: {body}";
    }
}

public sealed record ForgeLensWriteContext(string BaseUrl, string Repo, string? ApiToken);

public sealed record ForgeLensAnchorPayload(string File, int LineStart, int? LineEnd = null, string? MemberKey = null);

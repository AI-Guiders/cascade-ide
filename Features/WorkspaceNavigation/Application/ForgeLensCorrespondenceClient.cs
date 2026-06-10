#nullable enable

using System.Net.Http;
using System.Text.Json;
using CascadeIDE.Features.Workspace;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Forge Lens: issues/MR с code anchors для CRS (ADR 0158, FORGE-ADR-0003).</summary>
public static class ForgeLensCorrespondenceClient
{
    public const string Provenance = "forge_lens";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<ForgeLensCorrespondenceItem> TryLoadForFile(
        RepositoryWorkspaceToml? workspaceToml,
        string workspaceRoot,
        string repoRelativeFile)
    {
        var config = ResolveConfig(workspaceToml, workspaceRoot);
        if (config is null)
            return [];

        var normalizedFile = repoRelativeFile.Replace('\\', '/').Trim();
        if (normalizedFile.Length == 0)
            return [];

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(config.BaseUrl), Timeout = TimeSpan.FromSeconds(2) };
            var apiToken = ForgeLensCredentialResolver.ResolveApiToken(config.BaseUrl, config.ApiTokenEnv);
            if (!string.IsNullOrEmpty(apiToken))
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

            var path = $"/api/v1/repos/{Uri.EscapeDataString(config.Repo)}/lens?file={Uri.EscapeDataString(normalizedFile)}";
            using var response = http.GetAsync(path).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return [];

            using var stream = response.Content.ReadAsStream();
            var lens = JsonSerializer.Deserialize<ForgeLensApiResponse>(stream, JsonOptions);
            if (lens is null)
                return [];

            var items = new List<ForgeLensCorrespondenceItem>();
            foreach (var issue in lens.Issues)
            {
                items.Add(new ForgeLensCorrespondenceItem(
                    $"Issue #{issue.Number}: {issue.Title}",
                    issue.IssueUrl,
                    issue.Status,
                    issue.Number,
                    IsMergeRequest: false));
            }

            foreach (var mr in lens.MergeRequests)
            {
                items.Add(new ForgeLensCorrespondenceItem(
                    $"MR !{mr.Number}: {mr.Title}",
                    mr.MrUrl,
                    mr.Status,
                    mr.Number,
                    IsMergeRequest: true));
            }

            return items;
        }
        catch
        {
            return [];
        }
    }

    private static ForgeLensConfig? ResolveConfig(RepositoryWorkspaceToml? workspaceToml, string workspaceRoot)
    {
        _ = workspaceRoot;
        var fromToml = workspaceToml?.Workspace?.Forge;
        var baseUrl = (fromToml?.BaseUrl ?? "").Trim().TrimEnd('/');
        var repo = (fromToml?.Repo ?? "").Trim();
        if (baseUrl.Length == 0 || repo.Length == 0)
            return null;

        return new ForgeLensConfig(baseUrl, repo, fromToml?.ApiTokenEnv);
    }

    private sealed record ForgeLensConfig(string BaseUrl, string Repo, string? ApiTokenEnv);

    private sealed class ForgeLensApiResponse
    {
        public List<ForgeLensIssueDto> Issues { get; set; } = [];
        public List<ForgeLensMrDto> MergeRequests { get; set; } = [];
    }

    private sealed class ForgeLensIssueDto
    {
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public string IssueUrl { get; set; } = "";
    }

    private sealed class ForgeLensMrDto
    {
        public int Number { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public string MrUrl { get; set; } = "";
    }
}

public sealed record ForgeLensCorrespondenceItem(
    string DisplayTitle,
    string Url,
    string Status,
    int Number,
    bool IsMergeRequest);

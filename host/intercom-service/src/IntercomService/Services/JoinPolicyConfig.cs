using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntercomService.Services;

public sealed class JoinPolicyConfig
{
    [JsonPropertyName("github_orgs")]
    public List<string> GitHubOrgs { get; set; } = [];

    [JsonPropertyName("github_repos")]
    public List<string> GitHubRepos { get; set; } = [];

    public static JoinPolicyConfig Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new JoinPolicyConfig();

        try
        {
            return JsonSerializer.Deserialize<JoinPolicyConfig>(json, IntercomService.Contracts.IntercomJson.Web)
                ?? new JoinPolicyConfig();
        }
        catch (JsonException)
        {
            return new JoinPolicyConfig();
        }
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, IntercomService.Contracts.IntercomJson.Web);
}

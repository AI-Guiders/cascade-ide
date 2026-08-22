#nullable enable

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AIGuiders.Platform.CommandPlane;

namespace CascadeIDE.Features.Forge.Infrastructure;

/// <summary>Runtime import of forge DOI slash catalog (FORGE-ADR-0015 Phase D).</summary>
public static class ForgeCapabilitiesClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<IReadOnlyList<SlashCommandDescriptor>> FetchCommandsAsync(
        string baseUrl,
        string? apiToken,
        CancellationToken cancellationToken = default)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        using var http = new HttpClient { BaseAddress = new Uri(normalized), Timeout = TimeSpan.FromSeconds(15) };
        if (!string.IsNullOrEmpty(apiToken))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        using var response = await http.GetAsync("/api/v1/capabilities", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("commands", out var commandsElement)
            || commandsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<SlashCommandDescriptor>(commandsElement.GetArrayLength());
        foreach (var item in commandsElement.EnumerateArray())
        {
            var command = item.Deserialize<SlashCommandDescriptor>(JsonOptions);
            if (command is null || string.IsNullOrWhiteSpace(command.CommandId))
                continue;
            list.Add(command);
        }

        return list;
    }
}

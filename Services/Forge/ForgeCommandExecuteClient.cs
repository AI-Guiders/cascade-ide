#nullable enable

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CascadeIDE.Services.Forge;

public static class ForgeCommandExecuteClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<(bool Ok, string Message)> ExecuteAsync(
        string baseUrl,
        string? apiToken,
        string slashPath,
        string? argsTail,
        string repo,
        CancellationToken cancellationToken = default)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        using var http = new HttpClient { BaseAddress = new Uri(normalized), Timeout = TimeSpan.FromSeconds(30) };
        if (!string.IsNullOrEmpty(apiToken))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Add("X-Forge-Command-Client", "cide-slash");

        var payload = new
        {
            path = slashPath,
            args = argsTail ?? "",
            context = new Dictionary<string, string> { ["repo"] = repo },
        };

        using var response = await http.PostAsJsonAsync("/api/v1/commands/execute", payload, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return (false, FormatError(body, response.StatusCode));

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var kind = root.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null;
            if (string.Equals(kind, "redirect", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("redirectUrl", out var redirectEl))
            {
                var redirectUrl = redirectEl.GetString() ?? "";
                if (redirectUrl.StartsWith('/'))
                    redirectUrl = normalized + redirectUrl;

                if (!ForgeLensOpenService.TryOpenExternal(redirectUrl, out var openError))
                    return (false, openError);

                return (true, $"Opened {redirectUrl}");
            }

            if (root.TryGetProperty("body", out var bodyEl))
                return (true, FormatJsonBody(bodyEl));

            if (root.TryGetProperty("error", out var errorEl))
                return (false, errorEl.GetString() ?? "Command failed.");

            return (true, body);
        }
        catch (JsonException)
        {
            return response.IsSuccessStatusCode ? (true, body) : (false, body);
        }
    }

    private static string FormatJsonBody(JsonElement bodyEl)
    {
        if (bodyEl.ValueKind == JsonValueKind.String)
            return bodyEl.GetString() ?? "";

        if (bodyEl.TryGetProperty("issueUrl", out var issueUrl))
            return issueUrl.GetString() ?? bodyEl.ToString();

        if (bodyEl.TryGetProperty("mrUrl", out var mrUrl))
            return mrUrl.GetString() ?? bodyEl.ToString();

        return bodyEl.ToString();
    }

    private static string FormatError(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
                return error.GetString() ?? $"{(int)status}";
        }
        catch
        {
            // fall through
        }

        return string.IsNullOrWhiteSpace(body) ? $"{(int)status}" : body;
    }
}

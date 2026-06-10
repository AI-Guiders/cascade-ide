using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Forge Lens connect: OAuth first, device fallback (FORGE-ADR-0011 / 0010).</summary>
public sealed class ForgeLensDeviceConnectService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ForgeLensOAuthConnectService _oauth = new();

    public async Task<(bool Ok, string Message)> ConnectAsync(string baseUrl, CancellationToken ct = default)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
            return (false, "Некорректный base_url.");

        var (oauthOk, oauthMessage) = await _oauth.TryConnectAsync(normalized, ct).ConfigureAwait(false);
        if (oauthOk)
            return (true, oauthMessage);

        if (!oauthMessage.Contains("не настроен", StringComparison.OrdinalIgnoreCase))
            return (false, oauthMessage);

        using var http = new HttpClient { BaseAddress = new Uri(normalized + "/"), Timeout = TimeSpan.FromMinutes(6) };

        using var beginResponse = await http.PostAsJsonAsync(
            "/api/v1/auth/device",
            new { clientName = "cascade-ide", scopes = "read,write,accept_merge" },
            ct).ConfigureAwait(false);
        if (!beginResponse.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(beginResponse, ct).ConfigureAwait(false));

        using var beginDoc = JsonDocument.Parse(await beginResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var root = beginDoc.RootElement;
        var deviceCode = root.GetProperty("device_code").GetString()!;
        var userCode = root.GetProperty("user_code").GetString()!;
        var verificationUri = root.GetProperty("verification_uri").GetString()!;
        var interval = root.TryGetProperty("interval", out var intervalElement) ? intervalElement.GetInt32() : 5;
        var expiresIn = root.TryGetProperty("expires_in", out var expiresElement) ? expiresElement.GetInt32() : 900;

        try
        {
            Process.Start(new ProcessStartInfo(verificationUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            return (false, "Не удалось открыть браузер: " + ex.Message);
        }

        var deadline = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, interval)), ct).ConfigureAwait(false);

            using var pollResponse = await http.PostAsJsonAsync(
                "/api/v1/auth/device/token",
                new { deviceCode },
                ct).ConfigureAwait(false);

            if (pollResponse.IsSuccessStatusCode)
            {
                using var pollDoc = JsonDocument.Parse(await pollResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
                var token = pollDoc.RootElement.GetProperty("access_token").GetString()!;
                var tokenName = pollDoc.RootElement.TryGetProperty("token_name", out var nameElement)
                    ? nameElement.GetString()
                    : null;
                ForgeLensSecretsStorage.SetToken(normalized, token, tokenName);
                return (true, $"Forge Lens: вход выполнен ({tokenName ?? userCode}).");
            }

            using var errorDoc = JsonDocument.Parse(await pollResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var error = errorDoc.RootElement.TryGetProperty("error", out var errorElement)
                ? errorElement.GetString()
                : null;
            if (error is not ("authorization_pending" or "slow_down"))
                return (false, $"Ошибка device login: {error ?? pollResponse.ReasonPhrase}");
        }

        return (false, "Таймаут device login. Подтверди код на forge или выполни forge auth approve " + userCode + ".");
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("detail", out var detail)
                ? detail.GetString() ?? response.ReasonPhrase ?? "error"
                : body;
        }
        catch
        {
            return string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "error" : body;
        }
    }
}

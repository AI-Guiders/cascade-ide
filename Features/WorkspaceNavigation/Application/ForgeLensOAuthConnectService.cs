using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CascadeIDE.Services;
using CascadeIDE.Services.Forge;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>OAuth Sign in to forge (FORGE-ADR-0011, Intercom ADR 0144 pattern).</summary>
public sealed class ForgeLensOAuthConnectService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<(bool Ok, string Message)> TryConnectAsync(string baseUrl, CancellationToken ct = default)
    {
        if (!await HasProvidersAsync(baseUrl, ct).ConfigureAwait(false))
            return (false, "OAuth не настроен на forge.");

        var normalized = baseUrl.Trim().TrimEnd('/');
        var verifier = GenerateCodeVerifier();
        var challenge = ComputeS256Challenge(verifier);
        var port = FindFreePort();
        var redirectUri = $"http://127.0.0.1:{port}/callback";

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var loginUrl =
            $"{normalized}/api/v1/auth/login" +
            $"?provider=github" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256" +
            "&scopes=read%2Cwrite%2Caccept_merge";

        try
        {
            Process.Start(new ProcessStartInfo(loginUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            listener.Stop();
            return (false, "Не удалось открыть браузер: " + ex.Message);
        }

        using var http = new HttpClient { BaseAddress = new Uri(normalized + "/"), Timeout = TimeSpan.FromMinutes(6) };
        var waitTask = WaitForCallbackAsync(listener, http, redirectUri, verifier, ct);
        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMinutes(5), ct)).ConfigureAwait(false);
        listener.Stop();

        if (completed != waitTask)
            return (false, "Таймаут OAuth (5 мин).");

        var (ok, token, tokenName, error) = await waitTask.ConfigureAwait(false);
        if (!ok || string.IsNullOrEmpty(token))
            return (false, string.IsNullOrWhiteSpace(error) ? "OAuth не удался." : error);

        ForgeLensSecretsStorage.SetToken(normalized, token, tokenName);
        await ForgeSlashCatalogRefresh.RefreshAfterConnectAsync(normalized, ct).ConfigureAwait(false);
        return (true, $"Forge Lens: OAuth ({tokenName ?? "github"}).");
    }

    private static async Task<bool> HasProvidersAsync(string baseUrl, CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(3) };
            using var response = await http.GetAsync("/api/v1/auth/providers", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return false;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            return doc.RootElement.TryGetProperty("providers", out var providers) && providers.GetArrayLength() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(bool Ok, string? Token, string? TokenName, string Error)> WaitForCallbackAsync(
        HttpListener listener,
        HttpClient http,
        string redirectUri,
        string codeVerifier,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var ctx = await listener.GetContextAsync().ConfigureAwait(false);
            var query = ParseQuery(ctx.Request.Url?.Query);
            query.TryGetValue("code", out var code);
            query.TryGetValue("state", out var state);
            query.TryGetValue("error", out var error);

            var body = !string.IsNullOrWhiteSpace(error)
                ? $"<html><body><p>Ошибка: {WebUtility.HtmlEncode(error)}</p></body></html>"
                : "<html><body><p>Signed in to Forge. Close this window.</p></body></html>";
            var bytes = Encoding.UTF8.GetBytes(body);
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
            ctx.Response.Close();

            if (!string.IsNullOrWhiteSpace(error))
                return (false, null, null, error);

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return (false, null, null, "В callback нет code/state.");

            using var tokenResponse = await http.PostAsJsonAsync(
                "/api/v1/auth/token",
                new
                {
                    grant_type = "authorization_code",
                    code,
                    state,
                    code_verifier = codeVerifier,
                    redirect_uri = redirectUri,
                },
                ct).ConfigureAwait(false);

            if (!tokenResponse.IsSuccessStatusCode)
                return (false, null, null, await tokenResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));

            using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var token = doc.RootElement.GetProperty("access_token").GetString();
            var tokenName = doc.RootElement.TryGetProperty("token_name", out var nameEl) ? nameEl.GetString() : null;
            return string.IsNullOrEmpty(token)
                ? (false, null, null, "Пустой access_token.")
                : (true, token, tokenName, "");
        }

        return (false, null, null, "Отменено.");
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return result;
        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            var key = eq < 0 ? part : part[..eq];
            var val = eq < 0 ? "" : part[(eq + 1)..];
            result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(val);
        }

        return result;
    }

    private static int FindFreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeS256Challenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

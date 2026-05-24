using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Connect Intercom: system browser + loopback callback (ADR 0144 §8).</summary>
public sealed class IntercomOAuthConnectService(IntercomTransportApiClient api)
{
    public Task<(bool Ok, string Error)> ConnectAsync(
        string baseUrl,
        string teamId,
        string provider,
        CancellationToken ct) =>
        ConnectCoreAsync(baseUrl, teamId, provider, ct);

    public async Task<(bool Ok, string Error)> ConnectGitHubAsync(
        string baseUrl,
        string teamId,
        CancellationToken ct) =>
        await ConnectCoreAsync(baseUrl, teamId, "github", ct).ConfigureAwait(false);

    private async Task<(bool Ok, string Error)> ConnectCoreAsync(
        string baseUrl,
        string teamId,
        string provider,
        CancellationToken ct)
    {
        api.ConfigureBaseUrl(baseUrl);
        var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? "github" : provider.Trim().ToLowerInvariant();

        var verifier = GenerateCodeVerifier();
        var challenge = ComputeS256Challenge(verifier);
        var port = FindFreePort();
        var redirectUri = $"http://127.0.0.1:{port}/callback";

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var loginUrl =
            $"{baseUrl.TrimEnd('/')}/api/v1/auth/login" +
            $"?provider={Uri.EscapeDataString(normalizedProvider)}&team_id={Uri.EscapeDataString(teamId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256";

        try
        {
            Process.Start(new ProcessStartInfo(loginUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            listener.Stop();
            return (false, "Не удалось открыть браузер: " + ex.Message);
        }

        var waitTask = WaitForCallbackAsync(listener, ct);
        var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMinutes(5), ct)).ConfigureAwait(false);
        listener.Stop();

        if (completed != waitTask)
            return (false, "Таймаут OAuth (5 мин).");

        var (ok, access, refresh, expiresIn, error) = await waitTask.ConfigureAwait(false);
        if (!ok)
            return (false, error);

        var secrets = IntercomTransportSecretsStorage.Load();
        secrets.AccessToken = access;
        secrets.RefreshToken = refresh;
        secrets.SetAccessExpiry(DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, expiresIn - 30)));
        IntercomTransportSecretsStorage.Save(secrets);

        var me = await api.GetMeAsync(access, ct).ConfigureAwait(false);
        if (me is not null)
        {
            secrets.MemberId = me.MemberId;
            secrets.DisplayName = me.DisplayName;
            IntercomTransportSecretsStorage.Save(secrets);
        }

        return (true, "");
    }

    private static async Task<(bool Ok, string Access, string Refresh, int ExpiresIn, string Error)> WaitForCallbackAsync(
        HttpListener listener,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var ctx = await listener.GetContextAsync().ConfigureAwait(false);
            var query = ParseQuery(ctx.Request.Url?.Query);
            query.TryGetValue("access_token", out var access);
            query.TryGetValue("refresh_token", out var refresh);
            query.TryGetValue("expires_in", out var expiresRaw);
            query.TryGetValue("error", out var error);

            var body = !string.IsNullOrWhiteSpace(error)
                ? $"<html><body><p>Ошибка: {WebUtility.HtmlEncode(error)}</p><p>Можно закрыть окно.</p></body></html>"
                : "<html><body><p>Intercom подключён. Можно закрыть окно и вернуться в CIDE.</p></body></html>";
            var bytes = Encoding.UTF8.GetBytes(body);
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
            ctx.Response.Close();

            if (!string.IsNullOrWhiteSpace(error))
                return (false, "", "", 0, error);

            if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh))
                return (false, "", "", 0, "В callback нет токенов.");

            _ = int.TryParse(expiresRaw, out var expiresIn);
            if (expiresIn <= 0)
                expiresIn = 3600;

            return (true, access, refresh, expiresIn, "");
        }

        return (false, "", "", 0, "Отменено.");
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

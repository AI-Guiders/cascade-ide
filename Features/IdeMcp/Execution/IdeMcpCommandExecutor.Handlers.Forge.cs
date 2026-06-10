using System.Text.Json;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Execution;

internal sealed partial class IdeMcpCommandExecutor
{
    private static readonly ForgeLensDeviceConnectService ForgeLensConnect = new();

    private void RegisterForge(Action<string, Handler> add)
    {
        add(IdeCommands.ForgeLensConnect, async (args, ct) =>
        {
            var baseUrl = ResolveForgeBaseUrl(args);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "Error: укажи base_url или [workspace.forge] в .cascade/workspace.toml.";

            var (ok, message) = await ForgeLensConnect.ConnectAsync(baseUrl, ct).ConfigureAwait(false);
            return ok ? message : "Error: " + message;
        });

        add(IdeCommands.ForgeLensDisconnect, async (args, _) =>
        {
            var baseUrl = ResolveForgeBaseUrl(args);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "Error: base_url required.";

            return ForgeLensSecretsStorage.RemoveHost(baseUrl)
                ? $"Forge Lens: credentials removed for {baseUrl}."
                : $"Forge Lens: no credentials for {baseUrl}.";
        });

        add(IdeCommands.ForgeLensAuthStatus, async (args, _) =>
        {
            var baseUrl = ResolveForgeBaseUrl(args);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "Error: base_url required.";

            var cide = ForgeLensSecretsStorage.TryGetToken(baseUrl);
            if (!string.IsNullOrEmpty(cide))
                return $"Forge Lens: logged in (CIDE secrets) → {baseUrl}";

            var shared = ForgeSharedCredentialReader.TryGetToken(baseUrl);
            if (!string.IsNullOrEmpty(shared))
                return $"Forge Lens: logged in (~/.forge/credentials.json) → {baseUrl}";

            var (_, repo) = ForgeLensWorkspaceConfig.TryResolve(TryGetWorkspaceRoot(_actions));
            var envHint = repo is not null ? $" workspace repo={repo}" : "";
            return $"Forge Lens: not logged in for {baseUrl}.{envHint} Run forge_lens.connect or forge auth login.";
        });
    }

    private string? ResolveForgeBaseUrl(IReadOnlyDictionary<string, JsonElement>? args)
    {
        var fromArgs = McpCommandJsonArgs.String(args, "base_url")?.Trim();
        if (!string.IsNullOrWhiteSpace(fromArgs))
            return fromArgs.TrimEnd('/');

        return ForgeLensWorkspaceConfig.TryResolveBaseUrl(TryGetWorkspaceRoot(_actions));
    }
}

using System.Text.Json;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services;
using CascadeIDE.Services.Forge;

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
            if (ok)
                return message;

            return "Error: " + message;
        });

        add(IdeCommands.ForgeLensDisconnect, async (args, _) =>
        {
            var baseUrl = ResolveForgeBaseUrl(args);
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "Error: base_url required.";

            ForgeSlashCatalogOverlay.Clear(baseUrl);
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

        add(IdeCommands.ForgeLensCreateIssue, async (args, ct) =>
        {
            var title = McpCommandJsonArgs.String(args, "title")?.Trim() ?? "";
            if (title.Length == 0)
                return "Error: title required.";

            var ctx = ResolveForgeWriteContext(args);
            if (ctx is null)
                return "Error: укажи base_url+repo или [workspace.forge] в .cascade/workspace.toml.";

            var (ok, message) = await ForgeLensWriteClient.CreateIssueAsync(
                ctx.BaseUrl,
                ctx.Repo,
                ctx.ApiToken,
                title,
                McpCommandJsonArgs.String(args, "body"),
                BuildForgeAnchors(args),
                ct).ConfigureAwait(false);
            return ok ? message : "Error: " + message;
        });

        add(IdeCommands.ForgeLensCreateMergeRequest, async (args, ct) =>
        {
            var title = McpCommandJsonArgs.String(args, "title")?.Trim() ?? "";
            var sourceBranch = McpCommandJsonArgs.String(args, "source_branch")?.Trim() ?? "";
            if (title.Length == 0)
                return "Error: title required.";
            if (sourceBranch.Length == 0)
                return "Error: source_branch required.";

            var ctx = ResolveForgeWriteContext(args);
            if (ctx is null)
                return "Error: укажи base_url+repo или [workspace.forge] в .cascade/workspace.toml.";

            var (ok, message) = await ForgeLensWriteClient.CreateMergeRequestAsync(
                ctx.BaseUrl,
                ctx.Repo,
                ctx.ApiToken,
                title,
                sourceBranch,
                McpCommandJsonArgs.String(args, "target_branch"),
                BuildForgeAnchors(args),
                ct).ConfigureAwait(false);
            return ok ? message : "Error: " + message;
        });

        add(IdeCommands.ForgeArtifactGoto, async (args, _) =>
        {
            var bracket = McpCommandJsonArgs.String(args, "bracket")?.Trim() ?? "";
            if (bracket.Length == 0)
                return "Error: bracket required (e.g. [FRG:pilot/issues/1]).";

            if (!BracketForgeReferenceParser.TryParse(bracket, out var artifact, out var parseError))
                return "Error: " + parseError;

            var baseUrl = ResolveForgeBaseUrl(args);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var (cfgBase, _) = ForgeLensWorkspaceConfig.TryResolve(TryGetWorkspaceRoot(_actions));
                baseUrl = cfgBase;
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
                return "Error: base_url or [workspace.forge] required.";

            var viewUrl = ForgeLensOpenService.BuildViewUrl(baseUrl, artifact);
            if (!ForgeLensOpenService.TryOpenExternal(viewUrl, out var openError))
                return "Error: " + openError;

            var select = McpCommandJsonArgs.Bool(args, "select_code", defaultValue: true);
            if (!string.IsNullOrWhiteSpace(artifact.CodeBracket))
            {
                var ws = TryGetWorkspaceRoot(_actions);
                var solutionPath = TryGetAttachSolutionPath();
                var indexDir = CascadeIDE.Features.HybridIndex.Application.HybridIndexIndexDirectoryRelative.ResolveOrDefault(
                    _vm.GetCascadeSettingsForExecutor().HybridIndex.IndexDir);
                if (!ForgeLensOpenService.TryNavigateCodeTail(
                        artifact,
                        ws,
                        activeFilePath: null,
                        solutionPath,
                        indexDir,
                        _actions,
                        _vm.GetCascadeSettingsForExecutor().Intercom,
                        select,
                        out var navError))
                {
                    return $"Opened {viewUrl}. Code navigation failed: {navError}";
                }
            }

            return await Task.FromResult($"Opened {viewUrl}" + (artifact.CodeBracket is not null ? " and navigated to code anchor." : "."));
        });
    }

    private ForgeLensWriteContext? ResolveForgeWriteContext(IReadOnlyDictionary<string, JsonElement>? args) =>
        ForgeLensWriteClient.TryResolveContext(
            TryGetWorkspaceRoot(_actions),
            ResolveForgeBaseUrl(args),
            McpCommandJsonArgs.String(args, "repo"));

    private static IReadOnlyList<ForgeLensAnchorPayload>? BuildForgeAnchors(IReadOnlyDictionary<string, JsonElement>? args)
    {
        var file = McpCommandJsonArgs.String(args, "file_path")?.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(file))
            return null;

        var lineStart = McpCommandJsonArgs.Int(args, "line_start", 0);
        if (lineStart <= 0)
            return null;

        var lineEndRaw = McpCommandJsonArgs.Int(args, "line_end", 0);
        int? lineEnd = lineEndRaw > 0 ? lineEndRaw : null;
        var memberKey = McpCommandJsonArgs.String(args, "member_key");
        return [new ForgeLensAnchorPayload(file, lineStart, lineEnd, memberKey)];
    }

    private string? ResolveForgeBaseUrl(IReadOnlyDictionary<string, JsonElement>? args)
    {
        var fromArgs = McpCommandJsonArgs.String(args, "base_url")?.Trim();
        if (!string.IsNullOrWhiteSpace(fromArgs))
            return fromArgs.TrimEnd('/');

        return ForgeLensWorkspaceConfig.TryResolveBaseUrl(TryGetWorkspaceRoot(_actions));
    }
}

using System.Text.Json;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP: Intercom (reveal attachment) и editor bracket navigation (ADR 0131).</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterIntercom(Action<string, Handler> add)
    {
        add(Services.IdeCommands.IntercomRevealAttachment, async (args, ct) =>
        {
            var a = _actions;
            if (!IntercomRevealAttachmentMcpArgs.TryParse(args, out var anchor, out var selectExplicit, out var durationMs, out var err))
                return err;

            var workspaceRoot = TryGetWorkspaceRoot(a);
            var solutionPath = TryGetAttachSolutionPath();
            return await Task.FromResult(IntercomAttachmentNavigator.Apply(
                a,
                _vm.GetCascadeSettingsForExecutor().Intercom,
                workspaceRoot,
                anchor,
                selectExplicit,
                shiftSelect: false,
                durationMs,
                solutionPath));
        });

        add(Services.IdeCommands.EditorSelectCode, ExecuteEditorCodeRefNavigation(select: true));
        add(Services.IdeCommands.EditorRevealCode, ExecuteEditorCodeRefNavigation(select: false));

        add(Services.IdeCommands.IntercomMessagesForCode, async (args, _) =>
        {
            var a = _actions;
            return await a.FindIntercomMessagesForCodeAsync(args);
        });

        add(Services.IdeCommands.IntercomMessageRelate, async (args, _) =>
        {
            var a = _actions;
            return await a.RelateIntercomMessageRangeToCodeAsync(args);
        });

        add(Services.IdeCommands.IntercomConnectTeam, async (_, ct) =>
        {
            var (ok, message) = await _vm.ConnectIntercomTeamTransportAsync(ct).ConfigureAwait(false);
            return ok ? message : "Error: " + message;
        });

        add(Services.IdeCommands.IntercomDisconnectTeam, async (_, ct) =>
        {
            await _vm.DisconnectIntercomTeamTransportAsync(ct).ConfigureAwait(false);
            return "Intercom transport disconnected.";
        });

        add(Services.IdeCommands.IntercomServerStatus, async (_, ct) =>
        {
            var result = await _vm.RunIntercomAdminSlashAsync(
                CascadeIDE.Features.Chat.ChatSlashIntercomHandlers.Ids.ServerStatus,
                null,
                ct).ConfigureAwait(false);
            return result.Message;
        });

        add(Services.IdeCommands.IntercomServerStart, async (args, ct) =>
        {
            var url = args is not null
                && args.TryGetValue("base_url", out var el)
                && el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
            var result = await _vm.RunIntercomAdminSlashAsync(
                CascadeIDE.Features.Chat.ChatSlashIntercomHandlers.Ids.ServerStart,
                url,
                ct).ConfigureAwait(false);
            return result.Message;
        });

        add(Services.IdeCommands.IntercomServerStop, async (_, ct) =>
        {
            var result = await _vm.RunIntercomAdminSlashAsync(
                CascadeIDE.Features.Chat.ChatSlashIntercomHandlers.Ids.ServerStop,
                null,
                ct).ConfigureAwait(false);
            return result.Message;
        });

        add(Services.IdeCommands.IntercomTeamMembers, async (_, ct) =>
        {
            var result = await _vm.RunIntercomAdminSlashAsync(
                CascadeIDE.Features.Chat.ChatSlashIntercomHandlers.Ids.TeamMembers,
                null,
                ct).ConfigureAwait(false);
            return result.Message;
        });

        add(Services.IdeCommands.IntercomAgentProvision, async (args, ct) =>
        {
            if (args is null
                || !args.TryGetValue("display_name", out var nameEl)
                || nameEl.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(nameEl.GetString()))
                return "Error: display_name required.";

            var result = await _vm.RunIntercomAdminSlashAsync(
                CascadeIDE.Features.Chat.ChatSlashIntercomHandlers.Ids.AgentProvision,
                nameEl.GetString(),
                ct).ConfigureAwait(false);
            return result.Message;
        });
    }

    private Handler ExecuteEditorCodeRefNavigation(bool select) => async (args, ct) =>
    {
        var a = _actions;
        if (!EditorCodeRefMcpArgs.TryParse(args, out var codeRef, out var activeFile, out var durationMs, out var err))
            return err;

        if (!BracketCodeReferenceParser.TryParse(codeRef, out var reference, out err))
            return err;

        var workspaceRoot = TryGetWorkspaceRoot(a);
        var solutionPath = TryGetAttachSolutionPath();
        var indexDir = CascadeIDE.Features.HybridIndex.Application.HybridIndexIndexDirectoryRelative.ResolveOrDefault(
            _vm.GetCascadeSettingsForExecutor().HybridIndex.IndexDir);
        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(
                reference,
                activeFile,
                workspaceRoot,
                solutionPath,
                indexDir,
                out var anchor,
                out err))
        {
            return err;
        }

        return await Task.FromResult(IntercomAttachmentNavigator.Apply(
            a,
            _vm.GetCascadeSettingsForExecutor().Intercom,
            workspaceRoot,
            anchor,
            selectExplicit: select,
            shiftSelect: false,
            durationMs,
            solutionPath));
    };
}

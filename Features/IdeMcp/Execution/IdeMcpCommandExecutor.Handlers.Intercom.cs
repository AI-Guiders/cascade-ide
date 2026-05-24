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
        if (!BracketCodeReferenceParser.TryToAttachmentAnchor(reference, activeFile, workspaceRoot, out var anchor, out err))
            return err;

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

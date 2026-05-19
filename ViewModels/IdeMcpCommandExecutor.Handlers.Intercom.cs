using System.Text.Json;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: Intercom (reveal attachment).</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterIntercom(Action<string, Handler> add)
    {
        add(Services.IdeCommands.IntercomRevealAttachment, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (!IntercomRevealAttachmentMcpArgs.TryParse(args, out var anchor, out var select, out var err))
                return err;

            var workspaceRoot = tryGetWorkspaceRoot(a);
            var plan = IntercomAttachmentRevealPlan.Create(anchor, workspaceRoot);

            if (plan.ResolveOutcome == IntercomAttachmentRevealPlan.OutcomeFileMissing)
                return plan.Message;

            if (string.IsNullOrEmpty(plan.AbsoluteFilePath))
                return plan.Message;

            if (plan.Lines is { } range && !plan.OpenFileOnly)
            {
                if (select)
                {
                    a.SelectInEditor(plan.AbsoluteFilePath, range.Start.Value, 1, range.End.Value, 1);
                    return plan.Message.StartsWith("OK", StringComparison.Ordinal) ? "OK (select)" : plan.Message + " (select)";
                }

                a.RevealEditorRange(plan.AbsoluteFilePath, range.Start.Value, range.End.Value);
                return plan.Message;
            }

            a.OpenFile(plan.AbsoluteFilePath);
            return plan.Message;
        });
    }

    private static string? tryGetWorkspaceRoot(IIdeMcpActions actions)
    {
        try
        {
            var json = actions.GetSolutionInfo();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out _))
                return null;
            var sln = doc.RootElement.TryGetProperty("solution_path", out var sp) ? sp.GetString() : null;
            if (string.IsNullOrWhiteSpace(sln))
                return null;
            return WorkspaceDirectoryFromSolutionPath.Resolve(sln);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

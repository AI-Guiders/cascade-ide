using System.Text.Json;
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
            if (!IntercomRevealAttachmentMcpArgs.TryParse(args, out var anchor, out var select, out var durationMs, out var err))
                return err;

            var workspaceRoot = TryGetWorkspaceRoot(a);
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

                a.RevealEditorRange(plan.AbsoluteFilePath, range.Start.Value, range.End.Value, durationMs);
                return plan.Message;
            }

            a.OpenFile(plan.AbsoluteFilePath);
            return plan.Message;
        });
    }
}

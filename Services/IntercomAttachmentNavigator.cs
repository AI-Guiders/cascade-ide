#nullable enable

using CascadeIDE.Models;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services;

/// <summary>
/// Навигация по <see cref="AttachmentAnchor"/> в редакторе: reveal или select (ADR 0128 §8, 0130).
/// </summary>
public static class IntercomAttachmentNavigator
{
    public static string Apply(
        IIdeMcpActions actions,
        IntercomSettings settings,
        string? workspaceRoot,
        AttachmentAnchor anchor,
        bool? selectExplicit,
        bool shiftSelect,
        int? durationMs)
    {
        var select = shiftSelect
                     || selectExplicit == true
                     || (selectExplicit is null && settings.DefaultAttachmentNavigateSelects());

        var plan = IntercomAttachmentRevealPlan.Create(anchor, workspaceRoot);

        if (plan.ResolveOutcome == IntercomAttachmentRevealPlan.OutcomeFileMissing)
            return plan.Message;

        if (string.IsNullOrEmpty(plan.AbsoluteFilePath))
            return plan.Message;

        if (plan.Lines is { } range && !plan.OpenFileOnly)
        {
            if (select)
            {
                actions.SelectInEditor(plan.AbsoluteFilePath, range.Start.Value, 1, range.End.Value, 1);
                return plan.Message.StartsWith("OK", StringComparison.Ordinal)
                    ? "OK (select)"
                    : plan.Message + " (select)";
            }

            actions.RevealEditorRange(plan.AbsoluteFilePath, range.Start.Value, range.End.Value, durationMs);
            return plan.Message;
        }

        actions.OpenFile(plan.AbsoluteFilePath);
        return plan.Message;
    }
}

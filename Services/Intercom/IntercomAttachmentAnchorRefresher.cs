#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Services.Intercom;

/// <summary>
/// Пересчёт <see cref="AttachmentAnchor.ResolveOutcome"/> и строк после смены workspace/solution
/// (маркеры в ленте; reveal делает то же при клике — ADR 0128).
/// </summary>
public static class IntercomAttachmentAnchorRefresher
{
    public static bool NeedsRefresh(AttachmentAnchor anchor)
    {
        var outcome = anchor.ResolveOutcome?.Trim();
        if (string.Equals(outcome, IntercomAttachmentRevealPlan.OutcomeMemberNotFound, StringComparison.OrdinalIgnoreCase)
            || string.Equals(outcome, IntercomAttachmentRevealPlan.OutcomeFileMissing, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var hasMember = !string.IsNullOrWhiteSpace(anchor.MemberKey);
        AttachmentSyntaxScope.TryParse(anchor.SyntaxScope, out var syntaxScope);
        var hasScope = syntaxScope is not null;
        if (!hasMember && !hasScope)
            return false;

        if (string.IsNullOrWhiteSpace(outcome))
            return true;

        return string.Equals(outcome, IntercomAttachmentRevealPlan.OutcomeExcerptOnly, StringComparison.OrdinalIgnoreCase);
    }

    public static AttachmentAnchor Refresh(
        AttachmentAnchor anchor,
        string? workspaceRoot,
        string? solutionPath)
    {
        if (!NeedsRefresh(anchor))
            return anchor;

        var plan = IntercomAttachmentRevealPlan.Create(anchor, workspaceRoot, solutionPath);
        var updated = anchor with { ResolveOutcome = plan.ResolveOutcome };

        if (plan.Lines is { } lines)
        {
            updated = updated with
            {
                LineStart = lines.Start.Value,
                LineEnd = lines.End.Value,
            };
        }

        if (string.Equals(plan.ResolveOutcome, IntercomAttachmentRevealPlan.OutcomeResolved, StringComparison.OrdinalIgnoreCase))
        {
            updated = updated with
            {
                ResolvedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            };
        }

        return updated;
    }

    public static bool TryRefreshList(
        IReadOnlyList<AttachmentAnchor> anchors,
        string? workspaceRoot,
        string? solutionPath,
        out IReadOnlyList<AttachmentAnchor>? refreshed)
    {
        refreshed = null;
        if (anchors.Count == 0)
            return false;

        var list = new List<AttachmentAnchor>(anchors.Count);
        var changed = false;
        foreach (var anchor in anchors)
        {
            var next = Refresh(anchor, workspaceRoot, solutionPath);
            list.Add(next);
            if (!anchor.Equals(next))
                changed = true;
        }

        if (!changed)
            return false;

        refreshed = list;
        return true;
    }
}

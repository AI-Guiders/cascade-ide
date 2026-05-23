#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>
    /// После открытия решения / перезагрузки сессии: обновить resolveOutcome у вложений с ошибкой или без resolve.
    /// </summary>
    public void RefreshAttachmentAnchorsForCurrentScope()
    {
        var workspace = ResolveAttachWorkspaceRoot();
        if (string.IsNullOrWhiteSpace(workspace))
            return;

        var solution = ResolveAttachSolutionPath();
        var any = false;

        foreach (var msg in ChatMessages)
        {
            if (msg.Attachments.Count == 0)
                continue;

            if (!IntercomAttachmentAnchorRefresher.TryRefreshList(
                    msg.Attachments,
                    workspace,
                    solution,
                    out var refreshed)
                || refreshed is null)
            {
                continue;
            }

            msg.SetAttachments(refreshed, msg.SenderWorkspaceContext);
            any = true;
        }

        if (_pendingAttachDrafts.Count > 0)
        {
            var keys = _pendingAttachDrafts.Keys.ToList();
            foreach (var key in keys)
            {
                var draft = _pendingAttachDrafts[key];
                var next = IntercomAttachmentAnchorRefresher.Refresh(draft, workspace, solution);
                if (anchorDraftChanged(draft, next))
                {
                    _pendingAttachDrafts[key] = next;
                    any = true;
                }
            }
        }

        if (any)
        {
            refreshComposerAttachHint();
            RefreshChatSurfaceSnapshot();
        }
    }

    private static bool anchorDraftChanged(AttachmentAnchor before, AttachmentAnchor after) =>
        !string.Equals(before.ResolveOutcome, after.ResolveOutcome, StringComparison.OrdinalIgnoreCase)
        || before.LineStart != after.LineStart
        || before.LineEnd != after.LineEnd;
}

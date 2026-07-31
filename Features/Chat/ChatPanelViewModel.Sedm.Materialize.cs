#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>SEDM context-card materialization + outbound agent-context prefixes.</summary>
public partial class ChatPanelViewModel
{
    private string ApplySedmMaterializationToOutboundMessage(string input)
    {
        var prefix = _sedmScopeStrip.BuildAgentContextPrefix();
        if (string.IsNullOrWhiteSpace(prefix))
            return input;
        return prefix + Environment.NewLine + input;
    }

    private string ApplyAgentContextPrefixes(string input)
    {
        var withSedm = ApplySedmMaterializationToOutboundMessage(input);
        return ApplyProductSpineToOutboundMessage(withSedm);
    }

    private async Task MaybeMaterializeContextCardAsync(
        IReadOnlyList<AttachmentAnchor> attachments,
        string triggerReason,
        CancellationToken ct = default)
    {
        if (attachments.Count == 0)
            return;

        var anchor = attachments[0];
        var path = ResolveAnchorPath(anchor);
        if (string.IsNullOrWhiteSpace(path))
            return;

        var worklineId = ResolveSedmWorklineId();
        if (worklineId == Guid.Empty)
            return;

        var workline = SedmEventProjector.ResolveWorkline(
            SedmEventProjector.Project(_sessionEventsCache, worklineId, _threadDisplayTitles),
            worklineId);

        var label = _threadDisplayTitles.TryGetValue(worklineId, out var title) ? title : null;
        var workspace = ResolveAttachWorkspaceRoot();
        var applies = SedmAppliesResolver.Resolve(workspace, path);
        var pathHint = SedmAppliesResolver.BuildPathHint(applies, path);
        var payload = ChatHistoryPayloadMapping.ToContextCardMaterializedPayload(
            worklineId,
            path,
            ResolveAnchorSymbol(anchor),
            label,
            pathHint: pathHint,
            triggerReason: triggerReason,
            applies: applies);

        if (SedmEventProjector.IsSameContextCard(workline.ContextCard, payload))
            return;

        await PersistSedmEventAsync(
            ChatHistoryEventKind.ContextCardMaterialized,
            payload,
            worklineId,
            ct).ConfigureAwait(false);
    }

    private async Task MaybeMaterializeContextCardOnWorklineSwitchAsync(CancellationToken ct = default)
    {
        var worklineId = ResolveSedmWorklineId();
        if (worklineId == Guid.Empty)
            return;

        var projection = SedmEventProjector.Project(_sessionEventsCache, worklineId, _threadDisplayTitles);
        var workline = SedmEventProjector.ResolveWorkline(projection, worklineId);
        if (workline.ContextCard is not null)
            return;

        SedmContextCardMaterializedPayload? source = null;
        foreach (var wl in projection.ByWorkline.Values)
        {
            if (wl.ContextCard is not null)
                source = wl.ContextCard;
        }

        if (source is null)
            return;

        var label = _threadDisplayTitles.TryGetValue(worklineId, out var title) ? title : null;
        var applies = source.Applies?.Count > 0
            ? source.Applies
            : SedmAppliesResolver.Resolve(ResolveAttachWorkspaceRoot(), source.Anchor.Path);
        var pathHint = source.PathHint ?? SedmAppliesResolver.BuildPathHint(applies, source.Anchor.Path);
        var payload = ChatHistoryPayloadMapping.ToContextCardMaterializedPayload(
            worklineId,
            source.Anchor.Path,
            source.Anchor.Symbol,
            label,
            pathHint: pathHint,
            triggerReason: "workline_switch",
            applies: applies);

        await PersistSedmEventAsync(ChatHistoryEventKind.ContextCardMaterialized, payload, worklineId, ct)
            .ConfigureAwait(false);
    }

    private static string? ResolveAnchorPath(AttachmentAnchor anchor)
    {
        if (!string.IsNullOrWhiteSpace(anchor.File))
            return anchor.File.Replace('\\', '/').Trim();
        return null;
    }

    private static string? ResolveAnchorSymbol(AttachmentAnchor anchor) =>
        string.IsNullOrWhiteSpace(anchor.MemberKey) ? null : anchor.MemberKey.Trim();
}

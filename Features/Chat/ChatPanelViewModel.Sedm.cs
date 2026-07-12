#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    private readonly List<ChatHistoryEvent> _sessionEventsCache = [];

    private ChatSedmScopeStrip _sedmScopeStrip = ChatSedmScopeStrip.Empty;

    public ChatSedmScopeStrip SedmScopeStrip => _sedmScopeStrip;

    private IReadOnlyList<ChatSedmTimelineEntry> BuildSedmTimelineEntries() =>
        SedmTimelineBuilder.Build(_sessionEventsCache, ResolveSedmWorklineId());

    public string GetSedmScopeJson()
    {
        var strip = _sedmScopeStrip;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            strip_text = strip.FormatStripText(),
            context = strip.ContextOneLiner,
            intent = strip.IntentOneLiner,
            decision = strip.DecisionOneLiner,
            decision_status = strip.DecisionStatus,
            open_worklines = strip.OpenWorklineCount,
            intent_incomplete = strip.IntentIncomplete,
        }, ChatHistoryJson.Options);
    }

    public async Task<string> RecordIntentCardFromMcpArgsAsync(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
        CancellationToken ct = default)
    {
        var card = new SedmIntentCardBodyPayload(
            McpCommandJsonArgs.String(args, "outcome") ?? "",
            McpCommandJsonArgs.String(args, "trigger"),
            McpCommandJsonArgs.String(args, "chosen_approach"),
            McpCommandJsonArgs.String(args, "selection_rationale"),
            McpCommandJsonArgs.String(args, "constraints"),
            McpCommandJsonArgs.String(args, "validation_plan"));
        var considered = ParseConsideredArgs(args);
        return await RecordIntentCardFromMcpAsync(card, considered, ct).ConfigureAwait(false);
    }

    public async Task<string> RecordDecisionFromMcpArgsAsync(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
        CancellationToken ct = default)
    {
        var card = new SedmIntentCardBodyPayload(
            McpCommandJsonArgs.String(args, "outcome") ?? "",
            McpCommandJsonArgs.String(args, "trigger"),
            McpCommandJsonArgs.String(args, "chosen_approach"),
            McpCommandJsonArgs.String(args, "selection_rationale"),
            McpCommandJsonArgs.String(args, "constraints"),
            McpCommandJsonArgs.String(args, "validation_plan"));
        var considered = ParseConsideredArgs(args);
        var findings = ParseFindingsArgs(args);
        var touched = McpCommandJsonArgs.StringList(args, "touched_paths");
        var basis = touched is { Count: > 0 } || !string.IsNullOrWhiteSpace(McpCommandJsonArgs.String(args, "revision"))
            ? new SedmDecisionBasisPayload(McpCommandJsonArgs.String(args, "revision"), touched)
            : null;
        return await RecordDecisionFromMcpAsync(card, considered, findings, basis, ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<SedmIntentConsideredOptionPayload>? ParseConsideredArgs(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("considered", out var el) || el.ValueKind != System.Text.Json.JsonValueKind.Array)
            return null;

        var list = new List<SedmIntentConsideredOptionPayload>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != System.Text.Json.JsonValueKind.Object)
                continue;
            var approach = item.TryGetProperty("approach", out var a) ? a.GetString() : null;
            if (string.IsNullOrWhiteSpace(approach))
                continue;
            var rejected = item.TryGetProperty("rejected_because", out var r) ? r.GetString() : null;
            list.Add(new SedmIntentConsideredOptionPayload(approach, rejected));
        }

        return list.Count == 0 ? null : list;
    }

    private static IReadOnlyList<SedmDecisionFindingPayload>? ParseFindingsArgs(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("findings", out var el) || el.ValueKind != System.Text.Json.JsonValueKind.Array)
            return null;

        var list = new List<SedmDecisionFindingPayload>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != System.Text.Json.JsonValueKind.Object)
                continue;
            var kind = item.TryGetProperty("kind", out var k) ? k.GetString() : "note";
            var reference = item.TryGetProperty("ref", out var r) ? r.GetString() : "";
            var summary = item.TryGetProperty("summary", out var s) ? s.GetString() : "";
            if (string.IsNullOrWhiteSpace(summary))
                continue;
            list.Add(new SedmDecisionFindingPayload(kind ?? "note", reference ?? "", summary));
        }

        return list.Count == 0 ? null : list;
    }

    private Guid ResolveSedmWorklineId()
    {
        if (SelectedChatThreadId != Guid.Empty)
            return SelectedChatThreadId;
        if (_activeThreadId != Guid.Empty)
            return _activeThreadId;
        return _mainThreadId;
    }

    private int ResolveOpenWorklineCount()
    {
        var ids = new HashSet<Guid>();
        if (_mainThreadId != Guid.Empty)
            ids.Add(_mainThreadId);
        foreach (var msg in ChatMessages)
        {
            if (msg.ThreadId != Guid.Empty)
                ids.Add(msg.ThreadId);
        }
        foreach (var fork in _threadForks)
        {
            if (fork.NewThreadId != Guid.Empty)
                ids.Add(fork.NewThreadId);
        }
        return Math.Max(1, ids.Count);
    }

    private void RebuildSedmScopeStrip()
    {
        var worklineId = ResolveSedmWorklineId();
        var openCount = ResolveOpenWorklineCount();
        var projection = SedmEventProjector.Project(
            _sessionEventsCache,
            worklineId,
            _threadDisplayTitles,
            openCount);
        var workline = SedmEventProjector.ResolveWorkline(projection, worklineId);
        _sedmScopeStrip = ChatSedmScopeStrip.FromProjection(workline, openCount);
    }

    private void ReplaceSessionEventsCache(IReadOnlyList<ChatHistoryEvent> events)
    {
        _sessionEventsCache.Clear();
        _sessionEventsCache.AddRange(events);
        RebuildSedmScopeStrip();
    }

    private void AppendSessionEventCache(ChatHistoryEvent ev)
    {
        _sessionEventsCache.Add(ev);
        RebuildSedmScopeStrip();
    }

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

    /// <summary>MCP/агент: записать intent card (T1) в event log.</summary>
    public async Task<string> RecordIntentCardFromMcpAsync(
        SedmIntentCardBodyPayload card,
        IReadOnlyList<SedmIntentConsideredOptionPayload>? considered,
        CancellationToken ct = default)
    {
        var worklineId = ResolveSedmWorklineId();
        if (worklineId == Guid.Empty)
            return "no_active_workline";

        var payload = ChatHistoryPayloadMapping.ToIntentCardRecordedPayload(worklineId, card, considered);
        await PersistSedmEventAsync(ChatHistoryEventKind.IntentCardRecorded, payload, worklineId, ct)
            .ConfigureAwait(false);
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    /// <summary>MCP/агент: записать decision_recorded (unified model §12).</summary>
    public async Task<string> RecordDecisionFromMcpAsync(
        SedmIntentCardBodyPayload card,
        IReadOnlyList<SedmIntentConsideredOptionPayload>? considered,
        IReadOnlyList<SedmDecisionFindingPayload>? findings,
        SedmDecisionBasisPayload? basis,
        CancellationToken ct = default)
    {
        var worklineId = ResolveSedmWorklineId();
        if (worklineId == Guid.Empty)
            return "no_active_workline";

        var payload = ChatHistoryPayloadMapping.ToDecisionRecordedPayload(
            worklineId,
            card,
            considered,
            findings,
            basis);
        await PersistSedmEventAsync(ChatHistoryEventKind.DecisionRecorded, payload, worklineId, ct)
            .ConfigureAwait(false);
        await TryMarkCrossWorklineDecisionsStaleAsync(basis, worklineId, ct).ConfigureAwait(false);
        RefreshChatSurfaceSnapshot();
        return "OK";
    }

    public string GetSedmScopeStripText() => _sedmScopeStrip.FormatStripText();

    private async Task PersistSedmEventAsync<T>(
        string kind,
        T payload,
        Guid worklineId,
        CancellationToken ct)
    {
        await PersistEventAsync(kind, payload, worklineId).ConfigureAwait(false);
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

    private async Task TryMarkCrossWorklineDecisionsStaleAsync(
        SedmDecisionBasisPayload? basis,
        Guid activeWorklineId,
        CancellationToken ct)
    {
        if (basis?.TouchedPaths is not { Count: > 0 })
            return;

        var touched = new HashSet<string>(
            basis.TouchedPaths.Select(static p => p.Replace('\\', '/').Trim()),
            StringComparer.OrdinalIgnoreCase);
        var activeKey = activeWorklineId.ToString("N");

        foreach (var ev in _sessionEventsCache)
        {
            if (!string.Equals(ev.Kind, ChatHistoryEventKind.DecisionRecorded, StringComparison.Ordinal))
                continue;

            SedmDecisionRecordedPayload? decision;
            try
            {
                decision = System.Text.Json.JsonSerializer.Deserialize<SedmDecisionRecordedPayload>(
                    ev.PayloadJson,
                    ChatHistoryJson.Options);
            }
            catch (System.Text.Json.JsonException)
            {
                continue;
            }

            if (decision is null
                || string.Equals(decision.WorklineId, activeKey, StringComparison.OrdinalIgnoreCase)
                || decision.Basis?.TouchedPaths is not { Count: > 0 } paths)
                continue;

            if (!paths.Any(p => touched.Contains(p.Replace('\\', '/').Trim())))
                continue;

            var decisionKey = ev.EventId.ToString("N");
            if (_sessionEventsCache.Any(e =>
                    string.Equals(e.Kind, ChatHistoryEventKind.DecisionMarkedStale, StringComparison.Ordinal)
                    && e.PayloadJson.Contains(decisionKey, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (!Guid.TryParse(decision.WorklineId, out var staleWorklineId))
                continue;

            var stalePayload = ChatHistoryPayloadMapping.ToDecisionLifecyclePayload(
                staleWorklineId,
                ev.EventId,
                "cross_workline_path_touch");
            await PersistSedmEventAsync(
                ChatHistoryEventKind.DecisionMarkedStale,
                stalePayload,
                staleWorklineId,
                ct).ConfigureAwait(false);
        }
    }
}

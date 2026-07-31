#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>SEDM MCP intent/decision recording + cross-workline stale marking.</summary>
public partial class ChatPanelViewModel
{
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

    private async Task PersistSedmEventAsync<T>(
        string kind,
        T payload,
        Guid worklineId,
        CancellationToken ct)
    {
        await PersistEventAsync(kind, payload, worklineId).ConfigureAwait(false);
    }

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

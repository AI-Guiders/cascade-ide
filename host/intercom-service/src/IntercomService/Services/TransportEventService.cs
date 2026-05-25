using System.Text.Json;
using System.Text.Json.Nodes;
using IntercomWire;
using IntercomService.Contracts;
using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class TransportEventService(IntercomDbContext db, SseEventHub sse, TeamMembershipService teams)
{
    public async Task<TopicEntity> EnsureGeneralTopicAsync(string teamId, CancellationToken ct)
    {
        var existing = await db.Topics
            .Where(x => x.TeamId == teamId && x.Title == "general")
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing;

        var topic = new TopicEntity
        {
            TopicId = Guid.NewGuid().ToString("N"),
            TeamId = teamId,
            Title = "general",
            SpineKey = "general",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Topics.Add(topic);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return topic;
    }

    public async Task<TopicEntity> EnsureTopicBySpineAsync(
        string teamId,
        string spineKey,
        string title,
        CancellationToken ct)
    {
        var key = spineKey.Trim();
        var existing = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.SpineKey == key, ct)
            .ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var topic = new TopicEntity
        {
            TopicId = Guid.NewGuid().ToString("N"),
            TeamId = teamId,
            Title = string.IsNullOrWhiteSpace(title) ? key : title.Trim(),
            SpineKey = key,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Topics.Add(topic);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return topic;
    }

    public async Task<(TransportEventEnvelopeDto? Envelope, string? Error)> AppendAsync(
        string teamId,
        string topicId,
        AppendEventRequest request,
        string operatorMemberId,
        string operatorDisplayName,
        string clientKind,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientEventId))
            return (null, "client_event_id required");

        if (!IntercomWireTransportEventKinds.SyncDefault.Contains(request.EventKind))
            return (null, $"event_kind '{request.EventKind}' not allowed");

        var operatorRole = await teams.GetTeamRoleAsync(teamId, operatorMemberId, ct).ConfigureAwait(false);
        if (operatorRole is null)
            return (null, "not_a_member");

        var role = ResolveSenderRole(request);

        var topic = await db.Topics.FirstOrDefaultAsync(x => x.TopicId == topicId && x.TeamId == teamId, ct)
            .ConfigureAwait(false);
        if (topic is null)
            return (null, "topic not found");

        string senderMemberId;
        string senderDisplayName;
        string payloadJson;

        if (string.Equals(role, "agent", StringComparison.Ordinal))
        {
            if (!TeamRoleAuthorization.CanPublishMessages(operatorRole))
                return (null, "guest_cannot_publish");

            var agentMemberId = request.Sender?.MemberId?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(agentMemberId))
                return (null, "agent_member_id required");

            var agent = await db.TeamMembers
                .Include(x => x.Member)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TeamId == teamId && x.MemberId == agentMemberId,
                    ct)
                .ConfigureAwait(false);

            if (agent?.Member is null
                || !string.Equals(agent.Member.MemberKind, MemberKinds.Agent, StringComparison.Ordinal))
                return (null, "agent_not_in_team");

            senderMemberId = agentMemberId;
            senderDisplayName = string.IsNullOrWhiteSpace(request.Sender?.DisplayName)
                ? agent.Member.DisplayName
                : request.Sender.DisplayName.Trim();
            payloadJson = InjectOperatorMemberId(request.Payload, operatorMemberId);
        }
        else
        {
            if (!TeamRoleAuthorization.CanPublishMessages(operatorRole))
                return (null, "guest_cannot_publish");

            senderMemberId = operatorMemberId;
            senderDisplayName = operatorDisplayName;
            payloadJson = request.Payload.GetRawText();
        }

        var dup = await db.TransportEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.ClientEventId == request.ClientEventId, ct)
            .ConfigureAwait(false);
        if (dup is not null)
            return (ToEnvelope(dup), null);

        var maxSeq = await db.TransportEvents
            .Where(x => x.TeamId == teamId)
            .Select(x => (long?)x.Seq)
            .MaxAsync(ct)
            .ConfigureAwait(false) ?? 0;

        var occurred = DateTimeOffset.TryParse(request.OccurredAtUtc, out var at)
            ? at
            : DateTimeOffset.UtcNow;

        var entity = new TransportEventEntity
        {
            TeamId = teamId,
            Seq = maxSeq + 1,
            TopicId = topicId,
            ClientEventId = request.ClientEventId.Trim(),
            EventKind = request.EventKind,
            SenderMemberId = senderMemberId,
            SenderDisplayName = senderDisplayName,
            SenderRole = role,
            ClientKind = request.Sender?.ClientKind ?? clientKind,
            PayloadJson = payloadJson,
            OccurredAtUtc = occurred,
        };

        db.TransportEvents.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var envelope = ToEnvelope(entity);
        sse.Publish(teamId, envelope);
        return (envelope, null);
    }

    public async Task<IReadOnlyList<TransportEventEnvelopeDto>> ListAsync(
        string teamId,
        string topicId,
        long? afterSeq,
        int limit,
        CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 500);
        var query = db.TransportEvents.AsNoTracking()
            .Where(x => x.TeamId == teamId && x.TopicId == topicId);

        if (afterSeq is > 0)
            query = query.Where(x => x.Seq > afterSeq.Value);

        var rows = await query
            .OrderBy(x => x.Seq)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ConvertAll(ToEnvelope);
    }

    public async Task<IReadOnlyList<TransportEventEnvelopeDto>> ListTeamAsync(
        string teamId,
        long? afterSeq,
        int limit,
        CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 500);
        var query = db.TransportEvents.AsNoTracking().Where(x => x.TeamId == teamId);
        if (afterSeq is > 0)
            query = query.Where(x => x.Seq > afterSeq.Value);

        var rows = await query
            .OrderBy(x => x.Seq)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ConvertAll(ToEnvelope);
    }

    private static string ResolveSenderRole(AppendEventRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Sender?.SenderRole))
            return request.Sender.SenderRole.Trim();

        return InferSenderRoleFromPayload(request) ?? "human";
    }

    private static string? InferSenderRoleFromPayload(AppendEventRequest request)
    {
        if (!request.EventKind.StartsWith("message_", StringComparison.Ordinal))
            return null;

        try
        {
            if (!request.Payload.TryGetProperty("role", out var roleEl))
                return null;
            var role = roleEl.GetString();
            return role?.ToLowerInvariant() switch
            {
                "assistant" => "agent",
                "system" => "system",
                _ => "human",
            };
        }
        catch
        {
            return null;
        }
    }

    private static string InjectOperatorMemberId(JsonElement payload, string operatorMemberId)
    {
        var root = JsonNode.Parse(payload.GetRawText()) as JsonObject ?? new JsonObject();
        root["operator_member_id"] = operatorMemberId;
        return root.ToJsonString();
    }

    public static TransportEventEnvelopeDto ToEnvelope(TransportEventEntity e) =>
        new(
            SchemaVersion: 1,
            Seq: e.Seq,
            TeamId: e.TeamId,
            TopicId: e.TopicId,
            ClientEventId: e.ClientEventId,
            OccurredAtUtc: e.OccurredAtUtc.ToString("O"),
            EventKind: e.EventKind,
            Sender: new SenderDto(e.SenderMemberId, e.SenderDisplayName, e.SenderRole, e.ClientKind),
            Payload: JsonDocument.Parse(e.PayloadJson).RootElement.Clone());
}

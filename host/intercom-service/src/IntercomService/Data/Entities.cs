namespace IntercomService.Data;

public sealed class TeamEntity
{
    public required string TeamId { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class MemberEntity
{
    public required string MemberId { get; set; }

    public required string Issuer { get; set; }

    public required string Subject { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TeamMemberEntity
{
    public required string TeamId { get; set; }

    public required string MemberId { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; }

    public TeamEntity? Team { get; set; }

    public MemberEntity? Member { get; set; }
}

public sealed class TopicEntity
{
    public required string TopicId { get; set; }

    public required string TeamId { get; set; }

    public required string Title { get; set; }

    public string? SpineKey { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamEntity? Team { get; set; }
}

public sealed class TransportEventEntity
{
    public long Id { get; set; }

    public required string TeamId { get; set; }

    public long Seq { get; set; }

    public required string TopicId { get; set; }

    public required string ClientEventId { get; set; }

    public required string EventKind { get; set; }

    public required string SenderMemberId { get; set; }

    public required string SenderDisplayName { get; set; }

    public required string SenderRole { get; set; }

    public string ClientKind { get; set; } = "cide";

    public required string PayloadJson { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}

public sealed class RefreshTokenEntity
{
    public required string Id { get; set; }

    public required string MemberId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public MemberEntity? Member { get; set; }
}

public sealed class OAuthStateEntity
{
    public required string State { get; set; }

    public required string Provider { get; set; }

    public required string TeamId { get; set; }

    public required string RedirectUri { get; set; }

    public string? CodeChallenge { get; set; }

    public string? CodeChallengeMethod { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}

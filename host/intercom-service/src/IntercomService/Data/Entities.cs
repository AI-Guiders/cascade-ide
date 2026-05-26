namespace IntercomService.Data;

public sealed class TeamEntity
{
    public required string TeamId { get; set; }

    public required string DisplayName { get; set; }

    public string JoinPolicy { get; set; } = JoinPolicies.FirstOwner;

    public string DefaultTeamRole { get; set; } = TeamRoles.Member;

    public string? JoinPolicyJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class MemberEntity
{
    public required string MemberId { get; set; }

    public required string Issuer { get; set; }

    public required string Subject { get; set; }

    public required string DisplayName { get; set; }

    public string MemberKind { get; set; } = MemberKinds.Human;

    public string? AvatarGlyph { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class TeamMemberEntity
{
    public required string TeamId { get; set; }

    public required string MemberId { get; set; }

    public string TeamRole { get; set; } = TeamRoles.Member;

    public string? TeamDisplayName { get; set; }

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

    public string? InviteToken { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed class TeamInviteEntity
{
    public required string InviteId { get; set; }

    public required string TeamId { get; set; }

    public required string TokenHash { get; set; }

    public string TeamRole { get; set; } = TeamRoles.Member;

    public int MaxUses { get; set; } = 1;

    public int UseCount { get; set; }

    public required string CreatedByMemberId { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public TeamEntity? Team { get; set; }
}

public sealed class AgentCredentialEntity
{
    public required string CredentialId { get; set; }

    public required string MemberId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public MemberEntity? Member { get; set; }
}

public sealed class ProjectEntity
{
    public required string ProjectId { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ProjectRepoEntity
{
    public required string ProjectId { get; set; }

    public required string NormalizedRepoUrl { get; set; }

    public DateTimeOffset LinkedAtUtc { get; set; }

    public ProjectEntity? Project { get; set; }
}

public sealed class TeamProjectEntity
{
    public required string TeamId { get; set; }

    public required string ProjectId { get; set; }

    public DateTimeOffset LinkedAtUtc { get; set; }

    public TeamEntity? Team { get; set; }

    public ProjectEntity? Project { get; set; }
}

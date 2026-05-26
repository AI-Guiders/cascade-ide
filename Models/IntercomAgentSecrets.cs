namespace CascadeIDE.Models;

/// <summary>Agent credentials per team (LocalAppData, ADR 0147 §3).</summary>
public sealed class IntercomAgentSecrets
{
    /// <summary>Key: <c>{team_id}|{member_id}</c> → opaque credential token.</summary>
    public Dictionary<string, string> CredentialsByTeamAgent { get; set; } = new(StringComparer.Ordinal);

    public static string MakeKey(string teamId, string memberId) =>
        $"{teamId.Trim()}|{memberId.Trim()}";
}

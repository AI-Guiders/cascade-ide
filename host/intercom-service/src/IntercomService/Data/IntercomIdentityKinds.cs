namespace IntercomService.Data;

public static class MemberKinds
{
    public const string Human = "human";
    public const string Agent = "agent";
    public const string System = "system";
}

public static class TeamRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";
    public const string Guest = "guest";
    public const string Agent = "agent";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Owner, Admin, Member, Guest, Agent,
    };

    public static bool IsOwnerOrAdmin(string role) =>
        string.Equals(role, Owner, StringComparison.Ordinal)
        || string.Equals(role, Admin, StringComparison.Ordinal);

    public static int Rank(string role) => role switch
    {
        Owner => 100,
        Admin => 80,
        Member => 50,
        Guest => 20,
        Agent => 10,
        _ => 0,
    };
}

public static class JoinPolicies
{
    public const string InviteRequired = "invite_required";
    public const string GitHubOrg = "github_org";
    public const string FirstOwner = "first_owner";
    public const string Open = "open";
}

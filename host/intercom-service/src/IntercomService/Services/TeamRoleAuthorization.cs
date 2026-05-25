using IntercomService.Data;
using Microsoft.EntityFrameworkCore;

namespace IntercomService.Services;

public sealed class TeamRoleAuthorization
{
    public static bool CanManageMembers(string role) => TeamRoles.IsOwnerOrAdmin(role);

    public static bool CanManageInvites(string role) => TeamRoles.IsOwnerOrAdmin(role);

    public static bool CanManageAgents(string role) => TeamRoles.IsOwnerOrAdmin(role);

    public static bool CanPatchTeam(string role) => TeamRoles.IsOwnerOrAdmin(role);

    public static bool CanChangeJoinPolicy(string role) =>
        string.Equals(role, TeamRoles.Owner, StringComparison.Ordinal);

    public static bool CanPromoteToOwner(string callerRole) =>
        string.Equals(callerRole, TeamRoles.Owner, StringComparison.Ordinal);

    public static bool MeetsMinimum(string actualRole, string minimumRole) =>
        TeamRoles.Rank(actualRole) >= TeamRoles.Rank(minimumRole);
}

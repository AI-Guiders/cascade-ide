using IntercomService.Data;
using IntercomService.Services;

namespace IntercomService.Tests;

public sealed class TeamRoleAuthorizationTests
{
    [Theory]
    [InlineData(TeamRoles.Owner, true)]
    [InlineData(TeamRoles.Admin, true)]
    [InlineData(TeamRoles.Member, true)]
    [InlineData(TeamRoles.Guest, false)]
    [InlineData(TeamRoles.Agent, false)]
    public void CanPublishMessages_respects_role(string role, bool expected) =>
        Assert.Equal(expected, TeamRoleAuthorization.CanPublishMessages(role));
}

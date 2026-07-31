#nullable enable

using CascadeIDE.Features.UiChrome;
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SoftOrganChromeDensityPolicyTests
{
    [Theory]
    [InlineData("pressure")]
    [InlineData("alert")]
    [InlineData("qrh")]
    [InlineData("review")]
    [InlineData("sa_desk")]
    [InlineData("sa-desk")]
    public void Avalonia_facade_priority_matches_GlassCore(string id) =>
        Assert.Equal(
            SoftOrganChromeDensityPolicy.PriorityFor(id),
            AgentChromeHintDensityPolicy.PriorityFor(id));

    [Fact]
    public void Collapse_labels_match_across_hosts()
    {
        Assert.Equal(
            SoftOrganChromeDensityPolicy.CollapseLabel,
            SoftOrganChromeAggregator.CollapseLabel);
        Assert.Equal(
            SoftOrganChromeDensityPolicy.CollapseLabel,
            AgentChromeHintDensityPolicy.CollapseLabel);
    }
}

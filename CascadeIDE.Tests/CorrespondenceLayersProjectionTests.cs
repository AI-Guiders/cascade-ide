using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CorrespondenceLayersProjectionTests
{
    [Fact]
    public void BuildLayersBadge_orders_L0_through_L4()
    {
        var signals = new CorrespondenceLayerSignals(
            SemanticCanonL0: true,
            FeatureRegistryL1p: true,
            AdrPathMapL1: true,
            CodeIntentGraphL2: true,
            ChangeIntentL3: false,
            DiscourseCodeL4: false);

        var badge = CorrespondenceLayersProjection.BuildLayersBadge(signals);

        Assert.StartsWith("Correspondence:", badge, StringComparison.Ordinal);
        Assert.Contains("L0", badge, StringComparison.Ordinal);
        Assert.Contains("L1′", badge, StringComparison.Ordinal);
        Assert.Contains("L1", badge, StringComparison.Ordinal);
        Assert.Contains("L2", badge, StringComparison.Ordinal);
        Assert.DoesNotContain("L3", badge, StringComparison.Ordinal);
        Assert.DoesNotContain("L4", badge, StringComparison.Ordinal);
    }

    [Fact]
    public void CorrespondenceKindsCatalog_loads_embedded_layers()
    {
        var doc = CorrespondenceKindsCatalog.Load();

        Assert.Equal(1, doc.SchemaVersion);
        Assert.Equal(6, doc.Layers.Count);
        Assert.Contains(doc.Layers, l => l.Id == "L0");
        Assert.Contains(doc.Layers, l => l.Id == "L4");
        Assert.True(CorrespondenceKindsCatalog.TryGetLayer("L1p", out var l1p));
        Assert.Equal("L1′", l1p.Label);
    }
}

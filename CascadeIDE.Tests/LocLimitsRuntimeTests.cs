using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LocLimitsRuntimeTests
{
    public LocLimitsRuntimeTests() => LocLimitsRuntime.ResetToCodeDefaults();

    [Fact]
    public void TierFor_defaults_low_medium_high()
    {
        Assert.Equal(LocSizeTier.Low, LocLimitsRuntime.TierFor(299));
        Assert.Equal(LocSizeTier.Medium, LocLimitsRuntime.TierFor(300));
        Assert.Equal(LocSizeTier.Medium, LocLimitsRuntime.TierFor(799));
        Assert.Equal(LocSizeTier.High, LocLimitsRuntime.TierFor(800));
    }

    [Fact]
    public void ApplyWorkspaceToml_overrides_thresholds()
    {
        try
        {
            LocLimitsRuntime.ApplyWorkspaceToml(new UiWorkspaceToml
            {
                LocLimits = new UiWorkspaceLocLimitsToml { MediumMin = 100, HighMin = 200 }
            });
            Assert.Equal(LocSizeTier.Low, LocLimitsRuntime.TierFor(99));
            Assert.Equal(LocSizeTier.Medium, LocLimitsRuntime.TierFor(150));
            Assert.Equal(LocSizeTier.High, LocLimitsRuntime.TierFor(200));
        }
        finally
        {
            LocLimitsRuntime.ResetToCodeDefaults();
        }
    }
}

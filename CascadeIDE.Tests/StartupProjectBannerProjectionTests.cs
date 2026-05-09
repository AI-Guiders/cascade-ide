using CascadeIDE.Features.Launch.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class StartupProjectBannerProjectionTests
{
    [Fact]
    public void Empty_when_no_startup()
    {
        Assert.Equal("", StartupProjectBannerProjection.Format(false, false, null, ""));
    }

    [Theory]
    [InlineData(true, "", "short", "Старт отладки (F5): short")]
    [InlineData(false, "", "short", "Старт отладки (F5): short")]
    [InlineData(true, null, "App", "Старт отладки (F5): App")]
    [InlineData(true, "prof", "App", "Старт отладки (F5): App · prof")]
    public void Banner_with_optional_profile_suffix(
        bool showPicker,
        string? profileId,
        string shortLabel,
        string expected) =>
        Assert.Equal(
            expected,
            StartupProjectBannerProjection.Format(true, showPicker, profileId, shortLabel));

    [Fact]
    public void Picker_without_profile_id_uses_base_only()
    {
        Assert.Equal(
            "Старт отладки (F5): X",
            StartupProjectBannerProjection.Format(true, true, "", "X"));
    }
}

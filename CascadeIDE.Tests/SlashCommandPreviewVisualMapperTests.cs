#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SlashCommandPreviewVisualMapperTests
{
    [Theory]
    [MemberData(nameof(NonNoneKinds))]
    public void ToChromeSeverity_maps_every_non_none_kind(SlashCommandPreviewKind kind)
    {
        var chrome = SlashCommandPreviewVisualMapper.ToChromeSeverity(kind);
        Assert.NotEqual(SlashPreviewChromeSeverity.None, chrome);
        _ = SkiaSlashPreviewChrome.ToChipSeverity(chrome);
    }

    [Fact]
    public void ToChromeSeverity_none_maps_to_none()
    {
        Assert.Equal(SlashPreviewChromeSeverity.None, SlashCommandPreviewVisualMapper.ToChromeSeverity(SlashCommandPreviewKind.None));
        Assert.Equal(SkiaStatusChipSeverity.None, SkiaSlashPreviewChrome.ToChipSeverity(SlashPreviewChromeSeverity.None));
    }

    public static IEnumerable<object[]> NonNoneKinds() =>
        SlashCommandPreviewVisualMapper.NonNoneKinds.Select(static k => new object[] { k });
}

#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomFaceMetaTests
{
    [Fact]
    public void QuietRole_peels_legacy_seat_chrome()
    {
        Assert.Equal("Sierra", GlassIntercomFaceMeta.QuietRole("Sierra · guest @PF → @PM"));
    }

    [Fact]
    public void QuietRole_keeps_plain_name()
    {
        Assert.Equal("operator", GlassIntercomFaceMeta.QuietRole("operator"));
    }

    [Fact]
    public void QuietRole_empty_is_question()
    {
        Assert.Equal("?", GlassIntercomFaceMeta.QuietRole(null));
        Assert.Equal("?", GlassIntercomFaceMeta.QuietRole("  "));
    }
}

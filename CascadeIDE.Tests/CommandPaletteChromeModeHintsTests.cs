using CascadeIDE.Features.Shell.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteChromeModeHintsTests
{
    [Fact]
    public void Entries_Contains_f_t_m_x_c_InOrder()
    {
        var e = CommandPaletteChromeModeHints.Entries;
        Assert.Collection(
            e,
            x =>
            {
                Assert.Equal("f:", x.Prefix); Assert.Equal("файл", x.Label);
            },
            x =>
            {
                Assert.Equal("t:", x.Prefix); Assert.Equal("тип", x.Label);
            },
            x =>
            {
                Assert.Equal("m:", x.Prefix); Assert.Equal("член", x.Label);
            },
            x =>
            {
                Assert.Equal("x:", x.Prefix); Assert.Equal("текст", x.Label);
            },
            x =>
            {
                Assert.Equal("c:", x.Prefix); Assert.Equal("melody", x.Label);
            });
    }

    [Fact]
    public void SeparatorLineJoin_JoinsWithBullet()
    {
        var s = CommandPaletteChromeModeHints.SeparatorLineJoin;
        Assert.Contains("f: файл · t: тип · m: член · x: текст · c: melody", s, StringComparison.Ordinal);
    }
}

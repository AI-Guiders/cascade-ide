using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteGoToSearchBackendNormalizerTests
{
    [Fact]
    public void Parse_null_Returns_rg() =>
        Assert.Equal(CommandPaletteGoToSearchBackendKind.Rg, CommandPaletteGoToSearchBackendNormalizer.Parse(null));

    [Theory]
    [InlineData("", CommandPaletteGoToSearchBackendKind.Rg)]
    [InlineData("RG", CommandPaletteGoToSearchBackendKind.Rg)]
    [InlineData(" rg ", CommandPaletteGoToSearchBackendKind.Rg)]
    [InlineData("hci", CommandPaletteGoToSearchBackendKind.Hci)]
    [InlineData("auto", CommandPaletteGoToSearchBackendKind.Auto)]
    [InlineData("not-a-backend", CommandPaletteGoToSearchBackendKind.Rg)]
    public void Parse_Returns_kind(string? raw, CommandPaletteGoToSearchBackendKind expected) =>
        Assert.Equal(expected, CommandPaletteGoToSearchBackendNormalizer.Parse(raw));
}

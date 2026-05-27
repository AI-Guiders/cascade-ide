#nullable enable

using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

internal static class ChatSlashCatalogTestSupport
{
    public static void AssertResolves(
        string line,
        string expectedPath,
        string? expectedTail = null)
    {
        Assert.True(
            ChatSlashCommandCatalog.TryResolveInput(line, out var descriptor, out var tail),
            $"Expected resolve: {line}");

        Assert.Equal(expectedPath, descriptor.SlashPath);
        if (expectedTail is not null)
            Assert.Equal(expectedTail, tail);
    }

    public static void AssertDoesNotResolve(string line) =>
        Assert.False(ChatSlashCommandCatalog.TryResolveInput(line, out _, out _));
}

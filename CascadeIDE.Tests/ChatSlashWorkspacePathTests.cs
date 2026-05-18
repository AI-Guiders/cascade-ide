using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashWorkspacePathTests
{
    [Fact]
    public void TryNormalizePathArgument_relative_uses_workspace_root()
    {
        var ok = ChatSlashWorkspacePathHelper.TryNormalizePathArgument(
            "src/Foo.cs",
            @"D:\repo",
            out var full,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.EndsWith("Foo.cs", full, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryNormalizePathArgument_strips_quotes()
    {
        var ok = ChatSlashWorkspacePathHelper.TryNormalizePathArgument(
            "\"Features/Chat/Foo.cs\"",
            @"D:\repo",
            out _,
            out var error);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalizePathArgument_empty_tail_fails()
    {
        var ok = ChatSlashWorkspacePathHelper.TryNormalizePathArgument("  ", @"D:\repo", out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}

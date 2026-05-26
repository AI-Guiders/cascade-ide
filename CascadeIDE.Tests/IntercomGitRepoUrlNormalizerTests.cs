using CascadeIDE.Features.Intercom.Transport;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomGitRepoUrlNormalizerTests
{
    [Theory]
    [InlineData("https://GitHub.com/Org/Repo.git", "github.com/org/repo")]
    [InlineData("git@github.com:Org/Repo.git", "github.com/org/repo")]
    public void Normalize_matches_server_rules(string raw, string expected)
    {
        Assert.Equal(expected, IntercomGitRepoUrlNormalizer.TryNormalize(raw));
    }
}

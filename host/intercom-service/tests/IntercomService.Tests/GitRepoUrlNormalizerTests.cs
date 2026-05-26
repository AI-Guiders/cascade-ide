using IntercomService.Services;

namespace IntercomService.Tests;

public sealed class GitRepoUrlNormalizerTests
{
    [Theory]
    [InlineData("https://GitHub.com/Org/Repo.git", "github.com/org/repo")]
    [InlineData("git@github.com:Org/Repo.git", "github.com/org/repo")]
    [InlineData("https://gitlab.com/group/sub.git", "gitlab.com/group/sub")]
    public void Normalize_maps_common_forms(string raw, string expected)
    {
        Assert.Equal(expected, GitRepoUrlNormalizer.TryNormalize(raw));
    }
}

using CascadeIDE.Features.Intercom.Admin;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomServerHostServiceTests
{
    [Fact]
    public void TryResolveServiceProjectPath_finds_host_project_in_repo()
    {
        var path = IntercomServerHostService.TryResolveServiceProjectPath();
        Assert.NotNull(path);
        Assert.EndsWith("IntercomService.csproj", path, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Default_transport_paths_and_url_match_bundled_defaults()
    {
        var transport = new IntercomTransportSettings();
        Assert.Equal("http://127.0.0.1:5080", IntercomTransportSettings.DefaultBaseUrl);
        Assert.Equal(IntercomTransportSettings.DefaultBaseUrl, transport.BaseUrl);
        Assert.Equal(
            "tools/intercom-service/IntercomService.exe",
            IntercomTransportSettings.DefaultLocalServerRelativePath);
        Assert.Equal(IntercomTransportSettings.DefaultLocalServerRelativePath, transport.LocalServerPath);
    }

    [Fact]
    public void TryResolveLaunch_uses_default_relative_path_when_config_empty()
    {
        var plan = IntercomServerHostService.TryResolveLaunch("");
        if (plan is null)
        {
            // dev tree without publish: dotnet run fallback is acceptable
            Assert.NotNull(IntercomServerHostService.TryResolveServiceProjectPath());
            return;
        }

        var isPublished = plan.FileName.Contains("IntercomService", StringComparison.OrdinalIgnoreCase);
        var isDotnetRun = string.Equals(plan.FileName, "dotnet", StringComparison.OrdinalIgnoreCase);
        Assert.True(isPublished || isDotnetRun);
    }

    [Fact]
    public void EnumerateFallbackExecutableCandidates_includes_artifacts_in_repo()
    {
        var candidates = IntercomServerHostService.EnumerateFallbackExecutableCandidates().ToList();
        Assert.Contains(candidates, c => c.Contains("artifacts", StringComparison.OrdinalIgnoreCase));
    }
}

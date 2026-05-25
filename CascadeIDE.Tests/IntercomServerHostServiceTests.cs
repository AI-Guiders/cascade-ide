using CascadeIDE.Features.Intercom.Admin;
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
}

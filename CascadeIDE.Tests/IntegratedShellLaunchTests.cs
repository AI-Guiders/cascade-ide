using CascadeIDE.Features.Terminal.DataAcquisition;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntegratedShellLaunchTests
{
    [Fact]
    public void NormalizeStandardInput_ConvertsLfToPlatformNewline()
    {
        var input = "dotnet build\n"u8.ToArray();
        var normalized = IntegratedShellLaunch.NormalizeStandardInput(input);
        var text = System.Text.Encoding.UTF8.GetString(normalized);
        Assert.EndsWith(Environment.NewLine, text);
    }

    [Fact]
    public void ResolveLaunchConfiguration_OnWindows_ReturnsShell()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var launch = IntegratedShellLaunch.ResolveLaunchConfiguration(Environment.CurrentDirectory);
        Assert.False(string.IsNullOrWhiteSpace(launch.FileName));
        Assert.True(File.Exists(launch.FileName));
    }
}

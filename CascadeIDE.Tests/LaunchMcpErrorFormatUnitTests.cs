using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.Launch;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class LaunchMcpErrorFormatUnitTests
{
    [Fact]
    public void FormatResolveFailure_returns_profile_not_found_for_explicit_profile_without_resolve()
    {
        var readiness = LaunchReadinessSnapshot.NotReady("profile_project_not_found", "Web", @"src\Web\Web.csproj");
        var message = LaunchMcpErrorFormatUnit.Default.FormatResolveFailure(readiness, "Web", @"D:\repo");
        Assert.Equal("# Error: profile_not_found: Web", message);
    }

    [Fact]
    public void FormatResolveFailure_returns_active_profile_missing_for_implicit_profile_without_resolve()
    {
        var readiness = LaunchReadinessSnapshot.NotReady("startup_project_missing");
        var message = LaunchMcpErrorFormatUnit.Default.FormatResolveFailure(readiness, null, @"D:\repo");
        Assert.Equal("# Error: active_profile_missing", message);
    }

    [Fact]
    public void FormatResolveFailure_returns_project_not_found_when_profile_relative_path_is_known()
    {
        var readiness = new LaunchReadinessSnapshot(
            CanAttemptResolve: true,
            Source: LaunchReadinessSource.None,
            ReasonCode: "profile_project_not_found",
            ProfileId: "Web",
            ProfileProjectRelative: @"src\Web\Web.csproj",
            SelectedProjectFullPath: null);
        var message = LaunchMcpErrorFormatUnit.Default.FormatResolveFailure(readiness, "Web", @"D:\repo");
        Assert.Equal("# Error: project_not_found: D:\\repo\\src\\Web\\Web.csproj", message);
    }

    [Fact]
    public void LaunchMcpErrorFormatUnit_default_implements_ccu_contract()
    {
        ICockpitComputeUnit unit = LaunchMcpErrorFormatUnit.Default;
        Assert.NotNull(unit);
    }
}

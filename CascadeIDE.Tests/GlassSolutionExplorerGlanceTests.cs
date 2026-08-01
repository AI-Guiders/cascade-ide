using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassSolutionExplorerGlanceTests
{
    [Fact]
    public void TryFormatFromSlnText_lists_project_names()
    {
        const string sln = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CascadeIDE", "CascadeIDE.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CascadeIDE.Tests", "CascadeIDE.Tests\\CascadeIDE.Tests.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """;

        var body = GlassSolutionExplorerGlance.TryFormatFromSlnText(sln, @"C:\ws\CascadeIDE.sln");
        Assert.NotNull(body);
        Assert.Contains("CascadeIDE.sln", body);
        Assert.Contains("projects=2", body);
        Assert.Contains("· CascadeIDE", body);
        Assert.Contains("· CascadeIDE.Tests", body);
        Assert.Contains("SolutionExplorerView", body);
    }

    [Fact]
    public void TryFormatFromSlnText_empty_returns_null()
    {
        Assert.Null(GlassSolutionExplorerGlance.TryFormatFromSlnText("GlobalSection(SolutionConfigurationPlatforms)"));
        Assert.Null(GlassSolutionExplorerGlance.TryFormatFromSlnText(""));
    }
}

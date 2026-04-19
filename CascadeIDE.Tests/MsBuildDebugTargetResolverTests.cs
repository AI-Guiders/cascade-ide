using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MsBuildDebugTargetResolverTests
{
    [Fact]
    public void TryParsePropertiesOutput_ParsesDotnetMsbuildJson()
    {
        const string json = """
            {
              "Properties": {
                "OutputType": "Exe",
                "TargetPath": "D:\\proj\\bin\\Debug\\net10.0\\App.dll"
              }
            }
            """;

        var ok = MsBuildDebugTargetResolver.TryParsePropertiesOutput(json, out var outputType, out var targetPath);

        Assert.True(ok);
        Assert.Equal("Exe", outputType);
        Assert.Equal(@"D:\proj\bin\Debug\net10.0\App.dll", targetPath);
    }

    [Fact]
    public void TryParsePropertiesOutput_Library_ReturnsOutputType()
    {
        const string json = """{"Properties":{"OutputType":"Library","TargetPath":"X.dll"}}""";

        var ok = MsBuildDebugTargetResolver.TryParsePropertiesOutput(json, out var outputType, out var targetPath);

        Assert.True(ok);
        Assert.Equal("Library", outputType);
        Assert.Equal("X.dll", targetPath);
    }
}

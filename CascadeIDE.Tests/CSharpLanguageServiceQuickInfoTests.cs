using CascadeIDE.Services;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests;

public class CSharpLanguageServiceQuickInfoTests
{
    [Fact]
    public void GetQuickInfo_OnMethodName_ReturnsSignatureAndSummary()
    {
        const string path = @"D:\Fake\Hover.cs";
        var src = """
            namespace N;

            public class C
            {
                /// <summary>Tells a story.</summary>
                public void Tell() { }
            }
            """;

        var svc = new CSharpLanguageService();
        var line = 6;
        var col = 17;
        var info = svc.GetQuickInfo(path, src, line, col, TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Contains("Tell", info, StringComparison.Ordinal);
        Assert.Contains("Tells a story", info, StringComparison.Ordinal);
    }

    [Fact]
    public void GetQuickInfo_OutOfRangeLine_ReturnsNull()
    {
        const string path = @"D:\Fake\Empty.cs";
        var src = "class X { }";
        var svc = new CSharpLanguageService();
        var info = svc.GetQuickInfo(path, src, line: 99, column: 1, TestContext.Current.CancellationToken);
        Assert.Null(info);
    }
}

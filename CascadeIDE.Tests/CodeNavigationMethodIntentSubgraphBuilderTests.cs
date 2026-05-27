using System.Text.Json;
using CascadeIDE.Services.CodeNavigation;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регресс: intent-зерно subgraph (ADR 0053 / 0151) компактнее пошагового CFG на том же методе.
/// </summary>
public sealed class CodeNavigationMethodIntentSubgraphBuilderTests
{
    private const string FakePath = @"D:\sandbox\GrainIntentTest.cs";

    [Fact]
    public void Intent_HasFewerNodesThanDetailed_ForClassicForLoop_AndHasLoopStep()
    {
        var source =
            """
            class C {
                void M() {
                    for (int i = 0; i < N; i++) {
                        Foo();
                    }
                }
            }
            """;

        using var intentDoc = JsonDocument.Parse(
            CodeNavigationMethodIntentSubgraphBuilder.BuildJson(FakePath, source, line: 3, column: 14, 64, 64));
        using var detailedDoc = JsonDocument.Parse(
            CodeNavigationControlFlowSubgraphBuilder.BuildJson(FakePath, source, line: 3, column: 14, 64, 64));

        var intentCount = intentDoc.RootElement.GetProperty("nodes").GetArrayLength();
        var detailedCount = detailedDoc.RootElement.GetProperty("nodes").GetArrayLength();

        Assert.True(intentCount < detailedCount);

        var loopStep = false;
        foreach (var n in intentDoc.RootElement.GetProperty("nodes").EnumerateArray())
        {
            if (string.Equals(n.GetProperty("kind").GetString(), "loop_step", StringComparison.Ordinal))
            {
                loopStep = true;
                break;
            }
        }

        Assert.True(loopStep);
    }
}

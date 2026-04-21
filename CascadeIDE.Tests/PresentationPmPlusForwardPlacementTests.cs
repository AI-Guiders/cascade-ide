using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class PresentationPmPlusForwardPlacementTests
{
    [Theory]
    [InlineData(3, 1, true, 1, 0)]  // (P+M)(F), primary center → PM left
    [InlineData(3, 1, false, 1, 2)] // (F)(P+M), primary center → PM right
    [InlineData(2, 0, true, 0, 1)]
    [InlineData(2, 0, false, 0, 1)]
    [InlineData(2, 1, true, 1, 0)]
    [InlineData(2, 1, false, 1, 0)]
    public void ComputeForwardAndPmOrderedIndices_ThreeAndTwoMonitors(
        int orderedCount,
        int primaryIdx,
        bool pmBeforeForward,
        int expectForward,
        int expectPm)
    {
        PresentationPmPlusForwardPlacement.ComputeForwardAndPmOrderedIndices(
            orderedCount,
            primaryIdx,
            pmBeforeForward,
            out var f,
            out var pm);
        Assert.Equal(expectForward, f);
        Assert.Equal(expectPm, pm);
    }
}

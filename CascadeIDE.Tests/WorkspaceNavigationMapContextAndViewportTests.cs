using System.Text.Json;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationMapContextAndViewportTests
{
    [Fact]
    public void Viewport_rejects_nan_and_below_threshold()
    {
        Assert.False(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(double.NaN, 100d, out _));
        Assert.False(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(39d, 100d, out _));
    }

    [Fact]
    public void Viewport_clamps_and_skips_near_current()
    {
        Assert.True(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(500d, 100d, out var clamped));
        Assert.Equal(500d, clamped);

        Assert.False(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(100d, 100d, out _));
        Assert.False(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(100d, 102d, out _));
    }

    [Fact]
    public void Viewport_coerces_small_reported_values_to_minimum()
    {
        Assert.True(CodeNavigationMapViewportPolicy.ShouldApplyMeasuredWidth(41d, 10d, out var clamped));
        Assert.Equal(CodeNavigationMapViewportPolicy.MinClampedWidth, clamped);
    }

    [Fact]
    public void JsonBuilder_control_flow_without_file_returns_error_no_file()
    {
        var json = WorkspaceNavigationMapContextJsonBuilder.Build(
            CodeNavigationMapLevelKind.ControlFlow,
            wantGraph: false,
            currentPath: null,
            editorText: null,
            cursorLine: null,
            cursorColumn: null,
            [],
            solutionPath: null,
            navSettings: null);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("no_file", doc.RootElement.GetProperty("error").GetString());
    }
}

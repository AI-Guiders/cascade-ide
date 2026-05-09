using CascadeIDE.Features.Shell.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowPresentationDapProjectionTests
{
    [Fact]
    public void Debug_execution_projection_matches_session_and_stopped_flag()
    {
        Assert.False(MainWindowPresentationDapProjection.IsDebugExecutionPaused(
            hasActiveSession: false,
            executionStopped: true));
        Assert.False(MainWindowPresentationDapProjection.IsDebugExecutionRunning(
            hasActiveSession: false,
            executionStopped: false));

        Assert.True(MainWindowPresentationDapProjection.IsDebugExecutionPaused(
            hasActiveSession: true,
            executionStopped: true));
        Assert.False(MainWindowPresentationDapProjection.IsDebugExecutionRunning(
            hasActiveSession: true,
            executionStopped: true));

        Assert.False(MainWindowPresentationDapProjection.IsDebugExecutionPaused(
            hasActiveSession: true,
            executionStopped: false));
        Assert.True(MainWindowPresentationDapProjection.IsDebugExecutionRunning(
            hasActiveSession: true,
            executionStopped: false));
    }
}

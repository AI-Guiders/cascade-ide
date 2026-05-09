using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP debug actions.
/// Keeps snapshot payload shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpDebugOrchestrator
{
    /// <summary>План для UI после DAP-снимка; без <c>File.Exists</c> на Application-слое (CASCOPE031).</summary>
    public static IdeMcpDapSnapshotUiPlan BuildDapSnapshotUiPlan(DebugSessionSnapshot s, bool mfdDebugPagePrimedForCurrentStop)
    {
        bool mfdNext;
        bool activateDock;
        if (!s.IsExecutionStopped)
        {
            mfdNext = false;
            activateDock = false;
        }
        else if (!mfdDebugPagePrimedForCurrentStop)
        {
            mfdNext = true;
            activateDock = true;
        }
        else
        {
            mfdNext = true;
            activateDock = false;
        }

        var positionFile = !string.IsNullOrEmpty(s.StoppedFile)
            ? CanonicalFilePath.Normalize(s.StoppedFile)
            : null;

        var attemptOpen = s.IsExecutionStopped && !string.IsNullOrEmpty(s.StoppedFile);

        var stackIndex = s is { IsExecutionStopped: true, StackFrames.Count: > 0 }
            ? s.VariablesFrameIndex
            : -1;

        return new IdeMcpDapSnapshotUiPlan(
            mfdNext,
            activateDock,
            positionFile,
            s.StoppedLine,
            attemptOpen,
            attemptOpen ? s.StoppedFile : null,
            stackIndex);
    }

    public static string SerializeDebugSnapshot(DebugSessionSnapshot snapshot) =>
        JsonSerializer.Serialize(new
        {
            snapshot.HasActiveSession,
            snapshot.IsExecutionStopped,
            position = new { file = snapshot.StoppedFile, line = snapshot.StoppedLine },
            exception = snapshot.ExceptionText,
            breakpoints = snapshot.Breakpoints
                .Take(500)
                .Select(b => new { file = b.File, line = b.Line, condition = b.Condition })
                .ToList(),
            stack_frames = snapshot.StackFrames
                .Select(frame => new { frame.Name, frame.File, frame.Line })
                .ToList(),
            variables_frame_index = snapshot.VariablesFrameIndex,
            variable_root_scopes = snapshot.VariableRootScopes
                .Take(50)
                .Select(g => new
                {
                    scope = g.ScopeName,
                    roots = g.Roots
                        .Take(200)
                        .Select(v => new
                        {
                            v.Name,
                            v.Value,
                            v.Type,
                            variables_reference = v.VariablesReference,
                            v.NamedVariables,
                            v.IndexedVariables
                        })
                        .ToList()
                })
                .ToList()
        });
}

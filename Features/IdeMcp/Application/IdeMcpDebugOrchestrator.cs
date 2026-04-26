using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP debug actions.
/// Keeps snapshot payload shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpDebugOrchestrator
{
    public static string SerializeDebugSnapshot(Services.DebugSessionSnapshot snapshot) =>
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

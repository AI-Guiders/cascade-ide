#nullable enable
using CascadeIDE.Services.Lsp;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Input context for Environment Readiness channel build.
/// </summary>
public readonly record struct EnvironmentReadinessChannelContext(
    CascadeIdeSettings Settings,
    string? SolutionPath,
    CSharpLspDiagnosticsHost? CSharpHost,
    MarkdownLspDiagnosticsHost? MarkdownHost,
    bool IsMcpStdioHost = false,
    string? ActiveAiProvider = null,
    CancellationToken CancellationToken = default);

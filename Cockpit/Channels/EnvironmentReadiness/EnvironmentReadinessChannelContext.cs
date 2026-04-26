#nullable enable
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Input context for Environment Readiness channel build. LSP — тот же снимок, что на <see cref="IDataBus"/> (<see cref="IdeHostStateChanged"/>), без прямой ссылки на хосты.
/// </summary>
public readonly record struct EnvironmentReadinessChannelContext(
    CascadeIdeSettings Settings,
    string? SolutionPath,
    IdeHostStateChanged Lsp,
    bool IsMcpStdioHost = false,
    string? ActiveAiProvider = null,
    CancellationToken CancellationToken = default);

#nullable enable

namespace CascadeIDE.Features.Cockpit;

public readonly record struct CockpitCommandLineCommitResult(
    bool Handled,
    bool Success,
    string? UserMessage);

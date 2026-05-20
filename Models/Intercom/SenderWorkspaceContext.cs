#nullable enable

using System.Text.Json.Serialization;

namespace CascadeIDE.Models.Intercom;

/// <summary>Снимок workspace отправителя @ send (ADR 0128 §3.1).</summary>
public sealed record SenderWorkspaceContext(
    [property: JsonPropertyName("git_branch")] string? GitBranch = null,
    [property: JsonPropertyName("git_commit_short")] string? GitCommitShort = null,
    [property: JsonPropertyName("solution_path")] string? SolutionPath = null,
    [property: JsonPropertyName("captured_at_utc")] string? CapturedAtUtc = null);

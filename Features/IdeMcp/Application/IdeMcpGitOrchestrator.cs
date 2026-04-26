using System.Text.Json;
using GitMcp.Core;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level helpers for IDE MCP git actions.
/// Keeps common JSON/result shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpGitOrchestrator
{
    public static string BuildValidationError(string error) =>
        JsonSerializer.Serialize(new { success = false, error });

    public static string BuildCommandResult(bool success, int exitCode, string output) =>
        JsonSerializer.Serialize(new
        {
            success,
            exit_code = exitCode,
            output
        });

    public static string BuildStepFailure(string step, int exitCode, string output) =>
        JsonSerializer.Serialize(new { success = false, step, exit_code = exitCode, output });

    public static string BuildMissingCommitMessageError() =>
        JsonSerializer.Serialize(new { success = false, error = "Commit message is required." });

    public static string BuildPreflightReport(bool staged, GitPreflight.Report report) =>
        JsonSerializer.Serialize(new
        {
            success = true,
            staged,
            changed_files = report.ChangedFiles,
            untracked_files = report.UntrackedFiles,
            semantic_files = report.SemanticFiles,
            whitespace_only_files = report.WhitespaceOnlyFiles,
            eol_only_files = report.EolOnlyFiles,
            bom_only_files = report.BomOnlyFiles,
            suggested_safe_fix_commands = report.SuggestedSafeFixCommands
        });

    public static string BuildCommitResult(bool success, int exitCode, string output) =>
        JsonSerializer.Serialize(new
        {
            success,
            exit_code = exitCode,
            output
        });

    public static string BuildPreflightFixSafeResult(string postPreflightJson, IReadOnlyList<string> appliedCommands)
    {
        using var doc = JsonDocument.Parse(postPreflightJson);
        if (!doc.RootElement.TryGetProperty("success", out var ok) || ok.ValueKind != JsonValueKind.True)
            return postPreflightJson;

        return JsonSerializer.Serialize(new
        {
            success = true,
            applied = appliedCommands,
            changed_files = doc.RootElement.GetProperty("changed_files"),
            untracked_files = doc.RootElement.GetProperty("untracked_files"),
            semantic_files = doc.RootElement.GetProperty("semantic_files"),
            whitespace_only_files = doc.RootElement.GetProperty("whitespace_only_files"),
            eol_only_files = doc.RootElement.GetProperty("eol_only_files"),
            bom_only_files = doc.RootElement.GetProperty("bom_only_files"),
            suggested_safe_fix_commands = doc.RootElement.GetProperty("suggested_safe_fix_commands")
        });
    }

    public static string NormalizeAction(string? action, string defaultAction) =>
        string.IsNullOrWhiteSpace(action) ? defaultAction : action.Trim().ToLowerInvariant();
}

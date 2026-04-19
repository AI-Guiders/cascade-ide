#nullable enable
using System.Text.Json;

namespace CascadeIDE.Services.AgentContract;

/// <summary>Паритет с MCP <c>git_*</c>: тот же JSON (success, exit_code, output с усечением), что и в IDE.</summary>
internal static class AgentContractGitJson
{
    private const int MaxOutputChars = 4000;

    private static string TruncateOutput(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Length > MaxOutputChars ? text[..MaxOutputChars] + "\n... (output truncated)" : text;
    }

    public static string Serialize(bool success, int exitCode, string output) =>
        JsonSerializer.Serialize(new { success, exit_code = exitCode, output = TruncateOutput(output) });

    public static async Task<string> RunAsync(
        IGitCommandRunner runner,
        IReadOnlyList<string> gitArgs,
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(gitArgs, workspaceRoot, cancellationToken).ConfigureAwait(false);
        return Serialize(result.Success, result.ExitCode, result.Output);
    }
}

using System.Text.RegularExpressions;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Узкий <c>dotnet test --filter</c> по затронутым типам (ADR 0148 W6/F).</summary>
public static class AgentL3TouchedTestFilter
{
    private static readonly Regex s_typeName = new(
        @"\b(?:class|record|struct|interface)\s+(\w+)",
        RegexOptions.Compiled);

    public static async Task<string?> BuildFilterExpressionAsync(
        AgentEnvironmentLadderSettings ladder,
        IGitCommandRunner? git,
        string? workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        if (!ladder.L3TouchedTestsOnly)
            return null;

        if (git is null || string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        var paths = await AgentGitDirtyCsPaths.CollectAsync(
            git,
            workspaceRoot,
            ladder.L0GitDirtyMaxFiles,
            cancellationToken).ConfigureAwait(false);

        if (paths.Count == 0)
            return null;

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text;
            try
            {
                text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                continue;
            }

            foreach (Match m in s_typeName.Matches(text))
                names.Add(m.Groups[1].Value);
        }

        if (names.Count == 0)
            return null;

        return string.Join("|", names.Select(n => $"FullyQualifiedName~{n}"));
    }
}

using CascadeIDE.Models;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record L0DiagnosticsOutcome(
    bool Green,
    int ErrorCount,
    int WarningCount,
    string Detail);

/// <summary>
/// L0 Roslyn in-proc (ADR 0148): синтаксис/лексика через парсер (как <see cref="CSharpLanguageService.GetDiagnosticsForFile"/>).
/// По умолчанию — открытые вкладки; при <c>l0_cs_scope = open_tabs_and_git_dirty_cs</c> добавляются <c>.cs</c> из
/// <c>git diff --name-only</c> и staged (<c>--cached</c>) в пределах workspace.
/// </summary>
public sealed class AgentRoslynL0Diagnostics
{
    private readonly CSharpLanguageService? _language;
    private readonly Func<IReadOnlyList<(string Path, string Content)>>? _openCsDocuments;
    private readonly AgentEnvironmentLadderSettings _ladder;
    private readonly IGitCommandRunner? _gitRunner;
    private readonly Func<string?>? _getWorkspaceRoot;

    public AgentRoslynL0Diagnostics(
        CSharpLanguageService? language,
        Func<IReadOnlyList<(string Path, string Content)>>? openCsDocuments,
        AgentEnvironmentLadderSettings? ladderSettings = null,
        IGitCommandRunner? gitRunner = null,
        Func<string?>? getWorkspaceRoot = null)
    {
        _language = language;
        _openCsDocuments = openCsDocuments;
        _ladder = ladderSettings ?? new AgentEnvironmentLadderSettings();
        _gitRunner = gitRunner;
        _getWorkspaceRoot = getWorkspaceRoot;
    }

    public async Task<L0DiagnosticsOutcome> RunAsync(CancellationToken cancellationToken = default)
    {
        if (_language is null || _openCsDocuments is null)
            return new(true, 0, 0, "L0 skipped (no language service)");

        var docsDict = BuildDocumentMap(await CollectPathsAndContentAsync(cancellationToken).ConfigureAwait(false));

        if (docsDict.Count == 0)
        {
            var scopeNote = AgentL0CsScopeParser.IncludesGitDirtyWorktreeCs(_ladder.L0CsScope)
                ? "; no open .cs and git scope yielded none"
                : "";
            return new(true, 0, 0, $"L0 skipped (no .cs inputs{scopeNote})");
        }

        var errors = 0;
        var warnings = 0;

        foreach (var (path, content) in docsDict.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Diagnostic> raw;
            try
            {
                raw = await Task.Run(
                    () => _language!.GetDiagnosticsForFile(path, content ?? "", cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                continue;
            }

            foreach (var d in raw)
            {
                if (d.Severity == DiagnosticSeverity.Error)
                    errors++;
                else if (d.Severity == DiagnosticSeverity.Warning)
                    warnings++;
            }
        }

        var green = errors == 0;
        var detail = $"L0: {errors} errors, {warnings} warnings ({docsDict.Count} file(s))";
        return new(green, errors, warnings, detail);
    }

    private async Task<IReadOnlyList<(string Path, string Content)>> CollectPathsAndContentAsync(
        CancellationToken cancellationToken)
    {
        var resolver = _openCsDocuments!;
        var list = resolver();
        var merged = new List<(string Path, string Content)>(list ?? []);

        if (AgentL0CsScopeParser.IncludesGitDirtyWorktreeCs(_ladder.L0CsScope)
            && _gitRunner is not null
            && _getWorkspaceRoot is not null)
        {
            var ws = _getWorkspaceRoot();
            if (!string.IsNullOrWhiteSpace(ws) && Directory.Exists(ws))
                await AppendGitDirtyCsFromDiskAsync(merged, ws, cancellationToken).ConfigureAwait(false);
        }

        return merged;
    }

    private async Task AppendGitDirtyCsFromDiskAsync(
        List<(string Path, string Content)> merged,
        string workspaceRootRaw,
        CancellationToken cancellationToken)
    {
        var wsFull = Path.GetFullPath(workspaceRootRaw.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var seen = new HashSet<string>(
            merged.Select(t => Path.GetFullPath(t.Path)),
            StringComparer.OrdinalIgnoreCase);

        var unstaged = await GitNameOnlyAsync(wsFull, ["diff", "--name-only"], cancellationToken).ConfigureAwait(false);
        var staged = await GitNameOnlyAsync(wsFull, ["diff", "--name-only", "--cached"], cancellationToken).ConfigureAwait(false);
        var relCs = AgentL0CsScopeParser.MergeGitNameOnlyOutputs(unstaged, staged);

        var cap = Math.Max(1, _ladder.L0GitDirtyMaxFiles);
        var taken = 0;
        foreach (var rel in relCs)
        {
            if (taken >= cap)
                break;

            if (!AgentL0CsScopeParser.TryResolveWorkspaceCs(wsFull, rel, out var full))
                continue;
            if (!seen.Add(full))
                continue;

            string text;
            try
            {
                text = await File.ReadAllTextAsync(full, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                continue;
            }

            merged.Add((full, text));
            taken++;
        }
    }

    private async Task<string?> GitNameOnlyAsync(
        string workingDirectory,
        IReadOnlyList<string> gitArgsTail,
        CancellationToken cancellationToken)
    {
        if (_gitRunner is null)
            return null;

        var args = new List<string> { "-c", "core.quotepath=false" };
        args.AddRange(gitArgsTail);

        var (success, _, output) =
            await _gitRunner.RunAsync(args, workingDirectory, cancellationToken).ConfigureAwait(false);
        return success ? output : null;
    }

    private static Dictionary<string, string> BuildDocumentMap(
        IReadOnlyList<(string Path, string Content)> items)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, content) in items)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;
            dict[Path.GetFullPath(path.Trim())] = content ?? "";
        }

        return dict;
    }
}

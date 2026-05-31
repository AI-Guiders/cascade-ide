using CascadeIDE.Models;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record DiagnoseFilesOutcome(
    bool Green,
    int ErrorCount,
    int WarningCount,
    string Detail);

/// <summary>
/// In-proc Roslyn diagnostics for <see cref="VerifyRung.DiagnoseFiles"/> (ADR 0148): syntax/lexis via parser
/// (as <see cref="CSharpLanguageService.GetDiagnosticsForFile"/>).
/// Default — open tabs; when <c>diagnose_files_cs_scope = open_tabs_and_git_dirty_cs</c> adds <c>.cs</c> from
/// <c>git diff --name-only</c> and staged (<c>--cached</c>) within workspace.
/// </summary>
public sealed class AgentRoslynDiagnoseFilesDiagnostics
{
    private readonly CSharpLanguageService? _language;
    private readonly Func<IReadOnlyList<(string Path, string Content)>>? _openCsDocuments;
    private readonly AgentEnvironmentLadderSettings _ladder;
    private readonly IGitCommandRunner? _gitRunner;
    private readonly Func<string?>? _getWorkspaceRoot;
    private readonly Func<IReadOnlyList<string>>? _getWarmupCsFilePaths;

    public AgentRoslynDiagnoseFilesDiagnostics(
        CSharpLanguageService? language,
        Func<IReadOnlyList<(string Path, string Content)>>? openCsDocuments,
        AgentEnvironmentLadderSettings? ladderSettings = null,
        IGitCommandRunner? gitRunner = null,
        Func<string?>? getWorkspaceRoot = null,
        Func<IReadOnlyList<string>>? getWarmupCsFilePaths = null)
    {
        _language = language;
        _openCsDocuments = openCsDocuments;
        _ladder = ladderSettings ?? new AgentEnvironmentLadderSettings();
        _gitRunner = gitRunner;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getWarmupCsFilePaths = getWarmupCsFilePaths;
    }

    public async Task<DiagnoseFilesOutcome> RunAsync(CancellationToken cancellationToken = default)
    {
        if (_language is null || _openCsDocuments is null)
            return new(true, 0, 0, $"{VerifyRung.DiagnoseFiles} skipped (no language service)");

        var docsDict = BuildDocumentMap(await CollectPathsAndContentAsync(cancellationToken).ConfigureAwait(false));

        if (docsDict.Count == 0)
        {
            var scopeNote = AgentDiagnoseFilesCsScopeParser.IncludesGitDirtyWorktreeCs(_ladder.DiagnoseFilesCsScope)
                ? "; no open .cs and git scope yielded none"
                : _ladder.DiagnoseFilesIncludeWarmupCs
                    ? "; no open .cs and warmup scope yielded none"
                    : "";
            return new(true, 0, 0, $"{VerifyRung.DiagnoseFiles} skipped (no .cs inputs{scopeNote})");
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
        var detail = $"{VerifyRung.DiagnoseFiles}: {errors} errors, {warnings} warnings ({docsDict.Count} file(s))";
        return new(green, errors, warnings, detail);
    }

    private async Task<IReadOnlyList<(string Path, string Content)>> CollectPathsAndContentAsync(
        CancellationToken cancellationToken)
    {
        var resolver = _openCsDocuments!;
        var list = resolver();
        var merged = new List<(string Path, string Content)>(list ?? []);

        if (AgentDiagnoseFilesCsScopeParser.IncludesGitDirtyWorktreeCs(_ladder.DiagnoseFilesCsScope)
            && _gitRunner is not null
            && _getWorkspaceRoot is not null)
        {
            var ws = _getWorkspaceRoot();
            if (!string.IsNullOrWhiteSpace(ws) && Directory.Exists(ws))
                await AppendGitDirtyCsFromDiskAsync(merged, ws, cancellationToken).ConfigureAwait(false);
        }

        if (_ladder.DiagnoseFilesIncludeWarmupCs && _getWarmupCsFilePaths is not null)
            await AppendWarmupCsFromDiskAsync(merged, cancellationToken).ConfigureAwait(false);

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
        var relCs = AgentDiagnoseFilesCsScopeParser.MergeGitNameOnlyOutputs(unstaged, staged);

        var cap = Math.Max(1, _ladder.DiagnoseFilesGitDirtyMaxFiles);
        var taken = 0;
        foreach (var rel in relCs)
        {
            if (taken >= cap)
                break;

            if (!AgentDiagnoseFilesCsScopeParser.TryResolveWorkspaceCs(wsFull, rel, out var full))
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

    private async Task AppendWarmupCsFromDiskAsync(
        List<(string Path, string Content)> merged,
        CancellationToken cancellationToken)
    {
        var paths = _getWarmupCsFilePaths!();
        if (paths.Count == 0)
            return;

        var seen = new HashSet<string>(
            merged.Select(t => Path.GetFullPath(t.Path)),
            StringComparer.OrdinalIgnoreCase);

        var cap = _ladder.DiagnoseFilesWarmupMaxFiles > 0
            ? Math.Max(1, _ladder.DiagnoseFilesWarmupMaxFiles)
            : Math.Max(1, paths.Count);

        var taken = 0;
        foreach (var path in paths)
        {
            if (taken >= cap)
                break;

            string full;
            try
            {
                full = Path.GetFullPath(path.Trim());
            }
            catch
            {
                continue;
            }

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

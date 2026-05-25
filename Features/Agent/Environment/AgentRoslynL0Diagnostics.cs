using CascadeIDE.Services;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Features.Agent.Environment;

public sealed record L0DiagnosticsOutcome(
    bool Green,
    int ErrorCount,
    int WarningCount,
    string Detail);

/// <summary>L0 Roslyn in-proc (ADR 0148 W2). Open .cs tabs only for MLP.</summary>
public sealed class AgentRoslynL0Diagnostics
{
    private readonly CSharpLanguageService? _language;
    private readonly Func<IReadOnlyList<(string Path, string Content)>>? _openCsDocuments;

    public AgentRoslynL0Diagnostics(
        CSharpLanguageService? language,
        Func<IReadOnlyList<(string Path, string Content)>>? openCsDocuments = null)
    {
        _language = language;
        _openCsDocuments = openCsDocuments;
    }

    public async Task<L0DiagnosticsOutcome> RunAsync(CancellationToken cancellationToken = default)
    {
        if (_language is null || _openCsDocuments is null)
            return new(true, 0, 0, "L0 skipped (no language service)");

        var docs = _openCsDocuments();
        if (docs.Count == 0)
            return new(true, 0, 0, "L0 skipped (no open .cs)");

        var errors = 0;
        var warnings = 0;

        foreach (var (path, content) in docs)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Diagnostic> raw;
            try
            {
                raw = await Task.Run(
                    () => _language.GetDiagnosticsForFile(path, content ?? "", cancellationToken),
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
        var detail = $"L0: {errors} errors, {warnings} warnings ({docs.Count} file(s))";
        return new(green, errors, warnings, detail);
    }
}

#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Pure helpers for shell/settings reactive handlers.
/// Keeps small normalization and page-coercion decisions out of MainWindowViewModel.
/// </summary>
[ApplicationOrchestrator]
public static class ShellSettingsOrchestrator
{
    public static string NormalizeExternalMcpServersJson(string? value) =>
        value ?? "[]";

    public static string NormalizeKrokiBaseUrl(string value) =>
        string.IsNullOrWhiteSpace(value) ? "https://kroki.io" : value.Trim();

    public static string NormalizeAiMode(string value) =>
        AiSettings.NormalizeMode(value);

    public static string NormalizeCloudProvider(string value) =>
        AiSettings.NormalizeCloudProvider(value);

    public static bool ShouldRewriteWithNormalizedValue(string original, string normalized) =>
        !string.Equals(original, normalized, StringComparison.Ordinal);

    public static string? NormalizeOptionalSecret(string value) =>
        string.IsNullOrEmpty(value) ? null : value;

    public static bool ShouldCoerceCurrentPageWhenHidden(MfdShellPage currentPage, MfdShellPage hiddenPage) =>
        currentPage == hiddenPage;

    public static bool ShouldCoerceWhenInstrumentationHidden(MfdShellPage currentPage) =>
        currentPage is MfdShellPage.Events or MfdShellPage.Tests or MfdShellPage.Hypotheses or MfdShellPage.DebugStack;

    /// <summary>Hybrid Codebase Index: <c>workspace</c> or <c>workspace+solution</c> (TOML <c>[hybrid_index].scope_mode</c>).</summary>
    public static string NormalizeHybridIndexScopeMode(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.Equals(v, "workspace", StringComparison.OrdinalIgnoreCase))
            return "workspace";
        return "workspace+solution";
    }

    /// <summary>Whitespace-trim для каталога индекса под корнем workspace; пустая строка = дефолт в рантайме.</summary>
    public static string NormalizeHybridIndexDir(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}

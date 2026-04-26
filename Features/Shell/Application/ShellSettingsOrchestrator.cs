#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Pure helpers for shell/settings reactive handlers.
/// Keeps small normalization and page-coercion decisions out of MainWindowViewModel.
/// </summary>
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
}

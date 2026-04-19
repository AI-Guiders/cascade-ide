namespace CascadeIDE.Services.Lsp;

/// <summary>Пресеты запуска Markdown language server (Marksman и др.), один процесс на workspace.</summary>
public static class MarkdownLspProviderIds
{
    public const string Off = "Off";

    /// <summary>Marksman — ссылки и диагностики по <c>*.md</c> в корне workspace.</summary>
    public const string Marksman = "Marksman";

    public const string Custom = "Custom";

    public static readonly string[] All = [Off, Marksman, Custom];

    public static (string fileName, string arguments) ResolveProcessArgs(
        string providerId,
        string? userExecutable,
        string? userArguments)
    {
        var extra = string.IsNullOrWhiteSpace(userArguments) ? "" : userArguments.Trim();
        return providerId switch
        {
            Marksman => (
                string.IsNullOrWhiteSpace(userExecutable) ? "marksman" : userExecutable.Trim(),
                extra),
            Custom => (
                userExecutable?.Trim() ?? "",
                extra),
            _ => ("", "")
        };
    }
}

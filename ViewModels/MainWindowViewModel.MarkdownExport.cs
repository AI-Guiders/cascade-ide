using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Экспорт Markdown.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanExportExpandedMarkdown))]
    private async Task ExportExpandedMarkdownAsync()
    {
        if (!CanExportExpandedMarkdown())
            return;

        var sourcePath = CurrentFilePath!;
        var raw = EditorText ?? "";

        try
        {
            var expanded = MarkdownIncludeExpansion.ExpandMarkdown(raw, sourcePath);
            var outPath = RequestSaveMarkdownFile is not null
                ? await RequestSaveMarkdownFile(sourcePath).ConfigureAwait(true)
                : GetDefaultExpandedMarkdownPath(sourcePath);

            if (string.IsNullOrWhiteSpace(outPath))
                return;

            File.WriteAllText(outPath, expanded);
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Export expanded Markdown", $"Saved: {outPath}");
        }
        catch (IncludeExpansionException iex)
        {
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Export expanded Markdown failed", iex.Message);
        }
        catch (Exception ex)
        {
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Export expanded Markdown failed", ex.Message);
        }
    }

    private bool CanExportExpandedMarkdown() =>
        IsMarkdownFile && !string.IsNullOrWhiteSpace(CurrentFilePath);

    private static string GetDefaultExpandedMarkdownPath(string sourcePath)
    {
        try
        {
            var full = CanonicalFilePath.Normalize(sourcePath);
            var dir = Path.GetDirectoryName(full) ?? Directory.GetCurrentDirectory();
            var name = Path.GetFileNameWithoutExtension(full);
            if (string.IsNullOrWhiteSpace(name))
                name = "export";
            return Path.Combine(dir, $"{name}.expanded.md");
        }
        catch
        {
            return "export.expanded.md";
        }
    }
}


using CascadeIDE.Features.Markdown.Application;
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

        var outPath = RequestSaveMarkdownFile is not null
            ? await RequestSaveMarkdownFile(sourcePath).ConfigureAwait(true)
            : ExpandedMarkdownDefaultExportPath.Resolve(sourcePath);

        if (string.IsNullOrWhiteSpace(outPath))
            return;

        var outcome = ExpandedMarkdownFileExportProjection.TryExpandAndWriteAllText(sourcePath, raw, outPath);
        if (outcome.Ok)
        {
            if (RequestShowInfoAsync is not null)
                await RequestShowInfoAsync("Export expanded Markdown", $"Saved: {outPath}");
            return;
        }

        if (RequestShowInfoAsync is not null)
            await RequestShowInfoAsync("Export expanded Markdown failed", outcome.ErrorMessage ?? "Unknown error");
    }

    private bool CanExportExpandedMarkdown() =>
        IsMarkdownFile && !string.IsNullOrWhiteSpace(CurrentFilePath);

}


using CommunityToolkit.Mvvm.Input;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>RelayCommand для MCP UI preview (остались на MWVM, не на <see cref="Features.IdeMcp.Application.MainWindowIdeMcpHost"/>).</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void ShowMarkdownPreviewPage()
    {
        ApplyMfdRegionExpanded(true);
        MarkdownPreviewTool.RefreshFromEditor();
        TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }

    [RelayCommand]
    private void OpenPreviewWindow() => RequestShowMarkdownPreviewForEditor?.Invoke();
}

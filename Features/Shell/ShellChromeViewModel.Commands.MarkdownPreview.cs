using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Shell;

/// <summary>Relay: Markdown preview в MFD и отдельное окно.</summary>
public sealed partial class ShellChromeViewModel
{
    [RelayCommand]
    private void ShowMarkdownPreviewPage()
    {
        _host.ApplyMfdRegionExpanded(true);
        _host.MarkdownPreviewTool.RefreshFromEditor();
        _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }

    [RelayCommand]
    private void OpenPreviewWindow() => _host.RequestShowMarkdownPreviewForEditor?.Invoke();
}

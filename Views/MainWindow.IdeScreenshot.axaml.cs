using Avalonia.Controls;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private Task<string> CaptureMainWindowForMcpCoreAsync(string? workspacePath, string? outputRelativePath)
    {
        var (bytes, w, h) = IdeWindowScreenshot.CaptureMainWindowPng(this);
        string? saved = null;
        if (!string.IsNullOrWhiteSpace(workspacePath) && !string.IsNullOrWhiteSpace(outputRelativePath))
            saved = IdeWindowScreenshot.TrySaveUnderWorkspace(workspacePath, outputRelativePath, bytes);
        var json = IdeWindowScreenshot.BuildCaptureJson(bytes, w, h, saved);
        return Task.FromResult(json);
    }
}

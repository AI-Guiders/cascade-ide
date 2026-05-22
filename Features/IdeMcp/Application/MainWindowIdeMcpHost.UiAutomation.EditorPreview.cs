using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public void FocusEditor()
    {
        UiScheduler.Default.Post(() => _host.McpFocusEditorAction?.Invoke());
    }

    public void SetBreakpoint(string filePath, int line, string? condition) =>
        _host.McpRegisterIdeMcpBreakpoint(filePath, line, condition);

    public void RemoveBreakpoint(string filePath, int line)
    {
        if (!IdeMcpUiAutomationOrchestrator.TryGetRemoveBreakpointNormalizedPath(filePath, line, out var path))
            return;
        var ws = _host.McpGetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.RemoveBreakpointForBundledSampleTarget(ws, path, line);
        _host.McpNotifyBreakpointGlyphBindings();
        _host.McpResyncDapBreakpointsFireAndForget();
    }

    /// <summary>РџРµСЂРµРєР»СЋС‡РёС‚СЊ Р±СЂРµР№РєРїРѕРёРЅС‚ РІ .dotnet-debug-mcp-breakpoints.json РґР»СЏ С‚РµРєСѓС‰РµРіРѕ С„Р°Р№Р»Р° Рё СЃС‚СЂРѕРєРё (РєР»РёРє РїРѕ РїРѕР»СЋ РІ СЂРµРґР°РєС‚РѕСЂРµ).</summary>
    public void ToggleBreakpointInFile(int line)
    {
        if (IdeMcpUiAutomationOrchestrator.ShouldSkipToggleBreakpointInEditor(line, _host.CurrentFilePath))
            return;
        var ws = _host.McpGetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
            return;
        var filePath = _host.CurrentFilePath!;
        BreakpointsFileService.ToggleBreakpoint(ws, filePath, line);
        _host.McpNotifyBreakpointGlyphBindings();
        _host.McpResyncDapBreakpointsFireAndForget();
    }

    public void ShowPreview(string title, string content)
    {
        var t = IdeMcpUiAutomationOrchestrator.ResolveMarkdownPreviewTitle(title);
        var c = IdeMcpUiAutomationOrchestrator.NormalizeTextInput(content);
        UiScheduler.Default.Post(() => _host.RequestShowMarkdownPreviewWindow?.Invoke(t, c));
    }

    public void ShowEditorPreview()
    {
        UiScheduler.Default.Post(() =>
        {
            if (_host.ShowMarkdownPreviewPageCommand.CanExecute(null))
                _host.ShowMarkdownPreviewPageCommand.Execute(null);
        });
    }

}

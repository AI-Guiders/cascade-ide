using Avalonia.Threading;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Presentation;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Features.Editor;

/// <summary>HUD над редактором: диагностики и вхождения символа (ADR 0021 §9).</summary>
public sealed partial class EditorWorkspaceViewModel
{
    private EditorHudStabilizedContext? _hudStabilizedContext;
    private string? _editorHudBannerText;
    private IDisposable? _editorHudBannerDebounce;

    public string? EditorHudBannerText
    {
        get => _editorHudBannerText;
        set
        {
            if (SetProperty(ref _editorHudBannerText, value))
                OnPropertyChanged(nameof(IsEditorHudBannerVisible));
        }
    }

    public bool IsEditorHudBannerVisible => !string.IsNullOrWhiteSpace(_editorHudBannerText);

    internal void OnWorkspaceDiagnosticsChangedForHud()
    {
        InvalidateStabilizedHudDiagnosticSnapshot();
        RefreshEditorHudBanner();
    }

    internal void SetStabilizedEditorHudContext(EditorHudStabilizedContext? context) =>
        _hudStabilizedContext = context;

    internal void ScheduleEditorHudBannerRefresh()
    {
        StopEditorHudBannerDebounce();
        _editorHudBannerDebounce = DispatcherTimer.RunOnce(
            () => RefreshEditorHudBanner(),
            TimeSpan.FromMilliseconds(100));
    }

    internal void RefreshEditorHudBanner()
    {
        StopEditorHudBannerDebounce();

        var path = CurrentFilePath;
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            InvalidateStabilizedHudDiagnosticSnapshot();
            EditorHudBannerText = null;
            return;
        }

        int errors;
        int warns;
        if (_hudStabilizedContext is { } st
            && string.Equals(st.FilePath, path, StringComparison.OrdinalIgnoreCase))
        {
            errors = st.Snapshot.ErrorCount;
            warns = st.Snapshot.WarningCount;
        }
        else
        {
            var strips = _host.HostWorkspaceDiagnostics.GetStripsForFile(path);
            var snap = SemanticProjectionPipeline.FromDiagnosticStrips(strips);
            errors = snap.ErrorCount;
            warns = snap.WarningCount;
        }

        var diagPart = EditorHudBannerTextComposer.FormatDiagnosticSummary(errors, warns);

        string? refPart = null;
        var text = EditorText;
        if (!string.IsNullOrEmpty(text))
        {
            var (line, column) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(
                text,
                _host.McpEditorCaretOffset ?? EditorSelectionStart);
            try
            {
                var spans = _host.HostCsharpLanguageService.GetHighlightSpans(path, text, line, column);
                if (spans.Count > 0)
                    refPart = EditorHudBannerTextComposer.FormatReferenceOccurrenceSummary(spans.Count);
            }
            catch
            {
            }
        }

        var combined = EditorHudBannerTextComposer.Combine(diagPart, refPart);
        EditorHudBannerText = string.IsNullOrEmpty(combined) ? null : combined;
    }

    private void StopEditorHudBannerDebounce()
    {
        _editorHudBannerDebounce?.Dispose();
        _editorHudBannerDebounce = null;
    }

    private void InvalidateStabilizedHudDiagnosticSnapshot() => _hudStabilizedContext = null;
}

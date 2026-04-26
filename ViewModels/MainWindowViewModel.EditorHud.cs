using Avalonia.Threading;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Presentation;
using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Полоса HUD над редактором (ADR 0021 §9): баннеры без отдельного якоря-колонки.
/// Основной сценарий продукта — внешний агент (например Cursor) + Cascade; текст сюда задаётся явно
/// (MCP, диагностика, позже — встроенная автономия), а не «по умолчанию» от автономного цикла Power.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// После стабилизированного ввода (ADR 0103); инвалидируется при <see cref="Services.WorkspaceDiagnosticsCoordinator.DiagnosticsChanged"/>.
    /// </summary>
    private EditorHudStabilizedContext? _hudStabilizedContext;

    private string? _editorHudBannerText;

    /// <summary>Текст баннера; пусто — полоса скрыта (Dark Cockpit).</summary>
    public string? EditorHudBannerText
    {
        get => _editorHudBannerText;
        set
        {
            if (SetProperty(ref _editorHudBannerText, value))
                OnPropertyChanged(nameof(IsEditorHudBannerVisible));
        }
    }

    /// <summary>Показать полосу <see cref="EditorHudBannerText"/> под зоной HUD.</summary>
    public bool IsEditorHudBannerVisible => !string.IsNullOrWhiteSpace(_editorHudBannerText);

    private void OnWorkspaceDiagnosticsChangedForHud()
    {
        InvalidateStabilizedHudDiagnosticSnapshot();
        RefreshEditorHudBanner();
    }

    private IDisposable? _editorHudBannerDebounce;

    private void StopEditorHudBannerDebounce()
    {
        _editorHudBannerDebounce?.Dispose();
        _editorHudBannerDebounce = null;
    }

    /// <summary>
    /// Как при перетаскивании выделения <see cref="Cursor"/> двигается плотным потоком событий;
    /// <see cref="RefreshEditorHudBanner"/> (Roslyn + вхождения) на каждом — джанк вёрстки. Откладываем
    /// до «тишины» ~100ms после последнего сдвига.
    /// </summary>
    /// <remarks>
    /// Долгоживущий <see cref="DispatcherTimer"/> в VM рядом со стартом приложения иногда приводил к падению
    /// (создание таймера до готовности dispatcher / неверный поток). <see cref="DispatcherTimer.RunOnce"/> тот же API, что в <see cref="UiAgentHighlight"/>.
    /// </remarks>
    private void ScheduleEditorHudBannerRefresh()
    {
        StopEditorHudBannerDebounce();
        _editorHudBannerDebounce = DispatcherTimer.RunOnce(
            () => RefreshEditorHudBanner(),
            TimeSpan.FromMilliseconds(100));
    }

    /// <summary>Инвалидация: при смене диагностик снимок из hi-freq контура устарел.</summary>
    private void InvalidateStabilizedHudDiagnosticSnapshot() => _hudStabilizedContext = null;

    /// <summary>Вызывается из активной <c>DockDocumentView</c> через <see cref="EditorDocumentHudLayer.BuildStabilizedContext"/> (ADR 0103).</summary>
    internal void SetStabilizedEditorHudContext(EditorHudStabilizedContext? context) =>
        _hudStabilizedContext = context;

    /// <summary>
    /// Сводка для активного <c>.cs</c>: диагностики из <see cref="WorkspaceDiagnostics"/>
    /// (Roslyn по открытому файлу + внешний C# LSP, например OmniSharp, когда подключён) и
    /// опционально число вхождений символа под курсором в этом файле (тот же Roslyn, что подсветка вхождений).
    /// </summary>
    private void RefreshEditorHudBanner()
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
            var strips = WorkspaceDiagnostics.GetStripsForFile(path);
            var snap = SemanticProjectionPipeline.FromDiagnosticStrips(strips);
            errors = snap.ErrorCount;
            warns = snap.WarningCount;
        }

        var diagPart = EditorHudBannerTextComposer.FormatDiagnosticSummary(errors, warns);

        string? refPart = null;
        var text = EditorText;
        if (!string.IsNullOrEmpty(text))
        {
            var (line, column) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(text, _editorCaretOffset ?? EditorSelectionStart);
            try
            {
                var spans = _csharpLanguageService.GetHighlightSpans(path, text, line, column);
                if (spans.Count > 0)
                    refPart = EditorHudBannerTextComposer.FormatReferenceOccurrenceSummary(spans.Count);
            }
            catch
            {
                // одиночный файл / парсинг: не блокируем HUD
            }
        }

        var combined = EditorHudBannerTextComposer.Combine(diagPart, refPart);
        if (string.IsNullOrEmpty(combined))
        {
            EditorHudBannerText = null;
            return;
        }

        EditorHudBannerText = combined;
    }
}

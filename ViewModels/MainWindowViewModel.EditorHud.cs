using Avalonia.Threading;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Полоса HUD над редактором (ADR 0021 §9): баннеры без отдельного якоря-колонки.
/// Основной сценарий продукта — внешний агент (например Cursor) + Cascade; текст сюда задаётся явно
/// (MCP, диагностика, позже — встроенная автономия), а не «по умолчанию» от автономного цикла Power.
/// </summary>
public partial class MainWindowViewModel
{
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

    private void OnWorkspaceDiagnosticsChangedForHud() => RefreshEditorHudBanner();

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
            EditorHudBannerText = null;
            return;
        }

        var strips = WorkspaceDiagnostics.GetStripsForFile(path);
        var errors = strips.Count(s => s.Severity == DiagnosticSeverity.Error);
        var warns = strips.Count(s => s.Severity == DiagnosticSeverity.Warning);

        string? diagPart = null;
        if (errors > 0 && warns > 0)
            diagPart = $"{errors} ошибок, {warns} предупреждений";
        else if (errors > 0)
            diagPart = errors == 1 ? "1 ошибка" : $"{errors} ошибок";
        else if (warns > 0)
            diagPart = warns == 1 ? "1 предупреждение" : $"{warns} предупреждений";

        string? refPart = null;
        var text = EditorText;
        if (!string.IsNullOrEmpty(text))
        {
            var (line, column) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(text, _editorCaretOffset ?? EditorSelectionStart);
            try
            {
                var spans = _csharpLanguageService.GetHighlightSpans(path, text, line, column);
                if (spans.Count > 0)
                {
                    refPart = spans.Count == 1
                        ? "1 вхождение в файле"
                        : $"{spans.Count} вхождений в файле";
                }
            }
            catch
            {
                // одиночный файл / парсинг: не блокируем HUD
            }
        }

        if (diagPart is null && refPart is null)
        {
            EditorHudBannerText = null;
            return;
        }

        if (diagPart is not null && refPart is not null)
            EditorHudBannerText = $"{diagPart} · {refPart}";
        else
            EditorHudBannerText = diagPart ?? refPart;
    }
}

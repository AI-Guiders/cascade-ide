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

    private void OnWorkspaceDiagnosticsChangedForHud() => RefreshEditorHudBannerFromDiagnostics();

    /// <summary>Сводка по ошибкам/предупреждениям активного .cs в полосе HUD (ADR 0021 §9).</summary>
    private void RefreshEditorHudBannerFromDiagnostics()
    {
        var path = CurrentFilePath;
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            EditorHudBannerText = null;
            return;
        }

        var strips = WorkspaceDiagnostics.GetStripsForFile(path);
        var errors = strips.Count(s => s.Severity == DiagnosticSeverity.Error);
        var warns = strips.Count(s => s.Severity == DiagnosticSeverity.Warning);
        if (errors == 0 && warns == 0)
        {
            EditorHudBannerText = null;
            return;
        }

        if (errors > 0 && warns > 0)
            EditorHudBannerText = $"{errors} ошибок, {warns} предупреждений";
        else if (errors > 0)
            EditorHudBannerText = errors == 1 ? "1 ошибка" : $"{errors} ошибок";
        else
            EditorHudBannerText = warns == 1 ? "1 предупреждение" : $"{warns} предупреждений";
    }
}

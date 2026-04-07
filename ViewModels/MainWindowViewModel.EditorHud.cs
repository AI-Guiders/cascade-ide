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
}

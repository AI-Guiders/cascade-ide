using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Состояние редактора, Markdown и выбора модели Ollama.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private bool _ollamaAvailable;

    [ObservableProperty]
    private string _ollamaStatus = "Проверка Ollama…";

    /// <summary>True, если IDE запущена как MCP-сервер (--mcp-stdio). Показывать подсказку «управляется агентом».</summary>
    [ObservableProperty]
    private bool _isMcpServerMode;

    public ObservableCollection<string> OllamaModels { get; } = [];

    /// <summary>Список моделей + пункт "Install New" для ComboBox.</summary>
    public ObservableCollection<string> OllamaModelChoices { get; } = [];

    [ObservableProperty]
    private string? _selectedOllamaModel;

    /// <summary>Идентификатор модели Ollama как для чата: выбор в UI, иначе <c>[ai.local.ollama].model</c>.</summary>
    public string EffectiveOllamaModelId =>
        !string.IsNullOrWhiteSpace(SelectedOllamaModel) && SelectedOllamaModel != InstallNewSentinel
            ? SelectedOllamaModel
            : (_settings.Ai.Local.Ollama.Model ?? "qwen2.5-coder:7b");

    /// <summary>Краткое описание выбранной модели (размер, контекст, возможности) из Ollama API.</summary>
    [ObservableProperty]
    private string _selectedModelDetails = "";

    /// <summary>Последняя выбранная реальная модель (для восстановления после "Install New").</summary>
    public string? LastSelectedRealModel { get; set; }

}

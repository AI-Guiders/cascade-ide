using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel.ShellState"/>: режим ИИ и облачные ключи привязаны к нижнему приложению/чату.</summary>
public partial class MainWindowViewModel
{
    /// <summary>ADR 0083: <c>local</c> | <c>acp</c> | <c>mcp_only</c> | <c>cloud</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveAiProvider))]
    [NotifyPropertyChangedFor(nameof(ChatMcpOnly))]
    [NotifyPropertyChangedFor(nameof(IsOllamaSelected))]
    [NotifyPropertyChangedFor(nameof(IsAnthropicSelected))]
    [NotifyPropertyChangedFor(nameof(IsOpenAiSelected))]
    [NotifyPropertyChangedFor(nameof(IsDeepSeekSelected))]
    [NotifyPropertyChangedFor(nameof(IsCursorAcpSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentModelDisplay))]
    [NotifyPropertyChangedFor(nameof(IsAiCloudMode))]
    private string _aiMode = "local";

    /// <summary>Для <c>mode = cloud</c>: <c>anthropic</c> | <c>openai</c> | <c>deepseek</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveAiProvider))]
    [NotifyPropertyChangedFor(nameof(IsAnthropicSelected))]
    [NotifyPropertyChangedFor(nameof(IsOpenAiSelected))]
    [NotifyPropertyChangedFor(nameof(IsDeepSeekSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentModelDisplay))]
    private string _cloudActiveProvider = "anthropic";

    /// <summary>Совместимость с чатом и <see cref="ResolveProvider"/> — вывод из <c>mode</c> и облака.</summary>
    public string ActiveAiProvider => _settings.Ai.ResolveEffectiveProviderUiKey();

    public bool ChatMcpOnly => string.Equals(AiMode, "mcp_only", StringComparison.OrdinalIgnoreCase);

    public bool IsOllamaSelected =>
        string.Equals(AiMode, "local", StringComparison.OrdinalIgnoreCase)
        && string.Equals(_settings.Ai.Local.Backend, "ollama", StringComparison.OrdinalIgnoreCase);

    public bool IsAnthropicSelected =>
        string.Equals(AiMode, "cloud", StringComparison.OrdinalIgnoreCase)
        && string.Equals(CloudActiveProvider, "anthropic", StringComparison.OrdinalIgnoreCase);

    public bool IsOpenAiSelected =>
        string.Equals(AiMode, "cloud", StringComparison.OrdinalIgnoreCase)
        && string.Equals(CloudActiveProvider, "openai", StringComparison.OrdinalIgnoreCase);

    public bool IsDeepSeekSelected =>
        string.Equals(AiMode, "cloud", StringComparison.OrdinalIgnoreCase)
        && string.Equals(CloudActiveProvider, "deepseek", StringComparison.OrdinalIgnoreCase);

    public bool IsCursorAcpSelected => string.Equals(AiMode, "acp", StringComparison.OrdinalIgnoreCase);

    public bool IsAiCloudMode => string.Equals(AiMode, "cloud", StringComparison.OrdinalIgnoreCase);

    /// <summary>Отображаемое имя модели (для облачных — из настроек).</summary>
    public string CurrentModelDisplay => ActiveAiProvider switch
    {
        "Anthropic" => _settings.Ai.Cloud.Anthropic.Model,
        "OpenAI" => _settings.Ai.Cloud.OpenAi.Model,
        "DeepSeek" => _settings.Ai.Cloud.DeepSeek.Model,
        "CursorACP" => "Cursor ACP",
        _ => SelectedOllamaModel ?? _settings.Ai.Local.Ollama.Model ?? ""
    };

    [ObservableProperty]
    private string _anthropicApiKey = "";

    [ObservableProperty]
    private string _openAiApiKey = "";

    [ObservableProperty]
    private string _deepSeekApiKey = "";
}

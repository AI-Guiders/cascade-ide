namespace CascadeIDE.Models;

/// <summary>AI-провайдеры, модели и путь Cursor ACP (<c>[ai]</c> в <c>settings.toml</c>).</summary>
public sealed class AiSettings
{
    /// <summary>Ollama-модель по умолчанию для чата (TOML: <c>default_ollama_model</c>).</summary>
    public string DefaultOllamaModel { get; set; } = "qwen2.5-coder:7b";

    /// <summary>Активный провайдер: Ollama, Anthropic, OpenAI, DeepSeek, CursorACP (TOML: <c>provider</c>).</summary>
    public string Provider { get; set; } = "Ollama";

    public string AnthropicModel { get; set; } = "claude-sonnet-4-20250514";
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com";
    public string OpenAiModel { get; set; } = "gpt-4o";
    public string DeepSeekBaseUrl { get; set; } = "https://api.deepseek.com";
    public string DeepSeekModel { get; set; } = "deepseek-chat";

    /// <summary>Путь к <c>cursor-agent.cmd</c> или каталогу с <c>dist-package\\cursor-agent.cmd</c>; если пусто — поиск <c>cursor-agent</c> в PATH (TOML: <c>cursor_acp_path</c>).</summary>
    public string CursorAcpPath { get; set; } = "";

    /// <summary>
    /// Где показывать «Параметры AI и чата»: <c>mfd</c> — страница вторичного контура (зона Mfd); <c>window</c> — отдельное окно со всеми настройками (TOML: <c>ai_chat_settings_presentation</c>).
    /// </summary>
    public string AiChatSettingsPresentation { get; set; } = "mfd";

    /// <summary>
    /// Не вызывать встроенный провайдер (Ollama/облако/Cursor ACP) после отправки user-сообщения — ответы только через внешний MCP (<c>send_chat</c> с <c>role=assistant</c>). TOML: <c>chat_mcp_only</c>.
    /// </summary>
    public bool ChatMcpOnly { get; set; }

    /// <summary>
    /// Показывать reasoning/thinking сообщения в истории после завершения ответа. Если false — thinking виден только во время стриминга. TOML: <c>show_thinking_in_history</c>.
    /// </summary>
    public bool ShowThinkingInHistory { get; set; } = true;
}

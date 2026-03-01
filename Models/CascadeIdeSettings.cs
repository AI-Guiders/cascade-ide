using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.).</summary>
public sealed class CascadeIdeSettings : ModelBase
{
    /// <summary>Предпочитаемая модель Ollama для чата (под ноутбук + MCP/tool calling: qwen2.5-coder:7b).</summary>
    public string PreferredOllamaModel { get; set; } = "qwen2.5-coder:7b";

    /// <summary>Включить MCP-сервер IDE при запуске с --mcp-stdio (агент подключается к IDE по stdio).</summary>
    public bool IdeMcpServerEnabled { get; set; } = true;

    /// <summary>Активный провайдер: Ollama, Anthropic, OpenAI, DeepSeek.</summary>
    public string ActiveAiProvider { get; set; } = "Ollama";

    /// <summary>Модель Anthropic (например claude-sonnet-4-20250514).</summary>
    public string AnthropicModelId { get; set; } = "claude-sonnet-4-20250514";

    /// <summary>Base URL для OpenAI (https://api.openai.com).</summary>
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com";

    /// <summary>Модель OpenAI (например gpt-4o).</summary>
    public string OpenAiModelId { get; set; } = "gpt-4o";

    /// <summary>Base URL для DeepSeek (https://api.deepseek.com).</summary>
    public string DeepSeekBaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>Модель DeepSeek (например deepseek-chat).</summary>
    public string DeepSeekModelId { get; set; } = "deepseek-chat";

    /// <summary>Видимость панели «Решение» (Solution Explorer).</summary>
    public bool SolutionExplorerVisible { get; set; } = true;

    /// <summary>Видимость панели «Терминал».</summary>
    public bool TerminalVisible { get; set; } = false;

    /// <summary>Режим интерфейса: Focus, Balanced, Power.</summary>
    public string UiMode { get; set; } = "Balanced";

    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not CascadeIdeSettings o)
            return false;
        return PreferredOllamaModel.Is(o.PreferredOllamaModel)
            && IdeMcpServerEnabled.Is(o.IdeMcpServerEnabled)
            && ActiveAiProvider.Is(o.ActiveAiProvider)
            && AnthropicModelId.Is(o.AnthropicModelId)
            && OpenAiBaseUrl.Is(o.OpenAiBaseUrl)
            && OpenAiModelId.Is(o.OpenAiModelId)
            && DeepSeekBaseUrl.Is(o.DeepSeekBaseUrl)
            && DeepSeekModelId.Is(o.DeepSeekModelId)
            && SolutionExplorerVisible.Is(o.SolutionExplorerVisible)
            && TerminalVisible.Is(o.TerminalVisible)
            && UiMode.Is(o.UiMode);
    }

    public override ModelBase Clone()
    {
        return new CascadeIdeSettings
        {
            PreferredOllamaModel = PreferredOllamaModel,
            IdeMcpServerEnabled = IdeMcpServerEnabled,
            ActiveAiProvider = ActiveAiProvider,
            AnthropicModelId = AnthropicModelId,
            OpenAiBaseUrl = OpenAiBaseUrl,
            OpenAiModelId = OpenAiModelId,
            DeepSeekBaseUrl = DeepSeekBaseUrl,
            DeepSeekModelId = DeepSeekModelId,
            SolutionExplorerVisible = SolutionExplorerVisible,
            TerminalVisible = TerminalVisible,
            UiMode = UiMode
        };
    }
}

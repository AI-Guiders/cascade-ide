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

    /// <summary>
    /// Внешние MCP-серверы для автономного режима (stdio).
    /// Формат JSON массива:
    /// [{"name":"roslyn-mcp","command":"dotnet","arguments":["run","--project","..."],"toolPrefix":"roslyn"}]
    /// Поле toolPrefix опционально (если пустое — используется name).
    /// </summary>
    public string ExternalMcpServersJson { get; set; } = "[]";

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

    /// <summary>
    /// Путь к <c>cursor-agent.cmd</c> из архива Cursor ACP или к каталогу, где лежит <c>dist-package\cursor-agent.cmd</c>.
    /// </summary>
    public string CursorAcpAgentPath { get; set; } = "";

    /// <summary>Видимость панели «Решение» (Solution Explorer).</summary>
    public bool SolutionExplorerVisible { get; set; } = true;

    /// <summary>Видимость панели «Терминал».</summary>
    public bool TerminalVisible { get; set; } = false;

    /// <summary>Видимость вкладки «Git» в нижней док-панели.</summary>
    public bool GitPanelVisible { get; set; } = false;

    /// <summary>Вкладки нижней док-панели Balanced/Power: события, тесты, отладка (без терминала/сборки).</summary>
    public bool InstrumentationDockVisible { get; set; } = true;

    /// <summary>Режим интерфейса: Focus, Balanced, Power.</summary>
    public string UiMode { get; set; } = "Balanced";

    /// <summary>Язык UI (<c>ru-RU</c>, <c>en-US</c>). Пустая строка — при старте берётся системная локаль (<c>UiCulture.ApplyFromSystem</c>).</summary>
    public string UiCultureName { get; set; } = "";

    /// <summary>
    /// Источник диагностик C#: <c>ParseOnly</c> (только парсер), <c>OmniSharp</c>, <c>CSharpLs</c>, <c>Custom</c>.
    /// Один активный LSP-процесс при открытом решении.
    /// </summary>
    public string CSharpLspProvider { get; set; } = "ParseOnly";

    /// <summary>Путь или имя exe для LSP (пусто — пресет по умолчанию: OmniSharp / csharp-ls).</summary>
    public string CSharpLspExecutable { get; set; } = "";

    /// <summary>Дополнительные аргументы командной строки LSP (через пробел).</summary>
    public string CSharpLspArguments { get; set; } = "";

    /// <summary>
    /// Markdown LSP (Marksman): <c>Off</c>, <c>Marksman</c>, <c>Custom</c>. Корень workspace = каталог решения.
    /// </summary>
    public string MarkdownLspProvider { get; set; } = "Off";

    /// <summary>Путь или имя exe (пусто у Marksman = <c>marksman</c> в PATH).</summary>
    public string MarkdownLspExecutable { get; set; } = "";

    /// <summary>Доп. аргументы Markdown LSP.</summary>
    public string MarkdownLspArguments { get; set; } = "";

    /// <summary>
    /// Рендерить блоки <c>```mermaid</c> / <c>```plantuml</c> в превью Markdown через Kroki (текст диаграммы уходит на сервер).
    /// </summary>
    public bool MarkdownKrokiEnabled { get; set; } = true;

    /// <summary>Базовый URL Kroki (публичный <c>https://kroki.io</c> или свой инстанс).</summary>
    public string MarkdownKrokiBaseUrl { get; set; } = "https://kroki.io";

    /// <summary>Строка раскладки по физическим дисплеям (ADR 0017). Пусто — не задано.</summary>
    public string Presentation { get; set; } = "";

    /// <summary>Синоним <see cref="Presentation"/>; задаётся одно из двух, не оба (приоритет у <see cref="Presentation"/>).</summary>
    public string ZoneScreenLayout { get; set; } = "";

    /// <summary>Токены грамматики строки <see cref="Presentation"/> (TOML: секция <c>[presentation_grammar]</c>).</summary>
    public PresentationGrammarSettings PresentationGrammar { get; set; } = new();

    /// <summary>
    /// При пресете «Mfd на втором экране» и ≥2 мониторах — открыть окно-хост зоны Mfd при старте (ADR 0017 v1).
    /// </summary>
    public bool OpenMfdHostWindowOnStartup { get; set; } = true;

    /// <summary>Эффективная строка: <see cref="Presentation"/> или <see cref="ZoneScreenLayout"/>.</summary>
    public string GetEffectivePresentationLine()
    {
        var a = Presentation?.Trim() ?? "";
        if (a.Length > 0)
            return a;
        return ZoneScreenLayout?.Trim() ?? "";
    }

    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not CascadeIdeSettings o)
            return false;
        return PreferredOllamaModel.Is(o.PreferredOllamaModel)
            && IdeMcpServerEnabled.Is(o.IdeMcpServerEnabled)
            && ExternalMcpServersJson.Is(o.ExternalMcpServersJson)
            && ActiveAiProvider.Is(o.ActiveAiProvider)
            && AnthropicModelId.Is(o.AnthropicModelId)
            && OpenAiBaseUrl.Is(o.OpenAiBaseUrl)
            && OpenAiModelId.Is(o.OpenAiModelId)
            && DeepSeekBaseUrl.Is(o.DeepSeekBaseUrl)
            && DeepSeekModelId.Is(o.DeepSeekModelId)
            && CursorAcpAgentPath.Is(o.CursorAcpAgentPath)
            && SolutionExplorerVisible.Is(o.SolutionExplorerVisible)
            && TerminalVisible.Is(o.TerminalVisible)
            && GitPanelVisible.Is(o.GitPanelVisible)
            && InstrumentationDockVisible.Is(o.InstrumentationDockVisible)
            && UiMode.Is(o.UiMode)
            && UiCultureName.Is(o.UiCultureName)
            && CSharpLspProvider.Is(o.CSharpLspProvider)
            && CSharpLspExecutable.Is(o.CSharpLspExecutable)
            && CSharpLspArguments.Is(o.CSharpLspArguments)
            && MarkdownLspProvider.Is(o.MarkdownLspProvider)
            && MarkdownLspExecutable.Is(o.MarkdownLspExecutable)
            && MarkdownLspArguments.Is(o.MarkdownLspArguments)
            && MarkdownKrokiEnabled.Is(o.MarkdownKrokiEnabled)
            && MarkdownKrokiBaseUrl.Is(o.MarkdownKrokiBaseUrl)
            && Presentation.Is(o.Presentation)
            && ZoneScreenLayout.Is(o.ZoneScreenLayout)
            && PresentationGrammarEquals(o.PresentationGrammar)
            && OpenMfdHostWindowOnStartup.Is(o.OpenMfdHostWindowOnStartup);
    }

    private bool PresentationGrammarEquals(PresentationGrammarSettings? o)
    {
        if (o is null)
            return false;
        var a = PresentationGrammar;
        return a.ScreenMarkers.Is(o.ScreenMarkers)
            && a.ScreenSeparator.Is(o.ScreenSeparator)
            && a.ZoneSeparator.Is(o.ZoneSeparator)
            && a.PfdZoneIdentifier.Is(o.PfdZoneIdentifier)
            && a.ForwardZoneIdentifier.Is(o.ForwardZoneIdentifier)
            && a.MfdZoneIdentifier.Is(o.MfdZoneIdentifier);
    }

    public override ModelBase Clone()
    {
        return new CascadeIdeSettings
        {
            PreferredOllamaModel = PreferredOllamaModel,
            IdeMcpServerEnabled = IdeMcpServerEnabled,
            ExternalMcpServersJson = ExternalMcpServersJson,
            ActiveAiProvider = ActiveAiProvider,
            AnthropicModelId = AnthropicModelId,
            OpenAiBaseUrl = OpenAiBaseUrl,
            OpenAiModelId = OpenAiModelId,
            DeepSeekBaseUrl = DeepSeekBaseUrl,
            DeepSeekModelId = DeepSeekModelId,
            CursorAcpAgentPath = CursorAcpAgentPath,
            SolutionExplorerVisible = SolutionExplorerVisible,
            TerminalVisible = TerminalVisible,
            GitPanelVisible = GitPanelVisible,
            InstrumentationDockVisible = InstrumentationDockVisible,
            UiMode = UiMode,
            UiCultureName = UiCultureName,
            CSharpLspProvider = CSharpLspProvider,
            CSharpLspExecutable = CSharpLspExecutable,
            CSharpLspArguments = CSharpLspArguments,
            MarkdownLspProvider = MarkdownLspProvider,
            MarkdownLspExecutable = MarkdownLspExecutable,
            MarkdownLspArguments = MarkdownLspArguments,
            MarkdownKrokiEnabled = MarkdownKrokiEnabled,
            MarkdownKrokiBaseUrl = MarkdownKrokiBaseUrl,
            Presentation = Presentation,
            ZoneScreenLayout = ZoneScreenLayout,
            PresentationGrammar = new PresentationGrammarSettings
            {
                ScreenMarkers = PresentationGrammar.ScreenMarkers,
                ScreenSeparator = PresentationGrammar.ScreenSeparator,
                ZoneSeparator = PresentationGrammar.ZoneSeparator,
                PfdZoneIdentifier = PresentationGrammar.PfdZoneIdentifier,
                ForwardZoneIdentifier = PresentationGrammar.ForwardZoneIdentifier,
                MfdZoneIdentifier = PresentationGrammar.MfdZoneIdentifier,
            },
            OpenMfdHostWindowOnStartup = OpenMfdHostWindowOnStartup
        };
    }
}

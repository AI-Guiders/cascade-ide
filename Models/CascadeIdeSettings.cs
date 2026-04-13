using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.). В TOML: <c>[csharp_lsp]</c>, <c>[markdown_lsp]</c>, <c>[markdown_diagrams]</c>, <c>[presentation_grammar]</c> и корневые ключи.</summary>
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

    /// <summary>
    /// Где показывать «Параметры AI и чата»: <c>mfd</c> — страница вторичного контура (зона Mfd, ADR 0021);
    /// <c>window</c> — отдельное окно со всеми настройками (редактор, LSP, Markdown и т.д.).
    /// </summary>
    public string AiChatSettingsPresentation { get; set; } = "mfd";

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

    /// <summary>C# LSP — секция <c>[csharp_lsp]</c> (раньше: плоские <c>c_sharp_lsp_*</c>, см. миграцию в <c>SettingsService</c>).</summary>
    public CSharpLspSettings CSharpLsp { get; set; } = new();

    /// <summary>Markdown LSP — <c>[markdown_lsp]</c>.</summary>
    public MarkdownLspSettings MarkdownLsp { get; set; } = new();

    /// <summary>Kroki / диаграммы в превью Markdown — <c>[markdown_diagrams]</c>.</summary>
    public MarkdownDiagramSettings MarkdownDiagrams { get; set; } = new();

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

    /// <summary>Последняя сохранённая позиция окна <c>MfdHostWindow</c> (пиксели); вместе с шириной/высотой — или все заданы, или сброс.</summary>
    public int? MfdHostWindowPixelX { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public int? MfdHostWindowPixelY { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public double? MfdHostWindowWidth { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public double? MfdHostWindowHeight { get; set; }

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
            && AiChatSettingsPresentation.Is(o.AiChatSettingsPresentation)
            && SolutionExplorerVisible.Is(o.SolutionExplorerVisible)
            && TerminalVisible.Is(o.TerminalVisible)
            && GitPanelVisible.Is(o.GitPanelVisible)
            && InstrumentationDockVisible.Is(o.InstrumentationDockVisible)
            && UiMode.Is(o.UiMode)
            && UiCultureName.Is(o.UiCultureName)
            && CSharpLspEquals(CSharpLsp, o.CSharpLsp)
            && MarkdownLspEquals(MarkdownLsp, o.MarkdownLsp)
            && MarkdownDiagramsEquals(MarkdownDiagrams, o.MarkdownDiagrams)
            && Presentation.Is(o.Presentation)
            && ZoneScreenLayout.Is(o.ZoneScreenLayout)
            && PresentationGrammarEquals(o.PresentationGrammar)
            && OpenMfdHostWindowOnStartup.Is(o.OpenMfdHostWindowOnStartup)
            && MfdHostWindowPixelX == o.MfdHostWindowPixelX
            && MfdHostWindowPixelY == o.MfdHostWindowPixelY
            && Nullable.Equals(MfdHostWindowWidth, o.MfdHostWindowWidth)
            && Nullable.Equals(MfdHostWindowHeight, o.MfdHostWindowHeight);
    }

    private static bool CSharpLspEquals(CSharpLspSettings? a, CSharpLspSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Provider.Is(b.Provider) && a.Executable.Is(b.Executable) && a.Arguments.Is(b.Arguments);
    }

    private static bool MarkdownLspEquals(MarkdownLspSettings? a, MarkdownLspSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Provider.Is(b.Provider) && a.Executable.Is(b.Executable) && a.Arguments.Is(b.Arguments);
    }

    private static bool MarkdownDiagramsEquals(MarkdownDiagramSettings? a, MarkdownDiagramSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.KrokiEnabled == b.KrokiEnabled && a.KrokiBaseUrl.Is(b.KrokiBaseUrl);
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
            AiChatSettingsPresentation = AiChatSettingsPresentation,
            SolutionExplorerVisible = SolutionExplorerVisible,
            TerminalVisible = TerminalVisible,
            GitPanelVisible = GitPanelVisible,
            InstrumentationDockVisible = InstrumentationDockVisible,
            UiMode = UiMode,
            UiCultureName = UiCultureName,
            CSharpLsp = new CSharpLspSettings
            {
                Provider = CSharpLsp.Provider,
                Executable = CSharpLsp.Executable,
                Arguments = CSharpLsp.Arguments,
            },
            MarkdownLsp = new MarkdownLspSettings
            {
                Provider = MarkdownLsp.Provider,
                Executable = MarkdownLsp.Executable,
                Arguments = MarkdownLsp.Arguments,
            },
            MarkdownDiagrams = new MarkdownDiagramSettings
            {
                KrokiEnabled = MarkdownDiagrams.KrokiEnabled,
                KrokiBaseUrl = MarkdownDiagrams.KrokiBaseUrl,
            },
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
            OpenMfdHostWindowOnStartup = OpenMfdHostWindowOnStartup,
            MfdHostWindowPixelX = MfdHostWindowPixelX,
            MfdHostWindowPixelY = MfdHostWindowPixelY,
            MfdHostWindowWidth = MfdHostWindowWidth,
            MfdHostWindowHeight = MfdHostWindowHeight
        };
    }
}

using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.).</summary>
public sealed class CascadeIdeSettings : ModelBase
{
    /// <summary>AI-провайдеры, модели и путь Cursor ACP (<c>[ai]</c>).</summary>
    public AiSettings Ai { get; set; } = new();

    /// <summary>Встроенный MCP-сервер IDE и внешние MCP-конфиги (<c>[mcp]</c>).</summary>
    public McpSettings Mcp { get; set; } = new();

    /// <summary>Параметры панелей и UI-режима workspace (<c>[workspace_ui]</c>).</summary>
    public WorkspaceUiSettings WorkspaceUi { get; set; } = new();

    /// <summary>
    /// Где показывать «Параметры AI и чата»: <c>mfd</c> — страница вторичного контура (зона Mfd, ADR 0021);
    /// <c>window</c> — отдельное окно со всеми настройками (редактор, LSP, Markdown и т.д.).
    /// </summary>
    public string AiChatSettingsPresentation { get; set; } = "mfd";

    /// <summary>C# LSP — секция <c>[csharp_lsp]</c>.</summary>
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

    /// <summary>Пресеты фильтра видов для <c>get_workspace_navigation_context</c> (<c>[workspace_navigation_context]</c>).</summary>
    public WorkspaceNavigationContextSettings WorkspaceNavigationContext { get; set; } = new();

    /// <summary>
    /// При пресете «Mfd на втором экране» и >=2 мониторах — открыть окно-хост зоны Mfd при старте (ADR 0017 v1).
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
        return AiEquals(Ai, o.Ai)
            && McpEquals(Mcp, o.Mcp)
            && WorkspaceUiEquals(WorkspaceUi, o.WorkspaceUi)
            && AiChatSettingsPresentation.Is(o.AiChatSettingsPresentation)
            && CSharpLspEquals(CSharpLsp, o.CSharpLsp)
            && MarkdownLspEquals(MarkdownLsp, o.MarkdownLsp)
            && MarkdownDiagramsEquals(MarkdownDiagrams, o.MarkdownDiagrams)
            && Presentation.Is(o.Presentation)
            && ZoneScreenLayout.Is(o.ZoneScreenLayout)
            && PresentationGrammarEquals(o.PresentationGrammar)
            && WorkspaceNavigationContextEquals(WorkspaceNavigationContext, o.WorkspaceNavigationContext)
            && OpenMfdHostWindowOnStartup.Is(o.OpenMfdHostWindowOnStartup)
            && MfdHostWindowPixelX == o.MfdHostWindowPixelX
            && MfdHostWindowPixelY == o.MfdHostWindowPixelY
            && Nullable.Equals(MfdHostWindowWidth, o.MfdHostWindowWidth)
            && Nullable.Equals(MfdHostWindowHeight, o.MfdHostWindowHeight);
    }

    private static bool AiEquals(AiSettings? a, AiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.DefaultOllamaModel.Is(b.DefaultOllamaModel)
            && a.Provider.Is(b.Provider)
            && a.AnthropicModel.Is(b.AnthropicModel)
            && a.OpenAiBaseUrl.Is(b.OpenAiBaseUrl)
            && a.OpenAiModel.Is(b.OpenAiModel)
            && a.DeepSeekBaseUrl.Is(b.DeepSeekBaseUrl)
            && a.DeepSeekModel.Is(b.DeepSeekModel)
            && a.CursorAcpPath.Is(b.CursorAcpPath);
    }

    private static bool McpEquals(McpSettings? a, McpSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.StdioServerEnabled == b.StdioServerEnabled
            && a.ExternalServersJson.Is(b.ExternalServersJson);
    }

    private static bool WorkspaceUiEquals(WorkspaceUiSettings? a, WorkspaceUiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ShowSolutionExplorer == b.ShowSolutionExplorer
            && a.ShowTerminal == b.ShowTerminal
            && a.ShowGit == b.ShowGit
            && a.ShowInstrumentation == b.ShowInstrumentation
            && a.Mode.Is(b.Mode)
            && a.Culture.Is(b.Culture);
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

    private static bool WorkspaceNavigationContextEquals(WorkspaceNavigationContextSettings? a, WorkspaceNavigationContextSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return string.Equals(a.PresetsJson?.Trim(), b.PresetsJson?.Trim(), StringComparison.Ordinal);
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
            Ai = new AiSettings
            {
                DefaultOllamaModel = Ai.DefaultOllamaModel,
                Provider = Ai.Provider,
                AnthropicModel = Ai.AnthropicModel,
                OpenAiBaseUrl = Ai.OpenAiBaseUrl,
                OpenAiModel = Ai.OpenAiModel,
                DeepSeekBaseUrl = Ai.DeepSeekBaseUrl,
                DeepSeekModel = Ai.DeepSeekModel,
                CursorAcpPath = Ai.CursorAcpPath,
            },
            Mcp = new McpSettings
            {
                StdioServerEnabled = Mcp.StdioServerEnabled,
                ExternalServersJson = Mcp.ExternalServersJson,
            },
            WorkspaceUi = new WorkspaceUiSettings
            {
                ShowSolutionExplorer = WorkspaceUi.ShowSolutionExplorer,
                ShowTerminal = WorkspaceUi.ShowTerminal,
                ShowGit = WorkspaceUi.ShowGit,
                ShowInstrumentation = WorkspaceUi.ShowInstrumentation,
                Mode = WorkspaceUi.Mode,
                Culture = WorkspaceUi.Culture,
            },
            AiChatSettingsPresentation = AiChatSettingsPresentation,
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
            WorkspaceNavigationContext = new WorkspaceNavigationContextSettings
            {
                PresetsJson = WorkspaceNavigationContext.PresetsJson
            },
            OpenMfdHostWindowOnStartup = OpenMfdHostWindowOnStartup,
            MfdHostWindowPixelX = MfdHostWindowPixelX,
            MfdHostWindowPixelY = MfdHostWindowPixelY,
            MfdHostWindowWidth = MfdHostWindowWidth,
            MfdHostWindowHeight = MfdHostWindowHeight
        };
    }
}

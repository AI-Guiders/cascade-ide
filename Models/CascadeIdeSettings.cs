using System.Text.Json.Serialization;
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

    /// <summary>C# LSP — секция <c>[csharp_lsp]</c>.</summary>
    public CSharpLspSettings CSharpLsp { get; set; } = new();

    /// <summary>Markdown LSP — <c>[markdown_lsp]</c>.</summary>
    public MarkdownLspSettings MarkdownLsp { get; set; } = new();

    /// <summary>Kroki / диаграммы в превью Markdown — <c>[markdown_diagrams]</c>.</summary>
    public MarkdownDiagramSettings MarkdownDiagrams { get; set; } = new();

    /// <summary>Раскладка дисплеев и окно-хост Mfd — <c>[display]</c> в <c>settings.toml</c>.</summary>
    public DisplaySettings Display { get; set; } = new();

    /// <summary>Строка раскладки по физическим дисплеям (ADR 0017). Пусто — не задано.</summary>
    /// <remarks>В TOML задаётся в <see cref="Display"/> (<c>[display]</c>); свойство — прокси для кода.</remarks>
    [JsonIgnore]
    public string Presentation
    {
        get => Display.Presentation;
        set => Display.Presentation = value ?? "";
    }

    /// <summary>Синоним <see cref="Presentation"/>; задаётся одно из двух, не оба (приоритет у <see cref="Presentation"/>).</summary>
    [JsonIgnore]
    public string ZoneScreenLayout
    {
        get => Display.ZoneScreenLayout;
        set => Display.ZoneScreenLayout = value ?? "";
    }

    /// <summary>Токены грамматики строки <see cref="Presentation"/> (TOML: секция <c>[presentation_grammar]</c>).</summary>
    public PresentationGrammarSettings PresentationGrammar { get; set; } = new();

    /// <summary>Пресеты фильтра видов для <c>get_workspace_navigation_context</c> (<c>[workspace_navigation_context]</c>).</summary>
    public WorkspaceNavigationContextSettings WorkspaceNavigationContext { get; set; } = new();

    /// <summary>
    /// При пресете «Mfd на втором экране» и >=2 мониторах — открыть окно-хост зоны Mfd при старте (ADR 0017 v1).
    /// </summary>
    [JsonIgnore]
    public bool OpenMfdHostWindowOnStartup
    {
        get => Display.OpenMfdHostWindowOnStartup;
        set => Display.OpenMfdHostWindowOnStartup = value;
    }

    /// <summary>Последняя сохранённая позиция окна <c>MfdHostWindow</c> (пиксели); вместе с шириной/высотой — или все заданы, или сброс.</summary>
    [JsonIgnore]
    public int? MfdHostWindowPixelX
    {
        get => Display.MfdHostWindowPixelX;
        set => Display.MfdHostWindowPixelX = value;
    }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    [JsonIgnore]
    public int? MfdHostWindowPixelY
    {
        get => Display.MfdHostWindowPixelY;
        set => Display.MfdHostWindowPixelY = value;
    }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    [JsonIgnore]
    public double? MfdHostWindowWidth
    {
        get => Display.MfdHostWindowWidth;
        set => Display.MfdHostWindowWidth = value;
    }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    [JsonIgnore]
    public double? MfdHostWindowHeight
    {
        get => Display.MfdHostWindowHeight;
        set => Display.MfdHostWindowHeight = value;
    }

    /// <summary>Эффективная строка: <see cref="Presentation"/> или <see cref="ZoneScreenLayout"/>.</summary>
    public string GetEffectivePresentationLine()
    {
        var a = Display.Presentation?.Trim() ?? "";
        if (a.Length > 0)
            return a;
        return Display.ZoneScreenLayout?.Trim() ?? "";
    }

    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not CascadeIdeSettings o)
            return false;
        return AiEquals(Ai, o.Ai)
            && McpEquals(Mcp, o.Mcp)
            && WorkspaceUiEquals(WorkspaceUi, o.WorkspaceUi)
            && CSharpLspEquals(CSharpLsp, o.CSharpLsp)
            && MarkdownLspEquals(MarkdownLsp, o.MarkdownLsp)
            && MarkdownDiagramsEquals(MarkdownDiagrams, o.MarkdownDiagrams)
            && DisplayEquals(Display, o.Display)
            && PresentationGrammarEquals(o.PresentationGrammar)
            && WorkspaceNavigationContextEquals(WorkspaceNavigationContext, o.WorkspaceNavigationContext);
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
            && a.CursorAcpPath.Is(b.CursorAcpPath)
            && a.AiChatSettingsPresentation.Is(b.AiChatSettingsPresentation)
            && a.ChatMcpOnly == b.ChatMcpOnly;
    }

    private static bool McpEquals(McpSettings? a, McpSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ExternalServersJson.Is(b.ExternalServersJson)
            && a.AcpAutoInjectIdeMcp == b.AcpAutoInjectIdeMcp;
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

    private static bool DisplayEquals(DisplaySettings? a, DisplaySettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Presentation.Is(b.Presentation)
            && a.ZoneScreenLayout.Is(b.ZoneScreenLayout)
            && a.OpenMfdHostWindowOnStartup == b.OpenMfdHostWindowOnStartup
            && a.MfdHostWindowPixelX == b.MfdHostWindowPixelX
            && a.MfdHostWindowPixelY == b.MfdHostWindowPixelY
            && Nullable.Equals(a.MfdHostWindowWidth, b.MfdHostWindowWidth)
            && Nullable.Equals(a.MfdHostWindowHeight, b.MfdHostWindowHeight)
            && a.UseSkiaZoneGeometryPreview == b.UseSkiaZoneGeometryPreview
            && a.UseSkiaInstrumentWave3Preview == b.UseSkiaInstrumentWave3Preview;
    }

    private static bool WorkspaceNavigationContextEquals(WorkspaceNavigationContextSettings? a, WorkspaceNavigationContextSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return WorkspaceNavigationPresetListsEqual(a.Presets, b.Presets);
    }

    private static bool WorkspaceNavigationPresetListsEqual(
        IReadOnlyList<WorkspaceNavigationPresetEntry> a,
        IReadOnlyList<WorkspaceNavigationPresetEntry> b)
    {
        var da = a.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        var db = b.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        if (da.Count != db.Count)
            return false;
        foreach (var kv in da)
        {
            if (!db.TryGetValue(kv.Key, out var other))
                return false;
            if (!WorkspaceNavigationPresetEntryEquals(kv.Value, other))
                return false;
        }

        return true;
    }

    private static bool WorkspaceNavigationPresetEntryEquals(WorkspaceNavigationPresetEntry a, WorkspaceNavigationPresetEntry b)
    {
        if (!string.Equals(a.Id?.Trim(), b.Id?.Trim(), StringComparison.Ordinal))
            return false;
        if (!StringListEqual(a.IncludeKinds, b.IncludeKinds))
            return false;
        if (!StringListEqual(a.ExcludeKinds, b.ExcludeKinds))
            return false;
        return true;
    }

    private static bool StringListEqual(IReadOnlyList<string>? x, IReadOnlyList<string>? y)
    {
        if (x is null && y is null)
            return true;
        if (x is null || y is null)
            return false;
        if (x.Count != y.Count)
            return false;
        var sa = x.OrderBy(s => s, StringComparer.Ordinal).ToList();
        var sb = y.OrderBy(s => s, StringComparer.Ordinal).ToList();
        for (var i = 0; i < sa.Count; i++)
        {
            if (!string.Equals(sa[i], sb[i], StringComparison.Ordinal))
                return false;
        }

        return true;
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
                AiChatSettingsPresentation = Ai.AiChatSettingsPresentation,
                ChatMcpOnly = Ai.ChatMcpOnly,
            },
            Mcp = new McpSettings
            {
                ExternalServersJson = Mcp.ExternalServersJson,
                AcpAutoInjectIdeMcp = Mcp.AcpAutoInjectIdeMcp,
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
            Display = new DisplaySettings
            {
                Presentation = Display.Presentation,
                ZoneScreenLayout = Display.ZoneScreenLayout,
                OpenMfdHostWindowOnStartup = Display.OpenMfdHostWindowOnStartup,
                MfdHostWindowPixelX = Display.MfdHostWindowPixelX,
                MfdHostWindowPixelY = Display.MfdHostWindowPixelY,
                MfdHostWindowWidth = Display.MfdHostWindowWidth,
                MfdHostWindowHeight = Display.MfdHostWindowHeight,
                UseSkiaZoneGeometryPreview = Display.UseSkiaZoneGeometryPreview,
                UseSkiaInstrumentWave3Preview = Display.UseSkiaInstrumentWave3Preview,
            },
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
                Presets = WorkspaceNavigationContext.Presets
                    .Select(p => new WorkspaceNavigationPresetEntry
                    {
                        Id = p.Id,
                        IncludeKinds = p.IncludeKinds?.ToList(),
                        ExcludeKinds = p.ExcludeKinds?.ToList()
                    })
                    .ToList()
            },
        };
    }
}

using System.Text.Json.Serialization;
using OutWit.Common.Abstract;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.).</summary>
public sealed partial class CascadeIdeSettings : ModelBase
{
    public AiSettings Ai { get; set; } = new();

    public McpSettings Mcp { get; set; } = new();

    /// <summary>Hybrid Codebase Index integration toggles (in-proc). TOML: <c>[hybrid_index]</c>.</summary>
    public HybridIndexSettings HybridIndex { get; set; } = new();

    /// <summary>Прогрев при открытии solution (ADR 0141). TOML: <c>[solution_warmup]</c>.</summary>
    public SolutionWarmupSettings SolutionWarmup { get; set; } = new();

    /// <summary>Палитра команд и go-to workspace. TOML: <c>[command_palette.go_to_search]</c> и др.; ADR 0112.</summary>
    public CommandPaletteSettings CommandPalette { get; set; } = new();

    /// <summary>Agent-notes / knowledge (встроенный KB-Base и оверлей). TOML: <c>[agent_notes]</c>.</summary>
    public AgentNotesSettings AgentNotes { get; set; } = new();

    public WorkspaceSettings Workspace { get; set; } = new();

    public CodeNavigationMapSettings CodeNavigationMap { get; set; } = new();

    public LanguagesSettings Languages { get; set; } = new();

    public MarkdownSettings Markdown { get; set; } = new();

    public DisplaySettings Display { get; set; } = new();

    public EditorSettings Editor { get; set; } = new();

    /// <summary>Шрифты Intercom, редактора и др. TOML: <c>[fonts.*]</c>.</summary>
    public FontsSettings Fonts { get; set; } = new();

    /// <summary>Пресеты навигации по коду / решению (ADR 0039, Code Navigation Context). TOML: <c>[code_navigation]</c>.</summary>
    public CodeNavigationSettings CodeNavigation { get; set; } = new();

    /// <summary>Intercom (вложения, лента). TOML: <c>[intercom]</c>, code-attach — <c>[intercom.attachments.code]</c> (ADR 0130).</summary>
    public IntercomSettings Intercom { get; set; } = new();

    /// <summary>Диагностические логи. TOML: <c>[logging.intercom]</c> и др.</summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// При пресете «Mfd на втором экране» и >=2 мониторах — открыть окно-хост зоны Mfd при старте (ADR 0017 v1).
    /// </summary>
    [JsonIgnore]
    public bool OpenMfdHostWindowOnStartup
    {
        get => Display.Mfd.OpenOnStartup;
        set => Display.Mfd.OpenOnStartup = value;
    }

    [JsonIgnore]
    public int? MfdHostWindowPixelX
    {
        get => Display.Mfd.PixelX;
        set => Display.Mfd.PixelX = value;
    }

    [JsonIgnore]
    public int? MfdHostWindowPixelY
    {
        get => Display.Mfd.PixelY;
        set => Display.Mfd.PixelY = value;
    }

    [JsonIgnore]
    public double? MfdHostWindowWidth
    {
        get => Display.Mfd.Width;
        set => Display.Mfd.Width = value;
    }

    [JsonIgnore]
    public double? MfdHostWindowHeight
    {
        get => Display.Mfd.Height;
        set => Display.Mfd.Height = value;
    }

    [JsonIgnore]
    public bool OpenPfdHostWindowOnStartup
    {
        get => Display.Pfd.OpenOnStartup;
        set => Display.Pfd.OpenOnStartup = value;
    }

    [JsonIgnore]
    public int? PfdHostWindowPixelX
    {
        get => Display.Pfd.PixelX;
        set => Display.Pfd.PixelX = value;
    }

    [JsonIgnore]
    public int? PfdHostWindowPixelY
    {
        get => Display.Pfd.PixelY;
        set => Display.Pfd.PixelY = value;
    }

    [JsonIgnore]
    public double? PfdHostWindowWidth
    {
        get => Display.Pfd.Width;
        set => Display.Pfd.Width = value;
    }

    [JsonIgnore]
    public double? PfdHostWindowHeight
    {
        get => Display.Pfd.Height;
        set => Display.Pfd.Height = value;
    }

    [JsonIgnore]
    public bool OpenPmSplitHostWindowOnStartup
    {
        get => Display.Pm.OpenOnStartup;
        set => Display.Pm.OpenOnStartup = value;
    }

    [JsonIgnore]
    public int? PmSplitHostWindowPixelX
    {
        get => Display.Pm.PixelX;
        set => Display.Pm.PixelX = value;
    }

    [JsonIgnore]
    public int? PmSplitHostWindowPixelY
    {
        get => Display.Pm.PixelY;
        set => Display.Pm.PixelY = value;
    }

    [JsonIgnore]
    public double? PmSplitHostWindowWidth
    {
        get => Display.Pm.Width;
        set => Display.Pm.Width = value;
    }

    [JsonIgnore]
    public double? PmSplitHostWindowHeight
    {
        get => Display.Pm.Height;
        set => Display.Pm.Height = value;
    }

    [JsonIgnore]
    public bool MaximizePresentationHostWindowsOnDedicatedScreens
    {
        get => Display.MaximizeHostsOnDedicatedScreens;
        set => Display.MaximizeHostsOnDedicatedScreens = value;
    }

    public string GetEffectivePresentationLine() => Display.Screens.Topology?.Trim() ?? "";

    /// <summary>Грамматика для <see cref="GetEffectivePresentationLine"/> — <see cref="DisplayScreensSettings.Grammar"/>.</summary>
    public PresentationGrammarSettings GetEffectivePresentationGrammar()
    {
        var g = Display.Screens.Grammar;
        return new PresentationGrammarSettings
        {
            Brackets = g.Brackets,
            BetweenScreens = g.BetweenScreens,
            BetweenZones = g.BetweenZones,
            Pfd = g.Pfd,
            Forward = g.Forward,
            Mfd = g.Mfd,
        };
    }
}

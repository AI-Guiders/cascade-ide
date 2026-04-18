using System.Text.Json.Serialization;
using OutWit.Common.Abstract;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.).</summary>
public sealed partial class CascadeIdeSettings : ModelBase
{
    public AiSettings Ai { get; set; } = new();

    public McpSettings Mcp { get; set; } = new();

    public WorkspaceSettings Workspace { get; set; } = new();

    public SemanticMapSettings SemanticMap { get; set; } = new();

    public LanguagesSettings Languages { get; set; } = new();

    public MarkdownSettings Markdown { get; set; } = new();

    public DisplaySettings Display { get; set; } = new();

    /// <summary>Строка топологии дисплеев и грамматика (ADR 0017). TOML: <c>[presentation]</c>.</summary>
    public PresentationLayoutSettings Presentation { get; set; } = new();

    /// <summary>Пресеты навигации по коду / решению (ADR 0039, Code Navigation Context). TOML: <c>[code_navigation]</c>.</summary>
    public CodeNavigationSettings CodeNavigation { get; set; } = new();

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
    public bool MaximizePresentationHostWindowsOnDedicatedScreens
    {
        get => Display.MaximizeHostsOnDedicatedScreens;
        set => Display.MaximizeHostsOnDedicatedScreens = value;
    }

    public string GetEffectivePresentationLine()
    {
        var fromScreens = Display.Screens.Topology?.Trim() ?? "";
        if (fromScreens.Length > 0)
            return fromScreens;
        var a = Presentation.Line?.Trim() ?? "";
        if (a.Length > 0)
            return a;
        return Presentation.LineAlias?.Trim() ?? "";
    }

    /// <summary>
    /// Грамматика для <see cref="GetEffectivePresentationLine"/>: при непустом <c>display.screens.topology</c> — из
    /// <see cref="DisplayScreensSettings.Grammar"/>; иначе из <see cref="PresentationLayoutSettings.Grammar"/>.
    /// </summary>
    public PresentationGrammarSettings GetEffectivePresentationGrammar()
    {
        static PresentationGrammarSettings Copy(PresentationGrammarSettings g) => new()
        {
            Brackets = g.Brackets,
            BetweenScreens = g.BetweenScreens,
            BetweenZones = g.BetweenZones,
            Pfd = g.Pfd,
            Forward = g.Forward,
            Mfd = g.Mfd,
        };

        var topology = Display.Screens.Topology?.Trim() ?? "";
        if (topology.Length > 0)
            return Copy(Display.Screens.Grammar);
        return Copy(Presentation.Grammar);
    }
}

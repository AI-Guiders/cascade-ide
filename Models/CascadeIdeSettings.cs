using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

/// <summary>Настройки CascadeIDE (модель Ollama по умолчанию, MCP, провайдеры ИИ и т.д.).</summary>
public sealed partial class CascadeIdeSettings : ModelBase
{
    /// <summary>AI-провайдеры, модели и путь Cursor ACP (<c>[ai]</c>).</summary>
    public AiSettings Ai { get; set; } = new();

    /// <summary>Встроенный MCP-сервер IDE и внешние MCP-конфиги (<c>[mcp]</c>).</summary>
    public McpSettings Mcp { get; set; } = new();

    /// <summary>Параметры панелей и UI-режима workspace (<c>[workspace_ui]</c>).</summary>
    public WorkspaceUiSettings WorkspaceUi { get; set; } = new();

    /// <summary>Semantic Map в зоне PFD (<c>[semantic_map]</c>).</summary>
    public SemanticMapSettings SemanticMap { get; set; } = new();

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

}

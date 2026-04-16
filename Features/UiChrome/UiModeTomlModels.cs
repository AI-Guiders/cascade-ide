using CascadeIDE.Models;

namespace CascadeIDE.Features.UiChrome;

/// <summary>Корень <c>UiModes/index.toml</c> (schema_version только здесь).</summary>
public sealed class UiModesIndexToml
{
    public int SchemaVersion { get; set; }
    public List<string> Modes { get; set; } = [];
}

/// <summary>
/// Корень <c>UiModes/workspace.toml</c> и тот же формат для репо-файла <c>.cascade/workspace.toml</c> (без собственного schema_version).
/// Секция <see cref="WorkspaceNavigationContext"/> — опционально в репозитории (ADR 0039): командные пресеты навигации.
/// </summary>
public sealed class UiWorkspaceToml
{
    public int? PfdRegionDefaultWidthPixels { get; set; }
    public double? MainGridColumnSplitterWidthPixels { get; set; }
    public int? BottomPanelMinRowPixels { get; set; }
    public int? MfdRegionCollapsedWidthPixels { get; set; }
    public int? MfdRegionExpandedDefaultWidthPixels { get; set; }
    public int? MfdRegionExpandedPowerWidthPixels { get; set; }
    public int? MfdRegionExpandedAgentChatWidthPixels { get; set; }

    /// <summary>
    /// Куда показывать превью Markdown: <c>forward_split</c>, <c>mfd</c>, <c>separate_window</c> (или <c>window</c>);
    /// TOML: <c>markdown_preview_placement</c>.
    /// </summary>
    public string? MarkdownPreviewPlacement { get; set; }

    /// <summary>Привязка id поверхности к каноническому id зоны; TOML: <c>[attention_zone_panels]</c> (ADR 0021).</summary>
    public Dictionary<string, string>? AttentionZonePanels { get; set; }

    /// <summary>
    /// Репозиторный/бандловый слой карты размещения инструментов.
    /// TOML: <c>[[instrument_placement_rules]]</c>.
    /// </summary>
    public List<InstrumentPlacementRuleSettings>? InstrumentPlacementRules { get; set; }

    /// <summary>
    /// Пресеты навигации для репозитория (тот же контракт, что <c>[workspace_navigation_context]</c> в <c>settings.toml</c>).
    /// TOML: <c>[[workspace_navigation_context.presets]]</c>.
    /// </summary>
    public WorkspaceNavigationContextSettings? WorkspaceNavigationContext { get; set; }
}

/// <summary>
/// Один режим: <c>UiModes/&lt;Id&gt;.toml</c>.
/// Имена свойств в PascalCase; <see cref="Services.CascadeTomlSerializer"/> маппит ключи snake_case (как в <c>workspace.toml</c>).
/// Ключи по смыслу интерфейса, не именам полей VM.
/// </summary>
public sealed class UiModeFileToml
{
    public string? Inherits { get; set; }
    public string? Family { get; set; }
    public bool? PfdRegionExpanded { get; set; }
    public bool? BuildOutputVisible { get; set; }
    public bool? TerminalVisible { get; set; }
    public bool? MfdRegionExpanded { get; set; }
    public int? EditorGroupCount { get; set; }
    public string? ThemeSlot { get; set; }
    public bool? SelectTerminalTabWhenTerminalShown { get; set; }
    public int? MfdRegionExpandedWidthPixels { get; set; }

    /// <summary>Нижний док с вкладками событий/тестов и т.д.; TOML: <c>instrumentation_dock_visible</c>.</summary>
    public bool? InstrumentationDockVisible { get; set; }

    /// <summary>Полоса активной задачи под тулбаром; TOML: <c>active_task_strip</c>.</summary>
    public bool? ActiveTaskStrip { get; set; }

    /// <summary>Заголовок главного окна; TOML: <c>main_window_title</c>.</summary>
    public string? MainWindowTitle { get; set; }

    // --- capabilities (ADR 0010)

    public bool? QuickActions { get; set; }
    public bool? AgentOperationsPanel { get; set; }
    public bool? AgentTrace { get; set; }
    public bool? AutonomousAgentTelemetry { get; set; }
    /// <summary>Дубль Workspace Health на вкладке «Терминал» в Power; TOML: <c>workspace_health_on_terminal_tab</c>.</summary>
    public bool? WorkspaceHealthOnTerminalTab { get; set; }
    /// <summary>Column span нижней зоны Workspace Health в основной сетке (Power); TOML: <c>workspace_health_main_column_span</c>.</summary>
    public int? WorkspaceHealthMainColumnSpan { get; set; }
    public bool? InstrumentationTabs { get; set; }
    public bool? HypothesesTab { get; set; }
    public bool? RiskSummaryCard { get; set; }
    public bool? ResultSummaryCard { get; set; }

    /// <summary>Полоса Workspace Health под редактором; TOML: <c>workspace_health_strip</c>.</summary>
    public bool? WorkspaceHealthStrip { get; set; }

    /// <summary><c>bottom_strip</c> | <c>dedicated_page</c>; TOML: <c>workspace_health_surface</c>.</summary>
    public string? WorkspaceHealthSurface { get; set; }

    /// <summary>Панель инструментов под меню; TOML: <c>main_toolbar</c>.</summary>
    public bool? MainToolbar { get; set; }

    /// <summary>Вкладка Problems в нижнем доке; TOML: <c>problems_panel</c>.</summary>
    public bool? ProblemsPanel { get; set; }

    /// <summary>Включить полосу оповещений EICAS (W/C/A); TOML: <c>eicas_alerts_bar</c>. См. ADR 0021 §5, §1.1.</summary>
    public bool? EicasAlertsBar { get; set; }
}

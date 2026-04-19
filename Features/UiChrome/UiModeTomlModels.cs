using CascadeIDE.Models;

namespace CascadeIDE.Features.UiChrome;

/// <summary>Корень <c>UiModes/index.toml</c>: <c>[bundle]</c> со списком режимов.</summary>
public sealed class UiModesIndexToml
{
    public UiModesBundleToml? Bundle { get; set; }
}

/// <summary>TOML: <c>[bundle]</c>.</summary>
public sealed class UiModesBundleToml
{
    public int SchemaVersion { get; set; }
    public List<string> Modes { get; set; } = [];
}

/// <summary>Секция <c>[chrome]</c> в <c>workspace.toml</c> (ADR 0010).</summary>
public sealed class UiWorkspaceChromeToml
{
    public int? PfdRegionDefaultWidthPixels { get; set; }
    public double? MainGridColumnSplitterWidthPixels { get; set; }
    public int? BottomPanelMinRowPixels { get; set; }
    public int? MfdRegionCollapsedWidthPixels { get; set; }
    public int? MfdRegionExpandedDefaultWidthPixels { get; set; }
    public int? MfdRegionExpandedPowerWidthPixels { get; set; }
    public int? MfdRegionExpandedAgentChatWidthPixels { get; set; }

    /// <summary>
    /// Куда показывать preview Markdown: <c>mfd</c>, <c>separate_window</c> (или <c>window</c>);
    /// TOML: <c>markdown_preview_placement</c>.
    /// </summary>
    public string? MarkdownPreviewPlacement { get; set; }
}

/// <summary>
/// Корень <c>UiModes/workspace.toml</c> и <c>.cascade/workspace.toml</c>.
/// TOML: <c>[chrome]</c>, <c>[routing.attention]</c>, <c>[routing.instruments]</c>, <c>[[code_navigation.presets]]</c>.
/// </summary>
public sealed class UiWorkspaceToml
{
    /// <summary>Метрики хрома и превью Markdown.</summary>
    public UiWorkspaceChromeToml? Chrome { get; set; }

    public UiWorkspaceRoutingToml? Routing { get; set; }

    /// <summary>Пресеты навигации по коду (ADR 0039, CNC).</summary>
    public CodeNavigationSettings? CodeNavigation { get; set; }
}

/// <summary>TOML: <c>[meta]</c> — наследование, семья, заголовок, тема.</summary>
public sealed class UiModeMetaToml
{
    public string? Inherits { get; set; }
    public string? Family { get; set; }
    public string? MainWindowTitle { get; set; }
    public string? ThemeSlot { get; set; }
}

/// <summary>TOML: <c>[layout]</c> — видимость панелей и раскладка.</summary>
public sealed class UiModeLayoutToml
{
    public bool? PfdRegionExpanded { get; set; }
    public bool? BuildOutputVisible { get; set; }
    public bool? TerminalVisible { get; set; }
    public bool? MfdRegionExpanded { get; set; }
    public int? EditorGroupCount { get; set; }
    public bool? SelectTerminalTabWhenTerminalShown { get; set; }
    public int? MfdRegionExpandedWidthPixels { get; set; }
    public bool? InstrumentationDockVisible { get; set; }
    public bool? ActiveTaskStrip { get; set; }
}

/// <summary>TOML: <c>[capabilities]</c> (ADR 0010).</summary>
public sealed class UiModeCapabilitiesToml
{
    public bool? QuickActions { get; set; }
    public bool? AgentOperationsPanel { get; set; }
    public bool? AgentTrace { get; set; }
    public bool? AutonomousAgentTelemetry { get; set; }
    public bool? WorkspaceHealthOnTerminalTab { get; set; }
    public int? WorkspaceHealthMainColumnSpan { get; set; }
    public bool? InstrumentationTabs { get; set; }
    public bool? HypothesesTab { get; set; }
    public bool? RiskSummaryCard { get; set; }
    public bool? ResultSummaryCard { get; set; }
    public bool? WorkspaceHealthStrip { get; set; }
    public string? WorkspaceHealthSurface { get; set; }
    public bool? ProblemsPanel { get; set; }
    public bool? EicasAlertsBar { get; set; }
}

/// <summary>Один режим: <c>UiModes/&lt;Id&gt;.toml</c> — <c>[meta]</c>, <c>[layout]</c>, <c>[capabilities]</c>.</summary>
public sealed class UiModeFileToml
{
    public UiModeMetaToml? Meta { get; set; }
    public UiModeLayoutToml? Layout { get; set; }
    public UiModeCapabilitiesToml? Capabilities { get; set; }
}

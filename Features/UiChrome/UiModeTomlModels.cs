namespace CascadeIDE.Features.UiChrome;

/// <summary>Корень <c>UiModes/index.toml</c> (schema_version только здесь).</summary>
public sealed class UiModesIndexToml
{
    public int SchemaVersion { get; set; }
    public List<string> Modes { get; set; } = [];
}

/// <summary>Корень <c>UiModes/workspace.toml</c> (без собственного schema_version).</summary>
public sealed class UiWorkspaceToml
{
    public int? SolutionExplorerDefaultWidthPixels { get; set; }
    public double? MainGridColumnSplitterWidthPixels { get; set; }
    public int? BottomPanelMinRowPixels { get; set; }
    public int? ChatPanelCollapsedWidthPixels { get; set; }
    public int? ChatPanelExpandedDefaultWidthPixels { get; set; }
    public int? ChatPanelExpandedPowerWidthPixels { get; set; }
    public int? ChatPanelExpandedAgentChatWidthPixels { get; set; }
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
    public bool? SolutionExplorerVisible { get; set; }
    public bool? BuildOutputVisible { get; set; }
    public bool? TerminalVisible { get; set; }
    public bool? ChatPanelExpanded { get; set; }
    public int? EditorGroupCount { get; set; }
    public string? ThemeSlot { get; set; }
    public bool? SelectTerminalTabWhenTerminalShown { get; set; }
    public int? ChatExpandedWidthPixels { get; set; }

    /// <summary>Полоса активной задачи под тулбаром; TOML: <c>active_task_strip</c>.</summary>
    public bool? ActiveTaskStrip { get; set; }

    /// <summary>Заголовок главного окна; TOML: <c>main_window_title</c>.</summary>
    public string? MainWindowTitle { get; set; }

    // --- capabilities (ADR 0010)

    public bool? QuickActions { get; set; }
    public bool? AgentOperationsPanel { get; set; }
    public bool? AgentTrace { get; set; }
    public bool? AutonomousAgentTelemetry { get; set; }
    public bool? TelemetryOnTerminalTab { get; set; }
    public int? TelemetryMainColumnSpan { get; set; }
    public bool? InstrumentationTabs { get; set; }
    public bool? HypothesesTab { get; set; }
    public bool? RiskSummaryCard { get; set; }
    public bool? ResultSummaryCard { get; set; }
}

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

/// <summary>Один режим: <c>UiModes/&lt;Id&gt;.toml</c>.</summary>
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

    /// <summary>Полоса Task Cockpit под тулбаром (активная задача, quick actions). Если не задано — по умолчанию скрыто для семьи Debug.</summary>
    public bool? ShowTaskCockpit { get; set; }

    /// <summary>Полный заголовок главного окна; если не задан — встроенные строки по семье.</summary>
    public string? WindowTitle { get; set; }

    // --- capabilities (ADR 0010); ключи в TOML — snake_case (Tomlyn).

    public bool? ShowQuickActions { get; set; }
    public bool? ShowAgentOperationsBlock { get; set; }
    public bool? ShowAgentTrace { get; set; }
    public bool? ShowPowerTelemetry { get; set; }
    public bool? ShowPowerTelemetryOnTerminalTab { get; set; }

    /// <summary>При Power и видимой полоске телеметрии под редактором (обычно 3).</summary>
    public int? PowerTelemetryMainGridColumnSpan { get; set; }

    public bool? ShowInstrumentationTabs { get; set; }
    public bool? ShowHypothesesTab { get; set; }
    public bool? ShowRiskSummaryCard { get; set; }
    public bool? ShowResultSummaryCard { get; set; }
}

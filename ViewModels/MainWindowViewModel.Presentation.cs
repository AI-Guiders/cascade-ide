using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;

namespace CascadeIDE.ViewModels;

/// <summary>Вычисляемые свойства разметки, телеметрии и видимости панелей (режимы UI).</summary>
public partial class MainWindowViewModel
{
    public static readonly IReadOnlyList<string> UiModeOptions = UiModeLayoutRegistry.OrderedModeIds;
    public IReadOnlyList<string> UiModeOptionsList => UiModeOptions;

    /// <summary>Заголовок главного окна (в Power — подпись «Autonomous Agent Cockpit»).</summary>
    public string WindowTitle =>
        IsPowerMode
            ? "CascadeIDE — Power Mode [Autonomous Agent Cockpit]"
            : IsAgentChatMode
                ? "CascadeIDE — Agent Chat"
                : IsDebugMode
                    ? "CascadeIDE — Debug"
                    : "CascadeIDE";

    public bool IsFocusMode => string.Equals(UiMode, "Focus", StringComparison.OrdinalIgnoreCase);
    public bool IsBalancedMode => string.Equals(UiMode, "Balanced", StringComparison.OrdinalIgnoreCase);
    public bool IsPowerMode => string.Equals(UiMode, "Power", StringComparison.OrdinalIgnoreCase);
    public bool IsAgentChatMode => string.Equals(UiMode, "AgentChat", StringComparison.OrdinalIgnoreCase);
    public bool IsDebugMode => string.Equals(UiMode, "Debug", StringComparison.OrdinalIgnoreCase);

    /// <summary>Ширина колонки чата (пиксели); значения — <see cref="UiModeLayoutRegistry"/> и <see cref="UiModeLayoutDimensions"/>.</summary>
    public int ChatPanelColumnPixelWidth =>
        IsChatPanelExpanded
            ? UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels(NormalizeUiMode(UiMode))
            : UiModeLayoutDimensions.ChatPanelCollapsedWidthPixels;
    public bool ShowTaskBar => true;
    public bool ShowQuickActions => IsBalancedMode;
    public bool ShowAgentOperations => true;
    /// <summary>В Focus справа показываем план и гейт, в Power — trace/safety; блок «операции» остаётся в Balanced.</summary>
    public bool ShowAgentOperationsBlock => IsBalancedMode;
    public bool ShowAgentTrace => IsPowerMode;
    public bool ShowPowerTelemetry => IsPowerMode;
    /// <summary>Карточка уровня безопасности: в Power — крупные L1–L3; в Focus/Balanced — компактные кнопки (разметка в ChatPanelView).</summary>
    public bool ShowSafetyControls => true;
    public bool ShowTelemetryHiddenHint => ShowPowerTelemetry && !IsTerminalVisible;

    /// <summary>
    /// Дублирующая карточка телеметрии на вкладке «Терминал» в Power. Пока видна полоса <see cref="TelemetryStripView"/> под редактором —
    /// false, чтобы DockPanel не отдавал высоту дублю и не схлопывал область вывода консоли.
    /// </summary>
    public bool ShowPowerTelemetryOnTerminalTab => IsPowerMode && !ShowTelemetryStrip;

    /// <summary>Полоска build/tests/debug/git — и в Focus (по концепту).</summary>
    public bool ShowTelemetryStrip => true;

    /// <summary>
    /// В Power полоса телеметрии только под колонками «решение + редактор» (сетка 0–2: дерево, сплиттер, док);
    /// справа trace/safety тянутся вниз — как в макете Power cockpit.
    /// </summary>
    public int MainWorkspaceTelemetryColumnSpan =>
        IsPowerMode && ShowTelemetryStrip ? 3 : 5;

    /// <summary>Чат в одной строке с редактором; телеметрия и док — в нижней строке MainGrid (после сплиттера).</summary>
    public int ChatPanelMainGridRowSpan => 1;

    public string TelemetryButtonText => IsTerminalVisible ? "Telemetry: on" : "Show telemetry";
    public bool ShowEditorGroup2 => EditorGroupCount >= 2;
    public bool ShowEditorGroup3 => EditorGroupCount >= 3;

    /// <summary>Нижние вкладки «События / Тесты / Гипотезы / Отладка» при включённом доке.</summary>
    public bool ShowInstrumentationTabs =>
        IsInstrumentationDockVisible && (IsFocusMode || IsBalancedMode || IsPowerMode || IsAgentChatMode || IsDebugMode);

    /// <summary>Вкладка «Гипотезы» — только в UI-режиме Debug (ADR 0003).</summary>
    public bool ShowHypothesesTab => ShowInstrumentationTabs && IsDebugMode;

    /// <summary>Пункт меню для док-панели инструментирования (можно отключить и в Focus).</summary>
    public bool ShowInstrumentationLayoutMenu => true;

    public bool IsSafetyL1 => string.Equals(SafetyLevel, "L1", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL2 => string.Equals(SafetyLevel, "L2", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL3 => string.Equals(SafetyLevel, "L3", StringComparison.OrdinalIgnoreCase);

    /// <summary>Подпись режима безопасности (как на мокапе Power).</summary>
    public string SafetyLevelDescription =>
        SafetyLevel switch
        {
            "L1" => Resources.Safety_Description_L1,
            "L2" => Resources.Safety_Description_L2,
            "L3" => Resources.Safety_Description_L3,
            _ => ""
        };

    public double SafetyL1Opacity => IsSafetyL1 ? 1 : 0.38;
    public double SafetyL2Opacity => IsSafetyL2 ? 1 : 0.38;
    public double SafetyL3Opacity => IsSafetyL3 ? 1 : 0.38;

    public bool HasFocusPlanItems => FocusPlanItems.Count > 0;

    public bool IsRiskSummaryVisible =>
        !string.IsNullOrWhiteSpace(RiskSummary)
        && !string.Equals(RiskSummary, "Риски не зафиксированы.", StringComparison.Ordinal);

    public bool IsResultSummaryVisible =>
        !string.IsNullOrWhiteSpace(ResultSummary)
        && !string.Equals(ResultSummary, "Результатов пока нет.", StringComparison.Ordinal);

    public bool IsRiskCardVisible => !IsFocusMode && !IsAgentChatMode && IsRiskSummaryVisible;
    public bool IsResultCardVisible => !IsFocusMode && !IsAgentChatMode && IsResultSummaryVisible;
    public bool IsComplexityBadgeVisible => ComplexityBadge > 0;
    public bool IsImpactedTestsBadgeVisible => ImpactedTestsBadge > 0;
    public bool IsActiveTaskProgressVisible => ActiveTaskProgress > 0;

    public string TelemetryBuildText => IsBuilding ? "Build: running…" : "Build: idle";

    /// <summary>Короткий статус для «кольца» сборки в Power cockpit.</summary>
    public string TelemetryBuildCockpitShort => IsBuilding ? "BUILD…" : "READY";

    public string TelemetryTestsText =>
        !string.IsNullOrWhiteSpace(LastTestSummary)
            ? $"Tests: {LastTestSummary}"
            : $"Tests: impacted {ImpactedTestsBadge}";

    /// <summary>Компактная строка тестов для полосы Power.</summary>
    public string TelemetryTestsCockpitShort =>
        !string.IsNullOrWhiteSpace(LastTestSummary)
            ? (LastTestSummary.Length > 36 ? string.Concat(LastTestSummary.AsSpan(0, 33), "…") : LastTestSummary)
            : $"imp {ImpactedTestsBadge}";

    /// <summary>Есть активная DAP-сессия (режим отладки, как в VS).</summary>
    public bool HasDebugSession => _dapDebug.HasActiveSession;

    /// <summary>Выполнение остановлено — доступны шаги и просмотр стека.</summary>
    public bool IsDebugExecutionPaused => _dapDebug.HasActiveSession && _dapDebug.IsExecutionStopped;

    /// <summary>Процесс запущен под отладчиком, выполнение идёт.</summary>
    public bool IsDebugExecutionRunning => _dapDebug.HasActiveSession && !_dapDebug.IsExecutionStopped;

    public string TelemetryDebugText =>
        !_dapDebug.HasActiveSession
            ? "Debug: idle"
            : _dapDebug.IsExecutionStopped
                ? $"Debug: paused · frames {InstrumentationPanel.DebugStackFrames.Count}, vars {InstrumentationPanel.DebugVariables.Count}"
                : "Debug: running…";

    /// <summary>Короткий статус отладки для Power.</summary>
    public string TelemetryDebugCockpitShort =>
        !_dapDebug.HasActiveSession
            ? "DBG · —"
            : _dapDebug.IsExecutionStopped
                ? $"DBG · pause · {InstrumentationPanel.DebugStackFrames.Count}fr"
                : "DBG · run";

    public string ChatPanelToggleButtonText => IsChatPanelExpanded ? "◀" : "▶";
    public bool IsSolutionPanelHidden => !IsSolutionExplorerVisible;
    public bool IsBuildPanelHidden => !IsBuildOutputVisible;
    public bool IsChatPanelHidden => !IsChatPanelExpanded;
    public bool IsTerminalPanelHidden => !IsTerminalVisible;
    public bool IsProblemsPanelVisible => true;

    public bool IsBottomPanelVisible =>
        IsProblemsPanelVisible || IsTerminalVisible || IsBuildOutputVisible || ShowInstrumentationTabs || IsGitPanelVisible;
}

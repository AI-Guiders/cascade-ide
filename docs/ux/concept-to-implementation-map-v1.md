# Concept → Implementation Map v1 (CascadeIDE)

Scope: **CascadeIDE UI concepts** (Focus/Balanced/Power) mapped to current implementation in:

- `Views/MainWindow.axaml`
- `Views/MainWindow.axaml.cs`
- `ViewModels/MainWindowViewModel.cs`

This map is intended to drive incremental alignment work with clear acceptance checks.

## Legend

- **Status**
  - ✅ implemented
  - 🟨 partial / placeholder data
  - ❌ missing

---

## 1) Global layout / docking

| Concept element | XAML control (x:Name / binding) | VM property/command | Status | Notes |
|---|---|---|---|---|
| Main grid columns (left / center / right) | `MainGrid` `ColumnDefinitions="220,4,*,4,340"` | `IsSolutionExplorerVisible`, `IsChatPanelExpanded` | ✅ | Chat width is adjusted in code-behind based on mode (Power=420, else 340). |
| Mode hotkeys | `<Window.KeyBindings>` | `SetFocusModeCommand`, `SetBalancedModeCommand`, `SetPowerModeCommand`, `CycleUiModeCommand` | ✅ | `Alt+1/2/3`, `Ctrl+Alt+M`. |
| Mode switch UI | Toolbar ComboBox + menu radio items | `UiMode`, `UiModeOptionsList` | ✅ | `ModeBadge` adds `.power` class when `IsPowerMode`. |

---

## 2) Focus concept (`concept-generated/cascadeide-ui-concept-focus.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Top bar: task title + status pill | Task bar row (`Grid.Row="2"`) | `ActiveTaskTitle`, `ActiveTaskStatus`, `ActiveTaskProgress` | 🟨 | Present, but currently hidden in Focus because `ShowTaskBar => !IsFocusMode`. Concept shows it in Focus. Decide: either show a minimal task pill in Focus or keep hidden. |
| Minimal left navigation | `SolutionExplorerBorder` | `IsSolutionExplorerVisible` | ✅ | Toggle via menu. |
| Dominant editor | `Editor` (`AvaloniaEdit`) | `EditorText`, `CurrentFilePath` | ✅ | Inline markdown preview exists (`InlinePreviewBorder`). |
| Agent panel (plan/next action/confirmation) | Chat panel blocks | `ShowAgentOperations => !IsFocusMode` | 🟨 | In Focus the concept shows Agent panel; current impl hides Agent Operations in Focus. Needs a Focus-safe subset: plan + next action + confirmation. |
| Bottom status pills (Build/Test/Debug) | Terminal telemetry block (Row=5) + task bar | `ShowPowerTelemetry => IsPowerMode` | ❌ | Concept has a compact always-on bottom status line in Focus; current telemetry is Power-only and lives in Terminal panel. |

---

## 3) Balanced concept (`concept-generated/cascadeide-ui-concept-balanced.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Quick actions (Fix failing tests / Investigate nullref / Prepare commit) | Task bar row buttons | `FixFailingTestsCommand`, `InvestigateNullrefCommand`, `PrepareCommitCommand` + `ShowQuickActions` | ✅ | `ShowQuickActions => !IsFocusMode` so visible in Balanced & Power. |
| Editor badges (Complexity/Impacted/Files changed) | Task bar row borders | `ComplexityBadge`, `ImpactedTestsBadge`, `FilesChangedBadge` | 🟨 | Values exist in VM (currently placeholder numbers). Needs real calculation pipeline. |
| Agent operations card | Chat panel row 0 | `ShowAgentOperations` | ✅ | Present for Balanced/Power. |
| Build/Test/Debug tabs + event timeline | Terminal panel + `EventTimeline` ItemsControl | `EventTimeline`, `IsTerminalVisible` | 🟨 | Event timeline exists but is shown only inside the terminal telemetry block (Power-only). Balanced concept wants build/test/debug always accessible (tabs). Current impl has build output in editor column + debug panel; terminal is separate. |
| Dependency mini-map / solution graph | (no dedicated panel) | — | ❌ | Not implemented. Candidate: add a secondary left-bottom dock in Balanced, or reuse existing panels. |

---

## 4) Power concept (“cockpit”) (`concept-generated/cascadeide-ui-concept-power.png`)

| Concept element | XAML control | VM property/command | Status | Notes |
|---|---|---|---|---|
| Task bar / status cockpit (always-on) | `Grid.Row="2"` card | `ShowTaskBar` | ✅ | `ShowTaskBar => true` (полоска видна и в Focus). |
| “Telemetry” explicit control | Toolbar Telemetry button + hidden hint block | `TelemetryButtonText`, `ToggleTerminalCommand`, `ShowTelemetryHiddenHint` | ✅ | This directly addresses the “where is приборка?” problem. |
| Agent Trace Timeline | Chat panel trace block | `ShowAgentTrace => IsPowerMode`, `AgentTraceSteps`, `TimestampText`, `ExplainTraceStepCommand`, `RollbackTraceStepCommand`, `AppendAgentTraceStep` | ✅ | Structured cards with timestamp, status chips, per-step Explain/Rollback. |
| Safety Level Control + Emergency Stop | Chat panel Power block | `SafetyLevelDescription`, крупные карточки L1/L2/L3, `PowerNeonAccent` кольцо на активном, `EmergencyStopCommand` | ✅ UI / 🟨 enforcement | Визуал как на мокапе; политика инструментов — отдельно. |
| Bottom telemetry strip (build/tests/git/snapshot) | `TelemetryStripView` (Power: одна SOC-полоса + JSON `MaxHeight` 100) | `Telemetry*CockpitShort`, `WorkspaceSnapshotJson`, `power_cockpit` в `power-theme.json` | ✅ | Мини-графики тестов в мокапе — пока текст/компакт; при желании — отдельный шаг. |
| Task queue list (multiple tasks, L1/L2/L3 per item) | `SolutionExplorerView` bottom (Power only) | `PowerTaskQueueItems`, `PowerTaskQueueItemViewModel`, `HasPowerTaskQueueItems` | 🟨 | Panel + model; fills when agent pushes items (empty state until then). |
| Window title “Power Mode [Autonomous Agent Cockpit]” | `MainWindow` `Title` | `WindowTitle` | ✅ | — |

---

## 5) Concrete next alignment steps (minimal increments)

1. **Focus mode contract**: decide which minimal “instrument line” stays visible (task pill + last build/test/debug status) even in Focus.
2. **Task queue feed**: push `PowerTaskQueueItemViewModel` from MCP/agent when multi-task orchestration exists.
3. **Real badge computation**: compute `ComplexityBadge`, `ImpactedTestsBadge`, `FilesChangedBadge` from real state (git + solution graph + test selection).
4. **Dependency mini-map** (Balanced): optional graph dock under solution explorer.


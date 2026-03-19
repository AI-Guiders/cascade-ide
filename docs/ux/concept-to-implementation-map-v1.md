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
| Task bar / status cockpit (always-on) | `Grid.Row="2"` card | `ShowTaskBar` | ✅ | Visible in Power (`ShowTaskBar => !IsFocusMode`). |
| “Telemetry” explicit control | Toolbar Telemetry button + hidden hint block | `TelemetryButtonText`, `ToggleTerminalCommand`, `ShowTelemetryHiddenHint` | ✅ | This directly addresses the “where is приборка?” problem. |
| Agent Trace Timeline | Chat panel row 1 | `ShowAgentTrace => IsPowerMode`, `AgentTraceTimeline`, `ExplainCurrentStepCommand` | 🟨 | Present but currently a simple list + one “Explain” button; concept shows richer step cards with statuses and per-step actions. |
| Safety Level Control + Emergency Stop | Chat panel row 2 | `ShowSafetyControls => IsPowerMode`, `SetSafetyL1/2/3Command`, `EmergencyStopCommand` | 🟨 | Present; L1/L2/L3 are currently string state with buttons. Needs styling + enforcement in tool pipeline. |
| Bottom telemetry strip (build/tests/git/snapshot) | Terminal telemetry block (Row=5 top border) | `ShowPowerTelemetry => IsPowerMode`, `EventTimeline`, `RiskSummary`, `ResultSummary` | 🟨 | Some telemetry exists (build/result/risk + event timeline). Missing git summary + workspace snapshot JSON preview. |
| Task queue list (multiple tasks, L1/L2/L3 per item) | (no dedicated panel) | — | ❌ | Not implemented; currently only `ActiveTask*` single task fields. |

---

## 5) Concrete next alignment steps (minimal increments)

1. **Focus mode contract**: decide which minimal “instrument line” stays visible (task pill + last build/test/debug status) even in Focus.
2. **Telemetry strip extraction**: move Power telemetry from Terminal into a dedicated dock row (so it never “disappears” with terminal).
3. **Agent Trace upgrade**: replace string list with structured step model (status, timestamp, actions).
4. **Task queue model**: introduce `ObservableCollection<TaskQueueItem>` with safety + state; bind to left panel in Power.
5. **Real badge computation**: compute `ComplexityBadge`, `ImpactedTestsBadge`, `FilesChangedBadge` from real state (git + solution graph + test selection).


# CascadeIDE UI Concepts v1 (generated mockups)

Source images:

- `concept-generated/cascadeide-ui-concept-focus.png`
- `concept-generated/cascadeide-ui-concept-balanced.png`
- `concept-generated/cascadeide-ui-concept-power.png`

Goal: describe the concepts precisely enough to drive implementation and acceptance checks.

---

## 0) Shared vocabulary (all modes)

- **Left panel**: navigation (solution/project explorer).
- **Center**: editor surface (tabs + code editor).
- **Right panel**: agent/assistant surface (plan, operations, trace).
- **Bottom strip/panel**: status + build/test/debug signals (the “instrumentation line”).

Key idea: **IDE = cockpit**. The UI must keep agent execution, code context, and telemetry visible without requiring context switches across many windows.

---

## 1) Focus mode concept (`cascadeide-ui-concept-focus.png`)

### Purpose

Single-task flow: keep the editor dominant while preserving just enough navigation and agent guidance to complete one change safely.

### Layout

- **Top bar**:
  - Product label `CascadeIDE`.
  - Workspace/project breadcrumb (e.g., `MyEcommerceProject`).
  - Active task title (e.g., `Refactor payment retries`) + **status pill** (`In progress`).
  - Primary controls on the right: **Run**, **Checkpoint**, **Rollback**.
- **Left**: compact **Solution Explorer** tree.
- **Center**:
  - Document tab (e.g., `PaymentService.cs`).
  - Large code editor with:
    - inline diff hints (green/red bands),
    - breakpoint / marker gutter,
    - “current line” highlight.
- **Right**: **Agent** panel with three stacked blocks:
  - **Plan** checklist (tickable items).
  - **Next Action** (single focused step snippet).
  - **Confirmation** (explicit “Confirm/Cancel” action gate).
- **Bottom instrumentation line**: pill badges for outcomes:
  - Build status/time,
  - Test run counts,
  - Debug state (breakpoint hit),
  - Additional build status.

### Acceptance checklist

- Focus mode always keeps: **editor + minimal nav + agent plan + confirmation gate + bottom status**.
- No “power telemetry”/timeline blocks in focus by default.

---

## 2) Balanced mode concept (`cascadeide-ui-concept-balanced.png`)

### Purpose

General daily work mode: editor remains primary, but secondary panels help with safe iterative execution (build/test/debug) and risk checks.

### Layout

- **Top toolbar**: large action buttons:
  - **Fix failing tests**
  - **Investigate nullref**
  - **Prepare commit**
- **Left column** (two stacked panels):
  - **Solution Graph / Explorer** (projects/files tree).
  - **Dependency Mini‑Map** (graph view of touched dependencies).
- **Center**:
  - Code editor with optional top sub‑strip showing badges:
    - `Complexity: 12`
    - `Impacted tests: 5`
    - `Files changed: 3`
- **Right**:
  - **Agent Operations** stack:
    - Objective (with progress, ~75%),
    - Tool calls (running list),
    - Risk checks (alerts),
    - Result (collapsible).
- **Bottom row** (multi-tab panel):
  - **Build Output**
  - **Test Results**
  - **Debug Stack**
  - plus separate **Event Timeline** (timepoints for analysis / failure / fix).

### Acceptance checklist

- Balanced mode shows: **quick actions**, **risk checks**, **build/test/debug tabs**, and **event timeline**.
- Dependency mini‑map is visible or togglable without leaving the main window.

---

## 3) Power mode concept (“Autonomous Agent Cockpit”) (`cascadeide-ui-concept-power.png`)

### Purpose

Autonomous execution cockpit: a high signal, always-on view of what the agent is doing, why it’s safe, and how to stop/rollback.

### Layout

- **Title**: `CascadeIDE - Power Mode [Autonomous Agent Cockpit]`.
- **Left column**:
  - **Project Explorer** (tree).
  - **Task Queue List** with multiple tasks and **safety levels** per item (L1/L2/L3) + state (Pending/Queued/Paused).
- **Center**:
  - Editor with:
    - breakpoints,
    - debug line highlight,
    - inline diagnostics,
    - warnings block inside editor (e.g., “Highest action detected…”).
- **Right column**: **Agent Trace Timeline** with step cards:
  - Each step has timestamp, label (PLAN/ACTION/OBSERVATION/NEXT),
  - Status chips (SUCCESS/WARNING/PENDING),
  - Action buttons: **Explain this step**, **Rollback**.
- **Bottom cockpit / telemetry strip**:
  - **Build status** (PASSING, last build, duration).
  - **Affected tests run** (sparkline/histogram + totals).
  - **Git status summary** (branch, file counts, unstaged changes).
  - **Workspace snapshot JSON preview**.
- **Safety Level Control** block (right bottom):
  - L1 Read‑only, L2 Confirm edits, L3 Autonomous.
  - Prominent **Emergency Stop**.

### Acceptance checklist

- Power mode must make it impossible to miss:
  - “What is happening now?” (trace timeline),
  - “What changed?” (git + file/touch summary),
  - “Is it safe?” (safety level + confirmations),
  - “How to stop/rollback?” (emergency stop + rollback).

### Visual language (детальный референс дерева)

Автогенерация `concept-generated/cascadeide-ui-concept-power.png` задаёт общую композицию. **Крупный кадр стиля дерева проекта** (тёмный фон, бирюзовая полоска выделения, воздух между строками, иконки типов файлов, заголовок в духе *PROJECT EXPLORER*) сохранён отдельно: **`concept-screens/power-project-explorer-tree-concept.png`** (+ оглавление в `concept-screens/README.md`). В коде левая колонка пока ближе к **стандартному `TreeView` Fluent** внутри «острова» — см. таблицу **§4.1** в `concept-to-implementation-map-v1.md`.

---

## 4) Mapping to current implementation docs

Primary implementation layout reference: `cascade-ide-ui-layout-v1.md`.

When aligning UI, keep these invariants:

- **Focus**: minimal panels + explicit confirmation gate.
- **Balanced**: quick actions + risk checks + build/test/debug output with timeline.
- **Power**: task queue + trace timeline + telemetry strip + safety controls + emergency stop.

---

## 5) Визуальное соответствие коду (кратко)

Функциональные блоки Power в основном заведены на контролы из `cascade-ide-ui-layout-v1.md`. **«Глянец» PNG-концептов** (кастомный хром списков, редакторный gutter, спарклайны в телеметрии) **частично** — детальная матрица: **`concept-to-implementation-map-v1.md` §4.1**.  
Референсы: **`concept-generated/*.png`** (общий макет), **`concept-screens/power-project-explorer-tree-concept.png`** (фрагмент дерева).


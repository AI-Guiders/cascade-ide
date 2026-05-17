<!-- English translation of adr/0012-floating-workspace-chrome.md. Canonical Russian: ../../adr/0012-floating-workspace-chrome.md -->

# ADR 0012: Floating and Detachable Workspace Chrome (Bottom Zone and Situational Awareness)

**Status:** Accepted (direction; v1 scope and concrete controls per iterations)  
**Date:** 2026-04-02  

## Related ADRs

| ADR | Role |
|-----|------|
| [0011](0011-debug-situational-awareness.md) | Awareness without inflating the bottom |
| [0010](0010-ui-modes-toml-configuration.md) | Presets and `workspace.toml` |
| [0006](0006-presentation-layers-and-feature-slices.md) | VM/UI boundaries |
| [0013](0013-command-surface-and-discoverability.md) | See [“Split with 0013”](#scope-split-0013) |

## Split with [0013](0013-command-surface-and-discoverability.md)

- **0012 (this ADR)** — placement and floating workspace chrome.
- **[0013](0013-command-surface-and-discoverability.md)** — command surface: how the user invokes actions and discoverability without bloating the toolbar.

---

## Context

> **Flight (current):** long terminal, build, and Git streams are **MFD column pages** (`MfdShellPageStack` inside `MfdShellView`, snapshot region **`MfdContourStackHost`**). Below — **historical layout** with a full-width bottom zone for the same product pain (**height budget** vs editor).

In the **legacy topology**, the **bottom of the main window** was a **fixed grid** in `MainWindow`: a row with the **IDE Health** strip (build/tests/debug/git summary), splitter, and **`BottomPanelView`** (terminal, build, debug, git, etc.) occupied **one vertical layer** (`MainGrid` row 5, inner rows `Auto` / splitter / `*`). The **IDE Health** strip (`IdeHealthStripView`) sits **above** bottom tabs and **does not float** — it is tied to the same column layout as the work area.

Consequence: any **situational** or **short** UI (status, IDE Health strip, future compact debug strip from [0011](0011-debug-situational-awareness.md)) shares **one height budget** with **long output** (log, terminal). Raising the bottom to see something **costs editor pixels** — already recorded as product pain.

Separately: document layout explicitly assumes **one dock** without floating (`DocumentsDockView` — comment in XAML). That is **not** the same problem class as **chrome** around the editor; mixing them in one solution is risky in scope.

The **top command strip** (toolbar: “Open solution” and neighbors) is today **long and mixed by meaning** — the same fixed-chrome overload as the bottom, only **horizontal**: many buttons in one line without task grouping.

## Decision

<a id="adr0012-p1"></a>
1. **Product direction:** move **secondary and situational workspace chrome** toward **floating / detachable** panels (meaning: **not required to live in a fixed `MainGrid` row** competing with editor height). The user can keep a strip at a screen edge, on a second monitor, or on top — within what the window model allows ([see §3](#adr0012-p3)).

<a id="adr0012-p2"></a>
2. **First candidates to lift out of the fixed bottom column (priority):**
   - **IDE Health** (`IdeHealthStripView` and logically related “narrow” state), stuck **above** `BottomPanelView` in the legacy bottom cell.
   - Then **compact status strips** (including debug from [0011](0011-debug-situational-awareness.md)) so they are not duplicated only in the bottom dock.

<a id="adr0012-p3"></a>
3. **Mechanics (direction, not tied to one Avalonia control in this ADR):**
   - **In-client** floating layers (overlay, `Popup`, drag in client area) and/or **separate windows** (`Window` / tool-window style) — choice per iteration: accessibility, multi-monitor, Alt+Tab, position persistence.
   - **Geometry persistence** for new floating elements — in the general workspace settings stream ([0010](0010-ui-modes-toml-configuration.md): global metrics and extension if needed), not a one-off hidden store for a single strip when unified.

<a id="adr0012-p4"></a>
4. **What stays docked in v1 direction:** **long output streams** (terminal, build log, expanded locals/stack) remain a reasonable home for the **tabbed bottom panel**; this ADR does not destroy it — it stops **short status and IDE Health** from **having** to share that vertical axis.

<a id="adr0012-p5"></a>
5. **Documents (editors) as floating MDI:** **out of scope** for this ADR. Separate discussion/ADR if product needs it; current XAML comment stands until then.

<a id="adr0012-p6"></a>
6. **Link to UI modes:** mode presets ([0010](0010-ui-modes-toml-configuration.md)) can set **visibility and slot** of floating chrome (e.g. show debug strip by default in Debug, collapse IDE Health to an icon in Focus) without rewriting the whole `MainGrid` per mode.

<a id="adr0012-p7"></a>
7. **Toolbars:** top and other button strips — **same de-cluttering class** as floating chrome: **hideability** (whole strip or **groups/categories**), **split by category** (file/solution, build, debug, view, agent — groups TBD in UX), instead of one long mixed line. Group visibility may intersect UI modes ([0010](0010-ui-modes-toml-configuration.md)). While toolbars live **inside `MainWindow`**, MCP sees the same tree — parity without contract extension; toolbar-only window follows the same rules as in “MCP link”. **How many** commands stay on the strip and **discoverability** (palette; situational checklists — [0014](0014-situational-checklists.md)) — [0013](0013-command-surface-and-discoverability.md), not duplicated here.

## MCP and UI roots (agent)

**Human/agent parity** for automatable UI ([0002](0002-debug-human-agent-parity.md)) is **not** disputed here: a separate window does not “excuse” agent access — it **requires** extending the contract to multiple roots, or parity breaks.

Layout snapshot `ide_get_ui_layout` is built from **all** top-level windows in the process: JSON with a `windows` array (each element: `role`, `window_type`, `title`, `is_active`, `root` — control tree with bounds relative to that window). A second `TopLevel` for Mfd (`MfdHostWindow`, `role` = `mfd_host`) and other `Window` instances are included. Actions by control name (`ide_set_control_text`, `ide_click_control`, `ide_set_focus`, `ide_get_control_appearance`, etc.) search **main window first, then others** — parity with a human who sees both screens.

Options in [§3](#adr0012-p3):

- **Floating chrome inside client area** (overlay, `Popup`, drag within `MainWindow`) — same tree inside the matching `root`.
- **Chrome in a separate window** — convenient for multi-monitor and Alt+Tab; that window’s tree appears in `ide_get_ui_layout` as its own `windows[]` entry. Contract refinements for new window types — [0008](0008-mcp-contracts-and-testable-infrastructure.md).

This **does not** forbid separate product windows and **does not** cancel [§5](#adr0012-p5) (floating document MDI — separate topic).

## Consequences

- **Layout migration:** move part of row-5 content to another layer or window without breaking VM bindings and **one debug layer** ([0002](0002-debug-human-agent-parity.md)).
- If **separate window** for chrome — basic **multi-`TopLevel`** MCP (`ide_get_ui_layout`, control lookup across windows) already exists; new scenarios may need contract tweaks **in scope** with the feature (see “MCP link”, [0008](0008-mcp-contracts-and-testable-infrastructure.md)).
- UI tests and MCP that assume **only** `MainWindow` tree may need updates when the visual tree changes.
- User docs (later): how to re-dock, where position is saved.

## Rejected alternatives (as end state)

- **Keep all chrome forever in the fixed bottom grid** — rejected: conflicts with awareness without permanent editor height loss ([0011](0011-debug-situational-awareness.md)).
- **Float everything including documents immediately** — rejected: too costly; not required to solve strip vs log conflict.

## Discussion (open questions for next iterations)

- **Overlay vs separate window** for the first strip: fewer surprises on multi-monitor Windows; with a separate window the tree is already in `ide_get_ui_layout` — product choice and optional contract fields remain.
- **One** universal “floating islands” container vs one window per chrome type.
- Avoid **duplicate IDE Health** (Power strip vs narrow mode) — one data model, multiple presentations.
- **Toolbar:** category set and defaults in Power / Focus / Debug; avoid multiplying strips without need.

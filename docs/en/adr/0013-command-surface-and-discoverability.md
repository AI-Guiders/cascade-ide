<!-- English translation of adr/0013-command-surface-and-discoverability.md. Canonical Russian: ../../adr/0013-command-surface-and-discoverability.md -->

# ADR 0013: Command Surface and Discoverability (Palette, Minimal Toolbar)

**Status:** Accepted (direction; command set and UI iterations separate)  
**Date:** 2026-04-02  

## Related ADRs

| ADR | Role |
|-----|------|
| [0012](0012-floating-workspace-chrome.md) | See [“Split with 0012”](#scope-split-0012) |
| [0014](0014-situational-checklists.md) | Situational checklists — separate ADR |
| [0010](0010-ui-modes-toml-configuration.md) | Modes and visibility |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Commands and MCP |
| [0002](0002-debug-human-agent-parity.md) | One layer for human and agent when commands are shared |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Command id, hotkey, and UI registry layers (no single “everything” table yet) |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | FMS-style chord layer, S/T, overlay — **extends** keyboard-first, does not replace the palette |

### Outside ADR

| Document | Role |
|----------|------|
| [north-star — keyboard-first](../../design/north-star-cursor-mcp-cascade-workbench-v1.md) | Product principle: palette and hotkeys are primary paths |

## Split with [0012](0012-floating-workspace-chrome.md)

- **[0012](0012-floating-workspace-chrome.md)** — placement and floating workspace chrome.
- **0013 (this ADR)** — command surface: how the user invokes actions and discoverability without bloating the toolbar.

---

## Context

**Product orientation:** CascadeIDE is **planned as keyboard-first** — core actions should be reachable from the keyboard and command palette; this ADR defines the surface (palette, minimal toolbar, discoverability), not “mouse-first IDE”.

**0012** addresses **spatial** overload: bottom zone, telemetry, floating chrome, optional separate windows and MCP extension. Separately: **how many command elements** stay on screen and **how** user (and agent by name) find an action.

Today the top **toolbar** can be long and **mixed by meaning** (see [0012](0012-floating-workspace-chrome.md)). Bloating the button strip conflicts with de-cluttering — the same “everything mixed together” as fixed chrome, only horizontal.

**Palette alone** hurts **discoverability** for people who do not yet know command names — the classic Ctrl+Shift+P problem: powerful search, weak at “what should I press *in this* situation?”.

Aviation often uses **checklists and in-field anchors** for discoverability — not the full procedure catalog, but **situation → short list**. That is situational guidance, not a replacement for the command reference.

## Decision

1. **Axis separation** — see [“Split with 0012”](#scope-split-0012) and [ADR 0012](0012-floating-workspace-chrome.md); full wording for both ADRs lives in that [section](#scope-split-0012).

2. **Anchor — command palette** (Ctrl+Shift+P analog): one clear entry (Command button and/or hotkey), **search the command list** with hints and hotkey bindings, **fuzzy/substring**, **recents and frequent** — so a long button strip is not required for everything.

3. **Discoverability is not palette-only:** add **situational mini-checklists** (context: mode/scenario — debug, first run, “before commit”) — short pinned steps in the attention zone, aviation-style (not a replacement for the full command list). Mechanics — **[0014](0014-situational-checklists.md)**.

4. **Minimize toolbar:** keep only **anchor** high-frequency actions (or down to one palette opener — product iteration). Everything else — palette + modes ([0010](0010-ui-modes-toml-configuration.md)) + checklists when needed.

5. **Agent parity:** commands available to the human from the palette and tied to the same layer as `IdeCommands`/MCP stay **aligned** with automation ([0002](0002-debug-human-agent-parity.md), [0008](0008-mcp-contracts-and-testable-infrastructure.md)); names and ids must not diverge between “UI button” and “agent invocation”.

## Link to [0014](0014-situational-checklists.md)

**Situational checklists** (data model, triggers, scenario catalog, UI card, mermaid, rollout order after registry) are in **[0014](0014-situational-checklists.md)**. This ADR keeps the surface-level decision: palette as anchor, minimal toolbar, discoverability beyond name search; checklist mechanics are detailed there. Anchor for former “checklist vision” links: [0014 § Vision](0014-situational-checklists.md#checklist-vision).

## Consequences

- Need a **unified command registry** for the palette: **ids and execution** already in `IdeCommands` + `IdeMcpCommandExecutor`; separately — **UI metadata** (name, category, palette visibility, mode rules) and hotkeys from files — blueprint: [`docs/design/ide-command-registry-v1.md`](../../design/ide-command-registry-v1.md).
- Toolbar and palette — **two views of one layer**, not duplicated logic.
- **Two hotkey file layers:** (1) **shipped** catalog next to exe — `Hotkeys/hotkeys.toml` under `AppContext.BaseDirectory` (like `UiModes/`, `Themes/`) — full baseline `command_id` → gesture map **without** long string tables in source; (2) **user** `%LocalAppData%\CascadeIDE\hotkeys.toml` — overrides only. Load/save of the user layer is separate from `CascadeIdeSettings`; code parses, merges, binds to the command registry; missing/corrupt shipped file — narrow emergency fallback (minimum: open palette), not duplicating the whole map in C#.
- Checklist catalog and third invocation view — consequences in [0014](0014-situational-checklists.md).
- User docs (later): default palette hotkey; path to `hotkeys.toml`; checklists — [0014](0014-situational-checklists.md).

## Rejected alternatives (as end state)

- **Long toolbar only**, no palette or situational hints — rejected: conflicts with chrome de-clutter ([0012](0012-floating-workspace-chrome.md)).
- **Palette only**, no anchors or contextual checklists — rejected: insufficient for unfamiliar scenarios.
- **User gestures only inside `settings.toml` (section)** — rejected: hotkey catalog will grow; need file exchange and future presets without mixing AI/MCP and other settings fields.
- **All default bindings as string literals in C#** — rejected: gesture map should live in a **data file** beside `UiModes/` / `Themes/`; code keeps command ids and logic, not long key lists.

## Implementation decisions (post-discussion)

- **Baseline hotkeys (shipped):** **`Hotkeys/hotkeys.toml`** under `AppContext.BaseDirectory` — single source of **gesture strings** for product defaults; repo contains the same file as in the build. Format — `command_id` → gesture string; details — [`command-palette-ux-concept-v1.md`](../ui-ux/command-palette-ux-concept-v1.md) §9.
- **User layer:** **`hotkeys.toml`** in `%LocalAppData%\CascadeIDE\` (beside `settings.toml`) — **overrides** only on top of shipped map; missing keys are not errors — shipped value wins. Multiple presets — **not** minimal v1; when they appear — field like `hotkeys_preset` in `settings.toml` or naming convention, see UX doc §9.
- **Future (after several presets):** optional **`inherits`** in a hotkey preset file — like **`inherits` in `UiModes/<id>.toml`** ([0010](0010-ui-modes-toml-configuration.md)): one parent, resolve chain, then **merge** `command_id` → gesture — explicit child overrides parent; cycles — load error with clear message (pattern as `UiModeCatalog`).
- **Default palette hotkey (shipped file):** **Ctrl+Q** (Visual Studio Quick Launch style). Override — user `hotkeys.toml`; OS conflict — [`command-palette-ux-concept-v1.md`](../ui-ux/command-palette-ux-concept-v1.md) §8.
- **Focus:** on open — search field; on close — restore focus to the element before open (single main window). UX details — [`command-palette-ux-concept-v1.md`](../ui-ux/command-palette-ux-concept-v1.md) §6; multi-window — [0017](0017-multi-window-workspace-and-agent-surfaces.md).

## Discussion (open questions for next iterations)

- **Checklist** questions (scenario set, overlap with [0011](0011-debug-situational-awareness.md), [0012](0012-floating-workspace-chrome.md)) — [0014](0014-situational-checklists.md).
- **`inherits` schema for hotkeys** (when presets land) — refine with first preset implementation; guide — [0010 § `inherits`](0010-ui-modes-toml-configuration.md) and **`UiModeCatalog`** code.

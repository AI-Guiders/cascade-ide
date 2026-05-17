<!-- English translation of adr/0003-debug-ui-mode-separate-from-power.md. Canonical Russian: ../../adr/0003-debug-ui-mode-separate-from-power.md -->

# ADR 0003: Separate Debug UI Mode (Not the Power Cockpit)

**Status:** Accepted (product direction); **implementation** per release plan  
**Date:** 2026-04-02  

## Related ADRs

| ADR | Role |
|-----|------|
| [0001](0001-debug-hypotheses-json-storage.md) | Debug hypotheses stored in a single JSON file |

### Outside ADR

| Document | Role |
|----------|------|
| [ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md) | Current Focus / Balanced / Power |

---

## Context

**Power** mode is overloaded with telemetry, agent trace, task queue, and the autonomous-scenario cockpit. A “**mostly debugging**” scenario (breakpoints, stack, variables, hypotheses, reproducing a bug) should not require turning on that entire layer. In that scenario the IDE is an **observer and MCP executor** (debug state, commands, UI for the agent), while **dialogue with the agent and autonomous flows** stay in **Cursor**; the built-in IDE chat does not duplicate the primary chat role.

## Decision

Introduce a separate **Debug UI mode** (exact menu/registry name TBD at implementation):

- A layout focused on debugging and related panels (including hypotheses — [ADR 0001](0001-debug-hypotheses-json-storage.md)).
- **Minimal chrome:** no Quick Actions, no bottom panel “terminal / build / git / …” by default; build and console for the scenario live in **Cursor** or via explicit **View** toggles, not as permanent mode chrome.
- Hide or strongly reduce Power-specific UI (cockpit task queue, agent telemetry where not needed for debugging).
- Preserve the principle from [0002](0002-debug-human-agent-parity.md): one debug state for human and MCP.

### Panel visibility map (target state)

Main-window zones follow [ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md). The table below is not commentary — each row answers **what Debug keeps on screen** and **what Debug hides** (relative to Power and partly Balanced). Hotkeys and bindings land in the UI spec when the mode ships.

| Zone | Shown in Debug | Hidden in Debug |
| --- | --- | --- |
| **Task cockpit** (row under toolbar) | Minimum needed to **run under the debugger** and navigate the task (without duplicating a full “work” strip) | **Quick Actions** (Balanced / `quick_actions` capability); **Autonomous** block and rest of cockpit chrome for **Power** family (`UiModeFamily.Power`) |
| **Solution Explorer** | Solution tree, file navigation | **PowerTaskQueueItems** queue under the tree (yes in Power, no in Debug) |
| **Editor column** | 1–2 editor groups; **stack/variables** panel when a DAP session is active | Third group by default (as in Power); **inline build output** under the editor — **hidden** by default (build out of mode focus); Power-only doc chrome |
| **Right column (chat)** | By default **no chat column**: `ChatPanelExpanded: false` in mode spec, column width **0 px** (no narrow strip), splitter before chat hidden; area goes to editor and debug panels. Restore chat via **View → Chat**, toolbar **Show chat**, or MCP | Expanded built-in chat as in Balanced/Power; **Agent Trace**; **Agent Operations**; Power chat layout. Rationale: primary chat in **Cursor**, IDE autonomy **deferred**, no need to duplicate the dialogue strip in the debug window |
| **Telemetry strip** | Minimum info; **debug status** is appropriate; build/tests/git optional or hidden to avoid duplicating the “everything in Cursor” scenario | Extended Power-cockpit and duplicates that exist only in Power |
| **Bottom panel** | By default **collapsed** or only debug-meaningful tabs: **Debug** (stack/variables in dock), plus **Hypotheses** ([0001](0001-debug-hypotheses-json-storage.md)); see below | **Terminal**, **Build output**, **Git**, **Events**, **Tests** — **not expanded** by default; available via **View** when unavoidable (repro, manual git, etc.) |

### Hypotheses: UI zone (decided)

- **Right chat column** — not used for the hypothesis list (chat in Cursor; column collapsed by default in Debug).
- **Target:** dedicated **Hypotheses** tab in the **bottom panel**, on the same strip as **Debug** — shared debug context; expand the bottom panel via **View** when the list or stack in the dock is needed.
- **Fallback** (only if implementation is tight): part of the list or a duplicate next to stack/variables in the editor column; bottom-panel tab still wins.

### Breakpoints: UI zone, data model, agent

- **Window zone:** a separate “breakpoint list” column is not required. Points are set in the editor **gutter** today; if a list with actions appears later, place it in the **debug** zone (bottom panel / next to stack/variables), not as a fourth main-window column.
- **Model:** one record in storage (`.dotnet-debug-mcp-breakpoints.json` / `BreakpointEntry`) — an **active** point; toggling in the UI removes or adds the record, **without** Visual Studio–style “disabled but line still marked”.
- **Product boundary:** **enabled/disabled** without removing from JSON is **intentionally out of scope** until an explicit request and a separate schema/DAP decision. If needed later — separate ADR or breakpoint-storage addendum.
- **Agent (Cursor / MCP):** source of truth for the list is the **on-disk file** and tools like `debug_list_breakpoints`; no push to chat. After human edits in the IDE the agent sees current state on the **next** file read or tool call, not instantly.

### Initial layout (mode registry)

When added to `UiModeLayoutRegistry` (or equivalent), Debug gets its own `UiModeLayoutSpec`: **tree enabled**, **`TerminalVisible: false`**, **`BuildOutputVisible: false`**, **`ChatPanelExpanded: false`**, **editor group count** 1–2; bottom panel and inline build output under the editor off or collapsed in spec by default. Exact values are fixed in the commit that introduces the mode.

Control map and hotkeys — update `cascade-ide-ui-layout-v1` (or successor) after the mode exists in code.

## Consequences

- Requires extending `UiModeLayoutRegistry` / equivalent and conditional visibility, not only a new tab inside Power.
- Mode documentation updates when the mode ships in code.

## Rejected alternatives

- **Power only + new tab** without a separate mode — rejected: does not isolate the “debug only” scenario.
- **Full window duplication** — not required; a mode in the same main window is enough.

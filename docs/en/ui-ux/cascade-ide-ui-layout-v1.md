# Cascade IDE — main window layout (v1)

Layout reference for MCP, onboarding, and agents. **Source of truth:** `Views/MainWindow.axaml` and related views (`DocumentsDockView`, `MfdShellView`).

**UI mode:** the product ships one mode id in **`UiModes/index.toml`** — **Flight** (PFD · Forward · MFD polygon). There is **no** menu switch for legacy **Focus / Balanced / Power** presets. Family **`UiModeFamily.Flight`** and capabilities come from mode TOML; see [ADR 0010](../../adr/0010-ui-modes-toml-configuration.md#summary-en), [ui-modes-overview-v1.md](ui-modes-overview-v1.md), [ADR 0021](../../adr/0021-pfd-mfd-cockpit-attention-model.md#summary-en).

---

## 1. Overall structure

Window: root **Grid** → **DockPanel** with **Menu** on top, then **`MainGrid`** (single work-area grid).

**Default size:** 1000×600. Resize via window edges and **splitters** between PFD / Forward / MFD columns.

Overlays: **CommandPalette** (`ZIndex` 5000), optional zone geometry / highlight overlays.

---

## 2. Menu (`Menu`, DockPanel.Top)

Current items (see code):

- **File:** open solution / folder / file, export Markdown, exit.
- **Debug:** F5/start, startup project, attach, stop, step over/into/out.
- **View:** command palette; visibility toggles for **PFD** (explorer / map per layout), **build output** (MFD page), **MFD** column, **terminal** (MFD page), **Git** (MFD page), instrumentation dock; **Theme** submenu; UI language; Markdown preview in MFD; preview in separate window.
- **Settings:** AI and chat parameters.
- **Help:** about.

**MCP banner:** `McpBannerView` in `MainGrid` row 0 (not in the menu) when the IDE runs as an MCP server.

---

## 3. `MainGrid` grid

**Rows:** three — `Auto`, `Auto`, `*`.

| Row | Content |
| --- | --- |
| 0 | `McpBannerView` (when `IsMcpServerMode`) |
| 1 | **`TaskCockpitView`** — task strip, CascadeChord, quick actions (per capabilities) |
| 2 | **Three attention zones:** PFD · Forward · MFD (below) |

**Columns:** `220, 4, *, 4, 340` (base widths; PFD/MFD may collapse per bindings).

| Col | Content |
| --- | --- |
| 0 | **PFD** — `AttentionZoneContainer Zone="Pfd"`: solution explorer and/or workspace navigation map, optional tool mount. |
| 1 | `GridSplitter` |
| 2 | **Forward** — `AttentionZoneContainer Zone="Forward"`: **`DocumentsDockView`** (Avalonia Dock — document tabs, editor). |
| 3 | `GridSplitter` |
| 4 | **MFD** — `AttentionZoneContainer Zone="Mfd"`: **`MfdShellView`** (secondary contour: top band + **page stack**). |

Zone geometry debug overlay: `SkiaZoneGeometryOverlayPfd` / `Forward` / `Mfd`. Agent highlight: `AgentHighlightLayer` over the full `MainGrid`.

`UiModeBloomOverlay` — decorative bloom from chrome TOML.

---

## 4. Forward zone (`DocumentsDockView`)

- **HUD** ([ADR 0021](../../adr/0021-pfd-mfd-cockpit-attention-model.md) §9): banner strip above the dock when non-empty.
- **Dock manager:** `DockControl` — factory/layout from ViewModel. Long logs (build, tests, terminal) use **MFD pages**, not a bottom dock row in the main grid.

MCP control names should match `Name` in XAML where possible (`DocumentsDockView` and document factory).

---

## 5. MFD zone (`MfdShellView` + `MfdShellPageStack`)

Top to bottom inside the MFD column:

1. **`WorkspaceChromeBandView`** — EICAS / IDE Health style band (visibility from TOML/VM).
2. **`MfdContourStackHost`** — hosts **`MfdShellPageStack`**: one **active page** (`CurrentMfdShellPage`), e.g. Workspace Health, explorer in MFD, related files, Markdown preview, chat, AI settings, **terminal**, **build log**, Problems, **Git**, events, tests, hypotheses, debug stack, …

Page switching is via VM/commands/menu — there is **no** separate full-width bottom tab bar on the main window.

**Terminal** on the MFD page is a stub (single command → output), not a full integrated shell — see [mfd-terminal-stub-vs-integrated-shell-v1.md](../../ui-ux/mfd-terminal-stub-vs-integrated-shell-v1.md) (Russian body).

---

## 6. Key controls and MCP

| Zone / meaning | Name / note |
| --- | --- |
| Window root | `RootWindow` |
| Grid | `MainGrid` |
| Chat (MFD page) | inside `ChatMfdPageView` / stack |
| Chat input | `ChatInputBox` (on chat page) |
| Terminal input | `TerminalInputBox` (`TerminalMfdPageView`) |
| Agent highlight | `AgentHighlightOverlay` on `AgentHighlightLayer` |

`ide_set_panel_size` and similar follow the current MCP contract; geometry uses **three column splitters** and `workspace.toml` / capabilities.

---

## 7. Highlight overlay

`AgentHighlightLayer` (Canvas, `ZIndex` 1000) over the grid; `AgentHighlightOverlay` frames a target control (`ide_highlight_control`). `IsHitTestVisible=false` so clicks pass through.

---

## 8. Historical context (not current layout)

Older mockups with a **bottom panel** (Terminal / Build tabs in one `BottomPanelView`) and **Focus / Balanced / Power** presets are **legacy**. Current **Flight** is **PFD | Forward | MFD** in one grid; long streams are **MFD pages**. See [concept-to-implementation-map-v1.md](concept-to-implementation-map-v1.md) and `concept-generated/` — concept PNGs are not guaranteed to match code.

---

*Document version: 2.0 (English). Matches `MainWindow.axaml` and `UiModes/index.toml` (**Flight** only). Update this file when layout changes.*

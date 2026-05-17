<!-- English translation of adr/0017-multi-window-workspace-and-agent-surfaces.md. Canonical Russian: ../../adr/0017-multi-window-workspace-and-agent-surfaces.md -->

# ADR 0017: Multiple Application Windows (Multi-Window), Screen Zones, and Agent Surfaces

**Status:** Accepted · Implemented  
**Date:** 2026-04-05  
**Updated:** 2026-04-18 — canon for `TopLevel` count and `display.screens` / topology; details — [§ History](#adr0017-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | UI modes and TOML over window frame; **zone presentation topology** — separate subsection in 0010, not mixed with `attention_zone_panels` |
| [0012](0012-floating-workspace-chrome.md) | Floating chrome; separate windows as one mechanic |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / forward / MFD zone semantics — **not** synonymous with window count |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | Zones in SDK |
| [0016](0016-agent-client-protocol-external-agent.md) | External agent via ACP — orthogonal to *how many windows* show built-in UI |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Command ids, **`Hotkeys/hotkeys.toml`** + user overlay, palette |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Chat clarification batches — not PFD confirmations |
| [0002](0002-debug-human-agent-parity.md) | MCP parity |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **Instrument deck** axis and *presentation* vs CDS — see § there; **topology keys** — below in this ADR |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | `primary_work_surface` — Intercom or Editor in Forward (orthogonal to `presentation`) |

### Outside ADR

| Document | Role |
|----------|------|
| [`attention-zone-panel-playbook-v1.md`](../../design/attention-zone-panel-playbook-v1.md) | Zone ↔ panel ↔ topology; in code — `AttentionLayoutSurfaceKind` |
| [`concept-pfd-mfd-cascade-v1.md`](../ui-ux/concept-pfd-mfd-cascade-v1.md) | PFD/MFD UX concept (superseded by [0021](0021-pfd-mfd-cockpit-attention-model.md)) |
| [`concept-to-implementation-map-v1.md`](../ui-ux/concept-to-implementation-map-v1.md) | Concept → code map |
| [skia-surfaces-vs-overlays-v1.md](../../design/skia-surfaces-vs-overlays-v1.md) | Skia surfaces vs overlays |

## Summary

- **Multiple `TopLevel`** — product model for spreading zones across monitors; zone semantics ([0021](0021-pfd-mfd-cockpit-attention-model.md)) **≠** window count.
- Layout source of truth — **`presentation`** / **`zone_screen_layout`** string in **`settings.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), parser + `[presentation_grammar]`.
- **`(P+F)(M)`** → two windows; **`(P)(F)(M)`** → three; group order in the string = left to right on screens.
- **`MfdHostWindow` / `PfdHostWindow`** — full zone shell; placement only via `PresentationHostWindowPlacement`.
- MCP **`ide_get_ui_layout`** already returns all windows; roadmap — EICAS on second screen, fallbacks, agent confirmations ([“Open questions”](#adr0017-open-questions)).

<a id="adr0017-implementation-status"></a>

## Implementation status (current in repository)

Below — **what exists in code** so product backlog from [“Open questions”](#adr0017-open-questions) is not confused with shipped behavior.

### Already implemented

| Area | Where in code / what it does |
|--------|------------------------------|
| `presentation` string parsing | `Services/Presentation/PresentationParser.cs`, **`PresentationInnerEtoGrammar`** (Eto.Parse), `PresentationGrammarTokens`, `PresentationLayoutAnalyzer`, `PresentationAnchorKind`, **`PresentationAnchorSlot`** (weights); tests `CascadeIDE.Tests/PresentationParserTests.cs`. **Zone weights** within a group — [§](#adr0017-zone-weights). **Main window columns:** `PresentationMainGridColumnDefinitions.Get` → **`MainGridColumnDefinitions`** on `MainWindowViewModel`, applied in **`MainWindow.PresentationLayout.axaml.cs`** (`ColumnDefinitions.Parse` — Avalonia does not bind a string to `ColumnDefinitions` from VM); MFD tail on two-anchor preset — `340` or `0` when column hidden for host ([implementation](../../Services/Presentation/PresentationMainGridColumnDefinitions.cs)). |
| Topology ↔ displays | `PresentationMonitorTopology.OrderScreensForPresentation` — sort `Screen` left-to-right, then top-to-bottom (shared coordinate grid; not OS primary as anchor). |
| Host window placement (`MfdHostWindow`, `PfdHostWindow`) | `Services/PresentationHostWindowPlacement` — target screen by index from preset; else first “non-main” screen; with saved bounds — restore clamped to `WorkingArea`. On dedicated monitor default **`WindowState.Maximized`**; disable — `[display]` **`maximize_presentation_host_windows_on_dedicated_screens`** (`DisplaySettings.MaximizePresentationHostWindowsOnDedicatedScreens`). |
| Placement invariant | Positioning/restoring `PfdHostWindow` / `MfdHostWindow` geometry — **only** via `PresentationHostWindowPlacement` (and shared lifecycle in `MainWindow.PresentationHostWindows.axaml.cs`); do not duplicate screen/`WorkingArea` logic in other Views without extending this service. |
| Host geometry persistence | `CascadeIdeSettings` / `[display]`: MFD — `MfdHostWindowPixelX` / `PixelY` / `Width` / `Height`; PFD — `PfdHostWindowPixelX` / `PixelY` / `Width` / `Height`; written on `Closing` of the window (`Views/MainWindow.PresentationHostWindows.axaml.cs`). |
| MFD host window | `Views/MfdHostWindow.axaml` — full `MfdShellView` ([§8](#adr0017-p8-mfd-host-wide)), same `DataContext` as `MainWindow`; MCP highlight layer like main window. Column under Mfd anchor in `MainWindow` — **one** `MfdShellView` (pages: chat, terminal, solution explorer, etc.), no baked-in “tree + shell” split; second `TopLevel` repeats **same** secondary contour ([skia-surfaces-vs-overlays-v1.md](../../design/skia-surfaces-vs-overlays-v1.md)), not necessarily pixel-matched width. |
| Lifecycle | No separate menu “second window” — source of truth is `presentation` / `zone_screen_layout`; auto-start on `Loaded` (`MainWindow.PresentationHostWindows.axaml.cs`): when topology fits && `OpenPfdHostWindowOnStartup` / `OpenMfdHostWindowOnStartup` && enough monitors — open `PfdHostWindow` and/or `MfdHostWindow`; optional MCP `toggle_mfd_host_window` under same topology (`CanExecute`). When host open, matching column in `MainWindow` hidden (`SetPfdHostWindowShellOpen` / `SetMfdHostWindowShellOpen`). |
| Attention topology | `AttentionLayoutSurfaceKind`: with hosts — `MainWindowPlusMfdHostTopLevel`, `MainWindowPlusPfdHostTopLevel`, `MainWindowPlusPfdMfdHostTopLevel` (`MainWindowViewModel.Presentation.cs`). |
| MCP / snapshots | `UiLayoutSnapshot`, roles `mfd_host` / `pfd_host` for respective windows; `ide_get_ui_layout` across all top-levels. |

### Partial or outside current code (roadmap remains)

- **Preset with three groups `(…) (…) (…)`** (any anchor order, e.g. `(P) (F) (M)`): **canon** — **three** `TopLevel`, one zone per window; **spatially** left-to-right = group order in string (see “Spatial canon”). **In code:** `PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset`, host screen indices — `TryGetPfdHostPresentationScreenIndex` / `TryGetMfdHostPresentationScreenIndex`; windows **`PfdHostWindow`** and **`MfdHostWindow`**, main — Forward; placement/persist — `PresentationHostWindowPlacement` / `[display]`. Roadmap: product UX polish, monitor-change fallback, MCP beyond base `ide_get_ui_layout`.
- **Fallback** when monitor disappears / large OS layout change: clamp to work areas exists; dedicated “monitor unavailable” UX not mandatory here.
- **Closing main window:** on `MainWindow` `DataContext` reset, host close (`CloseMfdHostWindowIfOpen`); explicit `MainWindow.Closing` cascade — depends on app lifecycle.
- **EICAS / IDE Health on second screen**, chat/terminal density — product open items ([“Open questions”](#adr0017-open-questions)).
- **Agent confirmations** with two `TopLevel` — direction in [§6](#adr0017-p6); implementation details open.

---

## Context

Today **one main window** (`MainWindow`) defines the whole visible cockpit: editor, tree, chat, Mfd column, etc. Mode configuration ([0010](0010-ui-modes-toml-configuration.md)) describes **visibility and metrics on a fixed frame**; an alternative “whole window schema” in data is explicitly deferred.

Need: **spread roles across the screen** without cramming everything into one column — including:

- **Multiple physical monitors** — ideally **`(PFD) (Forward) (MFD)`** (three displays); see “Multiple monitors” and layout table **`(PFD+Forward+MFD)`** with default **`Z`** or **`(PFD|Forward|MFD)`** with **`zone_separator = "|"`** / **`(PFD+Forward) (MFD)`**.
- **Move an attention zone to another display** — especially **MFD** (chat, terminal, trace, heavy agent panels); optionally **PFD** or a compact confirmations/status strip. Product wording — **zone ↔ second or third monitor**, not abstract “second window”. Code type `MfdHostWindow`, MCP snapshot `role` = `mfd_host` for that second `TopLevel`.
- **Layout experiments** isolated from stable presets — **Flight** as sandbox; multi-window fits there first (see `concept-pfd-mfd-cascade-v1.md` §5).

[0012](0012-floating-workspace-chrome.md) already allows **separate windows** for part of chrome (telemetry, strips). **Basic MCP for multi-window exists:** `ide_get_ui_layout` returns JSON with `windows` (one tree per open `Window`, roles `main` / `mfd_host` / `other`); control lookup for inspect/act — main window first then others (`Cockpit/Surface/UiLayoutSnapshot`, `UiControlAppearance`). This ADR **extends** from technical “several roots in snapshot” to **product model** (which surfaces, lifecycle, state, modes and agent); **contract additions** when new window types or region semantics appear ([§7](#adr0017-p7)).

### One monitor and three semantic zones ([0021](0021-pfd-mfd-cockpit-attention-model.md))

Three spatial attention anchors (PFD, forward, MFD) **do not require** three separate windows. Current implementation — **one** main window, `MainGrid` columns, topology `AttentionLayoutSurfaceKind.MainWindowDockedGrid` (see playbook).

**Optional:** spread same semantics across **several `TopLevel` on one physical monitor** (e.g. very wide display) — one process, same panel→zone map ([`AttentionZonePanelRuntime`](../../design/attention-zone-panel-playbook-v1.md)), different **presentation geometry** only. **Not the default goal:** **main window + zone on another monitor** and/or **preset inside one `TopLevel`** ([0021](0021-pfd-mfd-cockpit-attention-model.md), FancyZones motive). **Three linked `TopLevel` on one monitor** — only with explicit feedback or ultrawide scenarios; otherwise risk/complexity outweighs two windows or in-window grid.

**Not** an argument for multi-window: “block OS-level window move”. Snapping, grid, predictable layout — within **one** `TopLevel` (splitters, presets) or programmatic positioning without promising OS forbid; see [0022](0022-mfd-visual-design-surface-axaml-blazor.md).

<a id="adr0017-several-monitors"></a>

### Multiple monitors

**Notation (do not mix “zones on one screen” and “number of displays”):**

- **`( … )`** — **one physical display** (one area in OS monitor configuration).
- **Separator between anchors inside one parenthesis pair** — **exactly** literal **`Z`** from **`zone_separator`** (default **`+`**). Multiple semantic anchors (**PFD**, **Forward**, **MFD**) on **that** display (as columns in one `MainWindow`) are written **`anchor` `Z` `anchor` `Z` `anchor`**. In prose **`+`** and **`|`** illustrate one meaning; **in `presentation` string** with **`Z = "+"`** only **`+`** is allowed, with **`Z = "|"`** only **`|`** (parser does not substitute a “synonym”).
- **Short form** — same keys **`pfd_zone_identifier`** / **`forward_zone_identifier`** / **`mfd_zone_identifier`**, e.g. **`"P"`**, **`"F"`**, **`"M"`**; only the chosen literal is valid in `presentation`.
- **Several `(…) (…) (…)` in a row** — **different** displays; left-to-right in text is a convenient orientation; actual order comes from geometry and preset. **Three displays for three anchors** — only **three parenthesis groups**, e.g. **`(PFD) (Forward) (MFD)`**. **Bracket-less** `PFD | Forward | MFD` is **ambiguous** — do not use in config/docs; symbol between anchors **inside** brackets is **`Z`**, not a display separator.

<a id="adr0017-display-screens-topology-naming"></a>

### Notation capacity and config naming direction (`display.screens` / `topology`)

**Intentional density:** the same topology string (**`presentation`** in `settings.toml` root, future **`topology`** inside the table below) **mixes in one layer** (a) **how many** display groups in the preset (several `(…) (…) (…)` — see [§ “Multiple monitors”](#adr0017-several-monitors)) and (b) **how groups relate** to each other and anchors (spatial order in text, semantics inside a group). Splitting “display count” and “topology description” into **separate** keys could be clearer but **costs** verbosity. Current notation trades that for brevity; meanings (a) and (b) are **not separated** without grammar context and examples ([§ grammar](#adr0017-presentation-grammar), [EBNF](#adr0017-presentation-ebnf)).

**TOML rename direction (reduce *presentation* confusion with UI “presentation” and other layers — [0063 § CDS](0063-instrument-deck-named-composition-one-anchor.md#adr0063-cds-not-presentation)):** canon — nest topology under semantic **`[display]`** ([0050](0050-declarative-instrument-zone-placement-toml.md), `Display` in model):

- Table-level “whole topology in one place” (today root **`presentation`** / **`zone_screen_layout`** and related fields — see code) → **`[display.screens]`**.
- EBNF / anchor placement string — key **`topology`**.
- Grammar: section **`[presentation_grammar]`** → **`[display.screens.grammar]`** (or consistent nesting; code today — `CascadeIdeSettings.PresentationGrammar`).

**What *screen* means in `display.screens`:** colloquial *screen* often means “OS monitor”. Here **screen** matches **`screen_markers`** in [token table](#adr0017-presentation-grammar): a **group** in the string, boundary of one preset presentation surface, PFD/MFD cockpit metaphor. “One `( … )` ↔ one physical OS display” is fixed [above](#adr0017-several-monitors); future table name **does not** change bracket semantics without explicit migration.

**Why not `display.layout`:** **layout** is overloaded — **CockpitPresentationLayout**, [0046](0046-presentation-layout-authority-and-cockpit-invariants.md), plus layout **inside** an anchor (**instrument deck**, [0063](0063-instrument-deck-named-composition-one-anchor.md)). **`[display.layout]`** with **`topology`** could read clearer without “screens” but risks mixing with *in-region* layout more than **`display.screens`** after the paragraph above. If product picks **`layout`**, document: *multi-window / multi-anchor topology preset only, not deck or inner region grid*.

**Migration:** read old keys as **legacy** N releases; on conflict new canon wins when present; update `settings.toml` examples when implemented. Immediate key rename in code **not required** by this section — direction only; timeline in TOML loader tasks.

<a id="adr0017-presentation-grammar"></a>

**Where to store and why not only `workspace.toml`:** layout across **physical displays** is **personal** (team members have different hardware). Repo **`.cascade/workspace.toml`** ([0021](0021-pfd-mfd-cockpit-attention-model.md) §2.1, [0028](0028-user-settings-toml-localappdata-and-secrets.md)) — **team convention** for panels/zones in shared attention model, **not** mandatory “three monitors for everyone”. In **`settings.toml`** root: **`presentation`** and/or **`zone_screen_layout`**; **optional** grammar tokens in **`[presentation_grammar]`** — personal monitor topology, **primarily** **`settings.toml`** (`%LocalAppData%\CascadeIDE\`, [0028](0028-user-settings-toml-localappdata-and-secrets.md)); on merge with `UiModes/` bundle and repo overlay **user** keys for these **override** bundle/repo if they ever appear (product default, not “team monitor decision”).

**`presentation` string grammar (configurable in TOML):** optional keys in **`[presentation_grammar]`** — **which symbols** are screen markers, screen separator, zone separator, and **three zone literals** (long or short — one key per zone). Values are **user choice**; below — **defaults** used in examples in this ADR:

| TOML key (inside `[presentation_grammar]`) | Default | Meaning |
|-------------|--------|--------|
| **`screen_markers`** | **`"()"`** | two-character string: open/close **one display** boundary |
| **`screen_separator`** | **`" "`** | separator **between** groups (displays), usually space between `)` and next `(` |
| **`zone_separator`** | **`"+"`** | separator **between anchors** inside one marker pair; **only this literal** allowed between anchors in `presentation` |
| **`pfd_zone_identifier`** | **`"PFD"`** | PFD anchor literal in `presentation` |
| **`forward_zone_identifier`** | **`"Forward"`** | forward anchor literal |
| **`mfd_zone_identifier`** | **`"MFD"`** | MFD anchor literal |

Three literals must be **pairwise distinct** (case-insensitive compare). On collision implementation **resets** all three identifiers to table defaults.

<a id="adr0017-anchor-aliases"></a>

**Anchor identifiers.** User may set e.g. **`forward_zone_identifier = "Lob"`** and write **`(PFD+Lob+MFD)`**. Short letters **P** / **F** / **M** — same keys: **`pfd_zone_identifier = "P"`**, etc.; then only **`(P+F+M)`** in the string.

Examples with **default** identifiers (**`PFD`**, **`Forward`**, **`MFD`**): full literals in string, e.g. **`(PFD+Forward+MFD)`**, **`(PFD) (Forward) (MFD)`**. Single-letter examples — after explicit **`[presentation_grammar]`** with short identifiers.

<a id="adr0017-presentation-ebnf"></a>

**Grammar (EBNF).** Unambiguous description below; anchor literals from TOML keys (see table). No whitespace **inside** `screen_markers` between anchors (only `zone_sep`).

```ebnf
(*--- Parameters from TOML: O, C, Z, S; anchor ids PfdId, ForwardId, MfdId (defaults — see table) ---*)

presentation ::= [ SP ] screen { SP screen } [ SP ]
screen       ::= "(" anchor { zone_sep anchor } ")"
zone_sep     ::= Z
anchor       ::= pfd_zone_identifier | forward_zone_identifier | mfd_zone_identifier

pfd_zone_identifier     ::= (* literal from pfd_zone_identifier key *)
forward_zone_identifier ::= (* literal from forward_zone_identifier key *)
mfd_zone_identifier     ::= (* literal from mfd_zone_identifier key *)

(* after parse: semantics PFD / forward / MFD — [0021](0021-pfd-mfd-cockpit-attention-model.md) *)

(* lexer: three anchor strings; match longest literal first; length-1 literals — case insensitive *)

SP           ::= U+0020 { U+0020 }   (* one or more spaces — display separator with default *)
```

**Parameterization from TOML:** let **`O`** and **`C`** be first/second char of **`screen_markers`**, **`Z`** — **`zone_separator`**, **`S`** — **`screen_separator`**, anchor literals — values of **`pfd_zone_identifier`**, **`forward_zone_identifier`**, **`mfd_zone_identifier`**. Then:

```ebnf
presentation ::= [ S ] screen { S screen } [ S ]
screen       ::= O anchor { Z anchor } C
(* anchor — as above; anchor tokens from TOML *)
```

Between anchors in one screen only literal **`Z`** from TOML (default **`+`**). Parser **does not** substitute **`|`** or **`+`** for chosen **`Z`**. `presentation` must use **same** literals as configured tokens. Parser reads grammar from **`[presentation_grammar]`** (or defaults), then applies production. In code — **`CascadeIdeSettings.PresentationGrammar`**; merge with bundle — in implementation ([0010](0010-ui-modes-toml-configuration.md), [0028](0028-user-settings-toml-localappdata-and-secrets.md)).

**In config:** same compact value may have **`# …`** comment explaining anchors and display count; ADR references in user file **not required**.

**Product layer (outside this ADR):** where/how to give users expanded explanations (external docs, in-IDE help) — **product decision**, not ADR; see [architecture-policy.md](../../architecture-policy.md).

**Three typical layouts by display:**

| Layout | Meaning |
|--------|--------|
| **`(PFD+Forward+MFD)`** … (default **`Z`**); **`(PFD\|Forward\|MFD)`** with **`zone_separator = "\|"`**; **`(P+F+M)`** with short identifiers in **`[presentation_grammar]`** | One display: three anchors in **one** `TopLevel` (`MainGrid`). |
| **`(PFD+Forward) (MFD)`** …; **`(P+F) (M)`** with short ids | **Two `TopLevel`:** main — **PFD and forward together**; second window — **MFD** zone (`MfdHostWindow`). Typical two-monitor compromise. |
| **`(PFD) (Forward) (MFD)`** …; **`(P) (F) (M)`** with short ids | **Three `TopLevel`:** **one zone per window** (PFD; forward; MFD). Ideal with three monitors ([0021](0021-pfd-mfd-cockpit-attention-model.md)); vs v1 code — [“Implementation status”](#adr0017-implementation-status). |

**Canon for window count:** **`(P+F)(M)`** ⇔ **two** top-level windows (main holds P+F, M out); **`(P)(F)(M)`** ⇔ **three** top-level windows (P, F, M separate). This is **`presentation` string semantics**, not “one window on three OS screens” and not three windows on one monitor as default goal.

**Spatial canon (three displays, typical row):** **left to right** on screens (per `PresentationMonitorTopology.OrderScreensForPresentation`: left-right, then top-bottom) follows **`(…) (…) (…)`** **write order**. First group — left screen in that order, second — middle, third — right. Examples: **`(P) (F) (M)`** — PFD left, Forward center, MFD right; **`(M) (F) (P)`** — MFD left, Forward center, PFD right. No fixed “P always left” — only bracket order. If physical monitors are not one row, user maps **i-th** group to **i-th** screen in chosen order (or adjusts OS layout).

<a id="adr0017-zone-weights"></a>

### Zone shares on one display (optional anchor weights)

**Accepted:** inside **one** **`screen_markers`** pair, with **more than one** anchor, optional **positive coefficients** — **real literals immediately before** anchor id (canonical **no** space between number and id: **`0.25P`**). Before parse, parser **strips all Unicode whitespace** inside the screen pair, so **`(0.25P + 0.75F)(M)`** equals **`(0.25P+0.75F)(M)`**. Meaning: **share** of main column strip width (typical — three columns in `MainGrid`); coefficients in a group **sum to 1**. Example **`(0.25P+0.75F)(M)`** — first screen P and F split 1∶3; second screen single **M** on full display, **no separate weights** (between groups **`(…) (…)`** there are no weights: monitor boundary is desk/OS geometry, not a share in the string).

**Invariant:** coefficients change only **shares within one group** (one screen). **Topology** — how many `(…)` groups, which anchors on which screen, how many physical displays, product effects like maximizing main on start — from **bracket composition**, not from picking **`0.25` vs `0.5`**.

**When weights omitted** — current behavior: equal shares between anchors in group or existing grid default.

**Within one group** with **two or more** anchors: either **every** anchor has a coefficient and **sum = 1**, or **none** (equal shares). Mixed like **`(0.25P+F+M)`** — **parse error** (ambiguous).

**Single anchor in brackets** — anchor literal only, no coefficient (or equivalently one anchor fills the screen).

**Number format:** decimal point **`.`** (TOML/JSON style); comma locale **not** supported in `presentation` string.

**Parser link:** `PresentationParser` parses optional weight prefixes; inside one `screen_markers` pair anchor list structure from **Eto.Parse** (`PresentationInnerEtoGrammar`, NuGet **Eto.Parse**). Result — `PresentationParseResult.Screens` as lists of **`PresentationAnchorSlot`** (`Kind` + `Weight?`). Strings **without** coefficients remain canonical (`Weight == null` on all anchors in group).

Extended **EBNF** relative to [base production](#adr0017-presentation-ebnf); may merge into one block in docs.

```ebnf
(*--- Extension: weights only inside screen with two+ anchors; sum weight = 1 ---*)

weight       ::= (* positive literal: integer or decimal, decimal point U+002E *)
weighted_anchor ::= weight anchor | anchor
screen       ::= O weighted_anchor { Z weighted_anchor } C
(* semantics: in one screen either every weighted_anchor is "weight anchor" or each is "anchor"; mixing forbidden; with weights sum=1 *)

(* Base form without weights — as now: anchor only *)
```

**Examples** (short ids **`P`**, **`F`**, **`M`** in **`[presentation_grammar]`**): **`(0.25P+0.75F)(M)`**; **`(0.2P+0.3F+0.5M)`**; **`(P+F+M)`** — no weights, three equal shares.

<a id="adr0017-nested-vh-notation"></a>

### Nested **v** and **h** axes (T-layout; note for agents and overview docs)

Sometimes layout on **one** physical display is described with **explicit axes**, e.g.:

**`0.3vPFD + 0.7v(0.8hForward + 0.2hMFD)`**

**Reading:**

1. Split screen **vertically** (**`v`**): **30%** width left — **PFD** (workspace context, link tree, primary indicators in cockpit model).
2. Remaining **70%** right — vertical band; split **horizontally** (**`h`**): **top 80%** — **Forward** (windshield: editor, work object), **bottom 20%** — **MFD** (telemetry, secondary contour, settings “at hand”).

Classic cockpit **T-layout**: main work (code and “flight status”) center field of view, auxiliary below. Without **`v`** / **`h`** and nesting order, analyzer or agent **cannot unambiguously** map shares to Avalonia **`RowDefinitions` / `ColumnDefinitions`**: need **which split is first** (here vertical 0.3/0.7), then how **right part** splits by **rows** (0.8/0.2).

**Link to current `presentation` string (v1 implementation):** parser and weights in [§ “Zone shares”](#adr0017-zone-weights) currently set **one axis** — typically **three `MainGrid` columns** (**PFD | Forward | MFD**) with width shares. **Nested** **`v` / `h`** notation in this ADR is **target semantics** for future grammar and/or composition layer ([0036](0036-cds-channel-compositor-surface-pipeline.md)); until nested-group parser exists, **do not** treat as equivalent to flat **`(0.3P+0.7F+…)`** without separate spec.

<a id="adr0017-weight-fuse-policy"></a>

**Fuse policy (geometry stability):** when weights and axes are **explicit in config**, product **must not** **dynamically recalculate** proportions at runtime “for convenience” (window resize must not replace configured shares).

- **Analyzer** (parse/validate): at **each nesting level** coefficient sum on that axis **= 1** (as for flat screen with anchor weights).
- **Surface compositor** ([0036](0036-cds-channel-compositor-surface-pipeline.md)): at session start **fix** computed **normalized** shares (and pixel bounds after measure if needed), **no** floating recalc of shares on ordinary resize; exceptions — separate explicit product rule, not silent coefficient change.

**Ideal (three physical displays):** **`(PFD) (Forward) (MFD)`** — target cockpit layout with enough hardware: workspace context / explorer and primary indicators on first screen, work object (editor) on second, secondary heavy panels (chat, terminal, trace…) on third. **Canon:** preset with **three `(…) (…) (…)` groups** means **three separate `TopLevel`** (see table above), not one window stretched across three monitors without zone split.

**Attention metaphor (cockpit):** besides **forward** field of view there are **side zones** — reachable with **one glance** (peripheral attention) without long break from “forward” work. **PFD** and **MFD** are **side** relative to **forward (Forward)**, not “secondary screens” only; multi-window and direction-from-forward anchor align with this model.

**Typical compromise (two displays):** **`(PFD+Forward) (MFD)`** with default **`Z`**, or **`(PFD|Forward) (MFD)`** with **`zone_separator = "|"`** — first monitor: PFD and forward together (as in one `MainGrid` today); second — **MFD** zone. Common; differs from three-screen ideal by merging PFD and forward on one display.

### `SkiaHost` instance matrix (fixed for v1)

`SkiaHost` is **per slot**, not “one per window”. Typical topologies — three work surfaces (PFD / Forward / MFD), distribution changes by `TopLevel` only.

| Topology (`presentation` formula) | `MainWindow` | `MfdHostWindow` | Total |
|--------|--------|--------|--------|
| **`(PFD+Forward+MFD)`** | `SkiaHost(PFD)`, `SkiaHost(Forward)`, `SkiaHost(MFD)` | — | **3** slots / **1** `TopLevel` |
| **`(PFD+Forward) (MFD)`** / **`(P+F) (M)`** | `SkiaHost(PFD)`, `SkiaHost(Forward)` | `SkiaHost(MFD)` | **3** slots / **2** `TopLevel` (canon) |
| **`(PFD) (Forward) (MFD)`** / **`(P) (F) (M)`** | per canon — one anchor per window (separate PFD/Forward hosts — roadmap) | `SkiaHost(MFD)` | **3** slots / **3** `TopLevel` (canon); **v1:** as row above since P+F in `MainWindow` |

**Consequence:** no extra “aggregating” `SkiaHost` for whole `MainWindow`; base geometry from `presentation`/`MainGridColumnDefinitions`, surfaces stay slot-oriented.

**Fourth and further** display — optional (e.g. fullscreen telemetry, second Mfd contour, external doc browser — per preset and product policy).

Not the only possible scheme; presets map display↔zone. **`presentation` TopLevel canon** — table and `SkiaHost` matrix above; triple-group code fact — [“Implementation status”](#adr0017-implementation-status). MCP snapshot for several windows already supported ([§7](#adr0017-p7)); new scenarios — field/role tweaks in same delivery.

## Decision (principles; detail per discussion outcomes)

<a id="adr0017-p1"></a>
1. **One process, several `TopLevel`.** Windows belong to one IDE instance; solution, agent, settings state — **unified** VM/service graph behind a facade, not a second “independent” app instance. *Exceptions* (if ever needed) — separate decision and ADR.

<a id="adr0017-p2"></a>
2. **Main frame.** Keep **main window** (editor, solution, default navigation). Additional `TopLevel` are primarily **carriers of detached zones** (see context: MFD/PFD on **second or third monitor**); they **need not** duplicate the full cockpit. Composition and display binding — mode preset and/or explicit user action (e.g. “detach MFD” / “detach chat page” as part of Mfd secondary contour). MCP role `mfd_host` is the Mfd zone host window, not “chat only”. Closing main window — **app exit policy** (close all children or confirm) — fixed in implementation, not implicit.

<a id="adr0017-p3"></a>
3. **What may live in second and further windows (candidates, not all in v1):** primarily **Mfd zone region** on a **separate physical display** — in [0021](0021-pfd-mfd-cockpit-attention-model.md) secondary attention semantics; in code same **`MfdShellView`** host switching **`SecondaryShellPage`** pages (chat, terminal, build, …) — **page is not “the zone”**, zone is **Mfd** attention anchor; optionally **PFD** or compact confirmations/critical indicators on another monitor; detachable chrome per [0012](0012-floating-workspace-chrome.md). **Editor documents as floating MDI** — **out of this ADR** until separate decision (as [0012](0012-floating-workspace-chrome.md)).

<a id="adr0017-p4"></a>
4. **Link to UI modes ([0010](0010-ui-modes-toml-configuration.md)).** Presets set **visibility and slots** over available surfaces: one or several windows. **Zone presentation topology** (`AttentionLayoutSurfaceKind`: one window / several `TopLevel`, etc.) aligns with merge layers in [0010](0010-ui-modes-toml-configuration.md) and separation from panel→zone map — see “Zone presentation topology” in [0010](0010-ui-modes-toml-configuration.md). **Display layout string** — **`presentation`** or **`zone_screen_layout`**; **one** of two, not both. Value — literal from “Multiple monitors” (e.g. `(PFD+Forward) (MFD)`); optionally grammar tokens from [table](#adr0017-presentation-grammar) ([EBNF](#adr0017-presentation-ebnf)). **Storage:** primarily user **`settings.toml`** ([§ above](#adr0017-presentation-grammar), [0028](0028-user-settings-toml-localappdata-and-secrets.md)) so **personal** monitor layout is not mixed with **team** repo `workspace.toml`. Extension: keys for “which panels in detached zone by default” — **mode TOML** / Flight if needed. Validation and enum — in implementation.

<a id="adr0017-p5"></a>
5. **Geometry persistence.** Positions/sizes of **detached zone** windows (incl. 2nd/3rd monitor) — **not** written back to shipped `UiModes/*.toml` ([0010](0010-ui-modes-toml-configuration.md) rule). **In v1 code:** **`MfdHostWindow`** geometry persists in user **`settings.toml`** (`MfdHostWindowPixelX` / `PixelY` / `Width` / `Height` — [“Implementation status”](#adr0017-implementation-status)); other windows / full workspace snapshot — when separately agreed.

   **Presets and display choice for detached zone (accepted direction):** besides raw OS display id and saved rectangles, allow **user-natural semantics** — neighbor direction relative to the screen where **forward** anchor is shown in current layout ([0021](0021-pfd-mfd-cockpit-attention-model.md)), not OS **primary** by default. **Cross-platform:** target stack **.NET + Avalonia**; enumerate displays and **bounds in shared coordinate grid** (neighbor as `left | right | up | down`) via graphics API on **Windows, Linux, macOS**; not single-OS. OS differences (names, hotplug, Wayland vs X11, etc.) — **implementation and tests**, not forward-anchor product model. Key names/schema — same delivery as multi-window / [0010](0010-ui-modes-toml-configuration.md). **Tie-break:** if **several** neighbors in chosen direction, **do not** auto-pick “best” — **user choice** (explicit screen in UI, preset tweak, or raw id/saved rectangle). **Forward spanning several physical displays** — rare; **v1** — **one forward anchor screen** enough; generalize on explicit need. On monitor config change — **fallback** (saved geometry, return to main window, etc.) — implementation topic.

   <a id="adr0017-p5-primary-vs-forward"></a>
   **OS primary display and Forward anchor (accepted):** system **primary** monitor (Windows “Make this my main display”) **is not identified** with **forward (Forward)** cockpit semantics. Primary serves OS/drivers (taskbar, scaling, etc.); typical **conflict** — touch monitor that **must** be primary for calibration though **forward** in user layout is **another** display. Defaulting Forward to primary **breaks** such setups. **Aligning** physical monitor arrangement, OS order, and **`presentation`** string — **user responsibility**; product does not replace desk layout or derive anchor semantics from primary alone. Implementation maps `(…) (…) …` groups to displays per accepted scheme (work area geometry and string order — see code), without “first in `presentation` = OS primary”.

<a id="adr0017-p6"></a>
6. **Agent (ACP, [0016](0016-agent-client-protocol-external-agent.md)).** Transport to external agent **does not depend** on window count: one stdio/session channel. This ADR decides only **where built-in** response/confirmation UI mounts (main vs additional). Short confirmations and critical signals should remain in **PFD attention** zone (concept §3); with second window — duplicate compact strip or explicit rule “focus main for confirmation”.

   <a id="adr0017-p6-confirmations"></a>
   **Confirmations with several `TopLevel` (accepted direction):** do not rely only on system modal over the “wrong” window. Show request in **PFD attention stream** (banner/strip; presentation may avoid classic full-screen modal). Optional **secondary attention channel** (sound, OS notification area flash) if focus is on MFD/second window. **User answer** — same semantics as buttons: **command palette** and **gestures from [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)** — shipped [`Hotkeys/hotkeys.toml`](../../Hotkeys/hotkeys.toml) and user overlay `%LocalAppData%\CascadeIDE\hotkeys.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)). Until separate protocol decision: one `request_confirmation` (and analogs) still ends **ok/cancel** after answer; “non-blocking” refers to **presentation UX**, not mandatory MCP model change.

<a id="adr0017-p7"></a>
7. **MCP and parity ([0002](0002-debug-human-agent-parity.md), [0008](0008-mcp-contracts-and-testable-infrastructure.md), [0012](0012-floating-workspace-chrome.md)).** **Already implemented:** UI snapshot for agent/tests not limited to `MainWindow` — `ide_get_ui_layout` builds **`windows[]`** for all open process `Window`s; control-by-name actions consider non-main windows (see context). **When new product windows or semantics appear** (e.g. dedicated second `TopLevel` for MFD with separate zone taxonomy) **extend contract in same delivery**: new `role` values, region/surface id fields, parity tests — per [0012](0012-floating-workspace-chrome.md). Multi-window surfaces must not stay “human only” without conscious exception.

<a id="adr0017-p8-mfd-host-wide"></a>
8. **`MfdHostWindow` — full secondary contour only.** For layouts like **`(PFD+Forward) (MFD)`** / **`(PFD|Forward) (MFD)`** the second display semantically carries **whole Mfd zone**: one **`MfdHostWindow`** with **full** **`MfdShellView`** — same **page** host (`SecondaryShellPage`: chat, terminal, build, solution explorer, …) and same `DataContext` (`MainWindowViewModel`) as **secondary contour** in main window. **Parity** here — **secondary contour semantics** (page set inside `MfdShellView`), **not** mandatory pixel copy of **entire** visual column under Mfd anchor in `MainWindow`: under Mfd anchor in main — **one** `MfdShellView`; secondary tools (including solution explorer) switch by **pages**, not baked “tree + shell” split ([0021](0021-pfd-mfd-cockpit-attention-model.md): **zone** ≠ **page**). Separate `TopLevel` **need not** duplicate other main-window panels outside secondary contour. **Do not** ship “second window with one page only” (narrow host); “chat only” alternative **rejected** as target. Placement, initial geometry save, screen index from `presentation` — [“Implementation status”](#adr0017-implementation-status); open: monitor-change fallback polish, product UX ([§5](#adr0017-p5)).

## Consequences

- **VM ownership design:** shared services; child windows as views or light VMs subscribed to shared session layer.
- UI tests/scenarios that assumed **only** `MainWindow` tree without `windows[]` in MCP need updates or explicit dual mode (one vs several windows); MCP snapshot for several `TopLevel` already supported.
- User docs (later): how to detach panel, return, where geometry is saved.

## Rejected alternatives (as target state)

- **Rely only on external Cursor/IDE on second monitor** without built-in multi-window — rejected: does not close “one Cascade + screen zones” and in-product PFD/MFD (concept §8 — external tools **complement**, do not replace meaningful Cascade layout).
- **Full document MDI immediately** — out of scope; see [0012](0012-floating-workspace-chrome.md).
- **Several windows on one monitor to “forbid OS drag”** — rejected as motivation: weak; covered by layout **inside one window** (see “One monitor and three semantic zones” above).

<a id="adr0017-modes-scope"></a>

## Clarification: UI modes (Power, Flight, etc.)

**Accepted:** rework of **Power**, **Balanced**, and other presets/families per [0010](0010-ui-modes-toml-configuration.md) is a **separate** line; in **first delivery** of second `TopLevel` those modes **are not changed**; later preset/family rework — **separate** line. Multi-window v1 **not blocked** and **not mixed** with agreeing “how Power will look”. Question “only **Flight** / **Power** with flag” for multi-window **closed** until separate modes roadmap. **Flight** remains a useful experiment sandbox (see context), without obligating product to only that mode.

<a id="adr0017-open-questions"></a>

## Open questions (for discussion before/during further code)

- **Three `TopLevel` on one physical monitor** (three PFD/forward/MFD anchors as three OS frames): **not a default goal** — enough with “main window + detached **zone** on another monitor” (2nd/3rd display), multi-monitor per [0021 §13](0021-pfd-mfd-cockpit-attention-model.md) and **one window + preset/FancyZones** in [0021](0021-pfd-mfd-cockpit-attention-model.md). Revisit “three on one” only with explicit feedback (e.g. single ultrawide).
- Second `TopLevel` for Mfd on separate monitor — see [§8](#adr0017-p8-mfd-host-wide) and [“Implementation status”](#adr0017-implementation-status): **full** `MfdShellView` in **`MfdHostWindow`**; zone/page semantics — [0021](0021-pfd-mfd-cockpit-attention-model.md). **Basic** placement, bounds save, autostart — in code. **Open:** EICAS/IDE Health when detached, **chat/terminal UX** (density, keyboard), full monitor-change fallback; UI mode rework — [“Clarification: UI modes”](#adr0017-modes-scope).
- **Agent confirmations (product details):** timeouts, destructive ops, separate process modal layer — when implementing on top of accepted direction ([§6](#adr0017-p6) above).
- **Presets and monitors (implementation details):** accepted guide — [§5](#adr0017-p5) ( **forward** anchor, neighbor `left | right | up | down`). **Tie-break** with several neighbors one direction — **user side** ([§5](#adr0017-p5)), not mandatory product heuristic. **Display layout keys** — **`presentation`** / **`zone_screen_layout`** and grammar tokens — **personal**, see [§4](#adr0017-p4) and [§ storage](#adr0017-presentation-grammar); **open implementation detail:** fallback completeness when OS layout mismatches.

## Acceptance

**2026-04-11** — status **Accepted**; link from [architecture-policy.md](../../architecture-policy.md). [concept-to-implementation-map-v1.md](../ui-ux/concept-to-implementation-map-v1.md) §6 — multi-window and `MfdHostWindow` (current file binding). **2026-04-11** — section [“Implementation status”](#adr0017-implementation-status): ADR synced with code (topology, placement, bounds persistence).

---

## Change history

<a id="adr0017-history"></a>

| Date | Change |
|------|--------|
| 2026-04-08 | [0021](0021-pfd-mfd-cockpit-attention-model.md), [0010](0010-ui-modes-toml-configuration.md); rejected motivation “forbid OS drag”. |
| 2026-04-11 | **[Zone shares on one display](#adr0017-zone-weights)** (optional anchor coefficients; no weights between `(…) (…)` groups). |
| 2026-04-11 | **[§5 addendum primary vs Forward](#adr0017-p5-primary-vs-forward)**. |
| 2026-04-11 | **[§8](#adr0017-p8-mfd-host-wide):** `MfdHostWindow` — full `MfdShellView` (all pages); narrow single-page host **not** shipped. |
| 2026-04-11 | **`(PFD+Forward) (MFD)` and analogs:** second screen = full MFD. |
| 2026-04-11 | **`presentation`** / **`zone_screen_layout`:** primarily **`settings.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), not team repo — see [§ storage](#adr0017-presentation-grammar), [§4](#adr0017-p4), [0010](0010-ui-modes-toml-configuration.md). |
| 2026-04-11 | **`zone_separator` strict:** parser does not substitute **`|`** for **`+`** or vice versa — see “Multiple monitors”, [EBNF](#adr0017-presentation-ebnf). |
| 2026-04-11 | **no separate `*_zone_alias`:** one literal per zone — only **`pfd_zone_identifier`** / **`forward_zone_identifier`** / **`mfd_zone_identifier`**; short names via same keys (e.g. **`pfd_zone_identifier = "P"`**). |
| 2026-04-11 | **`presentation` grammar:** EBNF in [§](#adr0017-presentation-ebnf); TOML keys — [§](#adr0017-presentation-grammar). |
| 2026-04-11 | **Ideal multi-monitor layout:** **`(PFD) (Forward) (MFD)`** (three displays); **side zone** cockpit metaphor; two-monitor compromise; Mfd zone host (`MfdHostWindow`), MCP `mfd_host`; three `TopLevel` on one monitor — not default goal; **v1 / `MfdShellView`** clarified in code; MCP `windows[]` ([§7](#adr0017-p7)); agent confirmations ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); presets and second display: direction anchor — **forward screen**, not OS primary; neighbor `left \| right \| up \| down`; displays — cross-platform; forward on several displays — out of v1; **tie-break** — user discretion; |
| 2026-04-11 | **Anchor literals in TOML** — [§ table](#adr0017-presentation-grammar), [EBNF](#adr0017-presentation-ebnf). |
| 2026-04-11 | **Display notation:** intra-bracket separator — **`zone_separator`** (**`Z`**, default **`+`**); prose **`+`** and **`|`** illustrate one meaning, `presentation` uses only chosen **`Z`**; several `(…) (…) (…)` — several displays; TOML `#` comments without mandatory ADR refs; product docs — [architecture-policy.md](../../architecture-policy.md). |
| 2026-04-11 | **UI modes (Power, etc.):** not in scope of first second-`TopLevel` delivery; mode rework separate; Flight vs Power for multi-window closed until modes line. |
| 2026-04-11 | **Status:** split “already implemented” / “roadmap” (parser and TOML not backlog). |
| 2026-04-11 | **Terminology:** attention zone **Mfd** vs **`SecondaryShellPage`** pages in `MfdShellView` (chat is a page, not a zone). |
| 2026-04-11 | **Grammar tokens** in **`settings.toml`** — **`[presentation_grammar]`** (`CascadeIdeSettings.PresentationGrammar`). |
| 2026-04-11 | status **Accepted**; navigator [architecture-policy.md](../../architecture-policy.md). |
| 2026-04-16 | [skia-surfaces-vs-overlays-v1.md](../../design/skia-surfaces-vs-overlays-v1.md); [§8](#adr0017-p8-mfd-host-wide): solution explorer — secondary contour page, not separate split in Mfd column `MainWindow`. |
| 2026-04-17 | **`TopLevel` count canon:** `(P+F)(M)` ⇒ two windows; `(P)(F)(M)` ⇒ three; left-to-right = group order in string; host placement only `PresentationHostWindowPlacement`. |
| 2026-04-18 | [§ `display.screens` / topology](#adr0017-display-screens-topology-naming); alignment with [0063](0063-instrument-deck-named-composition-one-anchor.md). |

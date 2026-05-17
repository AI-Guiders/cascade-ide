<!-- English translation of adr/0010-ui-modes-toml-configuration.md. Canonical Russian: ../../adr/0010-ui-modes-toml-configuration.md -->

# ADR 0010: UI Mode Data (Focus / Balanced / …) in TOML

**Status:** Accepted · Implemented  
**Date:** 2026-04-02  
**Updated:** 2026-04-25 — in `[capabilities]`, **IDE Health** keys: `ide_health_*`. Details — [§ History](#adr0010-history).

## Summary

UI behavior is driven by **TOML mode definitions** (`UiModes/`) — capabilities, layout metrics, and family (`UiModeFamily`) — not hard-coded booleans in the ViewModel. The shipping product uses **Flight** as the primary mode id. Older Focus/Balanced/Power names in docs refer to historical presets; the catalog and loader remain the extension point for future presets.

## Related ADRs

| ADR | Role |
|-----|------|
| [0003](0003-debug-ui-mode-separate-from-power.md) | Separate Debug UI mode (not Power cockpit) |
| [0006](0006-presentation-layers-and-feature-slices.md) | Layers, vertical slices, MainWindowViewModel role |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Multi-window and topology |
| [0022](0022-workspace-health-lexicon.md) | Canonical names and evolution for **IDE Health** (intersects `ide_health_*` table below). |

### Outside ADR

| Document | Role |
|----------|------|
| [`attention-zone-panel-playbook-v1.md`](../../design/attention-zone-panel-playbook-v1.md) | Zone ↔ panel ↔ topology |

### Implementation snapshot

| Element | Value |
|---------|----------|
| — | TOML loader, `UiModeCatalog`, capabilities, bundle `UiModes/` |
| — | Override and `docs/ui-ux` — as needed |

---

## Context

Today **mode specs** (panel visibility, editor groups, theme slot, expanded chat width, etc.) are **hard-coded in C#** (`UiModeLayoutRegistry`, related constants). Global chrome metrics (splitters, minimum row heights, etc.) live in **`UiWorkspaceLayoutDimensions`** and apply via **`UiWorkspaceLayout`**.

Need: **change mode layout without rebuild** (design, presets, experiments), consistent with **`settings.toml`** (Tomlyn already a dependency).

**JSON is not proposed** as the primary format: user settings are already TOML; duplicating a second “official” text format for mode configs is undesirable.

## Decision

<a id="adr0010-p1"></a>
1. **Mode data source** — **multiple TOML files**: an **index** defines **order and the full list of mode ids** in the menu (built-in and **additional**, see below) + **one file per mode** (`Focus.toml`, `Balanced.toml`, …; shipped set mirrors defaults from `UiModeLayoutRegistry` / `DefaultsForFamily`) so diffs and “one mode — one file” stay obvious. If a mode file is missing — loader uses the same code defaults. **Shipped set location** — **`UiModes/`** next to exe (same pattern as **`Themes/`**: copy to build output beside exe). Separate **user settings path** for override — possible later extension, **not required in v1**.

<a id="adr0010-p2"></a>
2. **Global chrome metrics** (default tree width, splitter thickness, bottom-zone minimums, collapse policy, etc.) — **once** in **`workspace.toml`** beside the index and modes (not copied into each `Debug.toml`).

<a id="adr0010-p3"></a>
3. **Window schema (invariants):** `MainGrid` column count, column meaning, control-to-column binding — **intentionally remain in code/XAML** in this ADR. TOML describes **modes on that frame** (visibility, metrics, theme slot, etc.), not an alternate window markup. Moving window schema to data — **separate ADR** if product needs it (multiple frames, plugins, etc.).

<a id="adr0010-p4"></a>
4. **Load at startup:** deserialize into a model aligned with `UiModeLayoutSpec` (+ numbers like expanded right-column width for the mode when it stays in data).

<a id="adr0010-p5"></a>
5. **Fallback:** missing, corrupt, or invalid file — **built-in code defaults**, IDE does not crash. See below on role of those defaults.

<a id="adr0010-p6"></a>
6. **Schema version:** field **`schema_version`** — **only at root of the index file** (first file read in the set). It versions the **whole** `UiModes/` bundle — index, **`workspace.toml`**, per-mode files. **`workspace.toml` has no separate `schema_version`** (avoid two numbers drifting). Format migrations — one number from the index.

<a id="adr0010-p7"></a>
7. **Load validation (minimum):** unknown **`theme_slot`** → fallback; shipped set missing spec for a **required** id → warning + built-in spec from code. **Minimum required ids** (product must not “lose” in data) — **defined in code** — today matches **`UiModeLayoutRegistry.OrderedModeIds`**. Index may list **more** ids than that minimum (**extra user/preset modes**). See inheritance and separate menu entry without replacing `Debug`.

<a id="adr0010-p8"></a>
8. **Capabilities** — type **`UiModeCapabilities`** (`Features/UiChrome/UiModeCapabilities.cs`). In **`UiModes/<id>.toml`** keys are **semantic** (what appears in UI), **snake_case**; deserialization via **`CascadeTomlSerializer`** (PascalCase model properties → snake_case). Merge: explicit value → on **`inherits`** from parent → else **`DefaultsForFamily`**. API: **`GetCapabilities`**, **`GetWindowTitleOverride`**.

   | TOML key | Meaning |
   |----------|---------|
   | `active_task_strip` | Active task / Task Cockpit strip under toolbar |
   | `main_window_title` | Full main window title |
   | `quick_actions` | Quick actions at task |
   | `agent_operations_panel` | Agent operations block in chat (Balanced) |
   | `agent_trace` | Agent trace panel (Power) |
   | `autonomous_agent_telemetry` | Power cockpit: explicit access to output (terminal and hints); **not** IDE Health channel |
   | `ide_health_on_terminal_tab` | IDE Health duplicate on Terminal tab (Power) |
   | `ide_health_main_column_span` | IDE Health area column span in main grid (Power) |
   | `ide_health_strip` | Show IDE Health strip under editor |
   | `ide_health_surface` | `bottom_strip` or `dedicated_page` — IDE Health presentation layer |
   | `instrumentation_tabs` | Events/tests/debug tabs in bottom dock |
   | `hypotheses_tab` | Hypotheses tab |
   | `risk_summary_card` / `result_summary_card` | Risk and result cards in chat |

### Model layers (cheat sheet for `UiModes/*.toml` authors)

To avoid confusing **mode id**, **layout**, **family**, and **capabilities**:

| Layer | What it is | Source |
|-------|------------|--------|
| **Mode id** | Stable string (menu item, catalog key, `Id.toml` filename) | `index.toml` → `modes`, plus built-in id table in code for fallback |
| **Layout** | Panel visibility, editor group count, theme slot, flags like “select terminal tab” | Base: `UiModeLayoutRegistry` for built-in ids; merge with current `*.toml`; on **`inherits`** — base = resolved parent |
| **`workspace.toml`** | Shared chrome numbers (splitters, chat widths per Power / AgentChat / others rule, row minimums, etc.) | One file per bundle; not copied per mode |
| **`family`** | Product role: Focus, Balanced, Power, AgentChat, Debug — affects **capability defaults** and code branches (`UiModeFamily`) | Order: explicit **`family`** in mode file → on **`inherits`** from resolved parent → built-in id table → else **Balanced** |
| **Capabilities** | What to show (hypotheses, quick actions, Power cockpit chrome, etc.) | Base: on **`inherits`** — **resolved parent capabilities**; without **`inherits`** — **`DefaultsForFamily(family)`**. Overlay — explicit keys in `*.toml` |

Narrow key in the same **`workspace.toml`** about **where to mount Markdown preview** (not to be confused with future general multi-`TopLevel` topology) — [0026](0026-markdown-preview-surfaces-and-placement.md).

**End users** in the “UI mode” combo mostly see **preset id**; **`family`** / **capabilities** axes are optional in UI until separate toggles exist.

### Zone presentation topology (future extension)

Today code has one topology — **`MainWindowDockedGrid`** (`AttentionLayoutSurfaceKind`, single main window, `MainGrid` columns). When **alternatives** appear (multiple `TopLevel`, scenarios in [0017](0017-multi-window-workspace-and-agent-surfaces.md)), **link** chosen topology with merge layers from [0010](0010-ui-modes-toml-configuration.md) and separate **per-monitor personal** layout (see next paragraph). **Without writing** dynamic window resize back into shipped files (see runtime subsection below).

**Do not mix** with panel→zone map (`attention_zone_panels` / `AttentionZonePanelRuntime`): that is “which panel in which zone”; here — **in which geometry** (one window vs several) regions appear. Details: [`attention-zone-panel-playbook-v1.md`](../../design/attention-zone-panel-playbook-v1.md).

**`presentation` / `zone_screen_layout` (root `settings.toml`) and grammar tokens in **`[presentation_grammar]`** (`screen_markers`, `screen_separator`, `zone_separator`, anchor literals `pfd_zone_identifier` / `forward_zone_identifier` / `mfd_zone_identifier`) — screen layout string from [0017](0017-multi-window-workspace-and-agent-surfaces.md) (“Multiple monitors”), e.g. `(PFD+Forward) (MFD)` or **`(P+F) (M)`** with short identifiers in TOML ([table](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-grammar)); **between anchors inside one parenthesis pair** only literal **`zone_separator`** (**`Z`**, default **`+`**); if **`Z = "|"`** the string uses **only** `|`. **EBNF** — [0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-ebnf). **Storage:** primarily **`settings.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)). Repo **`.cascade/workspace.toml`** ([0021](0021-pfd-mfd-cockpit-attention-model.md) §2.1) — team panel/zone conventions, **not** mandatory monitor layout for everyone; on merge **user** `presentation` and tokens **override** bundle/repo. **Alternate key name** — **`zone_screen_layout`**; only one of the two per config. `AttentionLayoutSurfaceKind` enumeration and validation — in implementation; separate TOML-schema ADR **not required** while rules fit this subsection.

**`inherits = "BaseId"`** — one parent: inherit the **full** resolved mode (layout, capabilities, chat width, task strip, window title — per merge rules in code). Child file sets **only deltas**. No separate `[inherits]` section listing “which fields inherit”: with this model the delta list in the file is already shorter than enumerating slices.

### Formal `family` resolution order

1. If **`Id.toml`** has **`family = "…"`** (case insensitive) — use it.
2. Else if **`inherits`** — take **`family`** of the resolved parent (recursive).
3. Else — for known built-in ids (`Focus`, `Balanced`, …) — **`BuiltinFamily(id)`** in code.
4. Else — **Balanced** (unknown id without explicit family).

### Examples: `inherits` and optional `family`

**Extra mode like Debug but with task strip and custom title** — without copying full spec:

`index.toml` (fragment; full index see shipped `UiModes/index.toml`):

```toml
schema_version = 1
modes = [ "Focus", "Balanced", "Power", "AgentChat", "Debug", "MySuperDebug" ]
```

`MySuperDebug.toml`:

```toml
inherits = "Debug"

active_task_strip = true
main_window_title = "CascadeIDE — my debug preset"
```

**`family` not needed:** family stays **Debug** (resolved parent), hypothesis/instrumentation behavior as debug.

**Two-level chain:** each file only deltas; chain root — built-in id with full spec (or another resolved mode).

```toml
# DeepWork.toml — branch from Focus
inherits = "Focus"
editor_group_count = 1
main_window_title = "CascadeIDE — Deep work"

# MyDeep.toml — branch from DeepWork
inherits = "DeepWork"
chat_expanded_width_pixels = 400
```

Both new ids must appear in **`modes`** in `index.toml`.

**Rare case: `inherits` from `Debug` and explicit `family`** (UiModeFamily axis differs from parent; see note after example):

```toml
inherits = "Debug"
family = "Balanced"
```

Layout — merge **Debug** spec with file fields. **`family`** for code (`UiModeFamily`, predicates like **`IsDebugFamily`**) becomes **Balanced**. Important: on **`inherits`**, capability base layer in the loader comes from **resolved parent** (`parentResolved.Capabilities`), not **`DefaultsForFamily(family)`** — UI flags stay “as Debug” until overridden in the same `*.toml`. If capabilities should start from **`DefaultsForFamily`** of the chosen family while inheriting layout — that is a **separate loader contract** change.

*See also subsection **`inherits` and `family`: not the same thing** below — modular merge and chat width.*

### Dynamic resize after startup and TOML (source of truth)

**`UiModes/*.toml`**, **`workspace.toml`**, and the mode index are **presets**: layout and metrics **at config load** (app start and, if added later, explicit “reload config”). They are **not** a live geometry journal.

**Splitter drag, panel width/height changes, and any dynamic chrome resize after launch** are **in-memory runtime state**. They are **never written back** by default to shipped **`UiModes/*.toml`** and **`workspace.toml`** beside exe — not on every splitter move, not on mode change, not on IDE exit. Repo diffs stay **design presets**; accidental window drift does not corrupt preset files.

If **persisting** geometry between sessions is needed later — **separate channel** (e.g. fields in **`settings.toml`**, user override under `%LocalAppData%` — see “Boundaries” below), **not** reverse write into distributable mode TOML.

**MCP (`ide_set_panel_size` and analogs)** and UI snapshots for tests/agent change **current** layout at runtime; same contract: **do not** assume the call overwrites TOML on disk unless documented as a separate “export preset to file” operation.

### Same `Debug` vs separate `MySuperDebug` (inheritance)

- **Editing `Debug.toml` or override on `Debug`** changes **Debug** for that config set: **stock Debug as shipped does not live beside** — expected when the goal is to override Debug. For “stock Debug **and** my variant in the menu” you need **two different ids**.
- **Extra id** (e.g. `MySuperDebug`), debug-like but different layout: listed in **index** (separate menu line, separate saved setting), file **`MySuperDebug.toml`**. **`inherits = "Debug"`** merges resolved Debug spec with overrides — no copy-paste of dozens of fields, no C# change per new id — index + mode file (and code rules for unknown ids: allowed bases, fallback).
- **UI label** for id (display name instead of raw `MySuperDebug`) — optional fields in index/TOML or localization; **not** mixed with required minimum ids from [§7](#adr0010-p7).

### Semantics for code: `family` and legacy `Is*Mode` flags

**Context (problem before one axis):** checks like “is this Debug for hypotheses?” via **`UiMode == "Debug"`** or **`IsDebugMode`** break for derived ids (**`MySuperDebug`**): id string differs, product meaning “debug family” is the same.

**Current code** already uses **`UiModeFamily`** and extension predicates — see **“Implementation in code”** below. Table of options and **`family` in TOML** — about **data after loader**; **`family`** after merge must land on the same axis as today’s `UiModeFamily`.

Additionally, **`NormalizeUiMode`** currently maps unknown mode to **`Balanced`**. **New ids** from the index must **be preserved** (after “id exists in loaded list” check), or user `MySuperDebug` silently becomes Balanced without warning.

**Discussion history (context; approach A implemented):**

| Approach | Idea | Pros | Cons |
|----------|------|------|------|
| **A. Family (`family`) in data** | After merge, model has **`family`** (enum or string: `debug`, `power`, …). Optional **`family = "debug"`** in TOML; if unset — **from base** on `inherits`. Code moves **`IsDebugMode`** to **`family == debug`** (or **`IsDebugFamily`**). | Explicit semantics; `MySuperDebug` gets “debug” via inheritance; easier tests. | Migrate existing id checks; finite `family` list still in code. |
| **B. `inherits` chain only** | “Debug-ness” from walking inherits to built-in id (**`ResolvesToBuiltIn("Debug")`**). | No duplicate `family` in TOML. | Harder to reason/debug; depth/cycles; unclear cutoffs for UX. |
| **C. Explicit `family` in every TOML** | Every mode including derivatives writes `family` by hand. | Maximally explicit. | Duplication; easy to omit `family` on `MySuperDebug`. |
| **D. Exact id match only** | `IsDebugMode` stays `== "Debug"`; derivatives **not** Debug for hypotheses etc. | Minimal code. | Contradicts “MySuperDebug as Debug”; misleading. |

**Implemented:** approach **A** — after load, **`UiModeFamily`**; optional **`family`** in TOML; VM/view checks use family and capabilities, not raw id where product role matters.

#### `inherits` and `family`: not the same thing (separation of concerns)

- **`inherits`** picks a **resolved parent**: **`UiModeLayoutSpec`** and base **`UiModeCapabilities`** (not “frame only”); child file applies **modular** overrides. For **`chat_expanded_width_pixels`**: explicit in file → else parent on `inherits` → else root mode — **`workspace.toml`** (via **`UiWorkspaceLayoutRuntimeMetrics`**) and Power / AgentChat / others rule.
- **`family`** is **product role** in code: which tabs/tools/policies count as “debug mode”, “power”, etc. **Different dimension** from `inherits`: you could inherit Debug layout but override family (rare).

Default rules so TOML does not look like two knobs for one thing:

1. **Built-in** ids — **`family`** from **code table** (same meaning as today’s ids).
2. With **`inherits`**, **`family` in TOML optional**: default **`family` = parent’s family** (e.g. `MySuperDebug` → `inherits = "Debug"` → **Debug**). File can be **`inherits` only** without `family` while semantics match base.
3. **Explicit `family` in TOML** — when you need to **override** computed family for **`UiModeFamily`** (rare; see capability-base note in example above).

So **`inherits` does not “set family” by itself** — it sets **parent chain** for layout/capability merge; **family** is explicit, inherited from parent, from built-in table, or **Balanced** for unknown id. Split: file/parent merge vs family axis for code.

**Related places (non-exhaustive):** `MainWindowViewModel.Presentation` (**`UiModeFamily`**, instrumentation), **`UiChromeViewModel.NormalizeUiMode`**, bloom by mode string, **`GetChatPanelExpandedWidthPixels`**, MCP handlers with special debug rules.

#### Do not multiply `Is*Mode` in VM

Booleans **`IsFocusMode`**, **`IsPowerMode`**, **`IsDebugMode`**, etc. were a typical **smell**. Code **removed** them in favor of **`UiModeFamily`** and predicates (see **“Implementation in code”**). After TOML loader, prefer **one computed context**: enum **`UiModeFamily`** and/or compact **capabilities** from resolved spec and defaults. **Do not** add another `IsNewThingMode` or raw id comparisons where **family** is needed.

#### Implementation in code (fixed before TOML loader)

Not a separate doc: mode-axis agreements live **here** in the modes/TOML ADR.

| Element | Role |
|---------|------|
| **`UiModeFamily`** + **`UiModeFamilyResolver.FromNormalizedMode`** | One axis after **`NormalizeUiMode(UiMode)`**; derived ids map like built-in strings. |
| **`UiModeFamilyExtensions`** (`IsFocusFamily`, `IsBalancedFamily`, `IsPowerFamily`, `IsAgentChatFamily`, `IsDebugFamily`) | Symmetric predicates instead of scattered **`== UiModeFamily.*`**. |
| **Domain names where enum is not enough** | e.g. private **`AutonomousCockpitActive`**: “autonomous agent cockpit”, inside — **`IsPowerFamily()`**. |
| **XAML** | Bindings to **`UiModeFamily`** via converters **`UiModeFamilyEq` / `UiModeFamilyNe`** (parameter — enum member name). |
| **After TOML** | Loader merges spec, **`family` → `UiModeFamily`**, **`UiModeCapabilities`** (optional **`main_window_title`**), no return to **`Is*Mode`**. |

### Fallback and built-in defaults (code vs TOML)

Built-in fallback **does not claim** to be the main source of pretty layout: it **predictably boots IDE** when disk data is missing or broken. **Normal path** — shipped TOML beside exe; fallback is rare (clean install, broken directory, dev without file copy).

Quality of current code constants (including around **Power**) may be **average** — **separate** work: polish defaults, align with design, maybe align built-in with shipped files. **Does not block** “TOML + code fallback” mechanics: reliable load and tests first, polish parallel or later.

### Panel visibility vs “0 px”

In mode config and product meaning, **visibility semantics are primary** (`visible` / `hidden` for a panel; later maybe “collapsed with strip” if non-zero strip policy appears). **Hidden should not be defined mainly as `width = 0`** in mode TOML: zero column width and hidden splitter are **derived** from visibility rules and global metrics from `workspace.toml` (and code invariants). One idea covers **any** collapsible column/zone, not only chat.

## Boundaries (explicitly out of this ADR)

- **Window schema** (MainGrid frame, new columns/zones from data) — see [§3](#adr0010-p3); out of scope.
- **`.toml` syntax highlighting in editor** — separate ([EDITOR-LANGUAGES.md](../../EDITOR-LANGUAGES.md): no TOML grammar in bundle yet); does not affect config load.
- **User override** in `%LocalAppData%\…` over shipped file — possible extension **after** base schema, not required in first commit.

## Consequences

- **Pros:** one human-readable format with settings; mode edits without compiler; easier layout diff review; extra modes with **inheritance** without duplicating spec or mandatory C# per new id.
- **Cons:** parsing, on-disk errors, fallback tests; TOML field docs for contributors; loader with **`inherits` merge**; mode combo in UI from **loaded index** (code keeps **minimum** for validation and fallback); **`family`** and **capabilities** must stay aligned with **`UiModeFamily`** and **`UiModeCapabilities.DefaultsForFamily`**; **`NormalizeUiMode`** preserves user ids from index; **VM `Is*Mode` booleans removed** (see “Implementation in code”). Main **`docs/ui-ux`** aligned to **`UiModeFamily`** / capabilities (overview: **`docs/ui-ux/ui-modes-overview-v1.md`**).
- **MCP link:** `ide_set_panel_size` and UI snapshots must not break; disk contract — runtime/MCP **do not** rewrite `UiModes/*.toml` / `workspace.toml` by default (see “Dynamic resize after startup and TOML”).

## Documentation (in scope)

Navigation and UX docs should describe **`UiModeFamily`**, **capabilities**, and TOML when needed — not legacy **`Is*Mode`**. Reader overview: **`docs/ui-ux/ui-modes-overview-v1.md`**; window layout and concept→code — **`docs/ui-ux/cascade-ide-ui-layout-v1.md`**, **`docs/ui-ux/concept-to-implementation-map-v1.md`**. Historical ADRs (e.g. approaches A–D) may mention old names in past tense — normal.

## Rejected alternatives

- **JSON only** — diverges from `settings.toml` and Tomlyn stack for user configs.
- **Code only** — leaves current state; rejected as main path given “mode file without release” goal.

## Next step (optional)

User override of `UiModes/` catalog from `%LocalAppData%` when product needs it; targeted **`docs/ui-ux`** updates when mode behavior changes in code.

---

## Change history

<a id="adr0010-history"></a>

| Date | Change |
|------|--------|
| 2026-04-08 | Intent to define **zone presentation topology** in TOML after alternatives to single `MainGrid` ([0017](0017-multi-window-workspace-and-agent-surfaces.md)); see subsection above. |
| 2026-04-11 | **`presentation`** / **`zone_screen_layout`:** primarily **`settings.toml`**, not team repo — [0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-grammar), [§4](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p4); grammar tokens — **`[presentation_grammar]`** (no separate **`pfd_zone_alias`** — short names via **`pfd_zone_identifier`** etc.); **`screen_markers`** / **`screen_separator`** / **`zone_separator`**, **EBNF** — [0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-ebnf). |
| 2026-04-11 | Anchors `adr0010-p1`…`p8` in Decision and links to [§3](#adr0010-p3), [§7](#adr0010-p7); convention — [ADR README](README.md#adr-anchors-policy). |
| 2026-04-25 | In **`[capabilities]`**, **IDE Health** contour keys in TOML: **`ide_health_*`** (`UiModeCapabilities` properties — `IdeHealth*`). |

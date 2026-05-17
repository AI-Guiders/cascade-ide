<!-- English translation of adr/0039-workspace-navigation-affordances.md. Canonical Russian: ../../adr/0039-workspace-navigation-affordances.md -->

# ADR 0039: Workspace navigation — multiple views and “current file + related”

**Status:** Accepted · Implemented  
**Date:** 2026-04-16  
**Updated:** 2026-04-16 — [language scope](#adr0039-language-scope); [“Product metaphor”](#adr0039-product-metaphor). Details — [§ History](#adr0039-history).  
**Implementation (MCP layer):** fixed in code (2026-04): presets in `settings.toml`, filter echo, subgraph semantics; see [§ Agent/MCP](#adr0039-mcp-workspace-navigation). Full Semantic Map UI and `ILayoutEngine` — outside this status.

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD, “where am I” anchor |
| [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) | Navigation ≠ strict PFD surface |
| [0010](0010-ui-modes-toml-configuration.md) | UI presets |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | commands, palette |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation`, surface placement |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Agent ↔ Roslyn MCP in `settings.toml` |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Roslyn profiles, Manager, EFB |
| [0065](0065-instrument-categories-domain-taxonomy.md) | `graph_kind`, intent map |
| [0067](0067-graph-backed-surfaces-contract.md) | graph-backed surfaces contract |

### Implementation snapshot

| Element | Value |
|---------|-------|
| — | MCP `get_code_navigation_context`, presets and filters in `settings.toml` |
| — | full Semantic Map UI / `ILayoutEngine` — planned, see ADR body |

## Summary

- Navigation **C# / .NET first**: Roslyn and solution are north-star, not “IDE for every language”.
- Multiple workspace views + **“around active document”** mode instead of one tree.
- MCP layer (presets, subgraph, `get_code_navigation_context`) — **implemented**; full Semantic Map UI — roadmap.
- Orthogonal to strict PFD surface ([0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md)).

---
## Context

Classic **file tree** (solution explorer) is mostly **isomorphic** to on-disk project structure. On **large** repos deep hierarchy imposes high **cognitive tax**: hold path in head, expand nodes repeatedly, miss the branch. Developers often think in **entities**: current symbol, related file, test for class, layer boundary.

Cascade IDE positions as an **agent-first** cockpit with explicit attention model ([0021](0021-pfd-mfd-cockpit-attention-model.md)); it is **not** obliged to copy “one tree — only navigation” from classic IDEs. Instead: **several coordinated workspace views** and **“around active document”** mode — deliberate product differentiator.

<a id="adr0039-language-scope"></a>

### Language scope (north-star)

**Near-term target — C# / .NET ecosystem**, not “IDE for every language”. Navigation semantics depth (“related” files, symbols, graph) leans on **Roslyn** and `.sln` / project workflow first. IDE UI on **Avalonia**; product may include **Blazor** and other .NET stacks — without **language-service parity** obligation with VS/Rider for Python, Java, Go, etc.

After product opening **additional languages** (editor, LSP, navigation) are possible — not **north star** and not v1 success criterion. “Universal” view or “languages without semantics” here reads as **C#-first**: universality is **not** a team obligation at cockpit/agent maturity stage.

<a id="adr0039-product-metaphor"></a>

### Product metaphor and target direction (discussion)

**“Cabinet vs battle map”:** classic project tree is closer to a **filing cabinet** (physical disk address) than a **map of the current fight**. Developers often hold **logical links** (“service — contract — tests”), not path `src/domain/...`. On large solutions the explorer becomes **long scroll** and needle-in-haystack — argument for **alternative primary** views, not forever dropping the tree.

**Link graph and situational awareness:** target image — **dynamic graph of relevant context** around active file or task: “where I am” node and **rays** to places work touches (dependencies, contracts, tests, data schema). In cockpit terms closer to **situational awareness** ([0021](0021-pfd-mfd-cockpit-attention-model.md): stable attention contour) than static folder picture. Full solution dependency graph is out of mandatory v1 scope (see **Non-goals**).

**Agent-first and “tree from goal”:** human and agent typically think **dependencies and task**, not folder nesting. Navigation **from goal / active node**, not “from disk root”, aligns UX with tools like Cursor — while honest file-on-disk work under the hood.

**PFD vs MFD for “cabinet” view:** policy may allow: **live context** (related nodes, graph, semantic map) in **primary attention** ([0021](0021-pfd-mfd-cockpit-attention-model.md) — PFD region), **classic file tree** as **secondary** (move files, rare structure overview) in **MFD** or equivalent ([0021](0021-pfd-mfd-cockpit-attention-model.md) — MFD). Exact placement — preset ([0010](0010-ui-modes-toml-configuration.md)); principle: **do not compete** for one attention anchor with two incompatible mental models without explicit mode switch.

**Semantic Map and placement:** target UX — **semantic map** (nodes like “Payment Logic”, “DB Schema”, “API Endpoint” with links), not necessarily path string `src/.../payment.cs`. Cockpit **zone topology** (where PFD / Forward / MFD sit) is set by **`presentation`** and **`[presentation_grammar]`** — per [EBNF in 0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-ebnf) only three zone anchors, **no** separate “window” literal. After parse, topology **may** yield a **second `TopLevel`** (e.g. **`MfdHostWindow`** on another display) — **consequence** of anchors and monitor count ([0017](0017-multi-window-workspace-and-agent-surfaces.md), incl. [§8](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p8-mfd-host-wide)), not a “magic token in the string”. Settings windows and **floating chrome** — other rules ([0012](0012-floating-workspace-chrome.md), code, [0028](0028-user-settings-toml-localappdata-and-secrets.md)). Presets — [0010](0010-ui-modes-toml-configuration.md), [0021](0021-pfd-mfd-cockpit-attention-model.md). Semantic Map field details — outside this ADR.

<a id="adr0039-presentation-vs-toplevel"></a>

*(Summary: **`presentation` sets anchors and screens** → from that **follows** whether a second `TopLevel` for MFD opens. It does **not** imply arbitrary “settings window” — no such token in EBNF.)*

## Solution (principles)

<a id="adr0039-p1"></a>

### 1. File tree is not the only canon

**Rule:** product **supports** classic solution explorer as familiar mode (repo structure overview, file ops where fit), but does **not** enshrine it as the **only** way to answer “where am I and what is next to my work”.

**Complementary** navigation (implementation directions; not all in one release):

- **“Current file and related”** — folder/namespace neighbors, partial classes, code/test pairs by convention, recent docs in same task slice;
- navigation by **symbols** (types, members) and active document **outline**;
- **multiple views** in one region: tabs or split (tree | related | symbols) with **sync** to active document.

<a id="adr0039-p2"></a>

### 2. Link to PFD zone and [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md)

Navigation panels in **PFD** region still anchor “where am I in work” ([0021](0021-pfd-mfd-cockpit-attention-model.md); [0037 glossary](0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-glossary-pfd-dual)). They are **not** required to be **strict PFD surface**: interactivity (clicks, expand) stays allowed. New navigation kinds do **not** weaken **instrument** invariants (EICAS, compact workspace health, agent status, etc.) marked per [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md).

<a id="adr0039-p3"></a>

### 3. Presets, placement, and layout canon (fixed)

<a id="adr0039-layout-canon"></a>

**Layout canon:** **`(PFD)(Forward)(MFD)`** — three regions in **one** main window shell: primary attention (**PFD**), main work (**Forward** — editor, document), secondary contour (**MFD**). Left-to-right order (or vertical preset equivalent) gives **predictable scan pattern** — stable gaze/hand path ([0021](0021-pfd-mfd-cockpit-attention-model.md)).

**Windows and attention (product default, not OS ban):** for **workspace navigation** and adjacent **main** work panels, **prefer** keeping them in **the same** `(PFD)(Forward)(MFD)` shell so **scan pattern** stays one continuous “instruments — work — secondary”. Separate **top-level** windows for this contour **easily break** gaze and motor path.

**`presentation` string and `TopLevel`:** the string has **no** “window” token — only **PFD / Forward / MFD** ([0017 § EBNF](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-ebnf)). Parse result (multi-monitor, MFD on second screen) **may** open **`MfdHostWindow`** — second `TopLevel` for full MFD ([0017 §8](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p8-mfd-host-wide)); that does **not** contradict “topology from `presentation`”, it is **interpretation** with monitors. Settings window, floating chrome — code and [0012](0012-floating-workspace-chrome.md), [0028](0028-user-settings-toml-localappdata-and-secrets.md), [0010](0010-ui-modes-toml-configuration.md). Canon `(PFD)(Forward)(MFD)` in **one** main window remains default for scan pattern; see [summary above](#adr0039-presentation-vs-toplevel).

**Multi-monitor:** prefer **extending** same logical scheme (e.g. move whole region to second display as **one** linked shell fragment), not spawning **duplicate** floating navigation windows on second screen without tight scan-path link. Details — presets ([0010](0010-ui-modes-toml-configuration.md), [0017](0017-multi-window-workspace-and-agent-surfaces.md)); this ADR fixes **principle**, not pixel-perfect spec.

Tool density **inside** PFD/MFD may still depend on UI mode / preset.

<a id="adr0039-semantic-map-layout"></a>

### 4. Semantic Map — v1 strategy and layout abstraction (fixed)

**Hybrid delivery (v1):**

- Baseline — **“related” list** (and outline if needed) — without mandatory graph.
- **Mini-map** — graph of **bounded** subgraph (**caps** = upper bounds on **node and edge count**, plus operational limits like layout timeout; see [Non-goals](#adr0039-non-goals), [closed: cap definition](#adr0039-closed-questions), [open: concrete numbers](#adr0039-open-questions) §3). Intent — do not overload PFD attention; when designing concrete N, **7±2** simultaneously **visible** entities (Miller) is an orienting heuristic, engineering threshold may be higher with clustering/collapse. Over cap — **degrade** to list, do not “break” PFD.
- Layout for mini-map: early iterations may use **simple built-in layout** (e.g. hierarchical/force in C#) **or** external engine (e.g. discussed **Skia + GraphViz**: GraphViz for layout, Skia/Avalonia for draw and interaction) — choice is **`ILayoutEngine` implementation**, not UI.

**`ILayoutEngine` abstraction (canonical name in code; “layout engine” in ADR text):**

- Input: neutral graph description (nodes, edges, label metadata).
- Output: node positions and edge geometry (or equivalent for renderer), or failure with reason (timeout, graph too large).
- **Several implementations** (simple managed layout, GraphViz, MSAGL if needed) — **interchangeable** without rewriting navigation panel; compare approaches over time, not v1 blocker.

**Operational limits:** layout timeout, explicit graph size cap, predictable fallback — **mandatory** in engine usage contract.

## Consequences

- Explicit **permission** to design navigation as **modes answering “where am I”**, not only copying other IDEs’ trees; canon **`(PFD)(Forward)(MFD)`** and **scan pattern** — cockpit default; link **`presentation` → topology → possible `MfdHostWindow`** and windows **outside** EBNF — [§3](#adr0039-layout-canon), [summary](#adr0039-presentation-vs-toplevel), [0017](0017-multi-window-workspace-and-agent-surfaces.md).
- **MCP:** agent can use **`get_code_navigation_context`** with presets, filter echo, explicit edge semantics in `subgraph` — [§ Agent/MCP](#adr0039-mcp-workspace-navigation); lowers “file list without meaning” risk in [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) scenarios, does not replace UI map.
- Implementation needs: **related files** model (heuristics, conventions, Roslyn when available for C#), **sync** UX between views, view-switch commands ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); for Semantic Map — **layout engine** behind `ILayoutEngine` and shared render contract (e.g. Skia), [§4](#adr0039-semantic-map-layout); **graph source of truth** and TOML layer — [fixed](#adr0039-semantic-map-data-layer).
- Onboarding and discoverability ([0027](0027-small-team-focus-vs-public-maturity.md)) can explain **why** several navigation kinds, not one.
- Metaphor **PFD = battle map / live context**, **MFD = cabinet when needed** — language for presets and marketing; stepwise implementation without mandatory “remove tree” in first release.

<a id="adr0039-non-goals"></a>

## Non-goals

- Full solution **dependency graph** as mandatory first-version screen.
- Replacing **file search**, **Go to symbol**, **command palette** — orthogonal to this ADR.
- **Navigation and language-service parity for all languages** like a “full” polyglot IDE: north-star stays **C# / .NET** ([language scope](#adr0039-language-scope)); other languages later — product queue, not v1 success condition.

<a id="adr0039-semantic-map-abstraction-layers"></a>

### Semantic Map: node abstraction level (configuration layers)

**UI switch** is **panel presentation mode** (map “layer scale”): what counts as a node and how the graph is built. Need not duplicate repo command policy.

**Defaults and memory** are not the same as instant “now” mode:

- **`settings.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)) — **personal default** abstraction on this machine (all workspaces unless team override).
- **`<repo>/.cascade/workspace.toml`** (merge with bundle, [0010](0010-ui-modes-toml-configuration.md)) — **optional** team convention (“map in types / features here”).
- **Session state** (and if agreed — same path as other UI “memory” per workspace, [0017](0017-multi-window-workspace-and-agent-surfaces.md), [0028](0028-user-settings-toml-localappdata-and-secrets.md)) — **last UI switch** choice; **not committed**.

**Priority on workspace open:** merge `workspace.toml` (team) → `settings.toml` (personal default) → product built-in default.

What **numeric caps** mean (node/edge thresholds, layout timeout, possibly traversal depth) and **degrade-to-list** principle — [closed](#adr0039-closed-questions); **concrete N** — [open](#adr0039-open-questions) §3. Orienting “glanceable” subgraph in PFD — order **7±2** entities (Miller), not sole law in code.

<a id="adr0039-semantic-map-data-layer"></a>

### Semantic Map: source of truth and configuration layer (fixed)

**Position:** for **C#**, “what links to what in code” graph structure — **runtime source of truth**: **Roslyn + solution** (and heuristics on top), not mandatory duplicate nodes/edges in TOML — avoids repo drift.

**Presets** and **`<repo>/.cascade/workspace.toml`** (merge with bundle, [0010](0010-ui-modes-toml-configuration.md)) — **layout and UI modes**, not mandatory hand-listed Semantic Map nodes. **`presentation`** ([0017](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-ebnf)) is **not** extended with map semantics.

**Optional later:** committed **annotations** (feature labels, manual clusters, what code cannot infer or team wants fixed for agent/onboarding) — **separate** section or file under `.cascade/` if product need arises; **v1** may omit.

**Cache** of index or subgraph in **`%LocalAppData%`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)) — **performance and volume**, not team Git contract; reset without losing truth.

<a id="adr0039-mcp-workspace-navigation"></a>

### Agent/MCP — implemented contract (Pareto layer)

IDE command **`get_code_navigation_context`** (`ide_execute_command`) — modes **`related`** (related files with brief rationale) and **`subgraph`** (nodes and edges with numeric caps `max_nodes` / `max_edges` and `max_related` for list). Anchor — active editor document or explicit **`file_path`**. File source — loaded solution tree; link semantics — C# heuristics / partial / XAML / tests / namespace / directory ([0028](0028-user-settings-toml-localappdata-and-secrets.md) is not graph-in-TOML but user settings channel).

**Base presets** — single repo source (**`CodeNavigation/presets.toml`**): file copied beside exe (like `UiModes/`, `Hotkeys/`); if missing on disk, same text from **embedded resource** (no duplicate literal in code). Default content: `peers_only`, `no_namespace_noise`, `tests_and_peers`, `structure_only`.

**Repository (team)** — in **`.cascade/workspace.toml`** at solution root, **`[code_navigation]`** and **`[[code_navigation.presets]]`** (same format as `settings.toml`). Merge by **`id`** over IDE bundle; lower priority than user layer (below). Matches UI metrics overlay pattern from [0021](0021-pfd-mfd-cockpit-attention-model.md) (`UiWorkspaceToml` + repo).

**User overlay** — **`%LocalAppData%\CascadeIDE\settings.toml`**, **`[[code_navigation.presets]]`**: each table has **`id`**, optional **`include_kinds`** / **`exclude_kinds`**. Same **`id`** overrides bundle and repo. Final order: **bundle → `.cascade/workspace.toml` → `settings.toml`**.

**MCP arguments:** besides mode and paths, **`preset`** (name from merged set), **`include_kinds`**, **`exclude_kinds`**. Merge rule: non-empty request **`include_kinds`** **replaces** preset include; request **`exclude_kinds`** **union** with preset exclude (dedupe by canonical kind name). Unknown preset or bad preset data → **`error`: `bad_preset`**.

**Canonical link kind names** (stable agent contract): `partial_peer`, `project_peer`, `xaml_codebehind_pair`, `test_counterpart`, `same_namespace`, `same_directory`. Unknown tokens in lists ignored; empty white list after normalize means “no include restriction” (see filter implementation).

**Filter echo in JSON:** both modes include **`kind_filter`**: **`preset`** (requested name or `null`), **`include_kinds_effective`**, **`exclude_kinds_effective`** — canonical lists after merge so agent sees effective filter without re-reading settings.

**`subgraph` mode:** node carries semantic **`kind`** — link kind to anchor (not generic “related”). Edge carries **`related_kind`** for the pair. Agent can distinguish e.g. project peer vs namespace match.

Call canon and argument table — **`docs/MCP-PROTOCOL.md`**; agent walkthroughs — **[workspace-navigation-mcp-cookbook.md](../design/workspace-navigation-mcp-cookbook.md)**.

This layer does **not** replace full Semantic Map UI or fix `ILayoutEngine` choice; it gives agent predictable JSON on current heuristics.

<a id="adr0039-closed-questions"></a>

## Closed questions (removed from open list)

Previously “open”; decision moved to ADR body or external ADRs.

1. **PFD vs MFD / second window / `presentation` vs `window`.** Closed: canon **`(PFD)(Forward)(MFD)`** and scan pattern for cockpit navigation; `presentation` has **no** `window` token but **`MfdHostWindow`** may **follow** topology and monitors ([0017](0017-multi-window-workspace-and-agent-surfaces.md)). See [§3](#adr0039-layout-canon), [summary](#adr0039-presentation-vs-toplevel).

2. **Semantic Map: abstraction level switch** (file / type / “feature”) **and where defaults live** (`settings.toml` vs `workspace.toml` vs session). Closed: switch is **panel presentation mode**; defaults and merge priority — [Semantic Map: node abstraction level](#adr0039-semantic-map-abstraction-layers). *(Which levels ship in **first delivery** — part of [open §1](#adr0039-open-questions) unless specified elsewhere.)*

3. **Semantic Map: what “numeric caps” mean** and **7±2** (Miller) heuristic. Closed as **definition and heuristic**: caps cut by node/edge count (and related limits: layout timeout, possibly traversal depth); above threshold — degrade to list or other fallback; 7±2 — glance orient, not hard coded N. See [§4](#adr0039-semantic-map-layout). **Concrete default numbers** — not closed, [open §3](#adr0039-open-questions).

4. **Semantic Map: data vs presets vs `presentation` — where “nodes” live (configuration layer, not DB).** Closed: **Roslyn-first** runtime truth; presets / `workspace.toml` not mandatory graph store; optional later annotations in `.cascade/`; `%LocalAppData%` cache — performance. See [Semantic Map: source of truth](#adr0039-semantic-map-data-layer).

5. **MCP `get_code_navigation_context`: stable link kind names, presets, predictable response.** Closed for **agent contract**: canonical strings (`partial_peer`, …), base presets in shipped **`CodeNavigation/presets.toml`**, overlay **`[[code_navigation.presets]]`** in `settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), merge by **`id`** with bundle and call args, **`kind_filter`** echo, in **`subgraph`** — **`kind`** on nodes and **`related_kind`** on edges. See [§ Agent/MCP](#adr0039-mcp-workspace-navigation). Deeper heuristics and new kinds — product iterations without breaking canon without major contract version.

<a id="adr0039-open-questions"></a>

## Open questions

1. Minimal view set for **first** Cascade navigation delivery (within **C#-first**). *(Partially directed by [v1 hybrid in §4](#adr0039-semantic-map-layout): related list + optional mini-map; final minimum and mandatory tabs/modes — TBD.)*

2. **“Related”** canon for **C#** (Roslyn): full data contract and panel UX in IDE; for **MCP**, link kind names and filter echo are fixed — [§ Agent/MCP](#adr0039-mcp-workspace-navigation). Other languages — **separate phase**, outside north-star.

3. **Semantic Map: default numeric thresholds** — concrete **node** and **edge** caps in mini-map subgraph, layout timeout, traversal depth if needed; **degrade-to-list** thresholds; **profiling** on very large solutions. *(Cap principle and 7±2 — [closed](#adr0039-closed-questions) §3; `ILayoutEngine` — [§4](#adr0039-semantic-map-layout). Data layer and presets — [closed](#adr0039-closed-questions) §4.)*

---

## Change history

<a id="adr0039-history"></a>

| Date | Change |
|------|--------|
| — | [Agent/MCP: `get_code_navigation_context`](#adr0039-mcp-workspace-navigation). |
| — | [open vs closed questions](#adr0039-open-questions); [canon `(PFD)(Forward)(MFD)`](#adr0039-layout-canon) (scan pattern; note: [`presentation` ≠ `window` token](#adr0039-presentation-vs-toplevel)); [Semantic Map: `ILayoutEngine`](#adr0039-semantic-map-layout). Also |
| 2026-04-11 | [Semantic Map: source of truth and configuration layer](#adr0039-semantic-map-data-layer) (fixed). |
| 2026-04-13 | expanded MCP contract: [named presets](#adr0039-mcp-workspace-navigation), [`kind_filter`](#adr0039-mcp-workspace-navigation), [subgraph: `kind` / `related_kind`](#adr0039-mcp-workspace-navigation); cookbook: [workspace-navigation-mcp-cookbook.md](../design/workspace-navigation-mcp-cookbook.md). |
| 2026-04-16 | [language scope](#adr0039-language-scope); [“Product metaphor”](#adr0039-product-metaphor). |

<!-- English translation of adr/0073-pfd-instrument-deck.md. Canonical Russian: ../../adr/0073-pfd-instrument-deck.md -->

# ADR 0073: PFD instrument deck — catalog of composition variants and surfaces (SA)

**Status:** Proposed  
**Date:** 2026-04-19

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD attention model |
| [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) | **strict PFD surface**, `[PfdStrict]` / `PfdStrictControl`; **not** synonym for entire PFD column geography |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **instrument deck** as named composition in one anchor; **ContentRepresentation** axis |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | instrument primitives / render palette |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs IDE presentation |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | `[instrument_routing]`, `pfd_primary` / … slots |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | `Instrument`, `CockpitInstrumentDescriptor` |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP / command parity |
| [0011](0011-debug-situational-awareness.md) | debug SA — related “awareness” axis |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | ADR map and PFD indicator — **candidate** for same deck |

## Summary

- Catalog of **PFD instrument deck** variants (SA, metrics, semantic map, ADR indicator…).
- Criteria for “on PFD vs on demand”; living draft until preset choice.

### Outside ADR

| Document | Role |
|----------|------|
| [§3](#adr0073-v3) | §3 |

**Purpose of this ADR:** a **working place** to iterate variants — **which** instruments and **in what mode** belong on **PFD** (primary scan, tactics), without mixing into MFD/palette without explicit decision. This **does not** duplicate [0063](0063-instrument-deck-named-composition-one-anchor.md) terminology; here — **subject-matter** candidate list and **open** forks.

---

## Context

**PFD** in [0021](0021-pfd-mfd-cockpit-attention-model.md) is **primary** attention (decision tree, navigation, tactical instruments). **Instrument deck** [0063](0063-instrument-deck-named-composition-one-anchor.md) describes composition *shape* (“several instruments on one screen”) but not *which* set is product-justified for PFD.

Separately: **situational awareness (SA)** — summaries of work context (code volume, complexity, knowledge map). Some signals already live **outside** PFD (e.g. LOC badge / Low·Medium·High in task cockpit; `[loc_limits]` in `workspace.toml`). **Code metrics** via MCP (`get_code_metrics`) returns JSON by scope — useful for agent scenarios, but **does not alone** answer “where to show the pilot.”

We need an **accumulating** document: **PFD instrument deck** variants and “here / on demand / not PFD” criteria.

---

<a id="adr0073-v1"></a>

## 1. Invariants (not re-litigated as “new axis” here)

- Slots and TOML merge — [0050](0050-declarative-instrument-zone-placement-toml.md); instrument descriptor — [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md).
- **EICAS** (W/C/A) and **LOC axis** (Low/Medium/High per `[loc_limits]`) are **different** semantics; do not mix colors/labels without legend (LOC badge is separate “file size” axis).

<a id="adr0073-v1b"></a>

### 1.1. Product direction: Strict and Glass Cockpit

**Ideal for PFD instrument area:** behavior closer to **read-only** and **strict** contract per [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) — components with **`[PfdStrict]`** / **`PfdStrictControl`**: input limits (**Input Lock**), weight (**Weight**), data channels; no heavy “office” interactivity inside marked instrument surface.

**Visual style:** **glass cockpit** — dark instrument field, luminous indicators, peripheral readability; primitive/palette details — [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md). Do not mix with shell chrome / **IDE presentation** overlays ([0066](0066-cockpit-ui-vs-ide-presentation-layer.md)).

**Nuance [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md):** **geographic** PFD zone **≠** “entire column read-only”. **Navigation** (solution tree, file pick, expand nodes) stays **interactive** — “where am I” context, not instrument strictness. Strict contract applies to **explicitly** marked indicators/tactical instruments, not everything drawn on the left.

<a id="adr0073-loc-composite"></a>

### 1.2. Example composite indicator: LOC (draft)

Target **LOC** display (non-empty lines, `[loc_limits]` thresholds) — **two channels in one instrument**, avionics metaphor (not mixed with EICAS W/C/A):

| Channel | Role | Image |
|---------|------|--------|
| **Zoned scale** | Where file sits vs **Low / Medium / High** | **Glide slope** (or local horizon): three segments, **position** marker (tooltip: size heuristic, not navigation course). |
| **Number** | Exact LOC without losing precision at zone edges | **Altimeter**: large digital/drum **readout** beside or inside widget. |

Practice: without **number** at `medium_min` / `high_min` user cannot see “how far” inside zone; without **scale** instant “green/yellow/red corridor” scan is lost. Primitive details — [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md).

---

<a id="adr0073-v2"></a>

## 2. Selection criteria (draft)

| Criterion | Question |
|-----------|----------|
| **Scan** | Fits in **1–2 s glance** without drill-down? |
| **Tactics** | About **current file/node/cursor** (vs whole-solution strategy)? |
| **Frequency** | Needed **constantly** on PFD or enough **on command** / MFD? |
| **Parity** | Should human and agent see **same** snapshot ([0008](0008-mcp-contracts-and-testable-infrastructure.md))? |

---

<a id="adr0073-v3"></a>

## 3. Variant catalog (to be filled)

Statuses: **idea** | **PFD candidate** | **likely not PFD** | **rejected / deferred**.

| # | Element | Essence | Draft verdict | Notes |
|---|---------|---------|---------------|-------|
| A | **Solution Explorer / tree** | project navigation | Already default PFD anchor | — |
| B | **Semantic Map (control flow)** | control flow in method | **PFD candidate** ([0053](0053-semantic-map-control-flow-pfd.md)) | tactics, cursor |
| C | **LOC / file size** | non-empty lines, L/M/H level | **Likely not separate PFD instrument v0** — already **badge** in task cockpit; PFD duplicate only with explicit “all SA on left edge” policy | `[loc_limits]`; target instrument — [§1.2](#adr0073-loc-composite): **BarWithLevels + marker** (glideslope) + **number** (altimeter). |
| D | **Code metrics (`get_code_metrics`)** | LOC, classes, methods, cyclomatic, `hot_methods` | **Likely on demand** (palette, MCP, optional compact panel) or **MFD** for scope=solution; **mini summary** on PFD only if “current file only” is fixed | Do not bloat PFD with full JSON |
| E | **ADR / knowledge indicator** | path → ADR map, intent | **Candidate** ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)) | documentation SA |
| F | **Git / status** | changed files | Often **MFD** or WH strip; on PFD if slot free and product chooses “git beside tree” | overlaps Git elsewhere |
| G | *(reserve row)* | — | — | add new rows at bottom |

**Editing rule:** new ideas — new letter or sub-item; do not rewrite history without note at bottom (date, what changed).

---

<a id="adr0073-v4"></a>

## 4. Open questions

1. Need **single** preset “PFD = navigation + one tactical instrument” vs “PFD = dense deck of N cells” ([0063](0063-instrument-deck-named-composition-one-anchor.md) § Page + deck)?
2. Should **code metrics** for **current file** duplicate **visually** on PFD when MCP already returns same numbers to agent?
3. **PFD vs Forward** boundary ([0021](0021-pfd-mfd-cockpit-attention-model.md)) for mini SA indicators — do not eat central editor.

---

## Decision

**Record as Proposed:** maintain **this ADR** as a **living catalog** of PFD deck variants and criteria; **do not** treat §3 table rows as accepted product norm until separate **Accepted** decision or code with reference here.

**Next step (outside this file):** as rows mature — move to **UiModes** / presets / `[instrument_routing]` ([0050](0050-declarative-instrument-zone-placement-toml.md)) citing §3 row number.

---

## Consequences

- Document may **change often** (§3 table); stable term definitions remain in [0063](0063-instrument-deck-named-composition-one-anchor.md) and [0021](0021-pfd-mfd-cockpit-attention-model.md).
- Implementing a specific PFD instrument row — separate commits and possibly narrow “how exactly” ADR (layout, CDS), without bloating **0073**.

---

## Change history (brief)

| Date | Change |
|------|--------|
| 2026-04-19 | Initial: context, §2 criteria, §3 starter table, §4 open questions. |
| 2026-04-19 | §1.1: **Strict** + **Glass Cockpit** direction; read-only column nuance per [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md). |
| 2026-04-19 | §1.2: LOC composite — zoned scale + marker (glideslope) + numeric readout (altimeter); row C §3. |

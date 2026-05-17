# ADR 0055: Skia instrument composition pipeline (Intent → Declutter → Layout → Render)

**Status:** Accepted  
**Date:** 2026-04-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → surface |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Instrument / slot |
| [0049](0049-skia-surface-rollout-over-avalonia-host.md) | Phased Skia rollout |
| [0053](0053-semantic-map-control-flow-pfd.md) | Semantic map, control flow |
| [0056](0056-semantic-map-pipeline-adoption.md) | First consumer — intent map |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | Render stage, deck primitives |

## Summary

- Shared Skia instrument pipeline: **Intent → Declutter → Layout → Render**.
- Common invariants for semantic map, deck, and future instruments.
- Consumers — [0056](0056-semantic-map-pipeline-adoption.md), [0057](0057-chat-surface-pipeline-adoption.md).

---
## Context

Skia instruments in CIDE (`workspace_navigation_map`, future instruments and overlays) are placed via the host surface compositor, but each instrument’s internal composition (data, declutter, geometry, render rules) was historically mixed into a single feature.

That produced two systemic effects:

1. Blurred responsibility boundary: “surface” and “instrument internals” get conflated in discussion and code.
2. Every new Skia feature risks reinventing its own mini-pipeline without shared invariants.
3. With `controlFlow` per [0053](0053-semantic-map-control-flow-pfd.md), readability on a small viewport degrades without a separate declutter layer.

---

## Decision

<a id="adr0055-p1"></a>
### 1) Introduce one pipeline for all Skia instruments

Each Skia instrument fixes an internal sequence:

1. **Intent**: build the instrument’s domain state model (source of truth for content).
2. **Declutter**: policy filtering/aggregation and prioritization before geometry.
3. **Layout**: geometric layout.
4. **Render**: draw the finished scene without business logic.

The surface compositor stays an outer layer and answers only “where the instrument is mounted” (slot/surface), not internal geometry/declutter.

<a id="adr0055-p2"></a>
### 2) Internal instrument compositor is mandatory

Each instrument introduces a dedicated compositor (e.g. `*Compositor`) that:

- selects a layout engine by domain detail level;
- applies viewport height/density policy;
- returns a finished scene and display parameters.

This is the canonical path for new Skia features so logic is not duplicated across VMs and controls.

<a id="adr0055-p3"></a>
### 3) Declutter — shared policy layer, not layout `if`s

Noise-reduction rules (density, repeat aggregation, main scenario over secondary detail — for graphs: main flow vs secondary edges) live in a separate policy layer with one interface across instruments.  
Layout must not decide “what to hide”, only “how to lay out what was already selected”.

<a id="adr0055-p4"></a>
### 4) Layout / Render: readability invariants (HF / cockpit guidance)

Below are **instrument-agnostic** rules for any Skia instrument with the §1 pipeline (chat timeline, scale, control-flow graph, schematic, etc.). Parenthetical examples illustrate a case; canonical control-flow detail — [0053](0053-semantic-map-control-flow-pfd.md). Cockpit attention model: [0021](0021-pfd-mfd-cockpit-attention-model.md), [0046](0046-presentation-layout-authority-and-cockpit-invariants.md).

1. **Attention hierarchy and draw order (Render).** The instrument defines a **fixed layer order** for the base scene: background/grid and “structural” primitives first, then main content, then labels and auxiliary markup within their zones. Attention signals from CDS/channels ([0036](0036-cds-channel-compositor-surface-pipeline.md)) — highlights, traces, health — draw **on top** of that base without reshaping domain geometry for an “alert”. Interactive focus (cursor, selection) is the top readable layer over static content. *(Example: graph — edges → nodes → labels in the band; chat — branch background → bubbles → timestamps.)*

2. **Noise budget: Declutter chooses *what*, Layout limits *how much fits*.** Declutter sets visible entities and aggregation; Layout applies **density and geometric ceilings** for the viewport (minimum gaps between blocks, row/column limits, reserves for auxiliary columns). Do not compensate overload with scale alone when policy already signals “too much” — reduce volume or detail level first. *(Example: graph — vertical step and band width; chat — visible message count and preview height.)*

3. **Detail levels (Glance / Normal / Inspect) — primarily Declutter, not geometry alone.** Domain detail level (including shared `glance` / `normal` / `inspect` from the adoption plan) sets **selection and aggregation before Layout**. Layout changes layout metrics but does not replace the decision “how many entities between Glance and Inspect”. *(Example: `CodeNavigationMapDetailLevel` for the map; another instrument — its own enum/scale, same role split.)*

4. **Readability floors in Glance (Render / theme).** When compressing the viewport, keep **lower bounds** on readability: glyph size, line weight, outline contrast — so “glance from the side” does not become indistinguishable noise. Numbers come from theme/instrument constants; invariant — **do not sacrifice distinguishability to fit the rect**. *(Example: minimum link thickness and node outlines; minimum list row height.)*

5. **Stable spatial roles (Layout).** Labels, legends, scales, and other auxiliary graphics **anchor to the main content zone** (adjacent area, fixed offset from the “data band”), not arbitrary viewport corners — lower cost to match label to object. *(Example: legend beside the graph band [0053](0053-semantic-map-control-flow-pfd.md); axis label at the edge of the same region as the data.)*

6. **Alerting on top of the base.** Channel compositors (TraceFlow, health, etc.) add an **overlay** to the instrument’s assembled base scene: they do not duplicate Intent/Layout of the domain model, only show attention/trace/status state on top — for any base instrument type.

**Contract checks (pipeline tests):** on a small viewport — no **instrument-forbidden** layout-primitive overlaps (collisions, unreadable stacking) at the declared detail level; when moving to a deeper level (e.g. Glance → Inspect) useful information **must not shrink** without explicit policy degradation (empty state, data error). What counts as a collision — in each instrument’s contract.

---

## Consequences

### Pros

- Clear architectural boundary: host surface vs instrument internals.
- One contract for all future Skia features and instruments.
- Declutter/layout/render can evolve independently without regressions in slot composition.

### Cons

- More entities and contracts in the Skia instrument layer.
- Separate pipeline-level tests needed (not only layout unit tests).

---

## Non-goals

- Do not change ADR [0036](0036-cds-channel-compositor-surface-pipeline.md) and [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md): external surface composition stays as is.
- Do not fix here the final UI style of all edge cases and all instruments (that evolves within pipeline and policy).

---

## Adoption plan (minimum)

1. Fix the base contract for the shared Skia instrument pipeline.
2. Canonize `CodeNavigationMapCompositor` as the first adapter of this contract.
3. Extract a shared `DeclutterPolicy` interface (minimum: `glance`, `normal`, `inspect`) and instrument-specific implementations.
4. Fix pipeline contract tests:  
   - stable scene composition for the same input;  
   - readability on a small viewport (no critical collisions/stacking per instrument contract);  
   - correct degradation on incomplete input (no false content).

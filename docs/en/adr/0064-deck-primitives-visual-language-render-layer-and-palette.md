<!-- English translation of adr/0064-deck-primitives-visual-language-render-layer-and-palette.md. Canonical Russian: ../../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md -->

# ADR 0064: Deck indicator kinds — visual language, render layer, and semantic palette

**Status:** Accepted  
**Date:** 2026-04-19

## Related ADRs

| ADR | Role |
|-----|------|
| [0063 § kinds](0063-instrument-deck-named-composition-one-anchor.md#adr0063-indicator-kinds) | `DeckPrimitiveKind`, Presence / Dark Cockpit |
| [0055](0055-skia-instrument-composition-pipeline.md) | **Render** pipeline stage |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Dark Cockpit |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | layout invariants |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | cockpit palette vs IDE chrome |

## Summary

- **Deck indicator kinds:** unified rendering + semantic palette.
- `DeckPrimitiveKind` — catalog of kinds; no extra architectural layer.

**Code:** `Cockpit/PrimitivesKit/` — annunciator, lamps, semantic map tokens.

---

## Context

[0063 § indicator kinds](0063-instrument-deck-named-composition-one-anchor.md#adr0063-indicator-kinds) fixes the **product taxonomy** of **indicator kinds**: *Lamp*, *Bar*, *Sign*, *Readout*, etc. — **signal shape** in a deck cell or Skia instrument fragment.

In parallel, code and conversation mixed meanings (formerly tied to “primitive”):

1. **Indicator kind** — what to show the operator: “lamp of this class”, “deviation bar”, “short label”.
2. **Low-level graphics ops** — line, fill, `FormattedText` in `DrawingContext` / Skia commands.
3. **Scene / metric tokens** — viewport, grid step, padding of a **whole** instrument (e.g. semantic map); **not** a `DeckPrimitiveKind` row, but shared layout constants.

Without explicit separation, features **duplicate colors and geometry** in XAML, VM, and render code: attention goes to **competing shades and “Christmas tree” UI**, not task and state hierarchy ([0021 §6](0021-pfd-mfd-cockpit-attention-model.md)). We need a norm: **one visual language per indicator kind** and **one render library** that implements it.

---

## Decision

<a id="adr0064-p1"></a>

### 1) Indicator kinds are high-level with unified graphics

An **indicator kind** per [0063](0063-instrument-deck-named-composition-one-anchor.md) is an **atomic glance**: UI data is *which kind* (`DeckPrimitiveKind` and state profile) plus **minimal payload** (legend text, severity, readout number, etc.), not “draw rectangle #rrggbb”.

**Invariant:** for each **indicator kind + state semantics** (e.g. *Lamp* + ok / caution / unavailable) the product has **exactly one consistent graphic embodiment** in the cockpit (shape, border weight, label typography, **semantic color** — see [§3](#adr0064-p3)). Arbitrary per-screen styling is **not allowed**: the operator spends attention on style, not state.

Features and deck compositors pass **kind semantics** into the render library, not low-level graphics op lists.

<a id="adr0064-p2"></a>

### 2) Render library for indicator kinds (shared with [0055](0055-skia-instrument-composition-pipeline.md))

To uphold [§1](#adr0064-p1) in code, use a **shared render library** (repo: `Cockpit/PrimitivesKit/`, **Render** stage per [0055](0055-skia-instrument-composition-pipeline.md)):

- per supported **kind** (or narrow family, e.g. annunciator **Lamp**) — **one** “how to draw” implementation: functions/types taking `DrawingContext` (and bounds if needed), no duplicated logic in controls and instruments;
- for Skia pipeline, same contract as **draw instructions** with the same semantics, without copy-paste colors/metrics from Avalonia XAML.

**Boundary:** render library **does not** replace instrument compositor (Intent → Declutter → Layout) or business logic; it **only** implements agreed **appearance** from semantic inputs.

**Folder name `PrimitivesKit`** is collective; see [lexicon](#lexicon-canonical-product--code) and [§4](#adr0064-p4) for whole-scene metrics.

<a id="adr0064-p3"></a>

### 3) Unified color language (semantic palette)

**Color in the cockpit** for deck indicators and instruments reads as **meaning** (readiness level, deviation severity, informational), not decoration.

**Invariants:**

- palette defined by **semantic roles** (e.g. nominal, attention, info, unavailable), not arbitrary hex per feature;
- aligned with **Dark Cockpit** ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0063 § Presence](0063-instrument-deck-named-composition-one-anchor.md#adr0063-presence-dark-cockpit)): in nominal state **do not** multiply bright pixels and animation “for beauty”;
- for **a11y** (not color alone) — extensions of the same semantics (icon, hatch), not a second “palette” without rules.

Concrete token values may live in theme/resources, but **role → display** mapping stays **single** and aligned with [§2](#adr0064-p2). EICAS W/C/A vs everyday Error/Warning/Information and `AnnunciatorLampLevel` — table in [0021 §5](0021-pfd-mfd-cockpit-attention-model.md).

<a id="adr0064-p4"></a>

### 4) Indicator kind vs whole-scene tokens

**Indicator kind** and boundary with instruments — [0063 § primitive vs instrument](0063-instrument-deck-named-composition-one-anchor.md#adr0063-primitive-vs-instrument) (section title uses historical “primitive”; meaning is **indicator kind** vs **instrument**).

**Layout / composition metrics** (instrument viewport, graph level step, scene padding) are **not** deck indicator kinds; code may use **layout tokens**, **instrument geometry** to avoid confusion with `DeckPrimitiveKind`.

---

## Non-goals (current phase)

- Full public render API contract for third-party plugins ([0063 open questions](0063-instrument-deck-named-composition-one-anchor.md#adr0063-open-questions)).
- Entire cockpit palette in one TOML before theme stabilizes.
- No exceptions: local experiments behind flags are allowed but must not define a second “canon” without ADR review.

---

## Consequences

- Product discussion and review use: **indicator kind** → **unified render** ([§2](#adr0064-p2)) → composition in instrument/deck — without an extra intermediate “layer” in architecture.
- Duplicated lamp/readout colors/geometry is **technical debt** until moved to [§2](#adr0064-p2) and [§3](#adr0064-p3) palette.
- Prefer **indicator kind** / **signal shape** in text; “primitive” only when citing `DeckPrimitiveKind` or legacy [0063](0063-instrument-deck-named-composition-one-anchor.md) section titles.

---

## Alternatives (brief)

| Option | Downside |
|--------|----------|
| Markdown guidelines only, no ADR | No stable reference in architecture policy |
| One huge `theme.json` without render layer | Semantics spread across bindings; palette drifts |
| Forbid any numbers outside PrimitivesKit | Too rigid for prototypes; [§4](#adr0064-p4) separation is enough |

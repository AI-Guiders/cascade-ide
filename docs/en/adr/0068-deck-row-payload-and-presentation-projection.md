<!-- English translation of adr/0068-deck-row-payload-and-presentation-projection.md. Canonical Russian: ../../adr/0068-deck-row-payload-and-presentation-projection.md -->

#ADR 0068: Channel line payload and surface projection (layout vs cell content)

**Status:** Accepted  
**Date:** 2026-04-19

## Related ADRs

| ADR/document | Role |
|----------------|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Channel vs slot; glossary presentation |
| [0021 § glossary](0021-pfd-mfd-cockpit-attention-model.md#glossary-presentation-vs-channel) | Presentation slot vs line projection (**not to be confused**) |
| [0023](0023-environment-readiness-glance.md) | Readiness vs IDE Health Channel |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → composer → UI |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | ContentRepresentation vs instrument deck |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | Lamp primitives, rendering |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs Chrome IDE |
| [`environment-readiness-glance-v1.md`](../../design/environment-readiness-glance-v1.md) | Drawing readiness |

**Not to be confused:** "view projection" here is how the **shot line** becomes a lamp/glyph; this is a level **lower** than the region slot in [0021](0021-pfd-mfd-cockpit-attention-model.md) and is not a replacement for **ContentRepresentation** in [0063](0063-instrument-deck-named-composition-one-anchor.md).

---
## Context

As the product evolves, the same **logical channel snapshot** (for example, environment readiness) is shown in **several ways**: a compact strip of lamps, a list of cards, a “table” with columns, possible future modes (density, badges only, separate MFD block).

In conversation and in code it is easy to **merge**:

1. **Layout** - where the lines are on the screen (grid, stripe, columns of the “Component / Details” header, narrow vs wide mode).
2. **Cell contents** - what exactly the user sees in the row position: lamp primitive, text glyph, link, button, mixed block.
3. **Domain string of the image** - stable id, signal level, title, detail, short signature on the lens, etc.

As long as all the rows are **uniform** (one record type, for example `AnnunciatorLampItem`), merging doesn't hurt. When **different kinds of rows** appear (cell action, rich text, secondary indicator), without named separation, refactoring turns into `DataTemplate` proliferation and duplication of stripe and table.

You need a **stable dictionary of layers**, not necessarily a separate assembly of types for each screen from day one.

---

## Solution

<a id="adr0068-p1"></a>

**1. Three meaningful layers (terms for ADR, review and code):**

| Layer | Question | Note |
|-----------|--------|-----------|
| **row payload** | What does **channel** claim about the world in this line? | Domain snapshot: identity, status, texts, future line appearance options. Built in `Services/` / at the data source, **without** Avalonia. |
| **Slot identity** | Where does the line **stand** in the ordered set and how do tests and presets refer to it? | Stable cell ids, order in deck (e.g. `EnvironmentReadinessInstrumentDeck.OrderedCellIds`), connection with the readiness channel ([0023](0023-environment-readiness-glance.md)). Orthogonal to **how** the cell is drawn. |
| **presentation projection** | How does a **specific surface** display the **same** payload: lens in a stripe, same lens in the first column of a table, placeholder text glyph, interactive in chrome IDE? | Selecting a primitive and template; may reuse `AnnunciatorLampMetrics` / `LabeledAnnunciatorLampFace` ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)); interactive and “not a device” - according to [0066](0066-cockpit-ui-vs-ide-presentation-layer.md). |

**Invariant:** changing the **layout** (strip ↔ table ↔ cards) **does not** change the **payload** contract; Only the projection and composer/View change. Change of **string semantics** (new type of content) - payload extension and/or string type discriminator, then separate projections by type.

<a id="adr0068-p2"></a>
**2. Connection with the axes [0063](0063-instrument-deck-named-composition-one-anchor.md):** the **ContentRepresentation** axis (Strip / Page / ...) specifies the **container shape** of the region; **instrument deck** — **what and in what order** in this container. **The view projection** in this ADR is the level **below**: *within* the selected shape, how **each line of the shot** turns into pixels/controls. The three layers above **do not replace** ContentRepresentation and deck; they clarify **where not to mix** channel data and options for drawing a single line.

<a id="adr0068-p3"></a>

**3. Pragmatic v1:** It is acceptable to store a snapshot in **one** row type (`AnnunciatorLampItem` and similar) as long as all rows are **uniform**. This does not negate the distinction between layers in the head and in the review: the strip and the table are **two projections** of the same payload, and not two different “channels”.

<a id="adr0068-p4"></a>

**4. When cell heterogeneity appears:** introduce explicit discrimination in the **payload model** (discriminated union, multiple record types, template key) and compare **projections** by row type; Do not multiply incompatible snapshots of the same channel without reason.

<a id="adr0068-p5"></a>

**5. Duplicating the same projection** (for example, a strip of lamps on top and again lamps in the first column of the table) is a **product** choice: deliberate duplication of glance vs detail or removal of redundancy. Architecturally, this is a decision at the level of **which projections are included in the mode**, and not mixing payload with layout.

---

## Consequences

- New channel screens with deck metaphor **explicitly share** image assembly, stable id/order and projection selection in View/composer.
- The "lamp in table cell instead of glyph" refactoring touches **projection** and primitive reuse ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)), and does not have to change `EnvironmentReadinessSnapshotBuilder` if the semantics of the string did not change.
- Tests: **payload** contract and id order - separate from visual **projection** tests (if they appear).

## Open

- Names of types/interfaces in the code (`IRowPayload`, `PresentationProjection`, etc.) - enter **as** the second heterogeneous channel or repeating pattern appears; this ADR specifies **terminology**, an optional entity in the repository on the date of acceptance.
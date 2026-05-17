<!-- English translation of adr/0088-host-slot-region-deck-cell-taxonomy.md. Canonical Russian: ../../adr/0088-host-slot-region-deck-cell-taxonomy.md -->

# ADR 0088: Host slot, attention region and deck cell - taxonomy (do not mix)

**Status:** Proposed  
**Date:** 2026-04-22  

## Related ADRs

| ADR | Role |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | zone **Mfd** vs **pages** `MfdShellView` |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD anchors, geography vs telemetry |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | `CockpitInstrumentDescriptor`, `slot_id` |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | `[instrument_routing]`, `pfd_primary` / `mfd_primary` |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **instrument deck**, `InstrumentDeckDescriptor`, `SemanticAnchorId` |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | row payload vs projection vs slot |
| [0073](0073-pfd-instrument-deck.md) | PFD Candidate Directory |
## Summary

- Taxonomy **host slot / region / deck cell** - do not mix levels.
- Contact with [0068](0068-deck-row-payload-and-presentation-projection.md).


**Summary:** in discussions **slot** was called both **the entire PFD column** and **small rectangle** in the instrument grid. This ADR captures the **dilution of levels**: what is a **host slot** in the host wire, what is a **region/attention anchor**, what is an **instrument deck cell** - and **what not to mix**. A **multi-tool** implementation on a PFD (surface deck composer) **is not** required to change the meaning of `CockpitSlotIds.Pfd` without separate agreement (see §4).

---

## 1. Context: where the confusion comes from

1. In the code **`CockpitSlotIds.Pfd` / `Mfd`** are identifiers for **`MainWindowHostSurfaceCompositor`**: **no more than one** `CockpitInstrumentDescriptor` per **each** such identifier is included in the frame; **geographically** this is **the entire** visible PFD (or MFD) column in the `DockedGrid`, not the "single tile" within it.
2. In product speech, **PFD** is **zone of primary attention** (whole column, separate P+M window, etc.).
3. **Instrument deck** [0063] introduces a **named composition** "multiple entities **in one anchor**" with **`SemanticAnchorId`** = for example `pfd` - the same word as `slot_id`, but **different level of abstraction** (`InstrumentDeckDescriptor` vs host frame).
4. In [0068](0068-deck-row-payload-and-presentation-projection.md) **table row/lane** and **cell type** are already separated; All that remains is to explicitly associate this with the **host** slot [0047].

Without fixed names, the question again arises: *“Is a slot the entire PFD or a cell in the deck?”*

---

## 2. Solution: canonical levels

Below are **recommended** terms in documentation, ADR, review; in the code, the identifiers (`CockpitSlotIds`, TOML fields) **do not** have to** match these Russian/English names verbatim, but **the meaning** must be mapped.

**Rough “staircase” from top to bottom (different branches):** *window topology / presentation* ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) → *attention region* (PFD, MFD) → **shell page** (`MfdShellPage`, §2.0) - for the secondary circuit; *in parallel* on the PFD side - **host slot** (§2.1) and then deck / internal blocks of the device. **Page** sits **above** the route “what is the cabin `instrument_id` in `pfd`”, otherwise **chat** would be confused with **instrument**.

<a id="adr0088-shell-page"></a>

### 2.0. Shell Page (Page, `MfdShellPage`)

- **Definition:** **navigation inside `MfdShellView`** - which **entire** mode occupies the **column** of the secondary outline: Chat, Terminal, Solution Explorer, Build, ... (`Models/MfdShellPage.cs`, command `set_mfd_shell_page` in [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)).
- **Level:** **above** §2.1–2.4: this is **not** `CockpitSlotIds`, **not** `CockpitInstrumentDescriptor`, **not** instrument deck cell. Changing the page is **orthogonal** to selecting a **PFD** device in the host frame.
- **Connection with [0017](0017-multi-window-workspace-and-agent-surfaces.md):** *Mfd focus area* (column/window) **≠** *page*; chat is **page**, not “another area” next to the PFD.
- **PFD branch:** in v1 there is no **analog** `MfdShellPage` for the PFD column as a **switchable set of unrelated UIs**: there is basically **one** mounted cockpit device (§2.1), its internal mosaic is §2.4. For Pfd, call navigation **layout**, not *page stack*: contract `IPfdLayout` + `PfdLayouts` in `Models/Shell/`. For Mfd - `IShellPage` / `IMfdShellPage` + `MfdShellPageDescriptor` (the same `MfdShellPage` in VM).

<a id="adr0088-level-1"></a>
### 2.1. Host slot (host slot, `slot_id` in `CockpitInstrumentDescriptor`)

- **Definition:** unit of **instrument routing in the host frame** of the main/sub window: *where* the composer places the **relevant** `instrument_id` to be displayed in **a given** **geometric** column zone.
- **Today (v1):** one `instrument_id` per host slot; example: `CockpitSlotIds.Pfd` + `CockpitStandardInstrumentIds.WorkspaceNavigationMap`.
- **Map:** `MainWindowHostSurfaceCompositor.BuildInstruments` → list `CockpitInstrumentDescriptor(InstrumentId, SlotId)`.
- **Not to be confused with:** small glyph, row in WH table, sub-slot inside Avalonia markup of one `UserControl`.

**Invariant v1:** host slot **does not** split into multiple `slot_ids` **in the same** frame model unless [0047] is expanded explicitly (see §4).

<a id="adr0088-level-2"></a>

### 2.2. Region/attention anchor (PFD, MFD, …)

- **Definition:** **semantic zone of the cockpit** by [0021] - *why* this part of the screen (primary scan, secondary contour, ...).
- **Relation to host slot:** for primary/docked columns **geography** “PFD column” coincides with host slot `pfd` **as screen area**; The **anchor** explains the *role* of the zone, the **host slot** is the *attachment point* in the frame DTO and the MCP narrative of “mount tool X in Y”.

<a id="adr0088-level-3"></a>

### 2.3. Instrument deck and deck cell (deck cell)

- **Definition [0063]:** **named** composition: an ordered set of **cells** (grid, stack, stripe) under **`SemanticAnchorId`**.
- **Deck cell:** **one** position in the layout: stable **`cell_id`** (like `EnvironmentReadinessInstrumentDeck.OrderedCellIds`, `IdeHealthInstrumentDeck.OrderedSegmentIds` for the **channel** IDE Health - analogue in **role**).
- **Critical:** the deck cell **is not** a `CockpitSlotIds` **in the current** contract [0047] unless an explicit **mapping** "cell → host slot / nested route" is entered.
- **Purpose:** **several** indicators/tools **within** the geography of one PFD region (for product decision) - through **another** layer: either a **composite** `UserControl` (internal layout = not a deck in the host sense), or a future **deck host** / **PFD surface deck compositor** (see §3).

<a id="adr0088-instrument-internal-blocks"></a>

### 2.4. The entire device and internal units (device cells)

Orthogonal to §2.3: **one** `instrument_id` in the host slot (for example, an intent map in Pfd) **itself** can be **composite** - the pilot perceives **one** instrument, and inside there are **several blocks** (cells), which contain **specific** indicators, subgraphs, legend, chrome, signatures ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) - types of primitives).

- **Internal cell / block** - unit of layout **inside** of one cabin instrument; stable id and composition policy are set **by the composer of this device** (for the mini-map - intent map pipeline, see [0055](0055-skia-instrument-composition-pipeline.md), [0056](0056-semantic-map-pipeline-adoption.md)), **not** to be confused with `cell_id` **instrument deck** at the region level (§2.3).
- **Meaning for UX:** “device = composition of blocks” - then, for example, a **graph** and a **legend** for it are **two** such blocks with explicit layout (reserve for a legend, independent scaling, etc. for the product), and not one indistinguishable mess.
- **Implementation (PFD v1 map, Intent Map product):** graph/legend partitioning - `CodeNavigationMapInstrumentBlockCompositor` + `CodeNavigationMapInstrumentBlockDescriptor` / `CodeNavigationMapInstrumentBlockIds` in `Services/CodeNavigation/` (stable ids `code_navigation.pfd_instrument.block.*`); alternative composer with id `code_navigation.workspace_instrument.block.*` - `CodeNavigationMapWorkspaceInstrumentBlockCompositor` in `Services/Navigation/`. The result of the pipeline is `CodeNavigationMapCompositionResult.CodeNavigationMapInstrumentBlocks` (after `CodeNavigationMapLayoutStage` in `CodeNavigationMapCompositor`). The drawing is uniform (`CodeNavigationMapSceneDrawing`), rectangles are consistent with `LegendColumnLeft` / `LegendBlockTopY`.

**Invariant of terms:** *deck* on PFD = **deck of several (cockpit) instruments** in the region; *block* = **part of one** device. When talking about “cell”, specify: *deck cell* (region) vs *internal cell of the device*.

---

## 3. Direction of implementation (without fixing a deadline)
1. **Toolbox Composer** on **one** PFD geometry: takes `InstrumentDeckDescriptor` (or PFD-specific variant) and **projects** `instrument_id` **to cells**; An **external** host frame can still contain **one** descriptor on `pfd` with `instrument_id` = *deck host*, or - when extending the model - otherwise; selecting a **separate** iteration after the prototype.
2. **CDS / MCP / snapshot:** what is considered an “atom” in a frame (one host instrument vs N internal ones) - **contract** boundary; update [0008] / `cds-contract` **together** with implementation, do not degrade "one `instrument_id` in `pfd`" undetectably.

---

## 4. Rule of distinction: “what is called a slot in a conversation”

| Question | Canonical answer (this ADR) |
|--------|------------------------------|
| “Is the slot the entire PFD?” | **Host slot** `pfd` **in the sense of [0047] / host frame** - **yes,** this is a **route to the column** (one selected tool for this area in v1). |
| “A slot is a small cell in the instrument grid?” | **No** for `CockpitSlotIds`; name **cell deck** / **`cell_id`**. |
| "Is `SemanticAnchorId` = `pfd` in `InstrumentDeckDescriptor` the same as `slot_id`?** | **Same attention anchor/region**, **not** a duplicate of a **separate** host slot in the DTO, until the deck is **raised** into the frame as a separate layer; avoid "two `pfd` in one frame" in speech without qualification. |

---

## 5. Consequences

- Documentation, new ADRs, review: use **host slot** vs **deck cell** vs **PFD region** according to table §4.
- [0073](0073-pfd-instrument-deck.md) (PFD catalogue) and [0063](0063-instrument-deck-named-composition-one-anchor.md) remain: this ADR **clarifies** the terms, **does not** replace them.
- Code: **immediate** change of `CockpitSlotIds` names **not** required; when a PFD multi-instrument appears - either an **internal** deck without a new `slot_id`, or an explicit **extension** 0047 + entry in the MCP changelog.

---

## 6. Rejected alternatives

- **Read "slot" micro-cell only** - breaks the current MCP language/"mount in `pfd`" and [0047] without migration.
- **Rename `CockpitSlotIds.Pfd` to `PfdColumn`** and enter `PfdCell_0`... - possible in the future, but **high** price; until consensus we use §2–4.
- **Mix** WH line and host slot - already covered in [0068]; we don't repeat.

---

## 7. Open questions (do not block Proposed)

- Exact wire: one `instrument_id` “PFD deck host” vs an array of descriptors **in** `MainWindowHostSurfaceFrame` - **based on** PoC of the composer.
- Stable `cell_id` for PFD-desk vs TOML presets - evolution of [0050] / [0063 § deck in presets].

---

<a id="adr0088-glossary"></a>

## Brief glossary (for copy-paste)

| Term | Meaning |
|--------|--------|
| **Page** | `MfdShellPage` - **what** content of the **secondary circuit** in the Mfd column (chat, terminal, ...); **above** host slot [0047]; [0017](0017-multi-window-workspace-and-agent-surfaces.md). |
| **Host slot** | `slot_id` in `CockpitInstrumentDescriptor` / `CockpitSlotIds` - **mount point in host frame**; v1: **entire** PFD (or MFD) column, one selected `instrument_id`. |
| **Region / Attention Anchor** | PFD, MFD - **cockpit area** [0021]; Geography is often 1:1 with host slot, semantics is “zone role”. |
| **Instrument deck** | Named composition **within** anchor; `SemanticAnchorId` = region. |
| **Deck cell (cell)** | Position in the **regional** deck; **not** `CockpitSlotIds` in v1. |
| **Block / internal cell of the device** | Unit **within** one `instrument_id` (graph, legend, indicator bar); composer of the **device**, not the host deck. |
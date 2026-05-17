<!-- English translation of 0063-instrument-deck-named-composition-one-anchor.md. Canonical Russian: ../../adr/0063-instrument-deck-named-composition-one-anchor.md -->

# ADR 0063: Instrument Deck — Named Composition of Instruments in a Single Field of Attention

**Status:** Accepted  
**Date:** 2026-04-17  
**Updated:** 2026-04-24 — Section deck on MFD page (composition within the Mfd page). Details: [§ History](#adr0063-history).## Related ADRs

| ADR / Document | Role |
|----------------|------|
| [0021](../../../design/adr/0021-pfd-mfd-cockpit-attention-model.md) | Anchors; composition **inside** the region |
| [0021 § zone](../../../design/adr/0021-pfd-mfd-cockpit-attention-model.md#anchor-pfd-mfd-content-vs-telemetry-page) | Difference in zone terms |
| [0050](../../../design/adr/0050-declarative-instrument-zone-placement-toml.md) | `[instrument_routing]`, slots |
| [0046](../../../design/adr/0046-presentation-layout-authority-and-cockpit-invariants.md) | Layout policy P/F/M |
| [0047](../../../design/adr/0047-cockpit-instrument-descriptor-and-slot-composition.md) | `Instrument`, slots |
| [0010](../../../design/adr/0010-ui-modes-toml-configuration.md) | UiModes, presets |
| [0064](../../../design/adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) | Indicator types, `PrimitivesKit` |
| [0068](../../../design/adr/0068-deck-row-payload-and-presentation-projection.md) | Row payload vs presentation projection |
| [`workspace-health-implementation-map-v1.md`](../../../../design/workspace-health-implementation-map-v1.md) | IDE Health deck diagram |## Summary

- **Instrument Deck** — a named composition around **one anchor** (code fragment / method).
- **`ContentRepresentation`**, a taxonomy of primitives (Readout, Trend, Gauge, Presence…).
- **Presence/Activity** vs **Dark Cockpit**; `DedicatedPage` — a page mode of the MFD, not a deck.
- Deck **inside** an MFD page — orthogonal to navigation between pages.## Context

In the discussion about the cockpit, there **used to be two axes**:

1. **Form of Presentation** — *how* to show content in markup: strip (**Strip**), block "page" region (**Page**), compact indicator, etc.
2. **Composition Unit** — *what* and *in what order*: set of tools / channel segments / indicators (tabs, stack, split, order in the composer).

In the current product, **`DedicatedPage`** / **`bottom_strip`** in the preset of the IDE Health channel (`ide_health_surface` → `IdeHealthUiSurface.DedicatedPage` / `BottomStrip`) belongs to **axis 1** (form of presenting the same logical IDE Health channel), not to a "named tool deck" of an overall anchor. The name **`DedicatedPage`** is channel-specific; abstractly, it represents the mode **`ContentRepresentation.Page`** for IDE Health ([§ "Two Axes"](#adr0063-two-axes) below).

Separately: in discussions about the cockpit, it often feels like you want to say **"page"** in the sense of axis **2** — *an ordered set of tools and layout in one place*. This intersects with the word **Page** on axis **1** → different names are needed ([0021](0021-pfd-mfd-cockpit-attention-model.md#anchor-pfd-mfd-content-vs-telemetry-page)).

Explicit terms are needed to prevent confusion:
- **form of presentation** of a channel (Strip / Page / …),
- **preset** UiMode (the whole picture of windows and anchors),
- **named tool composition** in one anchor — **instrument deck** (axis 2 for tools; for IDE Health, order of segments — channel composer, orthogonal to Strip/Page).

<a id="adr0063-two-axes"></a>

## Two orthogonal axes: representation vs composition

The meaning of **deck** here: do not duplicate the name **Page** on axis A (the form of a container in a region), but call axis B — **“what entities and in what order/arrangement”** in this container: tools, channel segments, grid cells. On the IDE Health channel on axis B is the order of segments in the `IdeHealthSurfaceCompositor`, **independent** from Strip/Page.

| | **Axis A — form representation** | **Axis B — composition** |
|---|-------------------------------|-------------------------|
| **Question** | *What template/container* to show content in a region (panel vs block "page" of the region …) | *What* on this container and *in what arrangement*: several tools on one screen, channel segments, tabs/stack, grid … |
| **Canonical name (this ADR)** | List of `ContentRepresentation`: minimum `Strip`, `Page`. Additional values (e.g. `Indicator`) — as needed by the product, not fixing a full list in Proposed. | For instruments in the anchor — **`instrument deck`** (below). For the IDE Health channel — order of segments in `IdeHealthSurfaceCompositor`, **independent** from Strip/Page ([drawing](../../design/workspace-health-implementation-map-v1.md)). |
| **IDE Health today** | TOML `ide_health_surface`: `bottom_strip` ↔ **`Strip`**, `dedicated_page` ↔ **`Page`**. In code — `IdeHealthUiSurface.BottomStrip` / `DedicatedPage` (**not** a synonym for "any MFD page"). | `IdeHealthSurfaceCompositor`: Build → Tests → Debug → Git. |

**Invariant:** Changing `ContentRepresentation` for IDE Health **does not** change the snapshot `IdeHealthInputSnapshot` and segment composition logic — only choosing **which View** (panel vs secondary page) to bind to the same `IdeHealthSegments` ([0021](0021-pfd-mfd-cockpit-attention-model.md#glossary-presentation-vs-channel), [drawing](../../design/workspace-health-implementation-map-v1.md)).

<a id="adr0063-page-plus-deck-pfd"></a>

**`ContentRepresentation.Page` and instrument deck — together:** typical case — **one region screen** (e.g. zone **PFD**), where **several tools** are visible at the same time in an arrangement — as on a real PFD: altimeter, speed pointer, bank angle and so on on **one** instrument panel. Here `Page` (axis A) sets **the form**: content of the channel/anchor is shown as a **region page block**, not a narrow strip (`Strip`). `Instrument deck` (axis B) sets **this very arrangement**: **which tools** and **how** they are arranged (grid, column fractions, cell priorities). Axes **do not mix**, but **stack**: "region page" without a deck would be an empty container; a deck without form selection does not define whether it is a strip or a full block.

**Narrow case of IDE Health:** for **one channel** IDE Health `DedicatedPage` — this is **`ContentRepresentation.Page`** only for **IDE Health data** (the same segments, different view), not necessarily "the entire PFD as an aviation instrument board"; the deck **of the entire PFD anchor** in the product can include **both** IDE Health and other tools — see [0021](0021-pfd-mfd-cockpit-attention-model.md#anchor-pfd-mfd-content-vs-telemetry-page).

**Tabs / stack** — **a special case** of deck when instruments are not shown simultaneously, but switched; for the image "one screen — many instruments" it is more important to have a **common grid on `Page`**.

<a id="adr0063-content-representation-code"></a>

**ContentRepresentation and code (direction; question closed):** canonical **name** of the axis **form representation** of content in the region — **`ContentRepresentation`** (`Strip`, `Page`, …). For the IDE Health channel this is expressed today by **`IdeHealthUiSurface`** and TOML `ide_health_surface` — **not a separate "channel axis"**, but choosing **layout template** (strip vs page block) for **IDE Health data**; correspondence of `Strip/Page` — in the table above.

When for **another data stream** (another channel in the sense of [0021](0021-pfd-mfd-cockpit-attention-model.md)) it will be necessary to have the same choice "strip or region page", the same **axis `ContentRepresentation`** is reused. Channels set **what** to show; **form** (Strip/Page) — **a general axis of representation**, they do not mix with "channel axes".

Code refactoring: introduce enum `ContentRepresentation` and map it to `IdeHealthUiSurface` (or rename the type over time) — **implementation step**, non-architectural choice after this ADR.

<a id="adr0063-deck-presets-evolution"></a>

**Deck in user presets (direction; evolutionarily closed):** for the current horizon, `deck` remains a **concept and internal drawing of the compositor (and code name), not an optional entity** in the user's TOML. **Declarative** description of named decks in `.cascade` / `settings` is **put off**: this drags contract merge, validation, versioning, and UI until the composition slots themselves are stabilized and it becomes clear whether a user needs to "save the deck."

Allow a **local experiment** (e.g. only in your repo or behind a flag), **without** product promise and without expanding the public contract — if this allows faster iteration. In the **general canon and mandatory setting**, the declarative deck falls into a separate step/ADR when there is data and scenarios, not "just in case" in advance.## Solution

<a id="adr0063-p1"></a>

**1. Introduce the product term *instrument deck* (working translation: *tool deck* or *slot scene*)** — is an **named specification**: which **instruments** (`instrument_id` / alias from [0050](0050-declarative-instrument-zone-placement-toml.md)) participate in the composition, **in what order** and with **what structure** (grid on one **Page**, tabs, stack, split — a list of patterns is defined by the implementation and policy [0046](0046-presentation-layout-authority-and-cockpit-invariants.md)). A typical case for **`ContentRepresentation.Page`**: **several instruments on one screen** anchors — see [§ Page + deck](#adr0063-page-plus-deck-pfd).

**Boundary:** one deck is bound to **exactly one semantic anchor of attention** in the sense of [0021](0021-pfd-mfd-cockpit-attention-model.md) — for example, a region **PFD** or **MFD** in this preset, but **not** "half of the screen arbitrarily". It does not describe moving an instrument from PFD to MFD without changing the preset (this is still a policy of the preset / UiMode).

<a id="adr0063-p2"></a>

**2. Difference from `[instrument_routing]` ([0050](0050-declarative-instrument-zone-placement-toml.md)):** routing v1 answers the question **"which one instrument occupies the semantic slot `pfd_primary` / `mfd_primary`"**. Deck — a level **finer**: *several* instruments and their **internal** composition **inside** the same anchor/slot when the product supports it. A specific merge (deck over routing or separate key) — **is not fixed** in this ADR until the implementation appears; the invariant is only **semantic**.

<a id="adr0063-p3"></a>

**3. Axes A and B do not substitute each other:** **`Strip` vs `Page`** — about **the container**; deck — about **filling and layout**. For the IDE Health channel **`DedicatedPage` / `bottom_strip`** — this is just a choice of form **for IDE Health** ([above](#adr0063-two-axes)); this **is not** canceling that for an anchor as a whole **`Page` + instrument deck** — is the normal way to describe **several instruments on one screen** ([§](#adr0063-page-plus-deck-pfd)).

<a id="adr0063-p4"></a>

**4. Difference from UiMode preset ([0010](0010-ui-modes-toml-configuration.md)):** the preset sets up **the global picture** (windows, anchors, topology [0017](0017-multi-window-workspace-and-agent-surfaces.md)). Deck — **local** named composition **inside** one context of an anchor; switching decks (if it will happen) should not substitute switching presets as a whole, unless the product explicitly introduces "over-preset" — this is **outside** the minimal definition.

<a id="adr0063-p5"></a>

**5. Naming in documentation:** avoid bare «page» without clarification in Russian texts. For **axis A** — «form Page» / «mode Strip» or `ContentRepresentation.Page`, for **axis B** — deck / tool deck / slot composition. For IDE Health explicitly: «IDE Health in Page mode (DedicatedPage)» vs «IDE Health strip (Strip)» — do not mix with the deck of an entire anchor.

<a id="adr0063-mfd-page-deck"></a>

### Deck of MFD page (nested level)

**Mfd shell wrapper** ([0017](0017-multi-window-workspace-and-agent-surfaces.md), `MfdShellView` / `MfdShellPageStack`) in product v1 switches **one active page** from the enumeration `MfdShellPage` (chat, terminal, build, …) — this is **top-level navigation** within the **Mfd** anchor, not the instrument deck of the entire region itself.

**Deck of MFD page** — a separate axis: **named composition of instruments inside the selected page** (grid segments, tabs within pages, order of cells — axis B in the sense of [§ two axes](#adr0063-two-axes)), **orthogonal** to switching `MfdShellPage`. The anchor of attention remains the **Mfd region** in the sense of [0021](0021-pfd-mfd-cockpit-attention-model.md); **nesting** — «wrapper page → if necessary, its own deck».

**Already in code (special cases):** on the Workspace Health page — a segment composer with IDE Health segments and `IdeHealthInstrumentDeck`; on the Environment Readiness page — `EnvironmentReadinessInstrumentDeck`. **Direction of implementation:** explicitly model «the MFD page optionally has its own `InstrumentDeckDescriptor`» and do not mix in docs **switching pages** with **deck inside pages». Taxonomy **host / slot / region / cell** during detailing cells — [0088](0088-host-slot-region-deck-cell-taxonomy.md).

**Mfd shell preset** (which `MfdShellPage` is available, order in palette, default page) — **technically feasible**, but like the declarative instrument deck in user TOML ([§ deck in presets](#adr0063-deck-presets-evolution)): merge, validation, schema version, UI — until **page → deck** is not stabilized in code and scenarios are unclear. Public contract on «building MFD pages from preset» — **a separate step**, not a requirement of this ADR.## Non-Goals (as of Proposed)

- Pin down the **TOML syntax** or keys for deck (a separate ADR or extension to [0050](../../design/0050-declarative-instrument-zone-placement-toml.md) after the prototype).
- Require **drag-and-drop** between anchors or arbitrary geometry outside of the preset policy.
- Replace **CDS** ([0036](../../design/0036-cds-channel-compositor-surface-pipeline.md)) or **display topology layer** (anchor placement row; see [0017 §](../../../IntentMelody/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-display-screens-topology-naming)).

<a id="adr0063-cds-not-presentation"></a>

**CDS and display topology – do not confuse:** **CDS** ([0036](../../design/0036-cds-channel-compositor-surface-pipeline.md)) — a contour **channel → contract → compositor → surface**, observability and synchronized frame for MCP; this is **not** a layer «how to draw the entire UI presentation» and **not** the same as **anchor topology on screens** ([0017](../../../IntentMelody/0017-multi-window-workspace-and-agent-surfaces.md), [0046](../../design/0046-presentation-layout-authority-and-cockpit-invariants.md)). The word **presentation** in the name **`[presentation]`** / policy **CockpitPresentationLayout** historically mixes this topology with the common meaning of «presentation» — **direction of TOML names, notation capacity, *screen* vs monitor, `display.layout` vs `display.screens`** — normatively in [0017 §](../../../IntentMelody/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-display-screens-topology-naming). **Deck**, **`ContentRepresentation`** and tool layout in the region describe **form and composition inside an anchor**; they **connect** with compositors and CDS as a data and frame contour, **without** substituting CDS for a separate «presentation engine» and **without** an extra parallel contour «above CDS».## Alternative Names (Name Abstraction)

| Variant | Pros | Cons |
|--------|------|------|
| Keep only "page" | Familiar | Collision with IDE Health Page and web terminology |
| **Deck** / deck | Concise, no airline cockpit metaphors; separate from Page | New jargon — glossary needed |
| **CompositionPage** (composite "page" slot) | Clear that it's about composition, not Strip/Page | The word **Page** causes confusion with **`ContentRepresentation.Page`** and IDE Health `DedicatedPage` — not canonical in ADR |
| Scene / scene | Clear that it's a frame assembly | Intersection with 3D/游戏 "scene" |
| Layout bundle | Technically clear | Long, "bundle" already taken by UiModes |

**Selection for ADR:** fix the English ***instrument deck*** as the canonical name of the abstraction; Russian equivalent in UI/docs — to be agreed upon with the product ("deck", "slot composition").

<a id="adr0063-composition-page-synonym"></a>

**CompositionPage and deck:** if **CompositionPage** means **named ordered content composition of a slot** (tools, order, tabs/stacks — axis B), it is **the same thing** as **instrument deck**; the difference is only in the name. The canonical term in normative texts is **deck**, to avoid mixing with **Page** on the form axis (**`ContentRepresentation`**) and with **`DedicatedPage`** from the IDE Health channel.

<a id="adr0063-indicator-kinds"></a>## Types of Indicators in the Deck (Direction)

To ensure that **deck** provides **dense, readable** screens without a "second monitor text", it makes sense to agree on a **small set of visual primitives** — *how* a cell in a deck or a fragment of an instrument shows its state. This is **not** a replacement for the `ContentRepresentation` axis (Strip/Page): primitives live **inside** the composition cell or inside a Skia tool. The relationship with the attention hierarchy — [0021](../../design/0021-pfd-mfd-cockpit-attention-model.md) (EICAS channel, W/C/A, Dark Cockpit): the type of indicator sets **the geometry of the signal**, not the semantics of the channel.

<a id="adr0063-primitive-vs-instrument"></a>

**Primitive vs Instrument:** A **primitive** (including Lamp / Bar / Sign below) is an **atomic glance**: a single visual response to "what's happening now?" without a full navigation scenario and without a mandatory interaction model like for the whole slot. An **instrument** in the cockpit ([0047](../../design/0047-cockpit-instrument-descriptor-and-slot-composition.md)) is a **scenario with state**: a descriptor of a slot, data, commands, usage context. Primitives **collect the image** inside an instrument or composition cell and **do not replace** the instrument as a whole; the growth of primitives into "mini-applications" — a sign that the boundary has shifted towards a new instrument or new deck cell.

**Proposed Taxonomy (names are references, not mandatory identifiers in code v1):**

| Type | Role | Note |
|------|------|--------|
| **Lamp** | Discrete state, "lit / off", latch of attention | Annunciator / master caution |
| **Bar** | Value or deviation along an axis (fill, marker position); linear bar | Glideslope / deviation / progress |
| **Sign** | Brief category marking (icon, badge, Warning/Error) | Do not confuse with **Readout** and **Caption** |
| **Readout** | Display: large digit or monospaced value string (time, counter, version) | Separate reading mode than Icon-Sign |
| **Trend** | Micro-graphic (sparkline) over time | "Where it's going", not just the current value of a Bar |
| **Gauge** | Scalar in a range on an arc/circle | When a linear Bar is inconvenient inside a cell; a ring can be interpreted as a **view** of Bar or a separate type — implementation solution |
| **Stack** | Multiple segments in one bar (100% split) | Different data model than one value Bar; permissible as a **variant of Bar** if the API generalizes |
| **Caption** | Single text line with rigid truncate (branch, file, command) | Context pointer, not "category" like Sign |
| **Presence** / **Activity** | **Role semantics** in the product (connection, expectation, work flow), not necessarily a separate "form" of primitive | By geometry see below; policy — [§](#adr0063-presence-dark-cockpit) |

**Static Presence and Lamp:** Yes — **discrete** status "in network / out / degraded" by meaning is **Lamp** (several stable states, without mandatory animation) or, if a metaphorical icon makes more sense, **stable Sign** (two icons / one with variant). A separate type of "Presence" in the table does not duplicate Lamp: this is a **data role**, which is usually drawn **through Lamp/Sign**; confusion arises if to name a separate primitive what is already visually covered by **Lamp**.

**Activity** does not have to be a separate geometric shape either: "busy" often — **Lamp** with another state or temporary change; work progress with segments — **Bar** / **Trend**; endless spinner without semantics — rather an anti-pattern ([§](#adr0063-presence-dark-cockpit)). The risk of **Dark Cockpit** is **animation and brightness in the normal state**, not the word Presence.

**Why:** One and the same **compact page** (deck in grid mode) can be built from cells with different primitives — without mandatory full text in each cell. This enhances **glanceability** in [0021](../../design/0021-pfd-mfd-cockpit-attention-model.md) and aligns with the general Skia pipeline ([0055](../../design/0055-skia-instrument-composition-pipeline.md)), without duplicating it.

<a id="adr0063-presence-dark-cockpit"></a>

**Presence / Activity and Dark Cockpit ([0021](../../design/0021-pfd-mfd-cockpit-attention-model.md) §6):** In the product language, **Presence** is a **signal "there is connection / agent on line / synchronization"**; statically it is usually expressed by **Lamp/Sign**, see the paragraph above. **Activity** — work progress (assembly, request), often with **pulse, spinner, blinking accent** — the same primitives in other modes, plus a policy not to be noisy.

Such a primitive can **violate Dark Cockpit** if it makes **constant** movement or bright color in **normal silence** ("all is well, but the point blinks every two seconds for beauty"). Then attention goes to noise instead of real escalation — against the idea of §6.

**How to align:**

1. **Nominal / no active work** — Presence is **silenced, static or hidden** (like an invisible chrome, until there's something to tell the operator). A decorative pulse "we're online" is not allowed.
2. **Significant work in progress** (assembly, long MCP, blocking operation) — **Activity** is permissible as a **temporary** or **linked to real progress** signal; prefer a **steady state** (Lamp "busy") or **Bar/Trend** with real dynamics, rather than an endless spinner without semantics.
3. **Deviation from normal** — divert into the **W/C/A hierarchy** and EICAS channel ([0021](../../design/0021-pfd-mfd-cockpit-attention-model.md) §5), not multiplying "live" pixels in the deck: escalation is noticeable, normality is quiet.
4. **Distinguish semantics:** *Static* Presence (connected / not connected) — by primitives this is **Lamp/Sign**, not a separate geometry; *temporal* Activity (work is happening) — stricter default policy (show only while there's evidence of work progress), visually more often **Lamp busy**, **Bar**/progress or **Trend**, rather than a decorative endless spinner.

Conclusion: **Presence/Activity are not prohibited** — they are prohibited in the mode of constant marketing flashing. The same policy as for EICAS zone: normal operation should not distract ([0021](../../design/0021-pfd-mfd-cockpit-attention-model.md), example of a block appearing only with active alerts — similarly aligned by spirit).

**Boundaries (Proposed):** Do not fix an enum here in the repository, sizes in DP, nor binding to a specific Avalonia control — just **direction**. A public contract for primitives for external authors is **not the goal of the current phase** (see [§ open questions](#adr0063-open-questions)). Decide separately when there is a need: a common layer of **tokens** (color/thickness by severity), **a11y** (more than color), relationship with **intent** ([0051](../../design/0051-intent-based-attention-routing-toml.md)), **product rules** for Presence/Activity **by different UiMode** — relevant when the product again has **multiple** modes; on the current development line, one mode is Flight, the rest intentionally out of scope ([§ open questions](#adr0063-open-questions), [0017 § modes scope](../../design/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-modes-scope)).

<a id="adr0063-intent-and-deck"></a>

### Intent routing and deck

**Intent routing and deck ([0051](../../design/0051-intent-based-attention-routing-toml.md); question clarified):** intent sets **the attention policy** (which anchor, which profile, which hints) — not "replaces" deck. Behavior inside the deck depends on the **composition mode**: in a **grid on one `Page`** (all cells visible, PFD image) intent can highlight a **cell** or **data flow**, not a "tab". In a **tabs/stack** (one visible instrument), it sets separately whether intent can **switch the active tab** or only raise an **anchor** — this is a product rule **after** such a composition appears in code, not a dogma of ADR.

<a id="adr0063-open-questions"></a>## Open Questions

**Context:** There is currently no public interface or contract for external auditors and the general public— we are developing it as convenient within the repository; when changing the solution, refactoring code is easier than breaking existing consumers. **Plugins and third-party authors of tools** are deferred; the question of a "stable enum in Contracts" **does not block** current work.

**UI Modes:** On the current development branch, there is **one product mode— Flight**; other families / presets UiMode **are intentionally not being pursued** (refactoring Power, Balanced and others— **a separate** road map, see [0017 § "Clarification: UI Modes"](#adr0017-modes-scope)). The product formulation "current line (Skia branch, ~2026), one mode Flight, old references Focus/Balanced/Power— archived" is in [`docs/ui-ux/README.md`](../ui-ux/README.md). The **`feature/skia`** branch and work on tools **do not need** to pre-coordinate the Presence policy for non-existent modes. **The question of differentiating Presence/Activity by UiMode** (Focus / Power / …) and a separate normative for "eternally" animated content is **deferred for the current phase**: sufficient general principles [§ Presence / Dark Cockpit](#adr0063-presence-dark-cockpit); return to presets per mode or a separate ADR— **if** there are again **multiple** product modes or explicit need.

- **Public enum / contract** for an expanded set of primitives (including Readout, Trend, Gauge, Stack, Caption, Presence/Activity) and **where the exact boundary** with arbitrary instrument rendering and [§ primitive vs instrument](#adr0063-primitive-vs-instrument)— leave it for **the phase before plugins and public surface;** currently, sufficient taxonomy in this ADR and internal types as needed.## Consequences

- The question "whether to include `ContentRepresentation` in the code" is considered **solved by its meaning ADR**: canonical name of the axis form — **`ContentRepresentation`**; `IdeHealthUiSurface` — current IDE Health binding; generalization to other data channels — **the same axis form**, not "second channel axis" — see [§](#adr0063-content-representation-code).
- The question "deck in presets vs only inside the compositor" is closed **evolutionarily**: initially internal drawing; declarative named decks — later and as a separate contract; local experimental allowed — see [§](#adr0063-deck-presets-evolution).
- The link between **`ContentRepresentation.Page` + instrument deck** is fixed as an image of **one screen with several instruments** (PFD); narrow case IDE Health — see [§](#adr0063-page-plus-deck-pfd). Combination of **intent** and deck — see [§](#adr0063-intent-and-deck).
- **MFD:** navigation between the pages of the shell (`MfdShellPage`) and **deck inside the page** is standardized — see [§ MFD page deck](#adr0063-mfd-page-deck); declarative presets **catalog** pages Mfd are postponed **in the same logic**, as [§ deck in presets](#adr0063-deck-presets-evolution).
- Documentation and ADRs where "page" in the sense of instrument composition figures can **eventually refer to this ADR** and clarify whether it refers to **deck** or **IDE Health Page**.
- The implementation of composition in a slot (grid on **`Page`**, tabs, order) receives a **stable name** for discussion and testing without mixing with routing [0050](0050-declarative-instrument-zone-placement-toml.md).
- A **common dictionary** is introduced for compact deck screens: which types of indicators are allowed in a cell, avoiding confusion with the **form** of the channel (Strip/Page) and with **deck** as a list of instruments.
- The rule is fixed: **primitive = atomic glance**, **instrument = scenario with state** — see [§](#adr0063-primitive-vs-instrument).
- Fixed: **CDS** is not presentation UI; the border with **display topology** and with **deck** — see [§](#adr0063-cds-not-presentation). The direction to clarify **presentation** (keys **`display.screens`**, **`topology`**) — standardized [0017 §](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-display-screens-topology-naming).
- At the **only Flight** phase, the question of a separate Presence/Activity policy by UiMode is not raised (see [§ open questions](#adr0063-open-questions)); upon return to multimode, it will be reinstated as needed.## Change History

<a id="adr0063-history"></a>

| Date | Change |
|------|----------|
| — | [§ CDS not presentation](#adr0063-cds-not-presentation). |
| — | [§ ContentRepresentation and code] - the question about enum was closed with ADR wording. |
| — | [§ Page + deck] (PFD) - one region screen, multiple tools (image PFD). |
| — | [§ Deck in presets] - evolved without a mandatory entity in TOML at the start. |
| — | [§ Primitive vs Instrument]. |
| — | [§ Indicator types] - direction **Lamp / Bar / Sign** for compact deck pages. |
| — | Display topology keys `/display.screens` - normatively [0017 §](../../../design/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-display-screens-topology-naming). |
| — | Extended taxonomy of primitives + [§ Presence and Dark Cockpit] (ADR 0063: Presence and Dark Cockpit). |
| — | Static Presence by geometry - **Lamp**/Sign, not a separate primitive. |
| — | Same in meaning, like informal name **CompositionPage** (see [§ Synonym](#adr0063-composition-page-synonym)). |
| 2026-04-18 | Agreed: **instrument deck** remains a separate axis (composition of instruments / order in tabs and so on), orthogonal to the axis **`ContentRepresentation`** (Strip/Page). |
| 2026-04-24 | [§ Deck page MFD] - composition **inside** one shell wrapping page MFD, orthogonal to navigation between `MfdShellPage`. |
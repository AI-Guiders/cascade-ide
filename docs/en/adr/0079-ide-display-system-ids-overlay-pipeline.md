<!-- English translation of adr/0079-ide-display-system-ids-overlay-pipeline.md. Canonical Russian: ../../adr/0079-ide-display-system-ids-overlay-pipeline.md -->

# ADR 0079: IDS (Ide Display System) - IDE overlay pipeline, orthogonal to CDS

**Status:** Accepted  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS: cabin, channel → CDS → composer → surface |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs presentation IDE: chrome and overlays |
| [0070](0070-command-palette-direct-overlay-surface.md) | Command Palette as direct overlay in active `TopLevel` |
| [0057](0057-chat-surface-pipeline-adoption.md) | chat: surface snapshot composer; analogy for the “composition” layer |
| [0013](0013-command-surface-and-discoverability.md) | palette and discoverability |
| [0115](0115-cds-graph-backed-shared-layer.md) | CDS - common layer of graph-backed devices (implementation in the cockpit, not IDS) |

## Summary

- **IDS** — IDE overlay pipeline (intent → composer → snapshot → surface).
- Orthogonal to CDS [0036](0036-cds-channel-compositor-surface-pipeline.md); Roslyn CASCOPE013–016.


---
## Context

[0036](0036-cds-channel-compositor-surface-pipeline.md) defines the chain for **cockpit semantics** (PFD/MFD/instruments, `CockpitSurfaceState`, instrument slots). It **shouldn't** grow to "all IDE pixels", otherwise the CDS will become a God layer and mix with [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).

In parallel, the product needs **global IDE overlays**: the command palette (`Ctrl+Q`), later toast, blocking dialogs, and other “on top of the entire workspace” surfaces. They have other invariants:

- binding to **active `TopLevel`** and returning focus ([0070](0070-command-palette-direct-overlay-surface.md));
- **keyboard-first** and predictable input interception;
- if possible - **snapshots** for tests and agent observability without reference to a specific `Control` tree.

Without an explicit name and boundaries, this outline spreads across `Views/*` and `*ViewModel`, duplicating the logic of “who is on top” and “who is eating the keys”.

---

## Solution

A separate product circuit **IDS - Ide Display System** is being introduced (namespace and types in the code: `CascadeIDE.IdeDisplay.*`).

### 1. Purpose of IDS

**IDS** describes a **presentation IDE** for **modal and semi-modal overlays** (and eventually other IDE-surfaces that are not cockpit devices): separating **data/intent** and **shot composition** from a specific render (Avalonia `Control`, Skia-host, hybrid).

**CDS** remains canon **cockpit**; **IDS** is the canon of **IDE overlays**. Mixing the semantics of cockpit slots and the IDE overlay stack in one type is an anti-pattern; if necessary, generally only at the level of the words “channel / composer / snapshot”, not at the level of one DTO.

<a id="adr0079-cds-vs-ids"></a>

### Distinguishing between CDS and IDS (one application, two domains)

Both circuits run **within the same process** CascadeIDE (geographically, “it’s all IDE”). The confusion is resolved by asking the **domain of meaning** rather than the exe file:

| | **CDS (and the cabin around it)** | **IDS** |
| --- | --- | --- |
| **Question** | Where in the **Cockpit Attention Model** does the content go: PFD/Forward/MFD, Instrument Slot, Secondary Loop Page? | How to show **shell overlay** on top of workspace: palette, modal, toast; focus, z-order, input interception? |
| **Typical test** | “On which MFD page / in which deck zone / in which device slot?” | “Modally on top of the entire window, like in a regular application, without the semantics of the device?” |
| **Standard** | [0036](0036-cds-channel-compositor-surface-pipeline.md), [0021](0021-pfd-mfd-cockpit-attention-model.md) | this ADR, [0070](0070-command-palette-direct-overlay-surface.md) |
| **Device vs Chrome Layers** | Semantics of **devices and zones** ([0036](0036-cds-channel-compositor-surface-pipeline.md)) | **Non-device** presentation shell ([0066](0066-cockpit-ui-vs-ide-presentation-layer.md)) |

**Mnemonic:** you decide “**where in the cockpit**” → **CDS**; “**globally on top of workspace**, not device slot” → **IDS**. The border is anchored by **CASCOPE016**: `Cockpit/` does not import `IdeDisplay`.

### 2. Same chain discipline as 0036 (in meaning, not in type)

For each overlay in IDS, the following chain is followed:

1. **Channel / state** - what the user entered, what is filtered, what mode (commands / go-to / melody), without pixel coordinates.
2. **Surface Composer** - pure function (or close): from the channel builds a **snapshot** for rendering and navigation (lines, selection, tooltips, opacity).
3. **Surface** - Avalonia/Skia: focus, hit-test, animations; not the source of overlay semantics.
General contract for composers: `IIdsSurfaceCompositor<TIntent, TSnapshot>` with the `TSnapshot Compose(TIntent intent)` method (parameter **without** `in` with contravariant `TIntent` is a C# language limitation).

### 3. Single input capture host

**directional** invariant: keyboard/focus hijacking for **IDE overlay stack** converges to **single place** - conditional **`IdsOverlayHost`** / **input router** at the root of the active host, rather than to disparate `PreviewKeyDown` in each overlay without policy.

At the time of ADR adoption, the full host implementation may be **strangler**; the palette may temporarily remain self-contained ([0070](0070-command-palette-direct-overlay-surface.md)), but **new** overlays must not add a third independent "global intercept" without explicit negotiation in that ADR or a separate ADR.

### 4. Slots (`SurfaceSlot`) - optional

**`SurfaceSlot` (or `IdsSurfaceSlot`)** is introduced when there are **multiple** competing IDS surfaces at the same time or an explicit **z-order / mount id** is needed for observability and tests.

For **one** palette as the only top overlay, the slot is not required. For "palette + toast + blocking dialog" - the slot or equivalent **stack** becomes part of the IDS host.

Naming: Defaults to **`Ids*`** in code to avoid confusion with **`CockpitInstrumentDescriptor`** and CDS slots ([0047](0047-cockpit-instrument-descriptor-and-slot-composition.md)).

### 5. Agent observability

IDS snapshots (e.g. composition of palette rows and selected index) can be supplied to the MCP/agent contract **separately** from the `cockpit_surface`, or as a subtle "overlay" field in the overall summary - the JSON field decision does not capture this ADR; only **semantics separation** CDS vs IDS is recorded.

### 6. Roslyn-guardrails (CASCOPE013–016)

On the assembly [`CascadeIDE.ArchitectureAnalyzers`](../../CascadeIDE.ArchitectureAnalyzers/README.md):

- **CASCOPE013** / **CASCOPE014** / **CASCOPE015** - in the `IdeDisplay/` directory, dependencies on `CascadeIDE.Cockpit...`, on Avalonia UI and on `CascadeIDE.Features.UiChrome` are prohibited (including types in class members in `CascadeIDE.IdeDisplay.*`).
- **CASCOPE016** - `using CascadeIDE.IdeDisplay…` is prohibited in `Cockpit/` (the cockpit does not support IDS).

Full formulations and levels are in the README of the analyzers.

---

## Implementation status (fixed as of ADR date)

- **Done:** contract `IIdsSurfaceCompositor<TIntent, TSnapshot>`; for the palette - `CommandPaletteSurfaceIntent`, `CommandPaletteSurfaceSnapshot`, `CommandPaletteSurfaceCompositor`; updating snapshot from `MainWindowViewModel` when the palette is open; composer tests; Roslyn **CASCOPE013–016** (see §6).
- **According to plan (strangler):** single `IdsOverlayHost` / input router; optional `IdsSurfaceSlot` for second overlay; if necessary, projection into MCP without mixing with CDS.

---

## Consequences

- New IDE overlays are by default designed via **IDS** (intent → compositor → snapshot → surface), and not “directly in AXAML with half the logic in the VM”.
- CDS and cockpit instrument registry **are not bloated** by responsibility for global IDE modals.
- Tests can rely on an **IDS snapshot** where list/selection semantics are important, without a fragile tie to the visual tree.

## Not goals

- Replacement of Avalonia as host for overlays.
- Renaming or repurposing **CDS** for IDE-chrome.
- Full JSON specification for the agent in this ADR.

## Rejected alternatives (briefly)

- **Everything in CDS:** rejected - mixes cockpit and IDE-shell, breaks [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).
- **Separate ADR for each overlay without a common IDS name:** acceptable as a temporary measure, but leads to duplication of the input/stack policy.
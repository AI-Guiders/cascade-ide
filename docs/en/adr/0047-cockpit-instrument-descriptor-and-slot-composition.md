<!-- English translation of adr/0047-cockpit-instrument-descriptor-and-slot-composition.md. Canonical Russian: ../../adr/0047-cockpit-instrument-descriptor-and-slot-composition.md -->

# ADR 0047: `Instrument` - slot composition handle, not `Control`

**Status:** Accepted · Implemented  
**Date:** 2026-04-15

## Related ADRs

| ADR/document | Role |
|----------------|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → composer → surface |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Areas of attention |
| [0039](0039-workspace-navigation-affordances.md) | Semantic Map, navigation |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | `presentation` invariants |
| [`cds-contract-v0.md`](../../design/cds-contract-v0.md) | Descriptors and slots drawing |

**File name (history):** Previously the draft was called `*widget*`; the canonical product term is **Instrument** (see point 1).

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | term **Instrument**, `CockpitInstrumentDescriptor`, `MainWindowHostSurfaceFrame`, `MainWindowInstrumentMountRegistry` |
| — | see [`cds-contract-v0.md`](../../design/cds-contract-v0.md) |
| — | expansion of the list of tools - according to the roadmap |

---
## Context

In the conversation about **Solution Explorer** and **Semantic Map**, a confusion of layers surfaced: “data”, “data view”, “Avalonia surface” and “attention slot” were mixed up. We need a **stable term** for the unit that the **composer** selects for the slot and which the **surface** mounts - without substituting the meaning of "any `Control`". The word **instrument** in English has multiple meanings (aircraft instrument, measuring instrument, musical instrument, etc.); in this ADR it is fixed in the sense of **cabin instrument**: a logical unit of indication/presentation in the area of ​​attention, in the spirit of the PFD/MFD metaphor, **not** necessarily a “arrow on the dashboard”.

## Solution

<a id="adr0047-p1"></a>

1. **`Instrument` (cab)** is a **named choice of representation in the attention slot**, the result of the work of the **composer**. It is **not** synonymous with Avalonia-`Control` and **not** a raw data feed (Build log, navigation graph, etc. remain in channels/projections).

<a id="adr0047-p2"></a>

2. **`CockpitInstrumentDescriptor`** (code) - minimal **contract** descriptor: stable `instrument_id`, slot identifier (`slot_id`, for example `pfd` / `mfd` / `forward`), `schema_version` descriptor strings. Expansion of fields (instrument parameters, data links) - as the second and third instruments appear in one slot.

<a id="adr0047-p3"></a>

3. **CDS** still responds to "**where** the tool is allowed to go" given the current `presentation` and topology; **composer** - to “**which** `instrument_id` in which `slot_id`”; **surface** - to "**which** `Control`/View implements this handle."

<a id="adr0047-p4"></a>

4. **Examples of PFD slots:** `solution_explorer_tree` and `workspace_navigation_map` (Semantic Map) - **two different tools** of the same slot class “workspace view”, mutually exclusive or with an explicit split - decided by the composer + capabilities, not arbitrary View code.

## Consequences

- A language appears for the MCP/agent: “mount tool X in slot Y” without reference to the control name in the tree.
- Regressions that “broke not the data, but the presentation” are localized in the composer and **instrument registry**.

## Not goals (v1)

- A complete registry of tools and hot-swap of all panels - in separate iterations after stabilization of the descriptor.
- Replacement of `UiLayoutSnapshot` to automate the UI tree.

## Rejected alternatives

- **`Widget`** — overloaded (web, Flutter, “OS widgets”); replaced with **Instrument** for a clear connection with the cockpit metaphor and less conflict with UI frameworks.
- Calling any `UserControl` a "tool" without a handle is rejected: blurs the composer/surface boundary.
- Hanging the SE vs Semantic Map choice only on TOML without a composer layer is rejected: does not scale to MCP and tests.
<!-- English translation of adr/0036-cds-channel-compositor-surface-pipeline.md. Canonical Russian: ../../adr/0036-cds-channel-compositor-surface-pipeline.md -->

#ADR 0036: Channel → CDS → surface composer → surface (Agent-first mapping)

**Status:** Accepted · Implemented  
**Date:** 2026-04-11

## Related ADRs

| ADR/document | Role |
|----------------|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Areas of attention; CDS vs `UiLayoutSnapshot` |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU: Channel DTO **to** CDS |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation`, window topology |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Contracts, Testable Boundaries |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Web Surface ≠ MCP |
| [0115](0115-cds-graph-backed-shared-layer.md) | Overall graph-backed layer in cockpit |
| [0067](0067-graph-backed-surfaces-contract.md) | Measurement contract graph-backed |
| [`cds-contract-v0.md`](../../design/cds-contract-v0.md) | Field and DTO drawing |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | layers `Cockpit/Cds`, `Cockpit/Channels`, `Cockpit/Composition`, `Cockpit/Surface` |
| — | field drawing - [`cds-contract-v0.md`](../../design/cds-contract-v0.md) §6–7, [`Features/README.md`](../Features/README.md) |

## Summary

- Chain: **channel** (meaning and payload) → **CDS** (where the data *has the right* to go) → **composer** (slot and layout) → **surface** (Avalonia/Skia).
- **Agent-first:** target agent commands are at the CDS/composer level, not direct access to arbitrary `Control` (exceptions are explicit strangler).
- **`UiLayoutSnapshot`** remains for the control tree and debugging; **orthogonal** CDS ([0021](0021-pfd-mfd-cockpit-attention-model.md)).
- The channel **doesn't** import `MainWindow.axaml`; CDS **doesn't** duplicate the full UI tree.
- Implementation: `Cockpit/Cds`, `Channels`, `Composition`, `Surface` - see [cds-contract-v0.md](../../design/cds-contract-v0.md).

---
## Context

In avionics, **CDS** (Cockpit Display System in the ARINC sense) is the idea of a **single display circuit**: application systems provide data and commands, and the **composition** of the screen (layers, priorities, valid regions) is at an agreed runtime. Cascade **does not** copy the 661 profile and does not require certification ([0021 § ARINC 661](0021-pfd-mfd-cockpit-attention-model.md#arinc-661-borrow)); We carry over the **principle of separation**: the agent and channels should not directly “draw” an arbitrary UI if the goal is **predictable cockpit** and Agent-first discipline.

Already fixed: **CDS in the product** is a layer of **cockpit semantics** (what topology, which zones/pages are active), orthogonal to the **control tree** (`UiLayoutSnapshot`) and a thin field drawing in [`cds-contract-v0.md`](../../design/cds-contract-v0.md). There is not enough **explicit chain of responsibility** from the data flow to the pixels so that new features do not punch a hole “right into Avalonia”.

## Solution

<a id="adr0036-p1"></a>

1. **Channel / flow** - a layer of **data and meaning** for a specific attention tool: health workspace, EICAS, readiness, later - other channels. Answers the questions: *what* to show, *in what priority*, *what payload* (segments, messages, metrics). **Doesn't** answer the question "at what pixel coordinate" and **isn't** tied to a specific `Control` in the tree.

<a id="adr0036-p2"></a>

2. **CDS (cabin contract)** - **routing layer in the attention model**: *where channel data **has the right** to go* given the current preset and topology. Aggregates semantics: UI mode, effective string `presentation`, surface view (`AttentionLayoutSurfaceKind`), visibility of PFD/Forward/MFD zones, Mfd host window state, active secondary contour page (`SecondaryShellPage`), etc. - in the spirit of the fields [`cds-contract-v0.md`](../../design/cds-contract-v0.md). Implementation in code: **snapshot** (for example `CockpitSurfaceState` / `CockpitSurfaceSnapshotBuilder`), and not “one God-class” for the entire UI.

<a id="adr0036-p3"></a>

3. **Surface Composer** - layer **linking a channel slot with a specific representation** within a region: stripe vs MFD page, segment order, EICAS/HUD rules regarding canons [0021](0021-pfd-mfd-cockpit-attention-model.md). Answers the question: *how to get **markup** for the VM/view* from a valid CDS location and channel data (without duplicating the full channel text in the CDS - see table of terms in drawing v0).

<a id="adr0036-p4"></a>
4. **Surface** - specific **Avalonia** controls (and bindings to `MainWindowViewModel`), focus, animations. **Avalonia is intentionally narrow in meaning:** window, input, host for controls; **not** primary source of cockpit zone semantics (it's in CDS/composer). Custom rendering (eg Skia) is allowed as a surface region implementation on top of this host. MCP tools like `ide_get_ui_layout` remain on this layer; **target** agent commands to change the cabin are formulated **at the CDS/composer level**, rather than direct access to an arbitrary control name - with the exception of deliberate exceptions (automation, strangler), which are documented separately.

<a id="adr0036-p5"></a>

5. **Invariant:** the channel **doesn't** import knowledge about `MainWindow.axaml`; CDS **does not** duplicate the full UI tree; composer **doesn't** override the zone policy from [0021](0021-pfd-mfd-cockpit-attention-model.md). A violation is a separate decision or temporary measure with an explicit label. **Clarification:** types in `Cockpit/Channels` **do not** depend on specific VM features (`UiChromeViewModel`, instrumentation panels, etc.) - only on DTOs, delegates (`Func<…>`) and non-UI services; substitution of live sources - in the root of the composition (for example `MainWindowViewModel`). This does not prevent the VM from knowing channels, CDS and visibility - this is its role.

## Consequences

- Agent-first and MCP extensions receive **stable language**: “where in the cabin” (CDS) separately from “what is in the band” (channel) and from “what control” (surface).
- Tests and snapshots can rely on a **CDS snapshot** without a fragile binding to the control tree for “attention and layout” scenarios.
- The implementation remains **phased**: the root VM still associates sources with the channel; this ADR sets the **direction** towards which new features try to converge.

## Not goals

- Full implementation of **ARINC 661** or **CDS** as in certified avionics.
- Replacement `UiLayoutSnapshot` for debugging and tree automation - layer **orthogonal** to CDS ([0021 §1.1](0021-pfd-mfd-cockpit-attention-model.md#glossary-cds-contract)).
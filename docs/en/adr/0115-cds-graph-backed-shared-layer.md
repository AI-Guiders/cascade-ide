<!-- English translation of adr/0115-cds-graph-backed-shared-layer.md. Canonical Russian: ../../adr/0115-cds-graph-backed-shared-layer.md -->

# ADR 0115: CDS - common layer of graph-backed devices (implementation in the cockpit, not IDS)

**Status:** Accepted  
**Date:** 2026-05-14  
**Updated:** 2026-05-14 - in code: `IGraphDataSource`, workspace navigation map adapter. Details - [§ History](#adr0115-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → composer → surface |
| [0067](0067-graph-backed-surfaces-contract.md) | Contract of a family of graph-backed surfaces |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS - shell overlays (**not** to be confused with CDS) |
| [0055](0055-skia-instrument-composition-pipeline.md) | Skia stages of composition |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Device slots and descriptors |
| [0065](0065-instrument-categories-domain-taxonomy.md) | Axis `graph_kind` |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Axis `relation_kind` |
| [0113](0113-hci-semantic-map-orientation-layer.md) | HCI, `edge_provenance` |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU → Channel DTO (§4 in text) |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | `SemanticMapInputSnapshot` and index integration |

## Problem

**[0067](0067-graph-backed-surfaces-contract.md)** sets **contract dimensions** (model, interaction, provenance, `relation_kind`, synchronization with workspace, ...) for *all* graph-backed surfaces, but **does not explicitly** specify in which **product loop** the overall implementation lives: cockpit (**CDS**) vs IDE overlay (**IDS**). The third “DisplaySystem” (conditional *GraphDisplaySystem*) easily appears in the discussion, which **blurs** the border **[0079 § CDS vs IDS](0079-ide-display-system-ids-overlay-pipeline.md#adr0079-cds-vs-ids)**.

We need ADR at the **location** level: where in the architecture the **reusable code** of the common parts of graph-backed devices is collected and how it fits into the already accepted chain **0036**.

---

## Solution

### 1. Placement invariant

**The general layer of graph-backed instruments** is **a subsystem within the cockpit circuit (CDS)**, and not a parallel **IDS** and not a separate top-level “Display System”.

- Product graph screens (Semantic Map, GitMap, future dependency graphs, etc.) remain **devices/surface regions** in the sense of **[0021](0021-pfd-mfd-cockpit-attention-model.md)** and go through the same **logical** chain **[0036](0036-cds-channel-compositor-surface-pipeline.md)**: data and intent - in **channel** / related services; routing by zones - **CDS**; convolution into slot layout - **composer**; **surface** - Avalonia/Skia host in `Cockpit/Surface` (or equivalent), without importing `IdeDisplay` from `Cockpit/` (**CASCOPE016**).

### 2. What refers to the “common layer” (target division)

**General layer** (name in code is implementation decision; workspace namespace in the spirit of `Cockpit.*` + suffix `Graph` / `GraphSurface` / `GraphBacked`):

- general protocols **graph document** (node/edge, session keys, optional fields **`graph_kind`**, **`edge_provenance`**, **`relation_kind`** - see **0065**, **0113**, **0114**);
- **data source abstraction** for surface: **`IGraphDataSource`** in `CascadeIDE.Cockpit.Graph` - `BuildNavigationJson` method accepts **`CodeNavigationMapJsonRequest`** (v0: wire JSON intent maps / workspace navigation); specific providers - **adapters** (currently `WorkspaceNavigationMapContextJsonDataSource` in `Features/WorkspaceNavigation/Application`) without binding the generic framework to Roslyn in the VM;
- **repeatable** pieces of **interaction** (pan/zoom/hit-test policy, Dark Cockpit limits), where the domain is not unique;
- **connection point** to **[0055](0055-skia-instrument-composition-pipeline.md)** (Intent / Declutter / Layout / Render), without duplicating domain graph loading;
- **command routing** and **observability** agreements for the agent, compatible with the **0067** dimension table.

**Remains domain specific** for each `graph_kind`/tool:

- **implementation** of the source (implementation of `IGraphDataSource` / composition of several sources: Roslyn, Git, HCI candidates, ...);
- choice of **layout engine** and visual semantics of the node (icons, signatures, color by domain);
- "go to source" command handlers where the semantics are **not** derived only from the **`relation_kind`** + standard action table.
**The “surface doesn’t care” nuance:** the framework really **doesn’t care** about a specific backend as long as the graph document and edge metadata satisfy the contract. But **not** 100%: trusted UX (signatures/icons by **`edge_provenance`**), default actions by **`relation_kind`**, navigation restrictions - often formalized **in the general layer** by policies and hints, and not just inside the adapter; otherwise each adapter duplicates the same thing.

### 4. Communication with CCU and external inputs

Rolling up raw materials in the channel DTO **to** or **around** CDS remains at **[0097](0097-cockpit-compute-units-transport-to-channel-dto.md)**. Inputs like **`SemanticMapInputSnapshot`** ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md), [0113](0113-hci-semantic-map-orientation-layer.md)) **feed** the channel/composer of the graph-backed device; **not** replace graph-backed interaction layer.

### 5. Explicit “we don’t do”

- **Do not** introduce **IdeDisplay.Graph*** and do not mix the cockpit graph with **IDS**, unless there is a separate product “graph only as a global overlay” (then a separate ADR and a conscious exception).
- **Do not** duplicate the text **0067**: this ADR is about **where the common layer code lives**, and not about repeating the dimension table.

---

## Consequences

- Review of new graph features: the question “where do we put the common code?” → **inside CDS/Cockpit**, reference to **0115**; “What measurements should I take?” → **0067**.
- Search for viewer duplication between Semantic Map and GitMap → transfer to a common package **within the boundaries of Cockpit**, and not in `IdeDisplay`.

---

## Rollout (sketch)

1. Document (this ADR) as a stable link for design review.  
2. Strangler: **v0** - `IGraphDataSource` + adapter to existing `WorkspaceNavigationMapContextJsonBuilder`; refresh PFD via interface. Next is the removal of the common parts of the composer/policy as the second consumer (**0067**).  
3. As stabilization progresses, clarify the namespace and CASCOPE rules in `CascadeIDE.ArchitectureAnalyzers` if necessary (a separate mini-ADR or editing existing guardrails).

---

## History of changes

<a id="adr0115-history"></a>

| Date | Change |
|------|-----------|
| 2026-05-14 | abstraction of the graph source (`IGraphDataSource`/equivalent) in the general layer; adapters - domain. |
| 2026-05-14 | in the code: `CascadeIDE.Cockpit.Graph.IGraphDataSource`, `CodeNavigationMapJsonRequest`, adapter `WorkspaceNavigationMapContextJsonDataSource`; `MainWindowViewModel` takes JSON through the interface. |
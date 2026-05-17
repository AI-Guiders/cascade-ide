<!-- English translation of adr/0067-graph-backed-surfaces-contract.md. Canonical Russian: ../../adr/0067-graph-backed-surfaces-contract.md -->

# ADR 0067: Graph-backed surfaces - a general contract for a family of graph screens

**Status:** Accepted  
**Date:** 2026-04-19  
**Updated:** 2026-05-14 - link to placing the shared implementation layer in **CDS** ([0115](0115-cds-graph-backed-shared-layer.md)), not IDS. Details - [§ History](#adr0067-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0065](0065-instrument-categories-domain-taxonomy.md) | Axis `graph_kind`, tool categories |
| [0113](0113-hci-semantic-map-orientation-layer.md) | Axes **provenance**, summary of three axes |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Directory `relation_kind` on edges |
| [0115](0115-cds-graph-backed-shared-layer.md) | General implementation layer in **CDS**, not IDS |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | GitMap - data domain, general pipeline |
| [0053](0053-semantic-map-control-flow-pfd.md) | Semantic Map, control flow |
| [0056](0056-semantic-map-pipeline-adoption.md) | Introduction of Skia pipeline into the map |
| [0055](0055-skia-instrument-composition-pipeline.md) | Intent → Declutter → Layout → Render |
| [0039](0039-workspace-navigation-affordances.md) | Navigation, MCP, subgraph |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Instrument, slot, surface |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD Focus Areas |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Device surface vs chrome IDE (`ModalOverlay`) |

## Summary

- General **contract of graph-backed surfaces**: data, interaction, layout, sync.
- Family: intent map, GitMap, future graphs; implementation - [0115](0115-cds-graph-backed-shared-layer.md).
- Axes **`graph_kind`**, provenance, **`relation_kind`** - in [0065](0065-instrument-categories-domain-taxonomy.md), [0114](0114-graph-edge-relation-kind-taxonomy.md).

---
## Context

In the IDE, **not one** graph screen appears, but a **family** of surfaces, where the user and agent work with the **graph** as the main object: Semantic Map (code intent / control flow), GitMap / submodules ([0062](0062-git-submodules-semantic-map-subgraph.md)), possible future graphs (dependencies, service topology, etc.).

If each script is run by a **separate ad-hoc viewer**, the following will inevitably be duplicated:

- data model and node/edge identity in the workspace;
- interaction (zoom, scrolling, hit-test, gestures);
- navigation semantics (“go to symbol”, “expand subgraph”, “synchronize with editor”);
- layout abstraction (so as not to copy the layout between domains);
- selection, focus, keyboard outline;
- coordination with the rest of the workspace (decision tree, open files, MCP, invalidation when changing branches/git).

We need a **general UI architectural class** - **graph-backed surface** - and an explicit **contract** for the dimensions below; **the graph domain and data source are different**, rather than reinventing the wheel for each use case.

A task at the **platform architecture** level, and not a request to “draw nodes and edges.”

<a id="adr0067-key"></a>

### Key idea: a graph is not just a visualization

**Graph** in this IDE is **not** synonymous with a picture for a report.

**Graph-backed surface** is an **navigation surface IDE** with the same rights as **editor**, **terminal**, **diagnostics**, **solution explorer**: not a decorative diagram and not an export-only preview, but an **operational surface** through which both a person and an agent can explore the structure, select nodes, go to sources, filter, focus and perform actions. Visualization is a consequence of the model and contract, and not vice versa.

---

## Solution

Fix the concept of **graph-backed surface** (working name): **tool or UI fragment**, in which **primary** is work with a **oriented (or labeled) graph** as an object of navigation and actions, coordinated with the cockpit and workspace according to uniform rules; rendering is one of the layers, not the definition of the surface. **Placing the general implementation** of reused parts of this class in the product - in the **CDS / Cockpit** circuit, not in the **IDS**; see **[0115](0115-cds-graph-backed-shared-layer.md)**.

<a id="adr0067-not"></a>

### Limitations: What it is not

So that the implementation and external agents do not “go” to the one-time viewer:
- this is **not** “diagram control” as a complete answer (diagram without navigation model and synchronization with workspace);
- this is **not** a layer **only** rendering;
- this is **not** a feature under **one** CFG / one use case;
is an **extensible platform** for **several** graph-backed tools (Semantic Map, GitMap, future dependency/relationship graphs, etc.).

<a id="adr0067-extra"></a>

### Additional platform requirements

In addition to the [dimensions table](#adr0067-dimensions), the target contract includes:

- **Single graph document abstraction** (e.g. **GraphDocument** or equivalent): nodes/edges, metadata, domain binding and `graph_kind` ([0065](0065-instrument-categories-domain-taxonomy.md)).
- **Unified semantics of navigation commands**: from a node - to **sources**, **details**, **related subgraph** / related graph (wording may differ by domain, **action channel** is the same).
- **Serializable state** surface: selection, viewport, filters - to restore the session, tests and consistency with other panels.
- **Agent introspection**: surface state is **readable** by agent and commands (MCP / `ide_*`), without sole reliance on pixels.
- **Different layout engines** as plug-in strategies **without** breaking the document model (see Layout abstraction and [0055](0055-skia-instrument-composition-pipeline.md)).

**Invariant:** Semantic Map, GitMap and subsequent graph screens are **representations of one class** in the sense of the contract; they **do not** have to** share the same `instrument_id` or the same JSON wire format, but **must** be comparable across dimensions [§2](#adr0067-dimensions) so that the team and agent can transfer expectations between screens.

Implementation is allowed **step by step** (strangler): first, two consumers (for example Semantic Map + GitMap) identify a common minimum; the contract in the code (`interface`/set of protocols) is extended without breaking ADR.

<a id="adr0067-dimensions"></a>

### Contract dimensions (what is explicitly agreed upon)
| Measurement | Question answered by layer | Note |
|-----------|-------------------------|-----------|
| **Data model** | What is a node and an edge in **this** domain; stable **key** within a session; association with `graph_kind` and tool category ([0065](0065-instrument-categories-domain-taxonomy.md)). | The domains are orthogonal: code vs git topology - different graphs, same surface class. |
| **Edge/node provenance** | What **source of truth** are the connections and nodes for this screen based on: symbolic model (Roslyn), workspace heuristic (MSBuild), full-text/vec on corpus (HCI), composite (for example HCI → Roslyn). | Orthogonal to **`graph_kind`**: the same map type can combine layers with different provenance; table and names - **[0113 § axes](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)**. |
| **Relation kind** | What **relationship** between entities does the edge assert (inherits, refers to, partial peer, text match, ...). | **[0114](0114-graph-edge-relation-kind-taxonomy.md)**; orthogonal to **`graph_kind`** and **provenance**. |
| **Interaction model** | Pan, zoom, drag-and-drop view, hit-test, FPS/Dark Cockpit limitations ([0021](0021-pfd-mfd-cockpit-attention-model.md) §6). | General patterns; details may vary by tool. |
| **Navigation semantics** | What does it mean to “go”, “open”, “request subgraph”, how does it fit in with the MCP and the agent ([0039](0039-workspace-navigation-affordances.md)). | Semantics of **actions**, not just rendering. |
| **Layout abstraction** | Where is the boundary between graph data and geometry: stages [0055](0055-skia-instrument-composition-pipeline.md); replaceable **layout engines** to match the appearance of the graph without duplicating Render. | GitMap and CFG are not required to have the same layout engine; must have **the same connection point** in the pipeline. |
| **Selection / focus model** | One or more selected nodes; keyboard focus; connection with the “current” node for the agent and UI. | It needs consistency with the rest of the cockpit. |
| **Command routing** | IDE commands, context menus, and hotkeys reach the surface predictably; do not duplicate disparate handlers without a policy. | Communication with [0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md). |
| **Deep-linking / reproducibility** | Follow the link to the node/filter/view state; consistency with serializable state. | Not necessarily in v1 completely; direction is fixed. |
| **Sync with workspace** | Communication of the selection with the editor, decision tree, git state; invalidation when changing a file, branch, solution; without a second source of truth. | Explicit events or snapshots, not hidden globals. |
| **Observability (agent)** | Surface state snapshot for agent and automation; not only “what is drawn”, but also selection, focus, domain keys of nodes. | See [add. requirements](#adr0067-extra). |

The contract **doesn't** require one common `Graph` type in memory for all domains - it requires **comparability** of protocols and **absence** of inconsistent one-off viewers without justification.

<a id="adr0067-agent-prompt"></a>

### Starting prompt for the agent (English)

The text below can be used as a single starting point for design and review (Cursor, etc.):

We need to design a **reusable graph-surface architecture** for the IDE, not a one-off graph viewer. Existing and upcoming features such as Semantic Map (CFG), Git submodules, and future dependency / relationship graphs should all fit the **same conceptual model**.

A graph in this IDE is **not** just a visualization; it is an **interactive workspace surface** with navigation, focus, selection, commands, synchronization with other panes, and **agent-readable state** — on par with the editor, terminal, diagnostics, and solution explorer.

**This is not:** only a diagram control; only a rendering layer; a single feature for one CFG use case. **It should be** an **extensible platform** for multiple graph-backed tools.

Please propose an architecture for a generic graph-surface framework, including where appropriate:
- graph **document** model and node/edge metadata;
- **layout** abstraction and pluggable layout engines without breaking the document model;
- **interaction** contract (pan/zoom/hit-test as needed);
- **command routing** and unified navigation semantics (e.g. node → source / details / related graph);
- **selection/focus** and synchronization with the rest of the IDE workspace;
- **deep-linking** and **serializable** surface state;
- **agent introspection** over surface state.

Cross-check with ADR **0067** (graph-backed surfaces) and related ADRs on `graph_kind`, Semantic Map, GitMap, and the Skia pipeline.

---

## Consequences

- New graph features are checked: **which dimension** is already covered by a general layer, **what** is domain-specific.
- Documentation and reviews can refer to **graph-backed surface** and dimension table instead of "another map".
- Pipeline [0055](0055-skia-instrument-composition-pipeline.md) remains a **common place** for Layout/Render; graph sources are connected as **adapters**, not viewer forks.

---

## Non-targets (current phase)

- A single **universal** layout for all types of graphs in one release.
- Full implementation of all measurements in the code before the appearance of the second and third consumers of the contract - let's say **minimum v0** and extension.
- Replacing [0062](0062-git-submodules-semantic-map-subgraph.md) or [0065](0065-instrument-categories-domain-taxonomy.md): they clarify **domain**; this ADR specifies the **UI class**.

---

## Alternatives (briefly)

| Option | Minus |
|--------|--------|
| Separate viewer for each graph | Duplication, divergence of navigation and synchronization with workspace |
| One hard `GraphView` control for all data | Does not bend to different domains and layouts; slows down evolution |
| Only guide in Markdown without ADR | No stable link for review and onboarding |

---

## History of changes

<a id="adr0067-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-19 | key idea “operating surface”, limitations, additional. requirements, seed for agent (EN). |
| 2026-05-14 | dimensions **Edge / node provenance** ([0113 § axes](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)) and **Relation kind** ([0114](0114-graph-edge-relation-kind-taxonomy.md)); both are orthogonal to `graph_kind`. |
| 2026-05-14 | reference to placing the shared implementation layer in **CDS** ([0115](0115-cds-graph-backed-shared-layer.md)), not IDS. |
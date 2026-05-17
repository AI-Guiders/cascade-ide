<!-- English translation of adr/0114-graph-edge-relation-kind-taxonomy.md. Canonical Russian: ../../adr/0114-graph-edge-relation-kind-taxonomy.md -->

# ADR 0114: Relation type on graph edges (`relation_kind`) - connection semantics

**Status:** Proposed  
**Date:** 2026-05-14

## Related ADRs

| ADR | Role |
|-----|------|
| [0065 §6](0065-instrument-categories-domain-taxonomy.md#adr0065-p6) | `graph_kind` - *what* domain graph (orthogonal) |
| [0067](0067-graph-backed-surfaces-contract.md#adr0067-dimensions) | Edge/node measurement **provenance** |
| [0113 § axes](0113-hci-semantic-map-orientation-layer.md#adr0113-axes) | Three Axes and HCI Summary |
| [0039](0039-workspace-navigation-affordances.md) | Navigation workspace, related files |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid index, `hit_kind` |

## Summary

- **`relation_kind`** axis on the edges of the graph (“inherits”, partial peer, ...).
- Orthogonal to `graph_kind` and provenance ([0113](0113-hci-semantic-map-orientation-layer.md)).


## Problem

For graph-backed surfaces we already breed:

- **`graph_kind`** - form of a domain graph (intent map, related files, Git modules tree, ...);
- **`edge_provenance`** — connection calculation source (Roslyn, MSBuild workspace navigation, HCI FTS/vec, composite).

This is **not enough** for the user and the agent to equally understand the **meaning of the edge**: “successor”, “implements an interface”, “refers to a type”, “same partial”, “just appears in the text next to the name” - different **relations**, and mixing them in one nameless “edge exists” breaks trust in the map, MCP hints and auto-actions.

We need an explicit dictionary of **relationship type** on an edge (or on a node→node logical connection), independent of *who* built it and *in what* `graph_kind` screen it is shown.

---

## Solution

### 1. Concept: `relation_kind` (working name)

Introduce the axis **`relation_kind`** (or equivalent in the wire model/MCP): **what relation semantics** we assert between two entities (or a node and an external anchor).

**Invariant:** `relation_kind` answers the question **“what kind of connection is this in meaning?”**, and not “who calculated” (`edge_provenance`) and not “what kind of graph screen is this” (`graph_kind`).

<a id="adr0114-three-axes"></a>

### 2. Three axes (briefly)

| Axis | Question |
|----------|--------|
| **`graph_kind`** | What **domain** graph are we working in ([0065](0065-instrument-categories-domain-taxonomy.md#adr0065-p6)). |
| **`edge_provenance`** | **What model/index** is the relationship based on ([0113](0113-hci-semantic-map-orientation-layer.md#adr0113-axes), [0067](0067-graph-backed-surfaces-contract.md#adr0067-dimensions)). |
| **`relation_kind`** | **What relationship** between entities are we showing or passing to the MCP (**this ADR**). |

The three values ​​**do not mutually replace** each other: the same `relation_kind` (for example, “type reference”) can occur with different `edge_provenance` (Roslyn vs draft HCI candidate - then **different confidence weight**, but **same meaning of the label**, if we deliberately map HCI-hit to “candidate refers to”).

### 3. Draft directory `relation_kind` (extensible, non-exhaustive enum in v1)

The groups below are **guidelines for the contract**; the exact string identifiers and set are fixed when the field appears in the DTO/MCP.

**A. Symbolic relations (C# / Roslyn as a source of truth for meaning)**  
Examples of meanings (conditional names before codification):

- inheritance / implementation (`inherits`, `implements`);
- reference to a type or member (`references_type`, `references_member`);
- call (`calls`);
- namespace/assembly import (`imports`, `assembly_reference` - if reflected in the graph).

**B. Workspace navigation relationships (MSBuild heuristics / related presets)**  
Align with **[0039](0039-workspace-navigation-affordances.md)** and related types:

- "same type partial" (`partial_peer`);
- “project neighbor / peer” (`project_peer`);
- “test for code” (`tests_peer`);
- other types from the workspace navigation presets - as separate `relation_kind`, and not “just edge without a name”.

**C. Hull relations (HCI / FTS / vec)**  
It is important here to **not name** the relationship with symbolic names from group A if the source is text only:

- “textual match / name occurrence” (`textual_name_match`);
- “similar fragment by embedding” (`vector_similarity`);
- in an explicit "symbolic referenced-by candidate" scenario, a separate **candidate** layer marker (for example `candidate_symbol_reference`) if the product introduces a two-step **HCI → Roslyn** loop ([0113](0113-hci-semantic-map-orientation-layer.md)).
**D. Structural/non-code** (other `graph_kind`)  
For example, for `repository_module_tree`: `submodule_parent_of`, `contains_path` - **different dictionary**, but the same `relation_kind` axis in the sense of “what the edge means”.

### 4. Communication with `hit_kind` (0105)

The **`hit_kind`** field in the HCI response describes the **hit channel** in the index (`text_fts`, `text_vector`, ...), and **not** necessarily the relationship between two graph nodes. Mapping "hit → edge with `relation_kind`" is a separate layer (CCU/composer), where **`relation_kind`** is chosen deliberately (often group C), and not copied from `hit_kind`.

### 5. Contract implications

- In subgraph/MCP wire models, when an edge or logical relationship is passed, an explicit **`relation_kind`** field (or a nested `relationship semantics` object) is **recommended** unless the meaning is unambiguously inferred from the `graph_kind` + domain node types.
- Agent hints and "next step" (go-to-def, usages) should take into account **`relation_kind`**: for `textual_name_match` there is a different action default than for `references_member` with `symbolic_roslyn`.
- Full closed enum for all languages ​​and domains **not** the purpose of this ADR - only **axis** and draft directory; expansion as scenarios progress.

### 6. Not goals (v1 document)

- RDF/OWL level ontology for the entire IDE.
- Replacing existing node domain fields where they already uniquely encode the same relation - no duplication required, but when exporting to MCP the agent benefits from a **stable** projection into `relation_kind`.

---

## Link to previous edits 0113 / 0067

**[0113 §1a](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)** — table of three axes; the third line refers to this ADR.  
**[0067](0067-graph-backed-surfaces-contract.md#adr0067-dimensions)** - the **Relation kind** line with a link here has been added to the graph-backed surface dimensions table.

---

## Rollout (sketch)

1. Document (this ADR) as a common language for UI and MCP reviews.  
2. The first field in JSON/DTO for one tool (for example related-files or semantic map export) with a minimum set of `relation_kind`.  
3. Catalog expansion and alignment with Roslyn API / workspace presets - iteratively.
<!-- English translation of adr/0113-hci-semantic-map-orientation-layer.md. Canonical Russian: ../../adr/0113-hci-semantic-map-orientation-layer.md -->

# ADR 0113: HCI and Semantic Map - orientation layer (not graph)

**Status:** Proposed  
**Date:** 2026-05-14  
**Updated:** 2026-05-14 - axes `graph_kind`, provenance, `relation_kind` ([0114](0114-graph-edge-relation-kind-taxonomy.md)). Details - [§ History](#adr0113-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Semantic Map Border/Layer B; IDE integration |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid index, MCP |
| [0039](0039-workspace-navigation-affordances.md) | Map as a navigation surface |
| [0053](0053-semantic-map-control-flow-pfd.md) | Control flow on PFD |
| [0067](0067-graph-backed-surfaces-contract.md) | Contract graph-backed surfaces |
| [0065 §6](0065-instrument-categories-domain-taxonomy.md#adr0065-p6) | Axis `graph_kind` |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Semantics of `relation_kind` on edge |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | `SemanticMapInputSnapshot` in CCU |

## Problem

**[0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** mixes in one document: Core connectivity, orchestrator, freshness, DataBus, MFD INDEX - and a brief statement of the role of HCI next to **Semantic Map**. It's not uncommon for a reader to read this as "HCI enriches the **graph** maps", even though ADR is already contrasting layer B and the canonical graph; An explicit **orientation contract** (what exactly we show, what we don’t do, what DTOs) is missing in one place.

---

## Solution

### 1. The role of HCI regarding Semantic Map

**Hybrid Codebase Index (HCI)** in conjunction with Semantic Map provides only **codebase orientation layer**:

- top **text** (FTS) and when enabled - **vector** hits with paths, snippets and explicit **`hit_kind`** ([0105](0105-hybrid-codebase-index-for-csharp-web.md));
- **index metadata** (readiness, format version, scope) as the context of trust in the orientation string;
- optional in perspective - **input for declutter** map highlighting (filtering “noise” in the UI), **without** replacing the symbolic topology of the graph.

HCI **doesn't** source CFG edges, **doesn't** replace **Roslyn** for go-to-definition/symbolic connectivity, and **doesn't** mix `hit_kind` with graph facts.

<a id="adr0113-axes"></a>

### 1a. Three orthogonal axes (`graph_kind`, `edge_provenance`, `relation_kind`)

In order not to confuse **what graph we are drawing**, **what the edges are based on** and **what the edge means**, we keep the layout explicit (the third axis is **[0114](0114-graph-edge-relation-kind-taxonomy.md)**):

| Axis | Question | Where recorded | Examples |
|-----|--------|----------|---------|
| **Graph Type/Domain** (`graph_kind`) | What is the **semantic** subgraph in the cockpit: code intents, associated files, Git module tree, ... | **[0065 §6](0065-instrument-categories-domain-taxonomy.md#adr0065-p6)** | `code_intent_code_navigation_map`, `related_files`, `repository_module_tree` |
| **Provenance** (`edge_provenance`) | **Where** does the provability of “node A is connected to B” come from: symbolic model, workspace heuristics, full text / vec on the corpus, chain from several sources | **0113** + dimension in **[0067](0067-graph-backed-surfaces-contract.md#adr0067-dimensions)** | `symbolic_roslyn`, `workspace_navigation_msbuild`, `hci_fulltext`, `hci_vector`, `composite_hci_then_roslyn` |
| **Relationship type** (`relation_kind`) | **What is the relationship** between entities we show: inherits, refers to type, partial peer, text match, ... | **[0114](0114-graph-edge-relation-kind-taxonomy.md#adr0114-three-axes)** | `inherits`, `references_type`, `partial_peer`, `textual_name_match`, ... |

**Why:** the same `graph_kind` (for example, an intent map) can **visually** be adjacent to data of different origins: CFG/Roslyn - canon for control flow; HCI - fast **corpus** layer (“where the type name/fragment pops up in the repo” → draft **referenced-by by text**), then refinement via Roslyn. In this case, the **meaning** of the connection for the UX and the agent is set by **`relation_kind`** (for example `references_type` for Roslyn vs `textual_name_match` or `candidate_symbol_reference` for HCI), and not just `edge_provenance`. A separate "graph from HCI only" tool is still a separate solution and consistent with [0067](0067-graph-backed-surfaces-contract.md).

### 2. Product circuit “card + HCI”
- **PFD/MFD:** short **orientation string** (or separate micro-channel) consistent with the same scope and `databasePath` as the HCI orchestrator ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)).
- **Next step for the user/agent:** from the HCI hit - meaningful **Roslyn** level actions (definition, usages, diagnostics), as in the **“Hybrid search → Roslyn accuracy”** sketch in 0106 / roadmap 0105 - without declaring the HCI hit to be a “character truth”. The product lens **"quickly referenced-by body"** fits into **`hci_fulltext` / `hci_vector`** on the provenance axis; symbolic **referenced-by** — **`symbolic_roslyn`**; The **meaning** of the connection is marked with **`relation_kind`** ([0114](0114-graph-edge-relation-kind-taxonomy.md)), so as not to confuse `references_type` with `textual_name_match`.

### 3. DTO and CCU (target state)

Normalized input for graph-backed surfaces after stabilization of the CCU boundary - **`SemanticMapInputSnapshot`** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)): the composition of the fields (top hits, index version, query errors, declutter flags) is fixed separately during implementation; **0113** specifies layer semantics, not a replacement for 0097.

---

## We don’t (explicit non-goals)

- We do not declare Semantic Map nodes/edges “indexed by HCI” without a separate ADR and agreement with **[0067](0067-graph-backed-surfaces-contract.md)**.
- We do not replace the palette **`t:`/`m:`** with the full HCI without the policy from **[0112](0112-command-palette-query-modes-strategy.md)** (ripgrep, FTS and semantic are already added there).

---

## Consequences

- **[0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** remains the entry point for **HCI integration into CascadeIDE**; detail of the “card ↔ HCI” contract for readers and reviews - **0113**.
- Changes to the UI orientation string or `SemanticMapInputSnapshot` reference **0113** (and optionally 0097) rather than inflating 0106.

---

## Rollout (sketch)

1. Fix only **orientation** and references to scope/HCI errors in the code/UX - without mixing them into the Roslyn graph.  
2. When DTO is ready - one PR with `SemanticMapInputSnapshot` + CCU boundary tests; update this ADR to **Accepted · Implemented** for the relevant items.

---

## History of changes

<a id="adr0113-history"></a>

| Date | Change |
|------|-----------|
| 2026-05-14 | axes **`graph_kind`**, **provenance** and **`relation_kind`** ([0114](0114-graph-edge-relation-kind-taxonomy.md)); lens “quick referenced-by body”. |
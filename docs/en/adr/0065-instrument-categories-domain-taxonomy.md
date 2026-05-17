<!-- English translation of adr/0065-instrument-categories-domain-taxonomy.md. Canonical Russian: ../../adr/0065-instrument-categories-domain-taxonomy.md -->

# ADR 0065: Instrument categories and graph types (orthogonal to slot and `instrument_id`)

**Status:** Accepted  
**Date:** 2026-04-18  
**Updated:** 2026-04-22 — “Intent map” = intent subgraph around the anchor, not the entire solution map. Details - [§ History](#adr0065-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | `Instrument`, slot, composer, surface |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | Form of presentation vs composition in anchor |
| [0039](0039-workspace-navigation-affordances.md) | Navigation, MCP, subgraph |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map, control flow |
| [0056](0056-semantic-map-pipeline-adoption.md) | Pipeline adoption for map |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | GitMap/submodules |
| [0055](0055-skia-instrument-composition-pipeline.md) | Pipeline inside a Skia tool |
| [0067](0067-graph-backed-surfaces-contract.md) | Contract graph-backed; `graph_kind` inside family |
| [0113](0113-hci-semantic-map-orientation-layer.md) | Axis **provenance** vs `graph_kind` |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Axis `relation_kind` on edges |

## Summary

- **Tool categories** and **`graph_kind`** - the domain is orthogonal to the slot.
- “Intent map” = subgraph of **code intents** around the anchor, not the entire solution map.

---
## Context

In the discussion of the cockpit the levels were repeatedly mixed:

- **`slot_id`** (where in the attention zone - PFD, MFD, ...) - see [0047 §1](0047-cockpit-instrument-descriptor-and-slot-composition.md#adr0047-p1);
- **`instrument_id`** (which named choice to mount in the slot) - see [0047 §1–2](0047-cockpit-instrument-descriptor-and-slot-composition.md#adr0047-p1);
- **data** (navigation graph, CFG, git tree, CDS channel);
- **product names** (“Intent Map” in the menu, `workspace_navigation_map` in the code).

[0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) has already separated **tool** and **`Control`**, but **does not introduce** an explicit axis of "**what kind of meaning** does this tool have for the pilot and the agent". As a result, the same word (“workspace”, “intent map” / outdated *semantic map*) is read either as **data domain**, sometimes as **device id**, sometimes as **graph name**.

We need a **stable, narrow taxonomy of categories** - not replacing `instrument_id` and not duplicating [0063](0063-instrument-deck-named-composition-one-anchor.md) (there are different axes: `ContentRepresentation` vs deck).

---

## Solution

<a id="adr0065-p1"></a>

1. **Instrument Category** (`instrument_category` in a future descriptor extension, see [consequences](#adr0065-consequences)) is a **separate axis**, orthogonal:
   - **`slot_id`** (geography of attention),
   - **`instrument_id`** (stable name of the composer choice),
   - **two axes [0063](0063-instrument-deck-named-composition-one-anchor.md)** (page shape vs deck composition in the anchor).

   The category answers the question: **what class of human/agent tasks** does this tool serve (navigation through code, through files, repository topology, ...).

<a id="adr0065-p2"></a>

2. **"Intent map"** in the narrow product sense is a **semantic map of code intent** (what steps, branches and intentions in the flow of a method/code fragment are reflected on the map), **not** "any graph of semantic connections" in general and **not** a synonym for `Instrument` and **not** a place of rendering. The mapping is still through the tool (for example `workspace_navigation_map` in the PFD slot) and pipeline [0055](0055-skia-instrument-composition-pipeline.md). A common wire container (`CodeNavigationMapSubgraphDocument` and JSON MCP) can carry **different types of graphs** - see [item 6](#adr0065-p6).

<a id="adr0065-p3"></a>

3. **Canonical minimum categories** (string identifiers, expandable as product needs):
| Category | Meaning | Note |
   |-----------|--------|-----------|
   | `code_navigation` | Navigation and connections **in code** (symbols, calls, control flow, predicates; agent contract and MCP for code). | Domain **CodeNavigation**. |
   | `workspace_navigation` | Dependencies and connections **between files/artifacts** in the sense of **workspace structure** (decision tree, peer files, directories). | Not to be confused with the git topology below. |
   | `repository_topology` | **Repository topology** (submodules, boundaries of nested repo) as **tree** / GitMap - [0062](0062-git-submodules-semantic-map-subgraph.md). | Separate from `code_navigation` and from the narrow `workspace_navigation`. |

   Additional categories (e.g. CDS **channel** projection, **readout** environment, **deck** by [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)) are introduced **separate ADR** or additions to the table, so as not to bloat the minimum without a real second tool.

<a id="adr0065-p4"></a>

4. **Relationship with existing `instrument_id`:** for current identifiers (for example `workspace_navigation_map`) **historical name** may not coincide with the category from the table - this is normal: **`instrument_id` remains a stable contract**; the category adds **semantics for documentation, filters and agent**, rather than renaming id directly.

<a id="adr0065-p5"></a>

5. **Skia pipeline** ([0055](0055-skia-instrument-composition-pipeline.md)) remains **internal** to a specific instrument; **category** does not replace Intent / Declutter / Layout - it is about **data source domain**, and not about pipeline steps.

<a id="adr0065-p6"></a>

6. **Graph type** (`graph_kind`, working name; optional in the wire model and MCP) - the axis of **structure and semantics of the subgraph**, orthogonal to the **tool category** where the same JSON container carries different forms of the graph (for example, control flow vs star of related files). Minimum set for discrimination (extensible):

   | `graph_kind` | Meaning |
   |----------------|--------|
   | `code_intent_code_navigation_map` | **Semantic code intent map** - the narrow meaning of the product **“Intent map”**: what intentions and steps in the code flow are reflected on the map; displaying control flow on PFD - in the same context ([0053](0053-semantic-map-control-flow-pfd.md)). (The legacy name `code_intent_semantic_map` is still accepted in the parser.) |
   | `related_files` | A graph of **related files** (peer, test, XAML, etc.), not a "code intent map". |
   | `repository_module_tree` | **Tree of modules / submodules** ([0062](0062-git-submodules-semantic-map-subgraph.md)). |

   So far the distinction is partially **indirect** (map level `file` vs `controlFlow`, `kind` fields on nodes). **Recommendation:** When you next change the subgraph contract, add an explicit **`graph_kind`** field (or equivalent in MCP) so that the agent and UI do not infer the graph type from heuristics.

   The **`graph_kind`** axis answers the question **what is the domain graph** (subgraph form). It is **orthogonal** to the **origin of connections** axis (Roslyn vs MSBuild workspace navigation vs HCI FTS/vec and composites) - see **[ADR 0113 § axes](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)**. The third axis is **relationship type** (`relation_kind`: “inherits”, “refers to”, ...) - **[ADR 0114](0114-graph-edge-relation-kind-taxonomy.md)**.

<a id="adr0065-p7"></a>

7. Relationship **tool category** ↔ **graph type**: typically `code_navigation` ↔ `code_intent_code_navigation_map`; `workspace_navigation` ↔ `related_files`; `repository_topology` ↔ `repository_module_tree`. The correspondence **doesn't** have to be 1:1 (one tool can switch the mode and thus the graph type).

---

<a id="adr0065-consequences"></a>

## Consequences
- The instrument registry and **MCP documentation** can reference **`instrument_category`** for grouping ("show code navigation only") without reading the code of each `instrument_id`.
- **`CockpitInstrumentDescriptor`** (and related types) **recommended** to be expanded with an optional category field when a second instrument with a clearly different domain appears in the same slot - **without** necessarily migrating all ids to v1.
- The code of wire models (`CodeNavigationMapSubgraphDocument`, etc.) **does not have to** be compressed into one name without a prefix: scene/render types - `CodeNavigationMapSceneDrawing` / `CodeNavigationMapVisualTheme`, etc.; clarifying the meaning of the graph - through **`graph_kind`** and domain fields.
- MCP/JSON subgraph contract: add **`graph_kind`** (or equivalent) **in the same major line** as the `CockpitInstrumentDescriptor` category extension - by separate decision, with agent migration.

## Not goals (v1)

- Full enum of categories and graph types in the code for all modes - only after stabilization of the list and one real scenario of “two different `graph_kind` in one tool”.
- Merger with **`DeckPrimitiveKind`** ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)): types of **indicators** and **tool category** / **graph type** are different axes.

## Rejected alternatives

- **Only rename `instrument_id`** to the domain - breaks the stability of the contract and MCP without gaining orthogonality to the slot.
- **Relying only on comments in the code** is not enough for the agent and external documentation.

---

## History of changes

<a id="adr0065-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-22 | product in UI - **"Intent Map"**: a subgraph of **intents and flow around the anchor** (code snippet/method), **not** a map of the entire solution in the spirit of Code Map in VS; in a narrow sense, it coincides with the **code intent map**. Previously (2026-04-18): Introduced the **graph types** axis (`graph_kind`). 2026-05-14: The **`graph_kind`** axis is orthogonal to the **link origin axis** - **[0113 § axes](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)**. 2026-05-14: third axis **`relation_kind`** - **[0114](0114-graph-edge-relation-kind-taxonomy.md)**. |
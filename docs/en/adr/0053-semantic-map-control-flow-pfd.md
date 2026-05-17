# ADR 0053: Intent map and control flow on PFD (control flow)

**Status:** Accepted · Implemented  
**Date:** 2026-04-17

## Related ADRs

| ADR / artifact | Role |
|----------------|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD as instruments, attention zones |
| [0051](0051-intent-based-attention-routing-toml.md) | Attention routing |
| [0055](0055-skia-instrument-composition-pipeline.md) | Skia pipeline |
| [0056](0056-semantic-map-pipeline-adoption.md) | Pipeline adoption on the map |
| [0039](0039-workspace-navigation-affordances.md) | MCP, subgraph |
| [0065](0065-instrument-categories-domain-taxonomy.md) | Code intent map |
| `CodeNavigationMapSubgraph*` | Subgraph models in code |

## Summary

- **Intent map** on PFD: control flow, subgraph, KISS navigation around a code anchor.
- Shared Skia pipeline and cursor semantics ([0056](0056-semantic-map-pipeline-adoption.md)).
- Not to be confused with GitMap ([0062](0062-git-submodules-semantic-map-subgraph.md)) or the full solution tree.

---
## Context

The semantic map in the PFD zone today relies on a **dependency graph** (symbols, calls, links between artifacts). When editing **a single method** `A()`, it is useful to see not only “who connects to whom”, but also the **execution route** inside the method: conditional calls, shared tail after branches.

Example intent:

```csharp
void A() {
    if (cond) B();
    else C();
    D();
}
```

On the map (simplified): **B** on the conditional path, **C** on the alternate branch, **D** as a merge point / shared continuation — without turning the screen into a full CFG or a copy of editor text.

Aviation analogy: **waypoints** and transition conditions on a navigation display, not a printout of every procedural page.

---

## Goals

1. Visualize **control-flow intent** inside the selected method where it helps scanning (Flight / “thin” mode), not syntax for its own sake.
2. Preserve **KISS**: do not overload the PFD with condition text and decorations by default.
3. Prepare a **data contract** (subgraph / JSON / MCP) so edges and, when needed, nodes carry **link type** and optional hints for hover / drill-down.

## Non-goals (first phase)

- Full rendering of the **entire** Roslyn CFG on the mini-map.
- Duplicating the **body** of `if (…)` as a permanent label on the PFD.
- Visualizing every local assignment and small noise inside branches.

---

## Display principles (keep the UI clean)

1. **Semantic compression**  
   By default: a **branch icon** (diamond / “?”) or a **thin fork** on the edge, **without** long predicate text — for **`if` and for `switch` / pattern matching** alike (see [§ Switch…](#adr0053-switch)).  
   **Condition detail** — on gaze dwell / hover / a separate layer (Skia tooltip), optional.

2. **Only meaningful “external” route points**  
   Priority: calls to **other methods** and heavy operations the user scans for. Local noise (`i++`, empty `return` with no map meaning) — do not lift onto the graph without an explicit policy.

3. **Thin lines and metaphors**  
   - **Loop** (`for` / `while`): see [§ Loops: loop on the edge](#adr0053-loop-edge).  
   - **`try` / `catch`**: later — a “protected” segment style (e.g. “umbrella” / unstable node outline) once vocabulary is agreed.

4. **Vertical “flight plan”**  
   Meaningful step nodes top to bottom, branches **to the side**, without IDE-style blocks filling the screen.

<a id="adr0053-loop-edge"></a>

### Loops (for / while): loop on the edge

**We do not draw** a separate “loop graph” like a classic flowchart (diamond + back arrow duplicating nodes). **We draw a loop on the link line** to a meaningful step inside the method (e.g. call `B()` in a `for`/`while` body): the line to the node is **not straight** but makes an **elegant coil** around the direction toward that node — in spirit like a **holding orbit** on a nav display, not “yet another rectangle”.

User meaning: **immediately obvious** — “this call spins here”, without reading code in Forward and without unrolling iterations on the map.

**Render (direction):** thin **neon** lines on the dark PFD background, **smooth curves** (Bezier / cubic splines in Skia), visually part of the **cockpit** layer, not an old flowchart. The central line is the conditional “main flow” of method `A()`; the coil is **on the edge** to a dependent step, not a decorative layer over the whole graph.

**Optional (when analysis allows a heuristic):** loop “weight” or expected iteration count — **coil density** (tighter spiral, slightly brighter/thicker line, etc.), without promising exact `n` on the PFD.

**Contract:** an edge with loop semantics carries a flag like `Loop` / `LoopCall` plus optional style metadata (see table below); coil coordinates are **derived from layout** (StarGraph / force / other engine) so curves do not cover neighboring nodes — Bezier control-point tuning is a separate task.

<a id="adr0053-switch"></a>

### Switch, `case`, and pattern matching

**Include** in the same semantics as `if` / `else`: this is flow branching, not a “special case off the map”. **Do not expand** all branches as a full table on the PFD.

- **Few meaningful outcomes (guide 2–4):** fan of edges from one fork point to meaningful steps; **`case` / guard** labels — optional, on hover or a second layer.
- **Many branches or empty-only cases:** **compression** — one multi-way branch node/icon; outward only calls that matter; the rest — aggregate (“other branches” / `default`) or hidden until drill-down.
- **Pattern matching** (`switch` expression, `when`): do not duplicate long expressions on permanent labels; same principle as for `if` predicates.

Contract link: edge type like **`MultiBranch`** / **`ConditionalCall`** from the common fork ancestor to a step; branch detail in optional metadata.

---

## Data and contract (direction)

Existing subgraph models (`CodeNavigationMapSubgraphNode`, `CodeNavigationMapSubgraphEdge`) are extended meaningfully, for example:

| Idea | Purpose |
|------|---------|
| Edge type | `Call`, `ConditionalCall`, `Merge`, `MultiBranch` (several outcomes: `switch`, `if` chain, pattern matching), `Loop` / `LoopCall` (loop on edge to a step inside a loop; not to be confused with low-level CFG `LoopBack` if needed separately) |
| Short label | Optional; do not duplicate full `cond` text. |
| Condition detail | Optional, for tooltip / second layer; may be compressed or deferred. |

Exact field names and JSON — **to be fixed** after a generator prototype (Roslyn / Control Flow Analysis + call filtering).

Analysis source in the stack: **Roslyn** (`ControlFlowAnalysis` and binding calls to branches), without mandatory reliance on text-only grep.

---

## Agreements (draft)

### Predicate on edge vs icon only

The boundary between “show a **short predicate**” and “**icon only**” is set by **user settings** (app / workspace — specific key and merge with bundle to be fixed at adoption), **not only** by a hard tie to a UI mode like Flight.

The **agent** path (MCP, context / navigation subgraph request) uses the **same** detail level: in product terms the agent is also a **user** of this view and does not live in a separate hidden mode bypassing settings, except with an **explicit** call-parameter override (one-request override).

### Map type: `controlFlow` vs “classic”

A separate **UiMode** preset (e.g. Flight only) **is not required** for a single CF-aware map: **which semantic map view to build** is the **user’s** choice, orthogonal to cockpit mode. **Flight** remains the PFD layout polygon; **map level/type** is set separately.

Draft TOML shape (values and merge with bundle / workspace — at adoption):

```toml
[semantic_map]
level = "controlFlow"   # control flow inside a method (CF-aware subgraph)
# level = "file"        # classic dependency / file slice (baseline intent map)
```

The same switch should be **reachable by the agent** (field in MCP / echo in subgraph request), without separate hidden semantics.

### Subgraph JSON versioning (MCP / CLI)

**Backward compatibility when adding fields is not yet guaranteed:** the product **has no users yet**; subgraph JSON and MCP/CLI may **change shape in sync with code** without a migration policy. When stable consumers appear outside the repo (integrations, long-lived agent contract), introduce explicit **schema versioning** and compatibility rules **in a separate decision** (ADR / MCP-PROTOCOL add-on).

---

## Next steps (draft)

1. Fix the minimum set of edge **kinds** and call-filtering rules.
2. Prototype: one method under the cursor → simplified flow → the same JSON the intent map on the PFD already consumes.
3. Render: line and node styles on PFD / Skia without mandatory condition text on permanent labels.

## Backlog intent (add incrementally)

To keep the map intent-focused and avoid turning into a full CFG, introduce extensions in portions:

1. **Early/abrupt exits**: `ThrowExit`, `Break`, `Continue`.
2. **Async boundaries**: `AwaitBoundary` (pause/resume point of the flow).
3. **Short circuit**: `ShortCircuit` for `&&` / `||` in guard conditions.
4. **Pattern guard**: explicit semantics for `when` in pattern matching / switch.
5. **Exceptions**: coarse `ExceptionFlow` for `catch`/`finally` without expanding full exception-CFG.
6. **Iterators**: `YieldExit` (`yield return` / `yield break`) as a separate exit type.
7. **Detail policy**: separate “operator noise” and “external steps” via declutter policy (e.g. helper calls inside arguments).

*(Sections below may be added: JSON examples, screenshots, performance limits, links to external discussions.)*

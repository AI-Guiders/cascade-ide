<!-- English translation of adr/0062-git-submodules-semantic-map-subgraph.md. Canonical Russian: ../../adr/0062-git-submodules-semantic-map-subgraph.md -->

# ADR 0062: GitMap - a map of git boundaries (submodules) separate from the workspace navigation context

**Status:** Proposed - draft for discussion; implementation is not fixed.  
**Date:** 2026-04-17  
**Updated:** 2026-04-17 - Fixed **intentional split** with Semantic Map / WSNC (see [Why not WSNC](#adr0062-not-wsnc)). Details - [§ History](#adr0062-history).

## Related ADRs

| ADR/document | Role |
|----------------|------|
| [0039](0039-workspace-navigation-affordances.md) | WSNC - Code Navigation (**not** GitMap) |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | General Git core |
| [0055](0055-skia-instrument-composition-pipeline.md) | General rendering pipeline |
| [0056](0056-semantic-map-pipeline-adoption.md) | Adoption pipeline in the map |
| [0067](0067-graph-backed-surfaces-contract.md) | GitMap as graph-backed surface |
| [`git-and-submodules-v1.md`](../../git-and-submodules-v1.md) | Product circuit |

## Summary

- **GitMap:** submodules and git boundaries **separate** from WSNC/intent map.
- General Skia pipeline; own contract/MCP.


<a id="adr0062-not-wsnc"></a>

## Why not mix with workspace navigation context (WSNC)

**Meaning solution (discussion → committed to draft):** visualization submodules **not** is an extension of `get_code_navigation_context` / `CodeNavigationContextBuilder` and **not** built into Semantic Map as another `kind` in the same JSON.

Reasons:

1. **WSNC about the solution and code.** The current contract is tied to the tree **`.sln` / `.slnx`**, project files and heuristics **source architecture** (partial, project, namespace, directory, control flow - see [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md)). This is the layer **“where am I in the code and what is nearby in terms of development”**.

2. **Submodules are about git topology, not about software design.** The repository boundary is an artifact of **history and VCS modules** (gitlink, `.gitmodules`, checkout). It relates indirectly to the application architecture; mix with the graph of “code neighbors” - mix **two axes of meaning** (code vs repository).

3. **Productive:** a separate mode/tool ​​is easier to explain to the user and agent than the `include_kinds` presets, in which half the nodes are “about git”, half are “about files”.

**WSNC naming:** the wording “workspace navigation” today intersects with the git map in the reader’s head; possible **renaming** of the MCP contract/command towards “solution graph” / “code navigation context” - **separate** solution (not a blocker for this ADR), see [open questions](#adr0062-open).

<a id="adr0062-gitmap"></a>

## GitMap - separate surface, common pipeline

**Working name:** **GitMap** - mini-map (and optional list) of **git neighbors**: parent repository, nested submodules, "contains/nested" edges.

**Reuse:** same **Skia pipeline** composition as Semantic Map ([0055](0055-skia-instrument-composition-pipeline.md), [0056](0056-semantic-map-pipeline-adoption.md)): Intent → (optional) Declutter → Layout → Render - with **another** graph source (git metadata, not `CodeNavigationContextBuilder`) and, if necessary, **another** tool/slot in the PFD ([0047](0047-cockpit-instrument-descriptor-and-slot-composition.md)) so as not to mix the "intent map" and the "repository map" in the same widget.

The semantic map (**Semantic Map**) remains about **code and task**; **GitMap** - about **where in the git tree I am and what the neighboring repositories are**.

---
## Task context

In monorepositories, some paths lead to **separate git worktrees** (submodule). The user and agent should see the **repository boundary** without necessarily opening the Git UI - in accordance with [git-and-submodules-v1.md](../../git-and-submodules-v1.md).

## Data source (open)

| Approach | Pros | Cons |
|--------|--------|--------|
| Parsing `.gitmodules` + paths from worktree root | Without the required `git` in the hot path | Consistency with actual `.git` in submodule |
| `git submodule status` (GitMcp.Core / [0019](0019-shared-git-core-ide-and-git-mcp.md)) | Commit, dirt, discrepancies | Process, cache, threads |
| Hybrid | Richer signatures | Two sources |

Root for binding: **git worktree**, in which the workspace is open (the multi-root issue is separate).

## GitMap Data Contract (draft, not WSNC)

Separate graph description (nodes/edges/labels), **not** payload `related` / `subgraph` extension from [0039](0039-workspace-navigation-affordances.md):
- Nodes: at least **root of the current repo**, **submodule roots** (directory path), optional status labels (dirt, lag) - as data is available.
- Edges: “parent - submodule”, when nested - a chain.
- MCP: separate **`get_git_map_context`** level command (name debated) or equivalent in the git IDE layer - **not** parameters to `get_code_navigation_context`.

**code level** presets (`[code_navigation]` in `settings.toml`) **do not apply** to GitMap; GitMap has its own limits and settings (or defaults in the code before TOML appeared).

## Open questions

<a id="adr0062-open"></a>

1. PFD slot: **tab** next to Semantic Map, **separate instrument_id**, or mode switch inside one slot - which is cheaper in terms of attention ([0021](0021-pfd-mfd-cockpit-attention-model.md))?
2. Nested submodules: depth and caps.
3. Click on the node: reveal in explorer, open the path, change the session root - MVP.
4. Renaming public name **WSNC** / MCP command for clarity "solution/code graph".
5. Agent parity: separate tool in the contract vs section in `get_ide_state`.

## Not goals (yet)

- Replacement of **full Git UI** (diff, commit, sync submodule) - GitMap **navigation** tooltip, not replacement of git panels.
- Full graph of **all** remotes and update policies.
- Mixing "file from WSNC" and "submodule" nodes in **single** JSON response without explicit layer (rejected; see [Why not WSNC](#adr0062-not-wsnc)).

## Rejected alternatives

- **Extend only Semantic Map** with new `kind` in the same subgraph - rejected: different axes of meaning, see above.
- **List only without graph** - acceptable as a GitMap degradation mode, not as the only type.

---

*After approval: update the status, describe the MCP/git layer and tool slot; implementation.*

---

## History of changes

<a id="adr0062-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-17 | fixed **intentional split** with Semantic Map / WSNC (see [Why not WSNC](#adr0062-not-wsnc)). |
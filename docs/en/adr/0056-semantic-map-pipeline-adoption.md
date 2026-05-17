<!-- English translation of adr/0056-semantic-map-pipeline-adoption.md. Canonical Russian: ../../adr/0056-semantic-map-pipeline-adoption.md -->

# ADR 0056: Semantic Map adoption of Skia composition pipeline

**Status:** Accepted · Implemented  
**Date:** 2026-04-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0053](0053-semantic-map-control-flow-pfd.md) | controlFlow intent, layout |
| [0055](0055-skia-instrument-composition-pipeline.md) | General Skia pipeline |
| [0067](0067-graph-backed-surfaces-contract.md) | Contract graph-backed surface |

---
## Context

After implementing `controlFlow` for Semantic Map it turned out:

1. Layout and density policy were partially spread between the VM/control/engine.
2. On the compact PFD viewport, the nodes stuck together, route reading deteriorated.
3. MCP behavior without explicit `line/column` required a predictable fallback to the current cursor.

---

## Solution

<a id="adr0056-p1"></a>
### 1) Translate Semantic Map to a separate composer tool

Introduced `CodeNavigationMapCompositor` as the first domain adapter of the general approach from ADR 0055:

- selection of layout by `semantic_map.level`;
- calculating the recommended viewport height for readability;
- return scene + display parameters.

<a id="adr0056-p2"></a>
### 2) Explicitly split file/controlFlow composition

- `file` uses a compact layout (historical star layout);
- `controlFlow` uses a vertical flight-plan layout with a large step between levels.

<a id="adr0056-p3"></a>
### 3) Preserve the semantics of the "under the cursor" method

For `controlFlow`:

- without a valid position, do not fallback to the “first file method”;
- if `line/column` is not sent via MCP, substitute the current carriage position;
- if there is no position and caret, return an empty subgraph rather than false content.

---

## Consequences

### Pros

- Semantic Map became the first validated consumer of the common pipeline.
- The "instrument internals vs host surface" border has become transparent in the code.
- Improved readability of controlFlow in a narrow viewport.

### Cons

- An additional layer of composition and new tests have appeared.
- Declutter-policy is still minimal and requires further evolution.

---

## Implementation (slice)

- `Services/Navigation/CodeNavigationMapCompositor.cs`
- `Services/Navigation/ICodeNavigationMapCompositor.cs`
- `ViewModels/MainWindowViewModel.WorkspaceNavigationMap.cs`
- `Views/WorkspaceNavigationMapView.axaml`
- `Services/Navigation/CodeNavigationMapControlFlowGraphLayoutEngine.cs`
- `Services/CodeNavigation/CodeNavigationControlFlowSubgraphBuilder.cs`

---

## Tests

- `CodeNavigationMapCompositorTests`
- `CodeNavigationMapControlFlowGraphLayoutEngineTests`
- `CodeNavigationControlFlowSubgraphBuilderTests`
- `CodeNavigationControlFlowMcpCursorFallbackTests`
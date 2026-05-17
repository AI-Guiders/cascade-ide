<!-- English translation of adr/0057-chat-surface-pipeline-adoption.md. Canonical Russian: ../../adr/0057-chat-surface-pipeline-adoption.md -->

# ADR 0057: Chat surface adoption of Skia composition pipeline

**Status:** Accepted · Implemented  
**Date:** 2026-04-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | refinement packages, threading |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | early hypothesis host/render split |
| [0055](0055-skia-instrument-composition-pipeline.md) | shared pipeline |
| [0056](0056-semantic-map-pipeline-adoption.md) | first implemented consumer |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | overview/detail layout and keyboard-first intent for themes over pipeline |

---
## Context

After implementing the pipeline in Semantic Map ([0056](0056-semantic-map-pipeline-adoption.md)), the next planned consumer is the chat surface:

- threads,
- confirmations,
- prioritization and declutter in high-density event mode.

The chat in `MfdShellView` remains a product MFD surface, but the canonical surface is now built around the Skia pipeline snapshot, and not around the Avalonia list/tree.  
The new Skia layer is not needed for “replacement for the sake of replacement”, but for scenarios where the linear tape does not provide sufficient situational readability and does not show the width of the branches.

---

## Solution

<a id="adr0057-p1"></a>

### 1) Accept chat surface as the next pipeline-consumer

The chat is being transferred to the same composition approach from [0055](0055-skia-instrument-composition-pipeline.md):

1. **Intent**: building a model of the current state of the dialogue (threads, pending confirmations, active branches).
2. **Declutter**: message/acknowledgment prioritization and compression of noise elements.
3. **Layout**: layout of conversation nodes and confirmation cards.
4. **Render**: Skia-rendering of the scene.

<a id="adr0057-p2"></a>

### 2) Fix single product path via Skia surface

After the snapshot composer appears:

- `ChatPanelView` remains a host container and an input form, but not an alternative feed;
- product rendering of the chat goes through a single Skia surface;
- legacy Avalonia list-path is not considered a mandatory fallback.

Avalonia remains a shell/host layer, and not a parallel product implementation of the chat scene.

<a id="adr0057-p3"></a>

### 3) Select chat intent units as first-class

Minimum set of domain entities for v1:

- `ThreadNode`,
- `MessageNode`,
- `ConfirmationNode`,
- `DecisionEdge` (`ask`, `confirm`, `resolve`, `supersede`).

The layout layer should not evaluate these entities from the UI tree; it gets them from the Intent-stage.

---

## Consequences

### Pros

- Chat becomes consistent with the general model of Skia tools (0055).
- Threads/confirmations get a clearly controlled composition rather than a "flat tape with crutches".
- Reuse pipeline practices already tested on Semantic Map.

### Cons

- The complexity of the chat subsystem is increasing.
- It is necessary to keep strict snapshot/contract tests because the surface is no longer duplicated by the second UI path.

---

## Non-targets

- Do not return the parallel Avalonia list-path as a "safety" baseline without a new ADR.
- Do not record the final visual language here (colors, typography, animations) - these are separate UX iterations.
- Do not change chat MCP contracts in this ADR without a separate contract fixation.

---

## Implementation plan (minimum)

1. Introduce the `ChatSurfaceCompositor` framework and stage contracts (`Intent/Declutter/Layout`) in the 0055 style.
2. Raise the intent layer: `ThreadNode` / `MessageNode` / `ConfirmationNode` / `DecisionEdge` on top of the canonical dialogue history.
3. Connect `ClarificationBatch` / `ClarificationResponse` to real chat flow and MCP entrypoints, without string collapse as the only truth.
4. Add snapshot/contract composition tests and threading/clarification scripts.
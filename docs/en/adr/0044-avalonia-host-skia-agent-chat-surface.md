# ADR 0044: Role split — Avalonia as host (“fuselage”), custom rendering for agent chat (Skia as hypothesis)

**Status:** Accepted · Implemented  
**Date:** 2026-04-13  
**Updated:** 2026-04-13 — iteration order: **model first**, UI and render spike follow. Details — [§ History](#adr0044-history).  

## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | clarification batches, beyond linear feed |
| [0039](0039-workspace-navigation-affordances.md) | Semantic Map; `ILayoutEngine`, Skia in prospect |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | MFD zone, `MfdShellView` |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | agent facade |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport |

### Outside ADR

| Document | Role |
|----------|------|
| [north-star — moving from Cursor](../../design/north-star-cursor-mcp-cascade-workbench-v1.md) | reduce friction |

---
## Context

The product goal to **stop relying on Cursor for chat** requires agent conversation **inside CascadeIDE** to be **comfortable and scannable** — otherwise migration friction stays high regardless of editor and MCP strength.

In parallel an architectural frame is discussed: **Avalonia** as a reliable **host** (window, DPI, input, monitors, `TopLevel` lifecycle, shared cockpit frame), not the sole carrier of **all** complex visualization forever. For surfaces like **non-linear** chat (branches, clarification-batch overview, dense structure instead of one feed — see [0031](0031-agent-chat-clarification-batches-and-threading.md)) and related context “maps”, a **separate render layer** is reasonable; **Skia** is a testable candidate (including alignment with [Semantic Map / layout](0039-workspace-navigation-affordances.md)).

<a id="adr0044-p1"></a>

## Solution (direction, no fixed schedule)

1. **Explicit role split:** **Avalonia** layer — fuselage: zone placement, standard controls where enough, integration with the rest of the cabin. **Custom render** layer (target candidate — **Skia**) — “instruments” in chat/agent zone and aligned surfaces, **without** promising to rewrite the whole IDE in Skia.

2. **Model first, UI not required to start:** first — **canonical model** of dialog flow and session (events, entities, clarification batches, stable IDs, artifact bindings — direction of [0031](0031-agent-chat-clarification-batches-and-threading.md)). It can be designed and verified **without** new UI (tests, serialization, scenarios). With a clean layer split, **sketching presentation on top** is easier than inferring structure from drawn UI.

3. **Render spike** (including Skia) — **after** or **in parallel with** model draft, on **synthetic** data or on the model when contours are clear; goal — host/render risks, not guessing domain shape from pixels.

4. **Model and pixels are orthogonal:** whether Skia or Avalonia controls in the chat zone, that **does not** cancel model requirements; this ADR fixes **host/render responsibility split**, not a single library.

<a id="adr0044-p2"></a>

## Consequences

- Possible **two UI styles** (Avalonia and Skia island) — need **boundaries**, shared color/type tokens and discipline so style and bugs do not diverge.
- Scale chat scenario **after** spike conclusions, not before.
- Early **model autotests** (no UI) lower cost of changing presentation.

## Rejected alternatives (at this stage)

- **Treat chat quality as secondary** to the editor — rejected for the goal “live in CIDE without Cursor for conversation”.
- **Rewrite all chat in Skia in one step** — rejected without measurable spike and data model.
- **Start from render layer without model canon** — rejected: risk cementing wrong structure in graphics.

---

## Change history

<a id="adr0044-history"></a>

| Date | Change |
|------|--------|
| 2026-04-13 | iteration order: **model first**, UI and render spike follow. |

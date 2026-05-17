<!-- English translation of adr/0084-agent-edits-editor-source-of-truth-presence-channel.md. Canonical Russian: ../../adr/0084-agent-edits-editor-source-of-truth-presence-channel.md -->

# ADR 0084: Agent edits — editor as sole text source of truth; chat for intent/status; presence layer (GDocs-like, no mandatory CRDT)

**Status:** Proposed  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | agent chat, surface, threads |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | agent facade, tools |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP, MCP, tool parity |
| [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | preview / review before apply — orthogonal, may align on “ghost” policy |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom as channel; team-product scale — outside baseline |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | single IDE ↔ MCP process — good base for one truth buffer |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | “collaborative editing” metaphor for activity banner — see file-level feedback table |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDE overlays — possible presence surface |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | Presence/Activity as product language |

## Summary

- **Editor** — sole source of truth for text; chat — intent/status.
- Presence layer (cursor, “typing”); diff in chat is not primary.

---

<a id="adr0084-context"></a>

## Context

Typical “chat + agent” UX shows code edits **in a side panel** as diffs, patches, or long quotes. In a full IDE the user already looks at the **editor** and file system: duplicating the same text in chat **wastes LLM context** and **breaks** the flow “I look at code,” not at conversation.

The **collaborative editing** image (cursors, “typing”, selection) is useful as a **UI metaphor**: not necessarily full multi-user CRDT like Google Docs; enough **one-way streaming of agent state** and **local application** of edits to the buffer.

**Personas and conflict frequency:** a role that **mostly reads and steers** (discussion, “agent team” lead, little daily typing in the same buffer) **lowers frequency** of “I type where the agent types.” That **does not cancel** the norm below: the same person may type again; **other people**, **branch merge**, **second agent**, or a one-off file edit still matter. IDE must behave predictably under **concurrent** buffer change — canon is not weakened for one persona.

---

<a id="adr0084-problem"></a>

## Problem

1. **Two “screens of truth”:** edit text in chat and in file — attention conflict and desync with local edits.
2. **Assistant context:** repeating diff in chat spends window on what is already on disk/in buffer.
3. **Speed and trust:** primary way to “see the edit” should be where the user already works — editor, with clear active zone and apply policy (below).

---

<a id="adr0084-decision"></a>

## Decision (principles, stack-agnostic)

<a id="adr0084-editor-source-of-truth"></a>

### 1. Single source of truth — document in editor

- Final **visible code text** (and applied edits) lives in **editor buffer** and on save — in file.
- Agent changes modeled as **buffer operations** or **positioned edit stream**, not a “patch picture” the user must trust without opening the file.
- Edit tools (`ide_*`, MCP contour) **bind to open documents** and editor visualization, not mandatory duplicate in chat.

<a id="adr0084-chat-intent-status"></a>

### 2. Chat — intent and status channel

- Chat carries **short messages**, commands, clarifications, status (“did X”, “need choice”), file/range links — **not** the primary way to read a large diff.
- Diff in chat allowed as **optional collapsed log** (audit, copy-paste, external host without IDE) — **not** product default “where to review edits.”

<a id="adr0084-presence-layer"></a>

### 3. Presence layer (separate data channel)

Stream orthogonal to file text:

- agent **cursor / selection** in buffer;
- **typing** indicator;
- optional **name / role** (agent, scenario).

Does **not** require “real” Google Docs CRDT for two people: enough **streaming from agent** + **local projection** in editor overlay/decorations.

<a id="adr0084-preview-vs-live"></a>

### 4. Apply policy: preview vs live

Product choice (may combine by trust mode / path):

- **Ghost / preview** → explicit **Accept**; or
- **Live** with strong **undo** and buffer history.

Ties to pre-flight / review — [0042](0042-pre-flight-planned-changes-and-review-before-apply.md); this ADR does not replace it; aligns on “where user sees intent.”

<a id="adr0084-safety-controls"></a>

### 5. Safety and control

- Visible edits **in same window** as working code reduce “surprises” vs chat-only stream.
- Still need: **stop**, **rollback**, **read-only** for sensitive paths, trust policy — orthogonal to placement ([0071](0071-ai-assistance-sovereignty-locality-invisibility.md)).

---

<a id="adr0084-risks"></a>

## Risks and nuances

| Risk | Mitigation direction |
|------|----------------------|
| **Conflict** with simultaneous human edit | Lock region, explicit **pause agent**, merge policy, or “agent lost” with realign |
| Frequent **review-only** (little typing in buffer) | Does not remove row above: policy needed for active typing, other participants, two authors |
| **Large files** | Stream **deltas** and/or **visible region**; avoid full-file redraw per token |
| **Multi-tab** | Explicit **target buffer / file** per agent session; highlight “agent in *this* tab” |
| **External host (ACP without local editor)** | Fallback: compact diff in chat or separate viewer — does not cancel canon for full IDE |

---

<a id="adr0084-rejected"></a>

## Rejected and boundaries

- **Entire diff only in chat** as primary IDE UX for “I look at code” — rejected (allowed as option/audit).
- **Mandatory full CRDT** for two people in v1 — not required; real multi-user editing — separate decision atop this ADR.
- **Duplicate chat norm** from [0031](0031-agent-chat-clarification-batches-and-threading.md) — not goal; this ADR fixes **screen role split** (chat vs editor).

---

<a id="adr0084-implementation-roadmap"></a>

## Implementation status and next step

Unified product “presence + editor only” may not exist yet; this is **UX architecture guidance**. Next: scenarios (live vs preview), events (**cursor**, **edit**, **save**, **typing**), table “what goes to chat vs editor/overlay only”, tie to MCP commands / IDS layers on acceptance.

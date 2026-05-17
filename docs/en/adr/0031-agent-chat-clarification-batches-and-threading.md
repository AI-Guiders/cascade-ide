# ADR 0031: Agent chat — clarification batches, answers beyond yes/no, threads (direction)

**Status:** Proposed (direction draft until chat UI rework; protocol and screen details — iteration by iteration)  
**Date:** 2026-04-12  
**Updated:** 2026-04-19 — chat surface on pipeline snapshot (`ChatSurfaceCompositor`). Details — [§ History](#adr0031-history).  
**Updated (earlier):** 2026-04-12 — “topic → subtopics”, **session scope overview on one screen** vs deep scroll; mix of subtasks in one session (ADR + branches) as the norm.

## Related ADRs

| ADR | Role |
|-----|------|
| [0016](0016-agent-client-protocol-external-agent.md) | ACP / external agent |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | MFD zone, `MfdShellView`, second window |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention model; chat in secondary loop |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | response layers and traces |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | contracts and parity when extending |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Avalonia / chat render layer split; Skia as hypothesis |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | topic cards UX / drill-in/back and intent navigation by topic |

---
## Context

In agent chat history it is **natural** for a single user or agent “turn” to contain **several questions or sub-items** — not an anomaly, but normal dialog density.

External products (including modes like Cursor **Plan**) sometimes show a **list of clarifying questions** before building a plan, but the answer is often reduced to **one short line** or rigid **yes/no**. In practice the answer may be:

- **more than binary** (caveats, options, file reference, priority);
- **linked to other list items** (answering one changes the meaning of the next).

A linear message feed **fits poorly** with the reasoning object “**question batch → structured answers**”. Current Cascade chat UI (`SecondaryShellPage.Chat`, MFD zone) is due for **rework**; this ADR sets **direction**, not final layout.

**Observation (external experience, e.g. Cursor):** to recall **what was already decided on clarifications**, users often **scroll far back** — the dialog structure exists in their head but is **dissolved in chronology** in the UI.

**Real sessions** split into **topic and subtopics**: editing one ADR, **along the way** branching to a side idea and a **new ADR**, then returning to the original line. That is a **normal** “play”, not process failure. Product hypothesis: chat may **show that full scope on one screen** (overview, map, table of active branches — exact form not fixed), while **shared context** for agent and human stays **single** (as today in spirit: one session, one context pass to the model — implementation details are not mixed with “two truths”).

## Problem

1. **One input for the whole clarification batch** does not scale to non-binary and multi-line answers.
2. **Role confusion:** batch clarifications for a plan/task are not the same as a one-off **confirmation** of a dangerous action (see PFD and `request_confirmation` in [0017](0017-multi-window-workspace-and-agent-surfaces.md)); they must not share one control.
3. Without **explicit structure** on the client, reproducible tests and human/agent parity are hard ([0008](0008-mcp-contracts-and-testable-infrastructure.md)).
4. **Disorientation on a long feed:** without a **snapshot of the active topic / clarification batch**, the user must **scroll deeply** to restore “what we already answered”.

## Solution (principles)

1. **Clarification batch** — a meaningful UI unit and, when agreed with transport, protocol unit: a set of items with **stable identifiers** inside the batch so answers are sent as **structure** (`id → text or chosen value`), not free parsing of one string.

2. **UI representation (target):**
   - **cards or blocks per item** with a **multi-line** answer field where needed;
   - optional **answer type** per item (short text / choice / yes–no);
   - optional **one shared field** in addition to items (“what I want overall”) when the scenario warrants it.

3. **Threads / topics** — a separate axis: not reply-thread for its own sake, but a map of durable work lines. `ThreadNode` represents a topic / branch / separate investigation; ordinary steps within a line are `MessageNode`. For **one** question batch it is usually enough to stay in the current branch; `ClarificationBatch` does not create a new branch automatically.

4. **Transport (ACP etc.):** extending message/tool contracts for structured batches is **in scope** with UI delivery, per [0008](0008-mcp-contracts-and-testable-infrastructure.md). Canonical truth should live in structured event flow (`ClarificationBatchOpened`, `ClarificationAnswerSubmitted`) and MCP entrypoints, not only in one collapsed string.

5. **Zone linkage:** main chat stays in the **secondary loop / MFD** ([0021](0021-pfd-mfd-cockpit-attention-model.md)); second monitor — [0017](0017-multi-window-workspace-and-agent-surfaces.md). Critical confirmations needing PFD attention — per [0017 §6](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p6), not via disguised “chat yes/no”.

6. **Scope overview (direction, not layout):** beside or instead of “feed only” — **topic → subtopics / open questions / decisions made**, fitting **session breadth** **in view** so dozens of screens need not be scrolled to restore clarification context. Mixing **several work lines** in one session (e.g. ADR edit and en route a separate ADR for a branch) appears as **branching**, not lost noise. **Single context** for model and user is preserved: overview is a **projection** of the same history, not a second inconsistent channel (details when designing dialog storage).

## Rejected alternatives (as sufficient target state)

- **Only one answer line** for the whole clarification list — rejected as systematically weak for non-binary answers and linked questions.
- **One unstructured thread**, relying on natural-language parsing to extract per-item answers — rejected as the main path for plan clarification batches (may remain fallback for compatibility).

## Open questions

- Minimal **v1**: visual improvement only (N manual fields in UI) vs **immediate** agreement on agent message format.
- Whether **draft** answers are needed (persist incomplete batch when switching tab/window).
- **Thread** boundaries: when to open a sub-thread for a plan item vs keep in main feed.
- **Scope overview:** separate panel (outline, graph, “map”) vs inline **anchors** in the feed; how not to duplicate source of truth with the model transcript.
- How to **automatically** suggest a “branch” on divergence (new ADR en route) without breaking perception of one topic.

## Consequences

- Rework of **Chat** surface and dialog state VM around canonical snapshot, not a direct Avalonia feed.
- UI tests and agent scenarios with **structured** clarification batch body.
- User documentation: how to answer a question batch, how it differs from a normal message and from safety confirmations.

## Status after acceptance

Once agreed: status **Accepted**, link from [concept-to-implementation-map-v1.md](../ui-ux/concept-to-implementation-map-v1.md) if needed, and navigator update in [architecture-policy.md](../../architecture-policy.md).

---

## Change history

<a id="adr0031-history"></a>

| Date | Change |
|------|--------|
| 2026-04-13 | v0 domain model for clarification batches and validation in code: `Models/AgentChat/` (`ClarificationBatch`, `ClarificationItem`, `ClarificationResponse`, `ClarificationBatchValidation`); chat UI not wired yet. |
| 2026-04-19 | chat surface moved to pipeline snapshot (`ChatSurfaceCompositor`: `Intent -> Declutter -> Layout -> Render`), Skia fixed as single product path; `ClarificationBatch` / `ClarificationResponse` wired to real chat flow and MCP commands `open_chat_clarification_batch` / `submit_chat_clarification_response`. |

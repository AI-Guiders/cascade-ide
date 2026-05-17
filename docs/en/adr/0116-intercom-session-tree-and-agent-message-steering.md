<!-- English translation of adr/0116-intercom-session-tree-and-agent-message-steering.md. Canonical Russian: ../../adr/0116-intercom-session-tree-and-agent-message-steering.md -->

# ADR 0116: Intercom - session tree (branching) and steer / follow-up during agent operation

**Status:** Proposed  
**Date:** 2026-05-15

## Related ADRs

| ADR | Role |
|-----|------|
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom as a channel |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Topic index, product spine |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Topic cards, overview/detail |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Append-only chat events |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Threads, clarification packages |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | Agent cycle, tool-run |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Agent presence in editor |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP/MCP, context |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Snapshots for tests |

## Summary

- Intercom: session tree (branching, rewind, bookmark).
- **Steer** / **follow-up** when an agent is working; [0045](0045-agent-chat-persistence-event-log-and-projections.md).


**Outside ADR:** [Pi](https://pi.dev/) - KB `kb-open-source-agents-patterns-landscape-v1.md`, section `pi-dev-coding-agent`.

## Problem

1. **Linear feed** of Intercom/chat does not handle real work well: a branch on an idea, a return to the old line, “what if we do it differently from that point” - without **branching** all that remains is deep scrolling ([0031](0031-agent-chat-clarification-batches-and-threading.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md)).
2. With a **long tool-run** of the agent, the operator needs to either **intercept** the move (new priority), or **add** a clarification **after** the current step - without two modes, everything is mixed into “another message in the feed,” which is unpredictable for the orchestrator ([0038](0038-agent-facade-ai-provider-and-tool-orchestration.md)).
3. External harnesses (eg [Pi](https://pi.dev/)) already separate **steer** and **follow-up**; in CIDE this is not yet fixed in canon - the risk of incompatible implementations in the UI, ACP and standalone loop.

---

## Solution (direction)

### 1. Intercom session tree (session tree)

An Intercom **Session** is stored and displayed as a **tree** of events/messages, rather than just one chronological feed.

| Concept | Meaning |
|---------|--------|
| **Node** | Message, system replica, tool-run boundary, opening/closing clarification batch - according to policy [0045](0045-agent-chat-persistence-event-log-and-projections.md) |
| **Rib** | “continuation from” is a response to the parent; when branching, the child branch has its own **head** |
| **Branch** | Line from the selected node; change head = “continue from this point” |
| **Bookmark** | Node label for navigation in the UI (`/tree`-analogue); does not have to be head |
| **Rewind / continue from** | The operator selects a node → **new** events are written as children of **this** node (the history “to the right” of the branch point is not rewritten) |

**Orthogonality of topic cards ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md)):**

- **Topic / thread** — product “thread” with a title and summary on the card.
- **Session tree** - **physical** storage and navigation throughout the session; a topic can reference a **subtree** or the head of a branch, but **does not replace** the entire tree.

**Persistence (direction, coordinate with [0045](0045-agent-chat-persistence-event-log-and-projections.md)):**

- In event payload (or `meta.json`) - stable `node_id`, `parent_id`, optional `branch_id`, `kind` (user/assistant/system/tool_boundary).
- New event types v2+ (sketch): `branch_created`, `head_moved`, `node_bookmarked` - only if necessary; minimum v1 - just `parent_id` on `message_added` and head policy in the projection.
- **Export/share:** like Pi - HTML or gist - **not** the goal of v1; put identifiers into the model so that export is possible later.

**UI (thumbnail):**

- Command or panel **"session tree"** in Intercom (not PFD): list/graph of branches, jump to node, "continue from here".
- Overview topic cards ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)) can show **active thread** and shortcut (breadcrumb).

### 2. Steer vs follow-up while the agent is working
Fix **two modes** of the operator's outgoing message while the agent is in the **running** state (tool-run / streaming):

| Mode | Semantics | When is it delivered | Impact on tool-run |
|-------|----------|-------------------|---------------------|
| **Steer** (stealing) | New priority **now** | After the **current** tool call (or safe cancel point), **aborts** the remaining tools in the agent's current turn | The remaining scheduled tools are **not** executed; orchestrator moves to new custom intent |
| **Follow-up** (queue) | Clarification **after** completion of the current move | When the agent has **finished** the current cycle (all tools/response) | The current turn is **not** interrupted; message in queue for next turn |

**UI (Pi reference):** `Enter` → steer by default; `Alt+Enter` (or explicit switch) → follow-up - **specific keys** are not captured by this ADR, only semantics.

**Invariants:**

- **visible** mode in the UI (icon, input field label, tooltip) - the operator does not have to guess.
- The **event log** stores `delivery_mode: steer | follow_up | normal` (name in wire - upon implementation).
- **Steer** does not replace system **confirmations** PFD ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) - dangerous actions are still through the cockpit policy.
- The external agent (ACP/Cursor) receives the same semantics where the transport allows; otherwise, a documented limitation ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md)).

**Connection with presence ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md)):** steer can be accompanied by resetting “agent writes” / canceling pending edit - details in the ADR implementation, not here.

### 3. Observability (agent and MCP)

- Intercom snapshot for MCP/tests ([0008](0008-mcp-contracts-and-testable-infrastructure.md)) includes: `active_branch_id`, `head_node_id`, optional bookmark list, **follow-up queue** (if available).
- When **steer** the snapshot records the fact that the move was interrupted (for reproducibility in tests).

---

## Consequences

- [0045](0045-agent-chat-persistence-event-log-and-projections.md) when implementing branching - expand the scheme of events and projections, not the second “truth file”.
- [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) / [0096](0096-intercom-topic-card-summary-and-product-spine.md) - topic cards refer to tree nodes, summary does not duplicate the entire tree.
- [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) — the orchestrator must understand steer (cancel the remaining tools).
- Reference Pi remains in KB; the canon of the product is this ADR.

## Not goals (v1)

- Full graph tree editor in the style of git log --graph.
- Synchronization of branches between several users (Intercom multiplayer).
- Mandatory export/share in v1.
- Replacing **topic cards** with a tree is just an addition.

## Rollout (sketch)

1. **Document (this ADR)** + link from [0080 § development ideas](0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities) if necessary.
2. **Persistence:** `parent_id` + head in projection; UI "continue from node" without the full `/tree`.
3. **Steer/follow-up:** input field + event `delivery_mode` + minimal support in one orchestrator ([0038](0038-agent-facade-ai-provider-and-tool-orchestration.md)).
4. MCP snapshot of the branch fields - after the model has stabilized.

## Open questions

1. Branching at the level of **the entire session** vs a separate tree **per topic** - v1 offers **session**; per-topic trees - if requested.
2. Steer when the response streaming token is **incomplete—the partial message trimming policy (separate specification).
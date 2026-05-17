<!-- English translation of adr/0096-intercom-topic-card-summary-and-product-spine.md. Canonical Russian: ../../adr/0096-intercom-topic-card-summary-and-product-spine.md -->

#ADR 0096: Intercom - topic card summary (card index) and product end-to-end (spine)

**Status:** Accepted · Implemented  
**Date:** 2026-04-24

## Related ADRs

| ADR | Role |
|-----|------|
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | topic cards, overview/detail, drill-in/back, **main thread** as one of the cards; keyboard intents |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom as a channel, not a “window to a bot” |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | session tree, steer/follow-up - orthogonal to topic cards |
| [0057](0057-chat-surface-pipeline-adoption.md) | chat surface pipeline |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | events and projections - where to put summary and line marks |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | chat in IDE; **what** falls into the agent's context - surface politics |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Mfd zone / attention |
| [0095](0095-workspace-solution-ide-health-stratification.md) | example of an end-to-end product line in one work session |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash commands in chat (`/card`, `/spine`) - local actions Intercom |
## Summary

- Intercom: topic card = title + **summary** (card index).
- Product spine is orthogonal to main thread; complements [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md).


**Relation to [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md):** 0072 remains canon **navigation** (overview of topic cards ↔ thread detail, intent commands, binding to `ChatSurfaceLayout`). **0096** fixes **product semantics of the card content** and **second axis** - **through line (spine)** of work on a product / project session on top of topics - **without** replacing the pipeline and without canceling drill-in/back. **Cascade IDE (CIDE)** in the text below is a **typical example** for the main repository; in another workspace spine - the same abstraction for **his** product line.

---
## Context

In the Intercom discussion it is clarified: a topic card is **not** a compressed flat bubble of the last message, but a **card index** entry: **topic title** (for example, “MFD Presets”, “Channel / Stratum”, “`System.Threading.Channel`”) plus a **brief summary** - what the topic is and where we left off in it, **before** falling into a full thread.

In parallel, topics in one session often relate to **one product line** - for example, work on **one product or repository** (in this repo a typical image is **Cascade IDE**: ADR, cockpit, MCP, infrastructure; in another project - its own line). Only **a set of cards** runs the risk of giving “islands” without a link; the operator also needs a **through line**: where the overall work **on this line** is going, even when another topic card is in focus.

---

## Solution (direction)

<a id="adr0096-p1"></a>

### 1. Topic card = title + **summary** (card index)

- **Title** - human-readable topic tag (from `ThreadNode.Title`, thread renaming, or generation policy from the first anchor - as in [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)).
- **Summary** - short text **on a card** (guideline: one to three lines in overview typography), **not** replacing the detail timeline: when drill-in, the complete correspondence of the topic is shown.
- Summary source on v1+ - **implementation direction**: explicit field in thread metadata / projection from the last significant message / brief manual commit; detailed event contract - [0045](0045-agent-chat-persistence-event-log-and-projections.md). **Non-target:** required **NLP auto-contractor** as UX base ([0072 § non-target](0072-chat-topic-cards-intent-melody-keyboard-contract.md)).

<a id="adr0096-p2"></a>

### 2. Product end-to-end (spine) **orthogonal** to main chat thread
- **Main thread** in the sense of [0072 §1](0072-chat-topic-cards-intent-melody-keyboard-contract.md) is **one of the threads** of the session (correspondence line).
- **Spine** - **product narrative** of the session above the selected line: where the work is moving as a whole (milestones, chain of ADR/tasks, “now the focus is ..." - **content depends on the project**). This **doesn't have to** coincide with the chronology of the main thread **one to one**; may appear as:
  - fixed **strip** or **anchor card** in overview;
  - or a collapsed **chronology of milestones** with links to artifacts (documents, commits) - the specific layout is not recorded by this ADR.
- **Invariant:** spine **does not replace** topic cards and **does not** duplicate the entire main thread; it answers the question “**where are we in the overall product trajectory**”, and the topic cards answer “**what are we discussing on individual threads**”. For **Cascade IDE** spine and theme often coincide in the “battlefield” (one repository) - this is a **special case**, not a definition of spine at all.

<a id="adr0096-p3"></a>

### 3. Link to code (current and next step)

- Pipeline [0057](0057-chat-surface-pipeline-adoption.md) already allocates **overview**: `ChatThreadOverviewItem` in [`ChatSurfaceSnapshot`](../../Features/Chat/ChatSurfaceSnapshot.cs) - a natural place for the **summary** field (and optional anchor label **spine** / product line) when implementing [0072 plan](0072-chat-topic-cards-intent-melody-keyboard-contract.md).
- [`ChatPanelViewModel`](../../Features/Chat/ChatPanelViewModel.cs): overview/detail mode (`IsChatOverviewMode`, selected thread) - expands under **card index visual** and a separate UI end-to-end circuit **after** card stabilization on 0072.

<a id="adr0096-p4"></a>

### 4. Spine and **agent context**: do not inflate prompt by default

Spine is useful to the **operator** as a compass; the agent's context is **limited and expensive**, so the UX and ACP/MCP submission policy ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md)) should be at odds with “put everything in one thread.”

**Transfer of recorded decisions from the spine to a thread** - in discussions with agents they are briefly called *carry-forward*: do not duplicate the entire spine, but **move into the topic** a condensed formulation of what has already been adopted (often with reference to the spine or ADR anchor).

- **By default** it is reasonable to include **active topic** in a work request to an agent: recent remarks / selection / summary cards - **not** the full history of the spine and **not** all parallel topics.
- **Spine in the prompt** - **compressed slice** (current milestone, 1-3 bullets “what has already been decided”, blockers) or **inclusion by explicit action** of the operator (“add a line to the context”, separate intent) - implementation details are not recorded here, the principle: spine **does not have to** be automatically duplicated in its entirety in each call.
- **Transfer of committed solutions from spine to thread** (*carry-forward*): when a solution is fixed in spine, a **short bridge** (one to three sentences + link to ADR/commit/spine anchor) is enough for the agent to **work in the topic** without necessarily “switching” to a separate spine circuit; spine remains the **canonical place** of the milestone, the thread is **working memory** with local context.
- **Artifacts outside the chat** (ADR, ticket) already unload the chat: in the prompt - **link and one line of summary**, and not inserting the full text of the document, unless the operator has chosen otherwise.

**Examples** (do not exhaust the implementation):
1. **Manually transfer the solution to the topic** (same *carry-forward*). In the “MFD Presets” thread, the operator writes: * “We have fixed the product line: spine is shown as an anchor card, not a second timeline - see [0096 §2](0096-intercom-topic-card-summary-and-product-spine.md#adr0096-p2). Here we continue only with the layout of the preset.”* The agent in this thread is not required to read the entire spine again.
2. **Explicit summarization protocol (already available for chat).** Long thread or long spine fragment → export via `ide_execute_command` with `command_id` **`chat_export_readable`** → short summary → **coordination** with the operator → insertion of **compressed** text into a topic or `.cascade-ide/agent-notes.md` (`ide_append_agent_notes`, etc.), without replacing the raw export with a “black box”. Scenario canon: [`MCP-PROTOCOL.md`](../../MCP-PROTOCOL.md) (section **"Summing up the chat session"**); agent playbook: `knowledge/playbook-session-summary-and-chat-export-v1.md` (**agent-notes** repository). The same **pattern** applies to moving a **solution from spine** to another topic: first an **explicit** “compress and commit” step, then a short quote in the thread.
3. **Only the link in the request to the agent.** *“Open `docs/adr/0095-…md`, we are only interested in the sub-item about umbrella .sln; the answer is three bullets and a risk for CDS.”* The full file is not included in the context unless the agent has consciously read it with the tool.
4. **Optional inclusion of spine.** The operator presses “add through line to context” (or intent in the spirit of [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)) - a **limited** slice is included in the packet (for example, the last *N* milestones or one field “current line focus”), and not the entire session history.

---

## Non-targets

- Replace [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) or [0057](0057-chat-surface-pipeline-adoption.md).
- Enter the full JSON event schema or MCP fields for the spine in this ADR.
- Promise parity with external products (Comet, etc.) - see [0072 Provenance](0072-chat-topic-cards-intent-melody-keyboard-contract.md).
- Consider that the **full text of the spine** must be included implicitly in **every** request to the agent - on the contrary, this is an **anti-pattern** for the scope of the context (see [§4](#adr0096-p4)).
- Duplicate the **long** spine into **every** related topic **without reducing the volume** (no short bridge, no explicit summarization via a protocol like export → agreed summary): otherwise, again, “islands” are out of volume, not out of meaning.

---

## Consequences

- When implementing topic overview, **card** is treated as **title + summary**, and not as the last bubble.
- An explicit product layer **spine (product end-to-end line)** appears in the UX docks and backlog, consistent with Intercom ([0080](0080-intercom-naming-and-multi-party-channel-model.md)); for the Cascade IDE repository, **CIDE** remains a convenient **working name** for the example, not a hard-wired data schema binding.
- Extending `ChatThreadOverviewItem` (or equivalent) and overview tests is an expected step after/along with [0072 implementation plan](0072-chat-topic-cards-intent-melody-keyboard-contract.md).
- The policy of **feeding context to the agent** (what from the spine/summary/thread goes to ACP by default and by command) must be **explicit** in the product and in conjunction with [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md), otherwise the spine risks inflating the prompt (see. [§4](#adr0096-p4)).

---

## Updated

- **2026-04-24** - canonical ADR file name: `0096-intercom-topic-card-summary-and-product-spine.md` (neutral slug `product-spine`). [Former file name](0096-intercom-topic-card-summary-and-cide-spine.md) is left as a short stub for external bookmarks.
- **2026-04-25** — [§4](#adr0096-p4): spine vs agent context; **transfer fixed solutions from spine to thread** (*carry-forward*); non-goals and consequences according to the volume of the prompt; block **Examples** (manual bridge, `chat_export_readable` / playbook, link-only, optional inclusion of spine).
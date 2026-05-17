# ADR 0080: Intercom — channel name and model (not only “chat with the agent”)

**Status:** Accepted (strangler: Intercom in UI and docs v1; multi-party and external contour — on roadmap)  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | threads, clarification batches, message model |
| [0057](0057-chat-surface-pipeline-adoption.md) | chat surface → Skia pipeline |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | topic cards, Melody in chat domain |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | catalog: summary on card; product spine, CIDE as example |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | event history, projections |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP / MCP and IDE surface |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | AI facade, orchestration |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | IDE presentation vs cockpit |
| [0033](0033-internationalization-resx-avalonia.md) | i18n |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Intercom in Forward anchor — agent-central layout |

## Summary

- **Intercom** is the product name for the communication channel (agent + team + system).
- External team contour vs building “our own mountain”; strangler for legacy “chat”.

### Outside ADR

| Document | Role |
|----------|------|
| [cascadeide-philosophy-v1](../../design/cascadeide-philosophy-v1.md) | product narrative |

---

## Context

In product language, **“chat”** almost always reads as **human ↔ single assistant** dialogue (often LLM-only). That narrows design: it is harder to honestly support **multiple participants**, **system lines** (CI, MCP, repo status), **context handoff** between people, and **different trust policies** without cognitive dissonance (“why isn’t that a person in chat?”).

Cascade already uses an **aviation cockpit / attention** metaphor ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0066](0066-cockpit-ui-vs-ide-presentation-layer.md)). **Intercom** (crew communication; colloquially *intercom*) better conveys a **channel** that can carry **multiple voices**, not one “small talk” window; UI may clarify it is a **communication channel in the IDE**, not a hardware intercom.

The technical **chat surface** chain ([0057](0057-chat-surface-pipeline-adoption.md)), **threads** ([0031](0031-agent-chat-clarification-batches-and-threading.md)), **topic UX** ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)) **need not** match the user-facing label “Chat”: those are different layers (model / pipeline / copy).

---

## Decision (direction)

### 1. Product name: **Intercom**

- **Written canon:** **Intercom** (Latin, **one** letter **m**). **Intercomm** (two **m**) is **not** used in product strings or docs — common typo; see [§4](#adr0080-spelling).
- **User-facing** term for the main communication surface in the IDE — **Intercom** (one capitalized brand in UI).
- **Internal** identifiers (`command_id`, feature paths, type names) may stay **`chat`*** during strangler migration ([0009](0009-strangler-migration-and-exceptions.md)); rename in phases, not one-commit explosion.
- **Developer docs** may say “Intercom (formerly chat in texts)” until the glossary stabilizes.

### 2. Mental model: **channel**, not “window to the bot”

Intent:

- The channel may include **multiple roles**: human(s), **agent**, **system** (automated lines: build, git, MCP replies as structured messages — as maturity allows).
- **One channel** ≠ one interlocutor; **one thread** ([0031](0031-agent-chat-clarification-batches-and-threading.md)) remains a work line inside channel/session.
- **Visibility, export, invite** policies are designed from **“who is on the air”**, not only “what the LLM answered”.

This ADR does **not** specify full multiplayer, shared sessions, and ACL — only **naming and semantics** so later ADRs do not fight the “chat” label.

### 3. Discoverability and i18n

- First screens — **short hint** (subtitle or tooltip): tie to familiar “chat” **or** “agent and team communication” — product choice; criterion: **search and onboarding** ([0013](0013-command-surface-and-discoverability.md)).
- **Command palette** and search: aliases “chat”, “agent” → same actions as “Intercom” until the brand is learned ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)).
- **Localization** ([0033](0033-internationalization-resx-avalonia.md)): either **Intercom** brand untranslated or separate discussion of local titles; ResX details are outside this ADR.

<a id="adr0080-spelling"></a>

### 4. Spelling and search

- **Wrong:** **Intercomm** (two **m**) — not brand; glossary may state canon is **Intercom**.
- Internal ADR URLs: this file is `0080-intercom-naming-…` for canon; remove old `intercomm` bookmarks when migrating.
- In-text ADR anchors are not renamed without need.

### 5. Ready-made “team contour” vs own implementation

Full **interpersonal** channel (org, invites, roles, history search, mobile clients, retention, audit, offline sync) is a **separate product scope**, not one IDE screen. Pulling that **entire stack** into Cascade is **not required** and often **not worthwhile** when mature recording systems already exist.

**Default direction (vendor-neutral):**

- **Intercom in the IDE** is what Cascade owns uniquely: **workspace context**, **agent**, **tools**, **threads/events** around code work ([0031](0031-agent-chat-clarification-batches-and-threading.md), [0045](0045-agent-chat-persistence-event-log-and-projections.md), [0057](0057-chat-surface-pipeline-adoption.md)).
- **Team scale “like Slack”** — preferably **outside**: self-hosted or corporate service with **API**, webhooks, deep links, notifications; Cascade as **integration client**, not rewriting the chat server.
- **Embedding a full web client** of another product in the IDE window (typically WebView) — **only deliberately**: UX tax, server versions, SSO, focus, and **two truths** about where team conversation lives; see MFD web boundaries ([0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md)) as adjacent risk even if the primary surface is not MFD.

Example system classes (non-normative for the repo): Mattermost, Matrix, corporate messenger APIs. Provider choice and integration contract — **separate ADR or design** when there is product commitment.

---

## Consequences

- New product ADRs and UX copy may use **Intercom** as the canonical name for the **communication** surface, not as a synonym for “agent only”.
- Code rename refactors — **separate steps**, aligned with MCP/snapshot risk ([0008](0008-mcp-contracts-and-testable-infrastructure.md)).
- Roadmap for **“people outside the IDE”** may rely on an **external team backend** (§5), not duplicating a mature comms stack inside Cascade without need.

## Non-goals

- Full spec of team rooms, ACL, and real-time sync.
- Choosing a vendor (Mattermost, Matrix, …) and OAuth/SSO scheme — outside this ADR.
- Removing the term “chat” from code on acceptance day.

<a id="adr0080-future-modalities"></a>

## Future directions (Intercom perspective, no v1 commitment)

Below are **directions** aligned with Intercom as a **channel** with multiple delivery modes; they are **not** in this ADR’s acceptance scope and need separate specs or ADRs.

- **Voice “in packets” (async):** user records a short clip → speech recognition → text (and optional attachment) enters the **same** thread/event feed as normal lines. Low product bar: no full duplex, no “radio” until explicit decision.
- **Expressive outbound voice (TTS):** separate line — give **agent/system** replies more **prosodic** resolution (stress, pauses, intonation) for long answers. Ecosystem may use **OpenTTS** (self-host, data policy); voice/SSML/preset details — not this document.
- **“Live radio” (duplex / low latency):** when **synchronous** voice between participants is needed — different requirements (VAD, jitter, PTT vs full duplex, moderation). Options: voice platforms (e.g. TeamSpeak class) or **libraries** and own transport (WebRTC/Opus + signaling). Prefer **outside** IDE core or behind an explicit integration contract (§5 spirit: do not drag the “mountain” without need).
- **Session tree and agent intercept:** Intercom history branching (continue from node, bookmarks), **steer** (intercept after current tool) vs **follow-up** (queue after turn) — canon in [0116](0116-intercom-session-tree-and-agent-message-steering.md); persistence and topic cards — [0045](0045-agent-chat-persistence-event-log-and-projections.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md), [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md).
- **Code anchors and alternate views:** a line may reference **workspace context** — file, **line/column range**, current **editor selection** — via stable **deep link** (open, jump, highlight the discussed region). In parallel — **alternate representation** of the same anchor: preview card, mini-diff, graph node/subgraph, without replacing the canonical message model. Overlaps: navigation ([0039](0039-workspace-navigation-affordances.md)), graph selection/sync ([0067](0067-graph-backed-surfaces-contract.md)); when publishing to **external** team contour — same privacy and “two truths” policies as §5.

Criterion for future ADRs: each modality must **map** to the unified message/event model ([0045](0045-agent-chat-persistence-event-log-and-projections.md)) and export policies, not live as a second truth without an explicit decision.

## Rejected alternatives (brief)

- **Keep only “Chat”:** stronger discoverability, weaker extension to “team + system” without reframing.
- **Internal code/types `Chat*`, UI “Chat”:** metaphor/code drift for agent/docs; OK as transition, not final canon without explicit decision.

---

## Open questions (for discussion)

1. **Final UI title:** “Intercom” only or “Intercom” + “chat” subtitle on first release?
2. **Russian locale:** Latin everywhere or short Russian header equivalent?
3. **Boundary with ACP/Cursor** ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md)): where copy says “Intercom” vs neutral “external agent”?
4. **System lines:** v1 minimum (agent + user only) or **message type** “system/operator” in event model ([0045](0045-agent-chat-persistence-event-log-and-projections.md)) from the start?
5. **External team contour:** default provider in product story and **criteria** (self-host, API, SSO, mobile) — before first integration ADR?

---

## State

Before **Accepted**: decide §1–2 (product), agree code/command rename roadmap (optional checklist), optional user glossary line (outside this ADR). §5 — when committing to external team system integration.

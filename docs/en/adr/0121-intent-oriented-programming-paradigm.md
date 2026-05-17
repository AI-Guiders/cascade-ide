# ADR 0121: Intent-Oriented Programming (IOP) — conceptual foundation of Cascade IDE

**Status:** Proposed  
**Date:** 2026-05-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0100](0100-project-constitution.md) | Constitution: agent-first, cockpit, shared operational model |
| [0013](0013-command-surface-and-discoverability.md) | Palette, keyboard-first, command discoverability |
| [0051](0051-intent-based-attention-routing-toml.md) | Intent-based attention routing (TOML) |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Intent-first: topic cards, Melody/Chords, `command_id` |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom — session and intent channel |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Intent Melody catalog (declarative intent layer) |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Intercom slash → same `command_id` as palette/MCP |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward anchor: Intercom or editor — where the IOP loop lives |
| [0122](0122-collaborative-iop-environment-and-shared-situational-display.md) | Environment: N `(P)(F)(M)` stations + shared room situational display |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Git parity: human and agent in one loop |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Editor — text source of truth; chat — intent/status |

### Outside ADR

| Document | Role |
|----------|------|
| [iop-manifest-v1.md](../iop-manifest-v1.md) | Short IOP manifest for the site and onboarding |
| [architecture-policy.md](../architecture-policy.md) | Architecture policy, north-star, KB |
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | IDE/MCP commands — intent execution |
| [intent-melody-language-v1.md](../../intent-melody-language-v1.md) | `c:` grammar (Melody), not chat slashes |
| [north-star-cursor-mcp-cascade-workbench-v1.md](../../design/north-star-cursor-mcp-cascade-workbench-v1.md) | “Cursor + MCP + Cascade” boundaries |

## Summary

- Adopt **Intent-Oriented Programming (IOP)** as the **named product paradigm** of Cascade IDE: first of all a **discipline of communication** in the development contour (**working implementation of a proposed paradigm in the product**), not a replacement for OOP/FP.
- Three IOP pillars in CIDE: **intent over manual syntax** (intent layer), **two-loop verification** (agent synthesizes — human approves diff), **epistemic context** (KB canon and context routing as normative layer for the agent).
- Public wording for the team and site — [iop-manifest-v1.md](../iop-manifest-v1.md); this ADR is the normative link to existing decisions and non-goals.

---

## Context

The agent-first IDE stack already has “intent-first” in individual ADRs ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0055](0055-skia-instrument-composition-pipeline.md) Intent→…→Render), an **Intent Melody** catalog, **Intercom**, **MCP** and **Roslyn** parity. What is missing is a **single paradigm name** that:

1. explains to newcomers (including on probation) *why* the product is shaped this way, not as “VS + chat”;
2. connects scattered ADRs into one mental model;
3. honestly separates **hypothesis + working in-product implementation** from “the only industry standard” or “the spec reference implementation”.

Team discussion (including with Atlas) proposed **IOP** alongside OOP and FP. The meaning runs deeper than UI commands: IT is about **information flow** (goals, intentions, processes, communication, transparency); software development is part of the flow, not the whole subject. Agents reinforced an old truth: without explicit intent and a shared picture, code and the team diverge into chaos.

---

## Problem

1. **Shallow reading of IOP:** “the basic unit is intent” sounds like “more slashes,” although the point is **agreement on goals** in the team’s information contour.
2. **Cognitive ceiling:** a human cannot hold 100k+ lines of a monolith as “one text in the head”; the human role is architecture and verification, not a manual syntax compiler.
3. **Split contours:** without a shared paradigm it is easy to duplicate command parsing (chat slash vs Melody vs MCP) — see motivation in [0119](0119-chat-slash-commands-intercom-surface.md).
4. **Weak agent context:** without KB canon and routing (`route_context`, playbooks) intents “drift”; we need an explicit **epistemic constraint** model, not prompt alone.
5. **Marketing vs engineering:** without an ADR the term IOP risks sounding like a “revolution” declaration without ties to code and ADR statuses.

---

## Decision

<a id="adr0121-p1"></a>

### 1. Definition of IOP (Cascade IDE scope)

**Intent-Oriented Programming (IOP)** is a way of organizing work in the IDE where:

- the **subject** is an aligned **information flow** (goals, processes, communication, transparency), not program text alone;
- an **intent** is a *named agreement* on intention or target state in that flow (not syntax and not “yet another slash”);
- **execution** (including code generation) is delegated to the agent and infrastructure (MCP, build, Roslyn, git) under **human observability**;
- **correctness** is checked via **delta** (diff, diagnostics, tests) and **normative knowledge** (KB), not only “something was generated”.

IOP in CIDE is a **discipline of communication** in an agent-first IDE (information flow made explicit and verifiable). **C#, projects, and the editor remain the source of truth** for program text ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md), [0098](0098-semantic-first-document-as-projection.md)).

<a id="adr0121-p2"></a>

### 2. Three IOP pillars in Cascade IDE

| Pillar | Meaning | In CIDE (existing / in flight) |
|--------|---------|-------------------------------|
| **1. Flow and explicit intent** | Aligned information flow; intent = agreement on goal/state | Intercom, topic cards, KB/ADR; Intent Melody, `command_id`, palette, [0119](0119-chat-slash-commands-intercom-surface.md) slashes → same contour as MCP; [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) |
| **2. Two-loop verification** | Agent synthesizes; human is architect and diff arbiter | Forward (editor) / Intercom ([0120](0120-primary-work-surface-intercom-or-editor.md)); Roslyn MCP, build/test MCP, git MCP; human-in-the-loop on merge |
| **3. Epistemic context** | Normative layer over code — KB canon, router, policies | kb-public, agent-notes, `knowledge/` tree (`domains/` is a **repo path**, not a “domain” term); [architecture-policy](../architecture-policy.md), [0100](0100-project-constitution.md) |

The manifest metaphor of an “intent compiler” is **not one binary** but the bundle: **Intercom + command surface + MCP + agent + IDE verification**.

<a id="adr0121-p3"></a>

### 3. Working implementation in the product

Cascade IDE is an **open working implementation** of the proposed IOP paradigm (**an in-product instance**, not an external-spec reference): IDE, Roslyn MCP, agent-notes, kb-public, documented at the [project site](https://ai-guiders.github.io/cascade-ide/).

Wording such as “the whole world will switch to IOP”, “the world’s only compiler”, or **reference implementation** in the ISO/W3C sense is **not** part of this ADR — only a **working paradigm hypothesis** for the product and the AI-Guiders community.

<a id="adr0121-p4"></a>

### 4. Terminology (glossary v0)

| Term | Meaning in IOP/CIDE |
|------|---------------------|
| **Intent** | Named agreement on goal/target state in the information flow; in CIDE carriers include Intercom, KB, `command_id`, Melody, slash (not “atom = slash”) |
| **Intent Melody** | Declarative/parametric language binding intents to UI and hotkeys |
| **Intercom** | Session channel: dialogue, topic cards, slashes — forward surface for intents ([0080](0080-intercom-naming-and-multi-party-channel-model.md)) |
| **Verification loop** | Synthesis → diff/diagnostics/tests → human accept or rollback |
| **Epistemic context** | KB, agent-notes, router/playbooks, policies — meaning constraints for the agent |

---

## Non-goals

- **Do not** reduce IOP to slash commands, palette, or Melody — surfaces, not the paradigm.
- **Do not** replace OOP, FP, or C# in the user repo with “intents instead of code”.
- **Do not** autonomous merge to main without human-in-the-loop (see [0100](0100-project-constitution.md), git policies).
- **Do not** IOP without verification infrastructure (Roslyn/build/test/git) — otherwise it is chat only.
- **Do not** claim an ISO/ECMA standard; IOP here is a **product and architecture** frame for CIDE.
- **Do not** duplicate the body of [0119](0119-chat-slash-commands-intercom-surface.md) / [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) — linking layer only.
- **Do not** promise “we will digest any inbound user stream” — IOP reduces communication chaos; it does not remove human attention limits.

---

## Risks and boundaries (honest)

| Risk | IOP/CIDE response |
|------|---------------------|
| **Intercom = endless feed** | Intercom is a **communication hub around a goal** ([0080](0080-intercom-naming-and-multi-party-channel-model.md), [0120](0120-primary-work-surface-intercom-or-editor.md)), not a generic messenger; topic cards, spine, threads ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0031](0031-agent-chat-clarification-batches-and-threading.md)) |
| **“We cannot handle the user stream”** | People cannot either without structure; the product **does not amplify** inbound noise — it **names intent** and separates synthesis from verification |
| **Agent does everything at once** | Human-in-the-loop, diff, non-goals on autonomous merge; slash/MCP are contracts, not message chaos |

---

## Consequences

- **Intercom** in perspective — **communication hub around a goal** (people + agents → intent → implementation), see [0120](0120-primary-work-surface-intercom-or-editor.md), [0080](0080-intercom-naming-and-multi-party-channel-model.md).
- **Team environment** — multiple cockpits + **shared situational display** (not a chat feed), see [0122](0122-collaborative-iop-environment-and-shared-situational-display.md).
- New command/chat/MCP features are described as **intent surface extension** + **parity** + **verification**, referencing IOP pillars.
- Documentation site: block on [home](../index.md), [IOP manifest](../iop-manifest-v1.md), Russian copy at `docs/iop-manifest-v1.md`.
- On **Accepted** — one line in [architecture-policy.md](../architecture-policy.md) (goal/positioning) and glossary in [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) if needed.
- Onboarding (probation, contributors): manifest + [concept overview](../concept-overview.md) first, then ADRs by topic.

---

## Implementation maturity (at Proposed)

| Pillar | Maturity | Notes |
|--------|----------|-------|
| Intent | Partially Implemented | Melody, palette, part of MCP; [0119](0119-chat-slash-commands-intercom-surface.md) — Proposed |
| Verification | Implemented (contour) | Editor, Roslyn/build/git MCP; UX completeness — roadmap |
| Epistemic context | Implemented (external stack) | kb-public, agent-notes-mcp; CIDE integration — [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) |

---

## History

| Date | Change |
|------|--------|
| 2026-05-17 | Proposed: IOP paradigm, three pillars, manifest, CIDE working implementation. |
| 2026-05-17 | Softened positioning: “reference implementation” → “working implementation in the product”. |
| 2026-05-17 | IOP: avoid “knowledge domains”; `knowledge/domains/` — repo path only. |
| 2026-05-17 | IOP depth: information flow, communication/transparency; intent ≠ slash. |
| 2026-05-17 | Anchor wording: IOP = **discipline of communication** (“communication is the whole key”). |
| 2026-05-17 | Intercom as goal-centric communication hub; honest risks on message volume. |
| 2026-05-17 | Link to [0122](0122-collaborative-iop-environment-and-shared-situational-display.md) — environment vs application. |

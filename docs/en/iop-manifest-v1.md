# IOP — Intent-Oriented Programming

**Intent-Oriented Programming (IOP)** is first of all a **discipline of communication** around development: not “slash commands reinvented,” but a way to agree on goals, processes, and changes so they stay **visible** to everyone in the contour (people, agent, artifacts).

**Communication is the whole key.** With communication come aligned intent, transparency, and meaningful code; without it — local order in files and global chaos, which agents made painfully visible. IT is about **information flow**; writing software is only part of that flow.

**Cascade IDE** is an open **working implementation** of IOP: a stack that makes this flow explicit in a **.NET** **agent-first** IDE.

!!! info "Normative detail"
    Non-goals and ADR links — [ADR 0121](adr/0121-intent-oriented-programming-paradigm.md) (Proposed).  
    Russian: [манифест IOP (RU)](../iop-manifest-v1.md).

---

## Why IOP

IT is **information** technology: the work is a **coherent flow of meaning** — who talks to whom, about what, toward which goals, with which processes, and what observers can see. Without communication and transparency, shipping code is pointless: local order in files, global chaos in the team.

IOP in the IDE centers **explicit intent** (goal, target state, agreed process) and an **observable execution delta**. C# and the repo stay the source of truth for program text; IOP is a **discipline of communication** in which code is the verifiable outcome of agreement, not a replacement for talking.

---

## What IOP is not

- **Not** “zoomers invented `/build`” — slashes, palette, and Melody are **surfaces** for one meaning.
- **Not** a replacement for OOP/FP: classes and functions remain; what changes is how the team **agrees on work** before and after edits.

---

## Three pillars in Cascade IDE

### 1. Information flow and explicit intent

At the center is a **aligned information flow** (people, agent, artifacts, status). An **intent** is not a button — it is a **named agreement** on a goal or target state in that flow. In CIDE it is carried by Intercom, topic cards, ADR/KB, `command_id`, Intent Melody (`c:`), slashes ([ADR 0119](adr/0119-chat-slash-commands-intercom-surface.md)), palette, and the **same commands via MCP** — one meaning, many channels, no scattered parsers.

### 2. Two-loop verification

| Loop | Who | What |
|------|-----|------|
| **Synthesis** | Agent + MCP | Edits, build, refactors, git |
| **Verification** | You | Diff in Forward, Roslyn diagnostics, tests, deliberate merge |

Infrastructure (HCI, Roslyn MCP, build/test, git) keeps intents inside project “physics”.

### 3. Epistemic context

Beyond relying on C# types alone — **knowledge canon and context routing**: [kb-public](https://github.com/AI-Guiders/kb-public), agent-notes, the `knowledge/` tree (folders such as `domains/agent-operations/` are **paths in the KB repo**, not a product/DDD/KE “domain”). The agent attaches playbooks via router / team **light ontology**; the KB is a higher-order normative layer.

---

## Intercom — communication hub around a goal (perspective)

**Intercom** ([ADR 0080](adr/0080-intercom-naming-and-multi-party-channel-model.md)) in the IOP perspective is not a “chat widget” but the **hub of communication around a goal**: people and agents **agree**, **surface intent**, clarify context, and **drive implementation** in the same contour (editor, MCP, verification). Topic cards, spine, slashes ([0119](adr/0119-chat-slash-commands-intercom-surface.md)) are **lines of work**, not a feed for its own sake.

Cockpit placement — [ADR 0120](adr/0120-primary-work-surface-intercom-or-editor.md) (Proposed): **`primary_work_surface = intercom`** when the forward anchor is connection and intent, not code alone.

---

## Honestly about human message volume

IOP does **not** promise “we will handle any inbound stream” — **people do not handle that either** when everything lands in one endless feed. The product bet is to **structure** communication, not amplify noise:

- **lines of work** (topic cards, overview/detail) instead of one chaotic chat;
- **clarification batches** and threads ([0031](adr/0031-agent-chat-clarification-batches-and-threading.md)), not every message = an immediate autonomous sprint;
- **intent-first** and MCP parity — less “wrote in chat / did in palette / agent missed it”;
- **verification** — the human is not required to digest everything; they arbitrate **delta**, not every token.

If communication is not structured, neither agents nor the IDE will save the day. IOP is about structuring it **first**.

---

## Session shape

```mermaid
flowchart LR
  subgraph intent ["Intent surface"]
    I["Intercom / Melody / Palette / MCP"]
  end
  subgraph synth ["Synthesis"]
    A["Agent + tools"]
  end
  subgraph verify ["Verification"]
    H["Human: diff + diagnostics + tests"]
  end
  subgraph knowledge ["Epistemic layer"]
    K["KB canon / agent-notes"]
  end
  I --> A
  K -.-> A
  A --> H
  H -->|"accept / revise"| I
```

---

## Read next

| If you want… | Document |
|--------------|----------|
| Cockpit PFD / Forward / MFD | [UI layout](ui-ux/cascade-ide-ui-layout-v1.md) |
| Intercom and slashes | [ADR 0119](adr/0119-chat-slash-commands-intercom-surface.md) |
| Intent Melody | [intent-melody-language-v1.md](../intent-melody-language-v1.md), [ADR 0109](adr/0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) |
| All decisions | [ADR navigator](site/adr-nav/index.md) |
| Agent-first policy | [architecture-policy.md](architecture-policy.md) |

---

*Cascade IDE — MIT · [GitHub](https://github.com/AI-Guiders/cascade-ide) · [AI-Guiders](https://ai-guiders.github.io/)*

# Cascade IDE — concept overview (English)

**Read this first** if you do not read Russian. Full ADRs in English live under [adr/](adr/README.md); this page and [UX docs](ui-ux/cascade-ide-ui-layout-v1.md) are the short onboarding path.

## What is Cascade IDE?

**Cascade IDE (CIDE)** is an **agent-first** desktop IDE for **.NET**, built with **Avalonia**. It is designed so that **you and an AI agent share the same cockpit**: the same commands, the same layout, and the same **Intercom** channel (session dialogue), not a separate “chat widget” bolted onto a classic IDE.

- **In-process MCP** — tools and IDE commands are available to agents without ad-hoc glue.
- **Cockpit attention model** — the window is organized like a flight deck, not “editor + side panels”.
- **Intercom** — the name of the primary human↔agent communication surface (see [ADR 0080](adr/0080-intercom-naming-and-multi-party-channel-model.md)).

## The three attention zones (Flight layout)

The shipping UI uses one product mode: **Flight**. The main window is a single grid with **three columns** (no full-width bottom tab strip):

| Zone | Name | Role |
|------|------|------|
| **PFD** | Primary Flight Display | Navigation: solution explorer, workspace map, optional tool mount — “where am I in the project?” |
| **Forward** | Forward field of view | **Primary work surface**: document editor (AvaloniaEdit dock) *or* full Intercom (proposed toggle, [ADR 0120](adr/0120-primary-work-surface-intercom-or-editor.md)) |
| **MFD** | Multi-Function Display | Secondary shell: **pages** (terminal, build output, Git, chat page, IDE Health strip, settings, …) — “instruments”, not the main horizon |

Long-running streams (build log, terminal) live on **MFD pages**, not in a legacy bottom panel across the whole window.

**Canonical layout doc:** [UI layout v1](ui-ux/cascade-ide-ui-layout-v1.md) · **Normative ADR:** [0021 PFD/MFD cockpit](adr/0021-pfd-mfd-cockpit-attention-model.md)

```
┌──────────┬─────────────────────────────┬──────────────┐
│   PFD    │          Forward            │     MFD      │
│ explore  │   editor OR Intercom        │  page stack  │
│  / map   │   (primary focus)           │ build/term/… │
└──────────┴─────────────────────────────┴──────────────┘
```

## Intercom (not “just chat”)

**Intercom** is the IDE’s session channel: topics, topic cards, agent steering, and (planned) slash commands such as `/build run` in the same input line ([ADR 0119](adr/0119-chat-slash-commands-intercom-surface.md)). Chat UI may be rendered with a Skia surface ([ADR 0044](adr/0044-avalonia-host-skia-agent-chat-surface.md)); the product idea is **parity** between what you type and what an agent can invoke via MCP.

## What we are building next (Proposed)

| ADR | Idea |
|-----|------|
| [0119](adr/0119-chat-slash-commands-intercom-surface.md) | Slash commands in Intercom input → same `command_id` as palette/MCP |
| [0120](adr/0120-primary-work-surface-intercom-or-editor.md) | Choose whether **Forward** is Intercom-centric (Cursor-like) or editor-centric |

## Where to go next

| If you want… | Start here |
|--------------|------------|
| Window layout & control names for automation | [UI layout v1](ui-ux/cascade-ide-ui-layout-v1.md) |
| Concept vs code (legacy Focus/Balanced/Power vs Flight) | [Concept → implementation map](ui-ux/concept-to-implementation-map-v1.md) |
| All decisions by lifecycle status | [ADR navigator](site/adr-nav/index.md) |
| Architecture policy & snapshot | [Policy](architecture-policy.md) · [Current architecture](architecture/current-architecture-v1.md) |
| Agent/MCP commands | [MCP protocol](../MCP-PROTOCOL.md) |
| Project principles | [ADR 0100 constitution](adr/0100-project-constitution.md) |

## Language on this site

- **Russian** (`/cascade-ide/…`) — canonical for the team: `docs/adr/` in the repository.
- **English** (`/cascade-ide/en/…`) — this overview, `en/ui-ux/`, **`en/adr/`**, and **`en/architecture-policy.md`** (+ architecture slice docs).
- Use the **RU / EN** switch in the header.

---

*Questions or contributions welcome via [GitHub](https://github.com/AI-Guiders/cascade-ide).*

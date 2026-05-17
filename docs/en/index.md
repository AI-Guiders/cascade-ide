# Cascade IDE — documentation

**Cascade IDE (CIDE)** is an agent-first IDE on **.NET** and **Avalonia**: in-process MCP, cockpit attention model (PFD / Forward / MFD), and the **Intercom** session channel.

!!! tip "New here?"
    Start with **[Concept overview](concept-overview.md)** — five minutes, no Russian required.

## Start here

| Section | Description |
|---------|-------------|
| **[Concept overview](concept-overview.md)** | What the product is and how the cockpit layout works |
| [UI layout (Flight)](ui-ux/cascade-ide-ui-layout-v1.md) | PFD · Forward · MFD, menus, MCP control names |
| [Concept → code map](ui-ux/concept-to-implementation-map-v1.md) | Flight vs legacy Focus/Balanced/Power |
| [ADR navigator by status](site/adr-nav/index.md) | Proposed, Accepted, Implemented — from ADR headers |
| [Full ADR index](../adr/README.md) | All decisions (Russian body; **Summary (EN)** on key ADRs) |
| [ADR status lifecycle](../adr/status-lifecycle.md) | How to read statuses |
| [Current architecture](../architecture/current-architecture-v1.md) | Implementation snapshot |
| [IDE MCP protocol](../MCP-PROTOCOL.md) | Commands for agents and humans |

## Repository

- Source: [github.com/AI-Guiders/cascade-ide](https://github.com/AI-Guiders/cascade-ide)
- Organization: [AI-Guiders](https://ai-guiders.github.io/)
- License: MIT · commercial use — [COMMERCIAL-NOTICE.md](../COMMERCIAL-NOTICE.md)

!!! note "Languages"
    **English:** this home page, [concept overview](concept-overview.md), and pages under `ui-ux/` here.  
    **Russian:** canonical text for most ADRs and deep architecture notes — use **RU** in the header or read the same paths without `/en/`.  
    Selected ADRs include a **`## Summary (EN)`** section at the top (e.g. [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md#summary-en), [0080 Intercom](../adr/0080-intercom-naming-and-multi-party-channel-model.md#summary-en)).

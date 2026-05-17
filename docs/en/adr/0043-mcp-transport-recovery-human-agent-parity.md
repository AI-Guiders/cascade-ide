# ADR 0043: MCP transport recovery parity (human ↔ agent) and host boundaries

**Status:** Proposed  
**Date:** 2026-04-13  

## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0016](0016-agent-client-protocol-external-agent.md) | ACP orthogonal to MCP |
| [0002](0002-debug-human-agent-parity.md) | unified debug state layer for human and agent |

### Outside ADR

| Document | Role |
|----------|------|
| [north-star-cursor-mcp-cascade-workbench-v1.md](../../design/north-star-cursor-mcp-cascade-workbench-v1.md) | Cursor + MCP + Cascade |
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | stdio transport, MCP visibility |

---
## Context

Product north star assumes a working **Cursor (or equivalent) + MCP + CascadeIDE** loop: the agent calls IDE tools and sees state aligned with the human ([north-star § criteria](../../design/north-star-cursor-mcp-cascade-workbench-v1.md#критерии-мы-в-зоне-цели-проверяемые)).

<a id="adr0043-motivation"></a>

### Product logic: MCP as “way to act in the world”

For the agent **MCP is not background detail but the action interface**: without a live tool channel it is not “just less convenient” but **limited in the same way a human without a hand** — except humans usually have **workarounds**: another tool, restart, settings, a “prosthesis” (alternate path to the goal). In the host ecosystem some servers may be **built-in**, most are **external processes**; a human operator on failure can **fix any broken link** in their chain.

**The agent in a typical chat cannot pick the host or restart its own tool:** it sends calls on **that** transport. If MCP is dead or hung the agent **lacks** the self-help class humans treat as normal. That is not pedantic UX complaint — **structural asymmetry of operability**: humans can restore or bypass the chain; the agent often cannot while the same results are expected. That **contradicts parity intuition** “we are in the same work” and deserves explicit product/host design recognition, not “just restart MCP by hand”.

### Technical: where the boundary runs

Host MCP servers are **processes**: stdio exchange with CascadeIDE child `--mcp-stdio`, plus **other host-configured MCP processes** (e.g. separate Roslyn tools server for C#, .NET debug server). That is **not** built-in Roslyn/DAP inside the CascadeIDE window — each process has its own transport and lifecycle managed by the **host**, not the IDE. Agent limitation follows from **ProcessHost** role, not “did not want to” in one repo.

**Link to [0002](0002-debug-human-agent-parity.md):** there — parity of **debug state** in the IDE; here — another axis: **MCP channel availability** and recovery after failure.

## Solution (direction, no deadline commitment)

<a id="adr0043-p1"></a>

1. **Explicit levels:**
   - **Level A — host (Cursor and equivalents):** start, stop, restart **any** MCP server the host holds in config. CascadeIDE **does not replace** the host and does not promise arbitrary restart of neighbor servers from the IDE process.
   - **Level B — CascadeIDE process:** in prospect — **controlled** recovery of **its own** MCP server role (e.g. re-init stdio session **within** supported model), only if safe for UI and not breaking [0008](0008-mcp-contracts-and-testable-infrastructure.md). Separate design/implementation iterations.
   - **Level C — observability:** extend diagnostics “transport alive / command did not arrive / IDE not in MCP mode” so **agent and human** rely on **the same** signals (state snapshot, explicit tool errors), per [MCP-PROTOCOL.md § “MCP visibility”](../../MCP-PROTOCOL.md#видимость-mcp-для-агента-на-будущее-свои-mcp-в-ide).

<a id="adr0043-p2"></a>

2. **Parity goal:** where technically possible, **recovery or explicit degradation** of the channel should be available **both** via human actions in the host **and** via a **supported** contract (IDE tools, host commands, or documented scenario), not only “tell the user to restart MCP”.

<a id="adr0043-p3"></a>

3. **ACP:** external agent per [0016](0016-agent-client-protocol-external-agent.md) stays **orthogonal** to this ADR; MCP transport recovery to IDE is not mixed with ACP transport except **clear errors** and observability.

## Consequences

- **Meaningful** backlog items: MCP health in IDE snapshots, if needed narrow “re-lift own server mode” commands, host alignment — separate.
- Dependence on **Cursor roadmap** (or other client) for full parity “agent restarts any MCP” — reflect honestly in docs and north-star, without promising “everything in one IDE repo”.

## Rejected alternatives

- **Arbitrary restart of foreign MCP processes from CascadeIDE** without host — rejected: violates ProcessHost boundary, unsafe, not portable.
- **Silence on breakage** and treating it as user-only problem — rejected as contradicting north-star on reducing friction moving from Cursor.

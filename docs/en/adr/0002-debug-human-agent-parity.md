<!-- English translation of adr/0002-debug-human-agent-parity.md. Canonical Russian: ../../adr/0002-debug-human-agent-parity.md -->

#ADR 0002: Single debug state layer for human and agent

**Status:** Accepted  
**Date:** 2026-04-02 (retrospective; content recorded previously in [debug-human-agent-parity-v1.md](../debug-human-agent-parity-v1.md))  
## Related ADRs

### Outside ADR

| Document | Role |
|----------|------|
| [architecture-policy.md](../../architecture-policy.md) | debugging |
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | MCP PROTOCOL |

---
## Context

The agent can control debugging through MCP, and the user can control debugging through the UI IDE. If breakpoints, stop, stack, and variables diverge between the outer loop and what a human sees, joint debugging becomes unreliable.

## Solution

**One source of truth** for debug status **inside CascadeIDE**:

1. Breakpoints are consistent with the editor's glyphs and with what tools like `ide_set_breakpoint` and display of a list of points reflect.
2. Stop: the current line and highlighting in the editor are consistent with the MCP (`ide_get_debug_snapshot`, `ide_debug_stack_trace`, debug panel state).
3. Stack and Variables: The debug panel UI and data for the agent via MCP (`ide_debug_stack_trace`, `ide_debug_variables`, `ide_get_debug_snapshot`) comes from **one session and one model**, not two unsynchronized processes.

The external debugger (netcoredbg, DAP) remains the **engine**; **state** for human and MCP goes through the IDE layer.

## Consequences

- The implementation entails an explicit layer (for example, a DAP session in the IDE), rather than separate traversals.
- The document [debug-human-agent-parity-v1.md](../debug-human-agent-parity-v1.md) remains the canonical description of the goal; this ADR captures the decision in ADR format.

## Rejected alternatives

- Two independent circuits without synchronization - rejected as contrary to the purpose of the product.
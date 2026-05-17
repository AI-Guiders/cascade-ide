<!-- English translation of adr/0008-mcp-contracts-and-testable-infrastructure.md. Canonical Russian: ../../adr/0008-mcp-contracts-and-testable-infrastructure.md -->

#ADR 0008: Stable MCP contracts and testable infrastructure

**Status:** Accepted · Implemented  
**Date:** 2026-04-02  
## Related ADRs

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | MCP PROTOCOL |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | `IIdeMcpActions`, MCP executor, contracts in MCP-PROTOCOL |

---
## Context

The agent communicates with the IDE via MCP. Within the application, predictable boundaries are needed: otherwise the tools duplicate business rules, and git/processes cannot be replaced in tests.

## Solution

###MCP

Agent contracts (`IIdeMcpActions`, DTO) - **stable**; changes consciously and with versioning of the contract where this is accepted in the repo.

The MCP implementation **delegates** to services and ViewModels without duplicating the same business rules in each tool handler.

### Outside world

Git, `Process`, long-term operations are behind **abstraction**, which can be replaced in tests (interface or `internal` + `InternalsVisibleTo` - depending on the situation).

## Consequences

New tools are added based on existing services; heavy logic is not copied to the MCP command manager layer.

## Rejected alternatives

- Making each tool self-sufficient with direct access to the file system and processes - rejected as unsupported as the number of commands grows.
<!-- English translation of adr/0007-signals-coupling-and-ui-backpressure.md. Canonical Russian: ../../adr/0007-signals-coupling-and-ui-backpressure.md -->

#ADR 0007: Signals, loose coupling and UI relief

**Status:** Accepted (implementation - strangler)  
**Date:** 2026-04-02  
## Related ADRs

| ADR | Role |
|-----|------|
| [0004](0004-ui-thread-marshaling.md) | Marshalling UI updates via IUiScheduler |

### Outside ADR

| Document | Role |
|----------|------|
| [architecture-migration.md](../architecture-migration.md) | phase 5 |

---
## Context

The IDE has many sources and consumers of signals (assembly, diagnostics, MCP, LSP, etc.). Without rules, connectivity, chaotic calls between subsystems, and thread errors increase. Large streams of text can overload UI bindings.

## Solution

Implement it step by step (strangler), without necessarily changing all the code at once:

1. **Boundaries and notifications** - sources, if possible, only publish facts (event, narrow bus, narrow contract); consumers do not pull each other directly where a reaction to the fact is sufficient. Specific mechanics (event bus, IObservable, mediator) are local. Goal: loose coupling and predictable dependency graph.

2. **Marshaling to the UI thread** - see ADR 0004 (`IUiScheduler` / `UiScheduler.Default`), without Post/Invoke spread throughout the code.

3. **Queues and batching (pointwise)** - where the data flow overloads the UI (long assembly output, avalanche of diagnostics): channel or queue and batch update of the VM, so as not to pull bindings on each line.

**Patterns:** listener/pub-sub for many-to-many topology; producer/consumer for areas with load; The dispatcher layer is primarily for marshaling to the UI thread and, if necessary, routing commands, and not replacing DI.

## Consequences

The step-by-step status is maintained in [architecture-migration.md](../architecture-migration.md).

## Rejected alternatives

- Introducing a heavy global event bus for the entire product at once was rejected in favor of point solutions and strangler migration.
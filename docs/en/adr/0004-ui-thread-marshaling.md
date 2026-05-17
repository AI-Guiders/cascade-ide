<!-- English translation of adr/0004-ui-thread-marshaling.md. Canonical Russian: ../../adr/0004-ui-thread-marshaling.md -->

# ADR 0004: Marshalling UI updates via IUiScheduler

**Status:** Accepted (implementation plan - strangler)  
**Date:** 2026-04-02 (retrospective)  
## Related ADRs

| ADR | Role |
|-----|------|
| [0007](0007-signals-coupling-and-ui-backpressure.md) | Signals, loose coupling and UI relief |

### Outside ADR

| Document | Role |
|----------|------|
| [architecture-migration.md](../architecture-migration.md) | architecture migration |
| [architecture-policy.md](../../architecture-policy.md) | architecture policy |

---
## Context

The IDE has many asynchronous sources (assembly, MCP, LSP, etc.). Spreading `Post`/`Invoke` throughout the code complicates maintenance and increases the risk of accessing the UI from a source other than the UI thread.

## Solution

ViewModel and UI updates after running in the background whenever possible go through **one consistent point** of marshaling: **`IUiScheduler`** and **`UiScheduler.Default`** (implementation on `Dispatcher.UIThread`), rather than arbitrary dispatcher calls throughout the code.

Point queues and batching for overloaded data streams - separately, where necessary (see [ADR 0007](0007-signals-coupling-and-ui-backpressure.md)).

## Consequences

- The new code follows this rule; the existing one migrates gradually.
- The order of implementation in the code is in [architecture-migration.md](../architecture-migration.md) (phase 5).

## Rejected alternatives

- Leaving marshaling completely spread out across the code without an agreed-upon point is rejected in favor of predictability and review.
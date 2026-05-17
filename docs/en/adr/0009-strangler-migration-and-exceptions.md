<!-- English translation of adr/0009-strangler-migration-and-exceptions.md. Canonical Russian: ../../adr/0009-strangler-migration-and-exceptions.md -->

#ADR 0009: Strangler migration and when policy deviations are allowed

**Status:** Accepted  
**Date:** 2026-04-02  
## Related ADRs

### Outside ADR

| Document | Role |
|----------|------|
| [architecture-migration.md](../architecture-migration.md) | architecture migration |
| [architecture-policy.md](../../architecture-policy.md) | navigator |

---
## Context

The code base takes a long time to develop; A complete shovel to fit the new rules is unacceptable in terms of cost. At the same time, we need clear rules for prototypes and merge into the main branch.

## Solution

### Migration

- **New code** - immediately according to the current architectural policy and ADR.
- **Old code** in a monolithic ViewModel - we edit when finalizing a feature or during explicit refactoring; **full transfer is not required** due to an artificial deadline (strangler).

### Deviations

Short prototype or spike - simplification is allowed. Before merging into the main branch, we either bring it to the policy, or explicitly mark it in PR and create a takeaway task.

## Consequences

The main story remains acceptable for review; technical debt is controlled by tasks, not just verbal agreement.

## Rejected alternatives

- “Freeze” refactoring until a big bang - rejected in favor of a gradual strangler.
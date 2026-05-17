<!-- English translation of adr/0005-defer-dynamic-plugins-mef.md. Canonical Russian: ../../adr/0005-defer-dynamic-plugins-mef.md -->

# ADR 0005: Non-target step - dynamic plugins (MEF and analogues)

**Status:** Accepted  
**Date:** 2026-04-02 (retrospective; short link in the table - [architecture-policy.md](../../architecture-policy.md))  
**Updated:** 2026-04-06 - plugins postponed; focus on cockpit ([0021](0021-pfd-mfd-cockpit-attention-model.md)). Details - [§ History](#adr0005-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | first attention model and slots, then plugin host |
| [0024](0024-ide-sdk-and-stable-contracts.md) | future stable extension contracts |

---
## Context

It is possible to load extensions from the DLL directory (MEF and the like). This makes it difficult to build, diagnose, and trust model without an explicit "third party plugins" product goal.

## Solution

**Don't** consider dynamic loading of plugins from a folder as the next target refactoring step. Modules remain **projects in a solution** with explicit registration in DI/composition until there is a separate product target.

**Note:** An IDE without an extension script will look incomplete in the long run; The solution above is about the **queue**, and not about “no plugins needed.” First, it makes sense to **press the attention model and the frontal anchor** (`forward`, PFD/MFD, presets; [0021](0021-pfd-mfd-cockpit-attention-model.md)): otherwise the host of plugins will appear before there are clear slots where to embed them. When we get to extensions, the rule for binding to zones/channels: [0021 § “Plugins and attention model”](0021-pfd-mfd-cockpit-attention-model.md#plugins-attention-binding).

## Consequences

- The architecture is not designed for a mandatory plugin host in the next iterations.
- When a target appears, the decision is reviewed by a separate ADR.

## Rejected alternatives

- Implementing MEF “for growth” without a goal is rejected as a premature complication.

---

## History of changes

<a id="adr0005-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-06 | note: plugins are inevitable in a mature IDE, but the immediate focus is the cockpit and zones ([0021](0021-pfd-mfd-cockpit-attention-model.md)); deferring does not negate the value of extensibility. |
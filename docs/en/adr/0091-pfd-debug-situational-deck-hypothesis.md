<!-- English translation of adr/0091-pfd-debug-situational-deck-hypothesis.md. Canonical Russian: ../../adr/0091-pfd-debug-situational-deck-hypothesis.md -->

# ADR 0091: Hypothesis - PFD instrument deck in debug mode (MFD DebugStack does not exhaust)

**Status:** Proposed  
**Date:** 2026-04-23

## Related ADRs

| ADR | Role |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | single debug snapshot |
| [0011](0011-debug-situational-awareness.md) | situational awareness without "bottom bar only" |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD and Attention |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | Instrument deck, one anchor |
| [0073](0073-pfd-instrument-deck.md) | catalog of PFD-deck options |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | Mfd Pages |

---
## Context

Currently, the detailed debug loop (stack, locals, editor and MCP alignment) is focused on the **Mfd secondary loop** (Debug · Stack page) and the tools dock. Practice and product intuition: **one Mfd surface may not be enough**, because when stopping, the following are simultaneously important:

- position in the code (frontal / editor);
- **brief** summary of debug status in **priority** view area (typically PFD);
- **expanded** picture (deep stack, locals tree, breakpoints, threads if necessary) - now it’s natural to go to Mfd or to the dock.

The cockpit model (PFD = brief situation, Mfd = detailed instruments) **formally respected**, but **physically** the entire “stop signal” ends up either sideways or in one Mfd page, with the risk of switching and vertical scrolling.

<a id="adr0091-hypothesis"></a>

## Suggested direction (without committing implementation)

**Hypothesis:** in an active DAP session and/or with `IsExecutionStopped` have a **separate, minimal** row/deck composition on **PFD** (conditionally *debug situational deck*): for example, “pause / run”, “file:line”, top of the stack frame, active breakpoints counter - in the amount of **1–3 readout**, without duplicating the full-size one debugger

**Mfd** remains the place for **full** lists (stack, locals, then tabs/sections as they grow).

**Risk** of PFD overload: show deck **only** in the context of debugging or only when stopped - visibility policy separately (do not mix with standard WH/EICAS without rules).

<a id="adr0091-open-questions"></a>

## Open questions

- Criterion “Mfd is enough” vs “you need a PFD-deck” (user research, narrow layouts, one monitor).
- Communication with **preset** `presentation` / separate profile “debug session” (see [0090](0090-launch-profiles-and-debug-startup-configurations.md)) - alternative to the permanent desk.
- Do not duplicate [0011](0011-debug-situational-awareness.md); clarify that **situational strip** and **PFD-deck** are different scales (strip vs anchor deck).

<a id="adr0091-consequences-if-accepted"></a>

## Consequences if the hypothesis is accepted later

- Explicit slots/channel for debug data in PFD composer (see [0063](0063-instrument-deck-named-composition-one-anchor.md), [0068](0068-deck-row-payload-and-presentation-projection.md)).
- Regression tests: do not degrade Dark Cockpit / PFD density outside of debug.

<a id="adr0091-rejected"></a>

## Fixed solutions rejected at this stage

- “Make the PFD a complete copy of the debug panel” - **not the goal**; duplication contradicts the PFD/Mfd split.
- “Leave only Mfd and do not touch PFD” - left as the **baseline** line until the hypothesis is tested; This ADR captures the **doubt** that one Mfd page may not be enough for all scenarios.
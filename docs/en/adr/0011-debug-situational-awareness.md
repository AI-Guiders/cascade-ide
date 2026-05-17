<!-- English translation of adr/0011-debug-situational-awareness.md. Canonical Russian: ../../adr/0011-debug-situational-awareness.md -->

# ADR 0011: Situational Awareness in Debugging (Priority Over a “Full” Bottom Panel)

**Status:** Accepted (direction; concrete screens and hotkeys per implementation iteration)  
**Date:** 2026-04-02  

## Related ADRs

| ADR | Role |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | Single state layer |
| [0003](0003-debug-ui-mode-separate-from-power.md) | Debug mode |
| [0012](0012-floating-workspace-chrome.md) | Floating chrome — where to put strips without competing with editor height |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | Debug commands |

---

## Context

While debugging, the user needs **process state** (stopped / running, where stopped, why) and **situational awareness** — without permanently expanding the bottom zone to large height.

Fact: **increasing bottom-panel height** (output, instrumentation, debug) **inevitably eats editor vertical space**. If the only way to “understand debugging” is a full locals/stack list at the bottom, reading code suffers. The **agent** can already get stack and variables via MCP; the **human** needs UX that delivers awareness **in the line of sight** and **at the code**, without keeping the panel inflated.

## Decision

<a id="adr0011-p1"></a>
1. **Product priority** in the debug zone: **state and situational awareness** matter more than the habit of “always seeing the full bottom panel”. Detailed variable/stack lists remain necessary but as a **secondary**, expandable layer.

<a id="adr0011-p2"></a>
2. **Primary layer (implementation direction):**
   - **Explicit debug state** in a persistent or near-persistent zone: *paused / running*, when possible *stop reason* (breakpoint, step, exception), brief **frame context** (at least top of stack one line: method / file:line).
   - **Current line** in the editor (highlight, arrow) stays the mandatory anchor for “where I am in code” — without weakening [0002](0002-debug-human-agent-parity.md).

<a id="adr0011-p3"></a>
3. **Compact “debug strip”** (or equivalent: status bar / narrow zone under toolbar): **small** height vs the editor; when needed **one line** of key info (e.g. top frame + paused). Full tabbed panel — **on explicit action** (click, hotkey, “detailed” mode), not the only way to know state.

<a id="adr0011-p4"></a>
4. **In-code depth (direction):** hints with **values** on identifier hover / at caret (**VS Data Tips analog**), via DAP **`evaluate`** (or adapter equivalent) in the current frame context. Reduces reliance on the locals list in the bottom panel for typical “what’s in this variable?”.

<a id="adr0011-p5"></a>
5. **Bottom panel** is not declared harmful; it is declared **not required for baseline awareness**. Reasonable defaults: **do not auto-expand to maximum height** on stop when a strip/status exists; optional setting “show debug tab on stop” (optional, not a blocker for [§2](#adr0011-p2)–[§4](#adr0011-p4)).

<a id="adr0011-p6"></a>
6. **Parity with the agent** ([0002](0002-debug-human-agent-parity.md)): MCP text responses (`debug_stack_trace`, `debug_variables` with child expansion) remain the channel for fullness; human UX adds a **compressed persistent layer** and **inline/hover**, not a full list everywhere.

## Consequences

- Separate UI work (strip/status), **evaluate on hover** (DAP + editor offset/symbol map), bottom-zone height policy — **without removing** the existing debug panel.
- User documentation (later): where to read state and how to open full stack/locals.
- Vertical splitter conflict remains an engineering constraint; this ADR **does not** require a separate output window but does not forbid it as a follow-up.

## Rejected alternatives (as the sole answer)

- **Bottom panel only** as the human source of debug truth — rejected: conflicts with awareness without losing code on screen.
- **MCP/agent only** without UI improvement — rejected: human and agent remain equal consumers of one state layer ([0002](0002-debug-human-agent-parity.md)), but human UX must be **self-sufficient** without mandatory chat.

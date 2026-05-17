# ADR 0034: Operator incapacitation, Emergency Mode, and optional presence sensing

**Status:** Proposed (intent and boundaries fixed; implementation — separate roadmap).  
**Date:** 2026-04-12 · **Updated:** 2026-04-15

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention model, EICAS as signal layer; **PIC** metaphor — [§1, glossary](0021-pfd-mfd-cockpit-attention-model.md#glossary-kvs) |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | zones and capabilities |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD / “smart” noise reduction when attention is limited |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | external MCP, stdio |

---
## Context

In aviation **Pilot Incapacitation** is when the pilot cannot continue safe control (health, fatigue, stress, loss of consciousness). The cockpit has handover procedures and risk limits.

For **Cascade IDE** as a “development cockpit”, the idea of **similar product-level triggering** comes up periodically: if the system detects the operator **stopped responding adequately** to critical signals (including EICAS-like alerts) or **does not confirm** dangerous agent actions for a long time with autonomy enabled, the IDE enters **Emergency Mode**: hard-fixes a safe working-copy state (e.g. no writes to protected branch, block autonomous mutations), requires explicit exit (session restart, re-auth, manual mode clear).

Separately, use of the **existing** meta-repo **open** MCP **webcam-capture-mcp** is discussed not as “entertainment” but as an **optional signal channel** — in prospect not only “pixel stream” but a **safety-level contract**: operator presence, coarse screen-attention estimate (with a separate analysis path, e.g. **webcam-analysis-mcp**), **liveness** for “operator in the loop” policies — **in addition** to purely software triggers.

### Role in “avionics” and EICAS

When this contract is implemented, **EICAS** (meaning in [0021](0021-pfd-mfd-cockpit-attention-model.md): layer of critical/warning signals for the operator) gains **another input class** — [**PIC**](0021-pfd-mfd-cockpit-attention-model.md#glossary-kvs) state (operator in frame / not in frame / no confirmed attention to monitor), with priority and merge with existing workspace health signals. The aviation analog is not literal combat avionics copy but **by meaning**: “hands on controls” / pilot presence sensors and systems that **recalculate automation risk** on loss of contact (in IDE — risk of autonomous agent work).

This ADR **does not** claim a finished implementation and **does not** mix:

- **repository / build / test health** (existing and planned IDE Health, EICAS in the sense of [0021](0021-pfd-mfd-cockpit-attention-model.md));
- with **human operator state** (new input class and policies).

## Solution (intent)

<a id="adr0034-p1"></a>

1. **Two signal classes (canon for the future):**
   - **A — software:** timeouts on critical confirmations, no reaction to high-priority alerts with agent autonomy on, explicit thresholds “how many times in a row a dangerous step was rejected” — without camera. Additionally **inside the app** (no video) **presence proxies** are possible: **pointer** activity (mouse move, events in IDE client area), **keyboard** (editor input etc.) — cheap private indicator “someone is interacting with the cockpit”. Limits: reading without cursor move, switching to another app, long idle while monitoring — yield **false “not in loop”** or the opposite; policy must combine signals and thresholds, not rely on mouse alone.
   - **B — optional external:** data from **webcam-capture-mcp** (or equivalent) as **in-frame / out-of-frame presence sensor** with user-tunable thresholds and **explicit opt-in**; without opt-in layer B is **inactive**.

<a id="adr0034-p1c"></a>

   - **C — hardware eye tracking (deferred, not baseline):** separate devices/API (Tobii etc.), calibration, cost, OS. **Strongly deferred** roadmap, not required for first presence-policy versions. Risks: for some users **eye movement / saccades** do not fit a “typical” calibration model with **normal field of view** and wide-screen work — the system must not treat that as “not looking” or deny interlock. **Accessibility:** any scenario that could use gaze must have an **equivalent without eye tracking** (focus, mouse, explicit confirmation).

<a id="adr0034-p2"></a>

2. **Emergency Mode (high-level target behavior):** when policy “operator unavailable for safe continuation” is met — **stabilization**: stop or suspend autonomous side-effect actions, **block dangerous git operations** (at least push/merge to protected branches — exact matrix — in design), preserve UI/session state where it does not conflict with safety, **explicit exit** only after deliberate operator action (or administrator in corporate scenario — out of v1 scope).

<a id="adr0034-p3"></a>

3. **Smart Attention / HUD:** on “operator not at screen” signals (layer B or heuristic from missing input — separate) — **reduce intrusiveness** of agent hints (e.g. ghost text, HUD intensity) aligned with [0032](0032-hud-banner-configuration-and-grammar.md) and Dark Cockpit from [0021](0021-pfd-mfd-cockpit-attention-model.md) §9; do not burn context window while the operator is away.

<a id="adr0034-p4"></a>

4. **Privacy and security:** video stream is **not** mandatory for Cascade IDE; storage and processing — per user policy; **no** mandatory image upload from IDE; MCP integration — **local** process and explicit settings. Camera false positives compensated by **conservative** thresholds and manual override.

<a id="adr0034-p5"></a>

5. **ADR boundaries (“biometrics” clarification):** do not replace agent **integrity** protocols in KB. **Biometrics** here means **presence and liveness signals for safety**, not identity recognition or medical diagnosis; separate **consent and policy** (including corporate) — if a channel ever becomes mandatory for a command class. **Do not** promise emotion recognition in early versions; **do not** position camera as sole source of truth — layer A (software) remains the base.

<a id="adr0034-p6"></a>

6. **Liveness / presence without “I’m here” button (target):** with opt-in on, the operator is **not required** to press a periodic confirmation button; the system uses **continuous or frequent** presence/attention signals (within policy and privacy) to distinguish “in the loop” from “dropped out / left” for autonomous-task risk. Combination of **layer A** (mouse/keyboard/window focus) and if needed **layer B** (camera) is set by policy; mouse alone without context is insufficient for strict interlocks.

<a id="adr0034-p7"></a>

7. **Contextual HUD and attention (target):** if **where attention is directed** can be estimated (even coarsely: PFD/telemetry zone vs code editor), agent/UI may **emphasize relevant telemetry** and **dim** secondary per Dark Cockpit ([0021](0021-pfd-mfd-cockpit-attention-model.md) §9) and configurable HUD ([0032](0032-hud-banner-configuration-and-grammar.md)). **Baseline order** — **window/panel focus** heuristics and layer A; **hardware eye tracking** ([§1, layer C](#adr0034-p1c)) — only as optional improvement when it exists and makes product sense; not v1.

<a id="adr0034-p8"></a>

8. **Safety interlock (target):** for a predefined class of **dangerous** actions (examples: deploy, destructive DB ops, irreversible git ops — exact list by policy) with active “confirmed presence required” policy — **block or two-step confirmation** if sensors **do not** confirm the operator is **in the loop** / attention to work zone (proxies: focus, layer A; if present — B; **not** mandatory literal “gaze” until a reliable accessible channel exists). This **supplements** PFD confirmations ([0017](0017-multi-window-workspace-and-agent-surfaces.md)), does not replace KB integrity.

## Consequences

- Architectural memory slot for roadmap: policies, settings, Emergency mode UI, git/agent autonomy integration.
- Implementation needs separate tasks: state model, policy tests, mode-exit UX, user documentation.

## Rejected / deferred alternatives

- **Camera only without software thresholds** — high false-positive and privacy risk; layer A remains base.
- **Mandatory camera for autonomy** — contradicts opt-in and varied environments (server without camera, org policy).
- **Mandatory eye tracking** for any critical scenario — rejected: cost, not everyone has a device, **accessibility and diversity of eye-movement patterns** vs field of view; allowed only as **option** ([§1, layer C](#adr0034-p1c)).

## Open questions

- Exact git-operation matrix in Emergency Mode and interaction with [0019](0019-shared-git-core-ide-and-git-mcp.md).
- Unified **signal registry** (repo EICAS vs “operator unavailable”) in UI — one layer or two visually linked; **priority** when critical repo alert and “operator not in loop” coincide.
- Separate **IdeCommands** / MCP tool for forced Emergency with **confirmation** (like PFD confirmations — see [0017](0017-multi-window-workspace-and-agent-surfaces.md), [0031](0031-agent-chat-clarification-batches-and-threading.md)).
- **Gaze/focus pipeline:** are “active panel / attention zone” heuristics without camera enough, or does interlock need a separate path (e.g. frame analysis in **webcam-analysis-mcp**); calibration and false positives.
- **Eye tracking (if ever):** calibration for different profiles; do not conflate “wide field of view” with tracker signal quality; **“without eye tracking”** mode always available in settings.
- **Mouse/keyboard as signal:** idle thresholds (ms), window focus, mixing with alerts; scenarios “reading without mouse move”, “left for browser” — explicit in policy or UX hints.
- Legal and organizational limits on **biometric** signals in corporate settings (even as liveness, not identification).

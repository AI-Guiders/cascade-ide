<!-- English translation of adr/0042-pre-flight-planned-changes-and-review-before-apply.md. Canonical Russian: ../../adr/0042-pre-flight-planned-changes-and-review-before-apply.md -->

# ADR 0042: Pre-flight briefing — Planned Changes and Review Before Apply

**Status:** Proposed  
**Date:** 2026-04-13

## Related ADRs

| ADR | Role |
|-----|------|
| [0039](0039-workspace-navigation-affordances.md) | semantic map and related files |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD, situational awareness |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | agent, ACP, MCP |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | clarification batches; orthogonal to this ADR |
| [0016](0016-agent-client-protocol-external-agent.md) | ACP as external agent transport |

---
## Context

With **high agent autonomy**, the cost of the “first overwritten line” error is higher than in “assistant at hand” mode: later rollback mixes with other changes, partial applies leave the repo unclear, trust falls faster than iteration speed rises.

In aviation a briefing before action (**pre-flight / pre-action briefing**) sets the **big picture**: what will be done, in what order, what risks, what is forbidden. Analog for the development cockpit: **pilot (human) and agent agree on intent before changes physically hit disk** — not replacing code review, but a **safeguard** and carrier of **situational awareness (SA)**.

Classic IDEs and “agent writes files immediately” flows force a choice between **blind trust** and **line-by-line approval** (slow). Cascade IDE targets **bundles of meaning**: plan and impact boundaries first, then agreed apply with **partial** approval and **clean** rejection.

## Solution (principles)

<a id="adr0042-p1"></a>

### 1. Two explicit stages: Planned Changes and Review Before Apply

**Planned Changes (“flight plan”):** before any physical write to working files the agent (via ACP/MCP and IDE contract) emits **structured intent**: not only “list of paths”, but **tie to semantic map** — which nodes/boundaries (contracts, tests, project boundaries, related files per [0039](0039-workspace-navigation-affordances.md)) are touched or at risk. The human may **approve part of the plan** and **exclude** files/areas (“do not touch here”).

**Review Before Apply (“alignment before landing”):** after the plan is fixed the system shows **final consent before apply**: not necessarily per-token approval, but mandatory **conscious arming** before write.

<a id="adr0042-p2"></a>

### 2. Semantic layer over text

**Text diff** (as in Git) remains the base verification layer. On top, the target contour is **semantic diff**: what changes in **contracts, visible symbols, public APIs** (orient: Roslyn and project model; north-star [0039](0039-workspace-navigation-affordances.md)). Goal — highlight **risk to neighboring modules and tests**, not only syntax shuffling.

Exact v1 depth is **not** mandatory in this ADR: staged rollout allowed (first “touched files + link kinds from navigation context”, then symbol-level refinement).

<a id="adr0042-p3"></a>

### 3. Preview and rejection without debris

Before confirm the user sees **in-place preview** (ghost edits image in the main work area — Forward / forward glass in [0021](0021-pfd-mfd-cockpit-attention-model.md) terms): what **will take effect** after Confirm.

**Reject / cancel** at this stage must return editor and work state to a **stable snapshot before the apply attempt** — no partially inserted fragments or “half-applied” files. Implementation (staging patches, buffer, transactional model) is implementation choice; the principle is normative.

<a id="adr0042-p4"></a>

### 4. State machine (analog “Arming the Autoland”)

Fix explicit states for the change cycle, for example:

| State | Meaning |
|-------|---------|
| **DISARMED** | No approved plan to write to disk |
| **PLANNED** | Planned Changes issued; human may edit/trim |
| **ARMED** | Plan and preview agreed; ready for one atomic write (or explicitly defined chunks) |
| **COMMITTED** | Changes applied; then normal git/workflow |

Transitions **ARMED → COMMITTED** only on explicit user action (or higher autonomy safety policy — separate decision, not mixed with this ADR baseline). “Course set — verify — touch down” (**autoland arming**) stays product metaphor; UI state names may differ.

<a id="adr0042-p5"></a>

### 5. Transport and authority

**MCP / `IdeCommands`** ([0008](0008-mcp-contracts-and-testable-infrastructure.md)) — command and observability channel. **ACP** ([0016](0016-agent-client-protocol-external-agent.md)) — external agent transport. **Authority** for dangerous state transitions — IDE and user; the agent must not bypass **Review Before Apply** with hidden file writes under the stated contract (under normal security configuration).

<a id="adr0042-p6"></a>

### 6. Link to MFD and SA

**Planned Changes** visualization fits the **secondary contour** (MFD) or a dedicated card per [0021](0021-pfd-mfd-cockpit-attention-model.md): the user sees **not “N files”**, but **meaningful impact areas** (contract ↔ tests ↔ neighbor module). That is the product answer to SA for high autonomy.

## Non-goals (v1 of this ADR)

- Full **formal verification** or behavioral equivalence proof.
- Replacing **Git** or mandatory **semantic merge** for the whole repo without a text layer.
- Promise that semantic diff is **always** complete for all languages; north-star remains **C# / .NET** per [0039](0039-workspace-navigation-affordances.md).

## Consequences

- Explicit **product contour** between “discussed intent” and “touched disk”, aligned with cockpit metaphor.
- Implementation needs **editor/document snapshots** before apply and command discipline in [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) (new or refined commands — separate iterations and ADRs if needed).
- Stronger requirement on **navigation and semantic layer** ([0039](0039-workspace-navigation-affordances.md)): without it the plan degrades to “file list” and loses claimed SA.

## Open questions

- **Partial approval** granularity (file, range, symbol) and effect on apply atomicity.
- Policy for **fully autonomous** mode (if ever allowed): separate safety tier, not mixed with baseline “human in the loop”.
- Link to **Emergency Mode** and interlock ([0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md)) — reference in later revisions if needed.

# ADR 0100: Project constitution

**Status:** Accepted  
**Date:** 2026-04-26

## Related ADRs

| ADR | Role |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | related ADR |
| [0009](0009-strangler-migration-and-exceptions.md) | related ADR |
| [0027](0027-small-team-focus-vs-public-maturity.md) | related ADR |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | related ADR |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | related ADR |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | related ADR |
| [0099](0099-ide-databus-typed-events-and-projections.md) | related ADR |
| [architecture-policy.md](../../architecture-policy.md) | outside numbered ADR |

## Summary

- **Project constitution:** long-lived principles, invariants, governance.
- Order of changes to “foundation”; hub with [0077](0077-tech-principles-hub.md).

For a short onboarding path in English, see [concept overview](../concept-overview.md).

---

<a id="adr0100-purpose"></a>

## 1. Purpose

This constitution records Cascade IDE’s long-term principles so day-to-day decisions stay aligned across architecture, product direction, and contribution process.

It is a meta-level ADR: not a feature spec, but a stable agreement on **how decisions are made**.

---

<a id="adr0100-mission"></a>

## 2. Mission

Build a keyboard-first .NET IDE with a cockpit-style interface where human and agent scenarios share one reliable operational model.

Key goals:

- transparent state and observability,
- deterministic, testable architectural boundaries,
- practical velocity for a small team,
- open-source collaboration without losing product direction.

---

<a id="adr0100-principles"></a>

## 3. Constitutional principles

1. **Single source of truth beats convenient duplication.**  
   State projections must have one canonical source; derived views may cache but must not diverge in meaning.

2. **Typed contracts beat ad-hoc glue.**  
   Channels, CCU boundaries, and DataBus events must be explicit and testable.

3. **UI is projection, not business-logic storage.**  
   ViewModels orchestrate; computation and aggregation live in dedicated modules and services.

4. **Strangler beats “big bang” rewrites.**  
   Migration proceeds in vertical slices with guardrails and stepwise stabilization.

5. **Human–agent parity is designed in.**  
   Debug and operation state must be observable and controllable for both interactive UI and automation. Parity also means **not reducing the agent to a pure tool** while working toward a solution: inside the IDE contour the agent is a process participant (partner dialogue), not only an executor under the operator’s sole will. The operational formulation for the built-in MAF IDE agent is the `agent_system` section in [`AiPrompts/maf-ide-agent.prompts.md`](../../AiPrompts/maf-ide-agent.prompts.md); evolving it should update that resource and cross-link here if needed, without bloating the constitution.

6. **Open source first, compatible with commercialization.**  
   Architecture and dependency policy must preserve open collaboration and future monetization options.

---

<a id="adr0100-hard-limits"></a>

## 4. Non-negotiable guardrails

- No hidden cross-layer bypasses around channel and CCU boundaries.
- No untyped event payloads on the IDE domain bus.
- No direct UI-framework coupling inside compute units.
- No new dependencies without explicit license visibility.
- No irreversible architectural shifts without an ADR update.

---

<a id="adr0100-governance"></a>

## 5. Governance

1. **ADR-first for durable decisions.**  
   Any change to boundaries, contracts, or operational principles is recorded in an ADR.

2. **Analyzer and CI checks where feasible.**  
   Repeated architectural violations move from guidance to build-time enforcement.

3. **Living documents with explicit status.**  
   Proposed → Accepted → Implemented cycle is required for project memory.

---

<a id="adr0100-contributions"></a>

## 6. Contribution contract

Contributors are expected to:

- preserve existing architectural invariants unless changed deliberately via ADR;
- prefer additive, reviewable slices over broad unstructured edits;
- add tests for changed contracts and boundary regressions.

Maintainers are expected to:

- keep guardrails explicit and current;
- propose migration paths, not only prohibitions;
- align product evolution with these principles.

---

<a id="adr0100-amendments"></a>

## 7. Amendment process

This constitution may change only through:

1. a dedicated amending ADR referencing this document;
2. explicit rationale for each changed principle and guardrail;
3. maintainer acceptance with an implementation plan where applicable.

---

<a id="adr0100-consequences"></a>

## 8. Consequences

<a id="adr0100-consequences-positive"></a>

### Positive

- Stable long-term direction during fast iteration.
- Less architectural drift in mixed human+agent development.
- More predictable onboarding and review.

<a id="adr0100-consequences-negative"></a>

### Negative

- Extra discipline at the gate for foundation-touching changes.
- Some experiments move slower until boundaries are explicit.

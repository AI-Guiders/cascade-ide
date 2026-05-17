<!-- English translation of adr/0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md. Canonical Russian: ../../adr/0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md -->

#ADR 0104: Reasoning Substrate and Cognitive Decomposition Loop for MAF

**Status:** Proposed  
**Date:** 2026-05-05

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → surface composer → surface (Agent-first display) |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | CLI for agent contract (parity with MCP) and snapshot tests |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD (control flow) |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus - Typed Events and State Projections |
| [0100](0100-project-constitution.md) | Constitution project |
## Summary

- **Reasoning Substrate** for MAF is a separate layer of thinking, not just if-then routing.
- Cycle: top-down model → bottom-up grounding → integration → execution.
- Rule/scorer - lower compiled layer of restrictions and fallback.
- Compatible with KB-Base (integrity, scope, completion discipline).


---

<a id="adr0104-context"></a>

---
## Context

The current routing of instructions solves the problem of choosing behavior, but does not specify the source of thinking. `if-then` chains are useful as an executable form, but they are a record of a decision already made, not a process in which understanding of a problem emerges.

For long and architecturally complex tasks, the agent needs a separate cognitive layer: a space of competing hypotheses, system decomposition, testing hypotheses with facts, and controlled uncertainty reduction.

---

<a id="adr0104-decision-summary"></a>

## Solution in one sentence

Introduce **Reasoning Substrate** as the main mechanism of the agent's thinking, and leave rule/scorer as the lower compiled layer of restrictions and fallback behavior.

---

<a id="adr0104-goals"></a>

## Goals

1. Separate “thinking” and “instruction routing” as different levels.
2. Fix a stable cycle: Top-Down model building + Bottom-Up grounding + Integration + Execution.
3. Reduce context noise: active focus only on the current subsystem and its contracts, store the rest as compressed invariants.
4. Maintain compatibility with the KB base contract (**KB-Base**, previously designated as L0: integrity, epistemic distrust, completion discipline, scope clarity).

<a id="adr0104-non-goals"></a>

## Non-goals (in the first stage)

- Full automatic verification of the architecture for compliance with ADR.
- Replacing the entire router with a semantic scorer.
- Mandatory architectural mode for minor local edits.

---

<a id="adr0104-architecture"></a>

## Architectural model

<a id="adr0104-layer-reasoning"></a>

### Level A: Reasoning Substrate

A workspace where the agent:

- holds multiple interpretations of a task;
- selects not the “most familiar pattern”, but the step with the maximum reduction of uncertainty;
- distinguishes between fact, hypothesis, invariant and solution;
- Maintains local focus across subsystems and periodically checks global consistency.

<a id="adr0104-layer-policy"></a>

### Level B: Compiled Policy Layer (executable control)

Rule/scorer layer:

- provides security, budget, deterministic fallback;
- formalizes already accepted heuristics;
- does not replace Reasoning Substrate as a source of solution.

---

<a id="adr0104-cognitive-cycle"></a>

## Cognitive loop (required logic)

1. **Top-Down pass (Decompose):**
   - highlight subsystems/circuits;
   - for each subsystem, record the goal, data, contracts, risks;
   - select `Focus Subsystem` based on the expected reduction in uncertainty.

2. **Bottom-Up pass (Ground):**
   - collect facts about `Focus Subsystem` from code, tool-output, history;
   - clearly separate facts from hypotheses;
   - update the uncertainty map.

3. **Integration pass:**
   - check intersubsystem connections and global risks:
   - consistency, privacy/security, performance, coupling, operational constraints.

4. **Execution pass:**
   - perform one verifiable step in `Focus Subsystem`;
   - update compressed invariants and the next step of the loop.

---

<a id="adr0104-kb-base-compat"></a>

## KB-Base compatibility (KB baseline)

The cognitive loop runs **on top** of KB-Base and does not override it:

- integrity/security (non-negotiable boundaries),
- epistemic-default-distrust (fact checking),
- response-one-step-before-finish (pressure before the final),
- scope-disambiguation-all-everywhere (strict scope).

If reasoning and policy diverge, the KB-Base contract takes precedence.

---
<a id="adr0104-prompt-representation"></a>

## Prompt view

- `meta_decomposition_contract` (always-on light): short reasoning cycle contract.
- `architecture_mode_contract` (on-demand): advanced mode for design and decomposition tasks.

Note: During the transition period, these contracts may technically be delivered through the existing pack mechanism, but in the ADR model these are not “two more packs”, but named reasoning contracts.

<a id="adr0104-architecture-mode-response-shape"></a>

### Required response structure in architecture mode

- `System Goal`
- `Subsystem Decomposition`
- `Focus Subsystem (current step)`
- `Interfaces/Dependencies`
- `Next Step`

---

<a id="adr0104-distribution-contract"></a>

## Distribution Contract (KB delivery with CIDE)

For reproducible behavior, reasoning substrate CIDE must be supplied with a minimum KB layer.

**Format contract:** knowledge and prompt artifacts are supplied as `md`; loading configuration and prioritization is `toml-first`.

<a id="adr0104-distribution-required"></a>

### Required artifact (always included)

- **KB-Base bundle**:
  - integrity/security;
  - epistemic baseline;
  - completion discipline;
  - scope clarity.

Without KB-Base, the installation is considered degraded according to the reasoning contract.

<a id="adr0104-distribution-optional"></a>

### Optional artifacts (on request)

- Extended playbook/knowledge packages (domain and project).
- Local or user knowledge layers.
- TOML connection/priority manifests (for example, sections in `workspace.toml` or a separate `kb-bundle.toml`).

<a id="adr0104-distribution-offline"></a>

### Offline / degraded behavior

- In the absence of optional packages, the agent continues to work in the KB-Base + local code context.
- In the absence of KB-Base normal mode is not allowed: only explicit degraded mode with explicit diagnostics in trace.

---

<a id="adr0104-kb-base-extended-policy"></a>

## KB-Base vs KB-Extended Policy

<a id="adr0104-kb-base-inclusion-criteria"></a>

### Criteria for inclusion in KB-Base

An artifact is included in KB-Base if the following conditions are simultaneously met:

1. Needed in the majority (about 60-80%) of CIDE working scenarios.
2. Without it, the agent's behavior becomes unreproducible or unsafe.
3. It cannot be replaced without loss of meaning by a short invariant in prompt.

If at least one condition is not met, the artifact is classified as KB-Extended.

<a id="adr0104-kb-layering-table"></a>

### Primary diversity

| Layer | Destination | Typical artifacts |
| --- | --- | --- |
| KB-Base (required) | Basic behavior and routing contract | integrity core/spec, basic router index, operating principles, key core-playbooks |
| KB-Extended (optional) | Domain extensions, deep evidence and cross-domain matrices | domain playbooks, large evidence corpora, highly specialized runbooks |

<a id="adr0104-kb-operational-rule"></a>

### Operational rule

- In normal mode, CIDE always loads KB-Base.
- KB-Extended is mixed on-demand by task/intent.
- KB-Base should remain compact: as it grows, the composition is revised, and not inflated endlessly.

<a id="adr0104-kb-include-governance"></a>

### KB-Base include governance checklist

For each change to `knowledge/kb-base-cide.include`:

1. **Usefulness criterion:** the file is needed in most everyday CIDE scenarios.
2. **Mandatory criterion:** the absence of a file breaks the reproducibility or safety of the behavior.
3. **Non-duplication criterion:** the meaning cannot be contained in a shorter invariant.
4. **Budget:** the change should not uncontrollably increase the size of the KB-Base bundle.
5. **Cohesion:** dangling links without underlying context are not added to include.
6. **Checking the build:** after the change, it is necessary to run `pwsh ./scripts/build-kb-base-cide.ps1` in the **canon of notes repository** (the root is set via `AGENT_NOTES_CANON_PATH`, otherwise your hooks will default to `D:\Experiments\agent-notes` - see. `scripts/git-hooks/pre-commit.ps1` in the root of PersonalCursorFolder; not to be confused with the `open/agent-notes` directory inside `financial-open`). The script collects `dist/kb-base-cide.zip` and copies to CIDE: the **`CASCADE_IDE_ROOT`** variable, the **`-CascadeIdeRoot`** parameter, or the adjacent `../cascade-ide` from the canon root. **`-SkipPublishToCascadeIde`** - zip only. Backup copy: `cascade-ide/tools/publish-kb-base-embed.ps1`.
7. **Mode check:** If possible, check normal and degraded boot scenarios.

---
<a id="adr0104-trace"></a>

## Trace and observability

Trace should show not only the choice of contracts/modes, but also the state of thinking:

- `focus_subsystem`
- `candidate_hypotheses`
- `selected_hypothesis_reason`
- `global_invariants`
- `integration_risks`
- `confidence`
- `policy_fallback_applied` (yes/no)

---

<a id="adr0104-hypothesis-schema"></a>

## Hypothesis Record Schema

To work structurally with competing hypotheses, a minimal hypothesis entry is introduced:

- `id` — stable identifier within the cycle.
- `statement` - formulation of the hypothesis.
- `assumptions` - explicit assumptions.
- `evidence_for` - confirmation signals.
- `evidence_against` - disproving signals.
- `falsifiers` - which observations/results will refute the hypothesis.
- `confidence` — numerical confidence (0..1).
- `next_probe` - next test step.

Step selection rule: priority is given to the hypothesis whose `next_probe` gives the greatest expected reduction in uncertainty.

<a id="adr0104-trace-json-template"></a>

### Trace JSON template (draft runtime view)

This JSON is not a KB delivery format, but an internal runtime observability format (trace/DTO).```json
{
  "focus_subsystem": "messaging",
  "global_invariants": [
    "event-ordering is monotonic per conversation",
    "privacy boundaries between private and group channels"
  ],
  "candidate_hypotheses": [
    {
      "id": "H1",
      "statement": "Duplicate messages caused by non-idempotent consumer retry.",
      "assumptions": [
        "retry policy can replay same event",
        "dedup key is not persisted"
      ],
      "evidence_for": [
        "duplicates appear after retry spikes",
        "same payload hash observed twice"
      ],
      "evidence_against": [
        "no duplicates in low-load path"
      ],
      "falsifiers": [
        "dedup key persisted and checked before write",
        "replay test shows single insert"
      ],
      "confidence": 0.62,
      "next_probe": "Run replay test with forced retry and inspect dedup storage writes."
    }
  ],
  "selected_hypothesis_id": "H1",
  "selected_hypothesis_reason": "Highest expected uncertainty reduction with one bounded experiment.",
  "integration_risks": [
    "cross-service event ordering drift"
  ],
  "policy_fallback_applied": false
}
```
---

<a id="adr0104-storage-model"></a>

## Storage Model (runtime vs knowledge)

In order not to confuse operational thinking and the long-lived canon, a division is introduced:

- **Knowledge (`knowledge/*.md`)**: long-term rules, playbooks, evidence and agreements.
- **Reasoning Memory Block (runtime)**: brief running state of the current task in CIDE.

Reasoning Memory Block is not automatically part of the KB canon and does not require an entry in `knowledge/` for each step.

<a id="adr0104-runtime-memory-block"></a>

### Runtime memory block (minimum content)

- `session_id`
- `task_id`
- `focus_subsystem`
- `global_invariants[]`
- `candidate_hypotheses[]`
- `selected_hypothesis_id`
- `next_probe`
- `updated_at_utc`

<a id="adr0104-storage-modes"></a>

### Storage modes

1. **Phase 1 (MVP):** in-memory + binding to the current chat/session state.
2. **Phase 2:** optional serialization into workspace-local state (for example, `.cascade-ide/reasoning-state.json`) for recovery after restart.
3. **KB write-back:** in `knowledge/` only stable knowledge according to an explicit criterion is written, and not intermediate RAM.

---

<a id="adr0104-consequences"></a>

## Consequences

**Pros**

- Less imitation of thinking through keyword routing.
- More stable solutions in long and inter-subsystem problems.
- Better explainability of “why the next step was chosen.”

**Cons**

- Higher complexity of implementation and validation.
- You will need the discipline of working with hypotheses and traces.
- Possible overhead for small tasks (lightweight mode required).

---

<a id="adr0104-rollout-plan"></a>

## Implementation plan

1. Add contracts `meta_decomposition_contract` and `architecture_mode_contract` to `AiPrompts/maf-ide-agent.prompts.md`.
2. Router: enable architecture mode using the intent "architecture/decomposition/system design", but saving fallback.
3. Expand trace with reasoning substrate fields ([§ Trace and observability](#adr0104-trace)).
4. Add tests to:
   - activation of architecture mode,
   - correct fallback in KB-Base-only,
   - focus-subsystem stability in a multi-pass cycle.
5. The second step is to add a semantic scorer as a signal for the policy layer, but not as a replacement for the reasoning cycle.

---

<a id="adr0104-open-questions"></a>

## Open questions

- Where to store compressed invariants between steps: trace-only or a separate memory block?
- How to formally evaluate the “reduction of uncertainty” when choosing a focus subsystem?
- Is there a need for a limit of subsystems in the primary decomposition (for example, max 5) to control the contextual budget?
- Where to draw the line between “light mode” and “architecture mode” so as not to overload simple queries?

---

<a id="adr0104-implementation-status"></a>

## Implementation status

**Not started.** After ADR approval:

1. Update prompt contracts (`meta_decomposition_contract`, `architecture_mode_contract`).
2. Update the router and trace under the reasoning substrate.
3. Add tests for a new thinking cycle and fallback policy.
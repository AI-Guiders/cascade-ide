<!-- English translation of adr/0020-agent-reasoning-visibility-and-provider-limits.md. Canonical Russian: ../../adr/0020-agent-reasoning-visibility-and-provider-limits.md -->

# ADR 0020: Agent Reasoning Visibility and LLM Provider Limits

**Status:** Proposed  
**Date:** 2026-04-06  

## Related ADRs

| ADR | Role |
|-----|------|
| [0016](0016-agent-client-protocol-external-agent.md) | External agent, stdio |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Contracts and infrastructure |
| [0002](0002-debug-human-agent-parity.md) | Human/agent parity on “what happened” |

---

## Context

1. In typical IDE-agent UX (including hosts with stepwise “inference”), the user sees a **compressed trace**: reasoning fragments, tool calls, outcome — but not necessarily the model’s **full** internal reasoning chain in one pass.

2. **Cascade IDE** aims to give agent and human a **shared anchor surface** (commands, MCP, state). Natural expectation is **more transparency** than a black box: what the model did, **in what order**, with which tool inputs/outputs.

3. **Limitation is not UI-only:** LLM **providers** differ in response contracts. Some models expose separate fields (extended reasoning, structured steps); some **hide** raw chain-of-thought by safety or product policy; some return only final text and usage metadata. Expecting “full internal monologue like a human” **regardless of provider** is unrealistic.

4. Confusing **“bad UI”** vs **“API does not expose it”** hurts trust: the user should know where **we show everything available** vs where there is a **provider-side gap**.

## Decision (direction)

1. **Layered visibility model** (conceptual; details when integrating a concrete provider):
   - **L0 — always:** final assistant reply to the user, error/cancel facts, **fully traceable actions** (MCP, git, terminal, etc. — what the IDE already controls).
   - **L1 — on by default when the API provides it:** streaming reasoning text chunks, separate `thinking` / analog fields, explicit “steps” from structured responses.
   - **L2 — optional / advanced:** raw request/response log (secrets redacted), token diagnostics, timings — for integration debugging and “what went in/out” trust.

2. **Honest labeling:** if the provider **does not return** hidden reasoning, do not fake it with placeholders; show an **explicit message** such as “provider does not expose internal chain” (user-facing wording, no jargon outside debug mode).

3. **Per-provider adapter:** map response fields (text, structure, streaming) into a **single internal chat event model**; the visibility layer subscribes to that model, not raw per-vendor JSON in UI.

4. **Parity of meaning with [0002](0002-debug-human-agent-parity.md):** human and agent should have **equally complete** information about **actions in the environment** (tools, commands). **Parity is not guaranteed** for hidden model weights — it is bounded by the API.

## Consequences

- Explicit **provider roadmap** dependency: improving “thought transparency” without changing model/mode may be **impossible**.
- Integration tests: scenarios for streaming display, missing field X, without brittle provider legal text.
- Cascade user docs: **what** the IDE can promise for reasoning visibility vs **what** it cannot.

## Rejected alternatives

- **Final answer only**, no tool traces — contradicts IDE goals and agent trust.
- **Generate or “reconstruct” reasoning after the fact** for polish — false completeness, undermines trust.
- **Identical guarantees for all providers** — unrealistic without an open reasoning standard on the API side.

## Open questions

- Concrete contracts for chosen providers (Anthropic, OpenAI, local, etc.) — **which fields** are available with production Cascade IDE keys.
- Whether L2 logs need a separate **retention policy** (local only, TTL, secret exclusion).

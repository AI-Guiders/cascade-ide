<!-- English translation of adr/0027-small-team-focus-vs-public-maturity.md. Canonical Russian: ../../adr/0027-small-team-focus-vs-public-maturity.md -->

# ADR 0027: Small Team (Human + Assistant) vs “Open” Maturity — Two Axes, Not One Queue

**Status:** Accepted  
**Date:** 2026-04-08  
**Updated:** 2026-04-08 — Accepted; discoverability and axis B triggers. Details — [§ History](#adr0027-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0024](0024-ide-sdk-and-stable-contracts.md) | Contracts and boundary discipline without promising SemVer “tomorrow” |
| [0005](0005-defer-dynamic-plugins-mef.md) | Deferred plugin host |
| [0013](0013-command-surface-and-discoverability.md) | Discoverability — separate axis from core speed |
| [0010](0010-ui-modes-toml-configuration.md) | TOML as honest configuration layer |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Canonical path and format for user `settings.toml` and secrets |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML-first; holistic settings UI deferred; point UI = canon facade |

### Outside ADR

| Document | Role |
|----------|------|
| [README](README.md) | Policy: major direction changes — separate commit + ADR |
| [onboarding-first-run-v1](../../design/onboarding-first-run-v1.md) | Living first-run/onboarding sketch — **not** required for “opening the repo” per this ADR; reference when deliberately deepening UI |

---

## Context

CascadeIDE targets a **mature desktop IDE** that can be **opened** to other developers and integrators. In parallel, actual delivery speed today relies on a **very small “team”**: author(s) plus assistant (LLM) in paired work.

Without an explicit split, risk is:

- **Spreading thin** — building “store-ready” (onboarding, external-tool installers, polished settings UI) and losing core velocity; or
- **Boundary debt** — shipping “for ourselves only” and rewriting contracts, config paths, and integrations at the first external consumer.

We need **one recorded model**: how to combine long-term openness and short-term focus without contradiction.

---

## Decision

Separate two independent dimensions — **product shape** and **delivery queue**.

### 1. Axis A — “shape” (boundary maturity)

**Invest early** in what is expensive to change later:

- stable **extension contracts** and protocols (per [0024](0024-ide-sdk-and-stable-contracts.md): layers, capabilities, out-of-proc integrations);
- **one source of truth** for configuration (global `settings.toml`, repo `workspace.toml`, etc. — [0010](0010-ui-modes-toml-configuration.md));
- **ADRs** and navigator ([architecture-policy.md](../../architecture-policy.md)) on direction changes — so external readers and future-you see *why*, not only *what* in code.

This is **not** a commitment to full onboarding or marketing packaging today; it **is** a commitment **not to break trust in boundaries** without an explicit step.

### 2. Axis B — “queue” (what the human–assistant pair ships in the sprint)

**Hard-prioritize** what delivers value to **current** process users (including paired assistant work): debugging, editor, diagnostics, LSP, predictable file paths.

**Deferred backlog** (see **triggers** below) for typical “store” items when they **do not** unblock core:

- standalone settings app, heavy external-server installer from IDE, extended first-run wizard;
- discoverability polish for strangers ([0013](0013-command-surface-and-discoverability.md) remains direction, **not** a core-speed blocker).

**Minimally sufficient** discoverability for “opening the repository” on axis B: **documentation** (including TOML and config paths), **examples** in the repo, **ADRs** with rationale — no objection as v1 baseline; full UI wizard not required until a trigger fires. Richer first-run/onboarding when the queue allows: sketch [onboarding-first-run-v1](../../design/onboarding-first-run-v1.md) — **not** part of this ADR’s minimum, a place for future work.

### Triggers: when to pull deferred axis B work

Raise “for strangers” tasks (wizards, installers, strong discoverability polish) from the deferred backlog **deliberately** when **at least one** holds:

1. **First external contributor** (outside current repo authors) with a real PR/issue — signal that “clone and build” is tested in a foreign context.
2. **Release candidate** or explicit milestone “showing the build wider than ourselves”.
3. **Explicit pain or request** from user/integrator (including recurring) — do not wait for “store” if it already hurts.
4. **Infrastructure necessity** — axis B task unblocks core or tests (then it is not “audience only”, a mixed payment; see §3).

Until a trigger fires, the queue stays on core and boundaries (axes A and “our” users).

### 3. Rule for reconciling axes

- If work **strengthens boundaries** (contract, protocol, file format) — take it **earlier**, even with a small team.
- If work **only reduces friction for strangers** — take it **after** core is stable for “our” users, or when a **trigger** from §2 fires.

### 4. Explicit heuristic for chat / review

For any large effort ask: *“Is this axis A (boundaries) or axis B (audience)?”*  
If both — split into two commits/approaches by meaning (logical commit policy in repo Cursor rules).

---

## Consequences

- **Planning:** issues/notes may label `boundary` vs `audience-friction` (names repo discretion); do not mix “must ship for open” without naming the axis.
- **SDK and [0024](0024-ide-sdk-and-stable-contracts.md):** “small team” does **not** waive contract discipline; contracts save human+assistant time on boundary alignment.
- **Open source and “IDE for others”:** readiness is not only wizard count but **predictable** configs, documented decisions, testable boundaries — compatible with a small team if the queue is honest.

---

## Rejected alternatives

- **“Hack for ourselves first, re-layer for people later”** — rejected: expensive boundary rewrites; axes A/B separate “fast inside” from “careful outside” without rejecting the latter.
- **“Maturity = mass-market UI now”** — rejected: mixes axes and kills core throughput with small bandwidth.
- **“While there are two of us, ADRs are unnecessary”** — rejected: ADRs are cheaper for a pair than oral memory and assistant session drift.

---

## Change history

<a id="adr0027-history"></a>

| Date | Change |
|------|--------|
| 2026-04-08 | Accepted; clarified minimum discoverability (docs + examples + ADR, onboarding sketch link); added **triggers** for pulling axis B from backlog. |

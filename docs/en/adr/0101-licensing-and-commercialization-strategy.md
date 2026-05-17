<!-- English translation of adr/0101-licensing-and-commercialization-strategy.md. Canonical Russian: ../../adr/0101-licensing-and-commercialization-strategy.md -->

# ADR 0101: Licensing and commercialization strategy

**Status:** Accepted  
**Date:** 2026-04-26

## Related ADRs

| ADR | Role |
|-----|------|
| [0009](0009-strangler-migration-and-exceptions.md) | related ADR |
| [0024](0024-ide-sdk-and-stable-contracts.md) | related ADR |
| [0027](0027-small-team-focus-vs-public-maturity.md) | related ADR |
| [architecture-policy.md](../../architecture-policy.md) | outside numbered ADR |

## Summary

- **License** matrix and dependency rules (copyleft, commercialization).
- Guardrails for stack choice and artifact publication.
- Governance — alongside [0100](0100-project-constitution.md).

---

<a id="adr0101-context"></a>

## 1. Context

CascadeIDE plans to remain open source while preserving commercialization options (cloud services, enterprise services, support, custom integrations).  
The project also considers AI and runtime dependencies with different license models, including “strong” copyleft.

Without explicit policy, dependency decisions can drift and create future IP conflicts (distribution limits, re-licensing risk, due diligence risk, contributor uncertainty).

---

<a id="adr0101-decision"></a>

## 2. Decision

Adopt **open source with commercial options** licensing strategy:

0. **CascadeIDE code in this repository** is distributed under **MIT** (see root [`LICENSE`](../../LICENSE)); commercial services and public contacts — [`COMMERCIAL-NOTICE.md`](../../COMMERCIAL-NOTICE.md).
1. **Default distribution path** uses only licenses from the base allowed list.
2. **“Strong” copyleft** components (GPL/AGPL family) are forbidden in core and runtime without separate approved ADR exception.
3. Commercialization remains open via:
   - managed cloud offerings,
   - enterprise extensions,
   - support/consulting/training.
4. Each new external dependency is reviewed and recorded with license metadata.

---

<a id="adr0101-license-matrix"></a>

## 3. License policy matrix

<a id="adr0101-matrix-allowed-default"></a>

### 3.1 Allowed by default

- MIT
- Apache-2.0
- BSD-2-Clause
- BSD-3-Clause

<a id="adr0101-matrix-allowed-reviewed"></a>

### 3.2 Allowed with explicit review

- MPL-2.0
- LGPL family

Review criteria for this group:
- linking and deployment model,
- obligations on modified files/libraries,
- impact on packaged desktop distribution.

<a id="adr0101-matrix-restricted"></a>

### 3.3 Restricted (ADR exception required)

- GPL family
- AGPL family
- SSPL
- custom non-commercial / source-available terms limiting business use

---

<a id="adr0101-dependency-rules"></a>

## 4. Dependency management rules

For each new package or library:

1. Record:
   - package name,
   - version,
   - source URL,
   - declared license,
   - notable transitive license risks.
2. Add and update project notices in [`docs/THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md).
3. Ensure CI license checks fail on:
   - forbidden licenses,
   - unknown/unapproved licenses.

---

<a id="adr0101-forbidden-architecture"></a>

## 5. Architectural guardrail for forbidden licenses

If a component with forbidden license is considered:

1. Start with PoC assessment outside core delivery path.
2. Run legal and product review (obligations, distribution model, update policy).
3. Allow only via separate ADR exception with rollback plan.

Do not assume “automatic safety” from process boundaries alone (e.g. sidecar/process split) without explicit legal review.

---

<a id="adr0101-commercialization"></a>

## 6. Commercialization model (non-exclusive)

Open source core is compatible with commercial scenarios via:

- cloud and managed services,
- enterprise add-ons,
- paid support/SLA,
- consulting and implementation.

This ADR does not fix a single monetization path; it preserves flexibility while supporting dependency hygiene.

---

<a id="adr0101-consequences"></a>

## 7. Consequences

<a id="adr0101-consequences-positive"></a>

### Positive

- Clear contribution and dependency rules.
- Cleaner IP chain for future due diligence.
- Lower risk of accidental license blockers.

<a id="adr0101-consequences-negative"></a>

### Negative

- Some technically attractive libraries may be excluded from core.
- More process load when adding dependencies.

---

<a id="adr0101-rollout-plan"></a>

## 8. Rollout plan

1. ~~Add `license-policy.md` (short developer policy).~~ Done: [`../../license-policy.md`](../../license-policy.md).
2. ~~Add/maintain `THIRD-PARTY-NOTICES.md`.~~ Done: [`../../THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md) (copied on publish via `CascadeIDE.csproj`).
3. Add CI license check as required gate.
4. Run baseline dependency license audit and remediate findings.
5. Track exceptions in separate ADRs.

<!-- English translation of adr/0102-data-acquisition-layer-boundary-and-contract.md. Canonical Russian: ../../adr/0102-data-acquisition-layer-boundary-and-contract.md -->

# ADR 0102: Data Acquisition Layer - boundary of external interfaces and adapters

**Status:** Accepted  
**Date:** 2026-04-26

## Related ADRs

| ADR | Role |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | linked ADR |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | linked ADR |
| [0009](0009-strangler-migration-and-exceptions.md) | linked ADR |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | linked ADR |
| [0095](0095-workspace-solution-ide-health-stratification.md) | linked ADR |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | linked ADR |
| [0099](0099-ide-databus-typed-events-and-projections.md) | linked ADR |


## Summary

- **DAL** is an explicit boundary for the extraction of external data.
- Contract DAL ↔ CCU ↔ UI; raw I/O is not in the VM.


---

<a id="adr0102-context"></a>

## 1. Context

The code base contains computational units (CCUs), an orchestration in the `MainWindowViewModel` and a set of services/adapters that read external sources (files, processes, configs, wire payload).  
Without a separate canonical term and boundary, this outer layer “spreads” across `Services`, `ViewModels`, and sometimes ends up in `ComputingUnits`.

This creates drift:

- CCUs begin to mix with IO;
- it is difficult to automatically check boundaries with analyzers;
- the connectivity and cost of refactoring increases.

---

<a id="adr0102-decision"></a>

## 2. Solution

Introduce and consolidate a single term: **Data Acquisition Layer (DAL)**.

`DAL` - a layer of external interfaces for the computing circuit: incoming collection/reading and outgoing data transfer to the outside.

<a id="adr0102-dal-in-scope"></a>

### DAL includes

- `fs` operations (`File`, `Directory`, existence check, paths);
- launching external processes and collecting their output;
- parse/serialize external formats (`json`, `toml`, wireload);
- integration resolvers and source adapters.
- outgoing transmission to the outside (recording, sending, external calls), where required by the feature.

<a id="adr0102-dal-out-of-scope"></a>

### Not included in DAL

- calculation of semantic snapshot/DTO of the channel;
- UI rendering and UI composition;
- slot/surface routing.

---

<a id="adr0102-ccu-ui-boundary"></a>

## 3. Border with CCU and UI

- **DAL**: Mines and normalizes external data.
- **CCU** (`Cockpit/ComputingUnits/*`): Counts semantic snapshot/DTO from an already prepared input.
- **CDS/UI**: displays completed images.

Invariant:

1. CCU does not make direct external calls (neither inbound nor outbound).
2. DAL does not replace the CCU channel composition.
3. `MainWindowViewModel` does not become a place for mass data extraction from the outside world.

---

<a id="adr0102-code-layout"></a>

## 4. Code placement (strangler)

Basic DAL placement:

- `Features/<Feature>/DataAcquisition/*`

Adjacent layer for orchestration/use-case logic:

- `Features/<Feature>/Application/*`

Division of responsibility:

- `DataAcquisition` - external world adapters (I/O, process, wire, API).
- `Application` - preparation of use-case payload/script rules without direct external calls.
  For orchestration classes, explicit naming `*Orchestrator` is recommended.
- `CCU` - calculation of semantic snapshot/DTO of the channel.

During the transition phase, narrow adapters in `Services/*` are allowed if:

- there is an explicit interface;
- the layer does not support the UI;
- there is a plan for transferring to the feature infrastructure.

---

<a id="adr0102-first-slice-example"></a>

## 5. Example of the first cut

Launch circuit:

- `LaunchProfilesStore` and `LaunchSettingsJsonImport` refer to DAL;
- `LaunchReadinessUnit`, `LaunchPreResolvePipelineUnit`, `LaunchProfileProjectResolveUnit` remain in the CCU;
- `MainWindowViewModel` performs orchestration.

---

<a id="adr0102-guardrails"></a>

## 6. Bounds checking

Architectural guardrails (CASCOPE*) are introduced for DAL/CCU:

- prohibition of IO/process/wire parse in `Cockpit/ComputingUnits/*`;
- prohibit CCU dependencies on `ViewModels/Views/Ui*`;
- step-by-step rollout: warning -> baseline cleanup -> error.

---

<a id="adr0102-consequences"></a>

## 7. Consequences

- The boundary between “data mining vs meaning calculation” becomes clear.
- Refactoring `Services` into feature slices becomes deterministic.
- Analyzers get a clear target for the rules.

---

<a id="adr0102-non-goals"></a>

## 8. Not goals

- Big-bang moving all `Services/*` into one commit.
- Forced rename of all existing namespaces in one step.
- Changing the DataBus contract with this ADR.
<!-- English translation of adr/0097-cockpit-compute-units-transport-to-channel-dto.md. Canonical Russian: ../../adr/0097-cockpit-compute-units-transport-to-channel-dto.md -->

# ADR 0097: Cockpit Computing Units (CCU; analogous to LRU *Unit*) - layer between transport, meaning and channel

**Status:** Accepted · Implemented  
**Date:** 2026-04-24  
**Updated:** 2026-04-25 - second tag: CCU reference in IDE Health channel (§5), `Build` boundary in **CASCOPE019** (see [0099](0099-ide-databus-typed-events-and-projections.md) and `IdeHealthPipelineAnalyzer`).

## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → CDS → surface composer → surface |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | **delivery** bus in UI, analogous to AFDX |
| [0095](0095-workspace-solution-ide-health-stratification.md) | three **levels** of A/B/C meaning and a `stratum` field |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | explicit boundary **DAL**: External data mining separate from CCU |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | payload vs projection |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention zones; EICAS separate from “work” |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | IDE Health as a product channel |
| [0055](0055-skia-instrument-composition-pipeline.md) | another **compute** circuit for Skia tools |

### Outside ADR

| Document | Role |
|----------|------|
| [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) | actual IDE Health chain |
| [`CascadeIDE.ArchitectureAnalyzers/README.md`](../../CascadeIDE.ArchitectureAnalyzers/README.md) | Roslyn **CASCOPE*** - fixing layer boundaries on an assembly; CCU - see §4 |

## Summary

- **CCU** (*cockpit compute unit*) - **meaning convolution** layer between raw events and channel DTO; analogue of LRU *Unit*, not a graph node.
- [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) delivers; [0095](0095-workspace-solution-ide-health-stratification.md) classifies the stratum; **CCU thinks** what to show.
- [0036](0036-cds-channel-compositor-surface-pipeline.md) routes a ready-made image to the cabin; CDS **doesn't** build laws from raw MSBuild.
- The standard in the code is **IDE Health** (`IdeHealthInputSnapshot`, formatting unit, compositor; **CASCOPE019** on `Build`).
- Bulk renaming of types **optional**; new channels - in the same discipline (strangler).

---

<a id="adr0097-analysis"></a>

## 1. Analysis: what already exists and where is the “hole”

<a id="adr0097-analysis-aviation"></a>

### 1.1 Aircraft support (no claim for certification)

In avionics, next to the **bus** (limited transport) and **indication** (CDS/display) there are **computational LRU** (*Line **Replaceable Unit***): blocks that, from raw or partially normalized data, build **consistent state** and **display laws** for the crew (navigation integration, filtering, limits, priorities). The last letter in the abbreviation is **Unit**; in the Cascade engineering language, it is more canonical to say **compute unit** (convolution unit/block) for this layer, rather than “node”, so as not to diverge from the usual LRU and not to be confused with a graph, network or AST node.

They **don't** replace the tire and **don't** draw pixels - they **figure** out what makes sense to show.

<a id="adr0097-analysis-covered"></a>

### 1.2 What is already covered in Cascade by other ADRs
| Layer | ADR/artifact | Answers the question |
|------|-----------------|---------------------|
| Delivering streams to the UI without an avalanche of updates | [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | *How* to deliver pieces of log/events to the VM with backpressure and batching |
| Semantics of “what is the signal about” (folder vs solution vs IDE process) | [0095](0095-workspace-solution-ide-health-stratification.md) | *Which level* (A/B/C) should the contract field be assigned to |
| Cockpit channel → attention route → slot composition → controls | [0036](0036-cds-channel-compositor-surface-pipeline.md) | *Where in the booth* and *how to associate a slot with a view* |
| Payload of a line vs how it's drawn | [0068](0068-deck-row-payload-and-presentation-projection.md) | Separating channel DTO and **projection** into template |
| Guardrails layers on **assembly** (imports, sensitive areas) | [`CascadeIDE.ArchitectureAnalyzers`](../../CascadeIDE.ArchitectureAnalyzers/README.md) (**CASCOPE***, including in the spirit of [0036](0036-cds-channel-compositor-surface-pipeline.md), [0066](0066-cockpit-ui-vs-ide-presentation-layer.md), [0079](0079-ide-display-system-ids-overlay-pipeline.md)) | Less **drift** of architecture during refactoring (human and agent): violation - **compiler diagnostics**, not just a review note |

Between **raw materials** (build log, DAP events, git, LSP) and **DTO channel / snapshot for CDS** one more meaning is often needed: **aggregation, normalization, conflict resolution, meaning debounce** - neither the 0094 bus nor the 0095 level table does this all automatically.

<a id="adr0097-analysis-ide-health"></a>

### 1.3 Fact in the code: IDE Health is already close to the standard

According to [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md) the chain has already been separated:

- **`IdeHealthInputSnapshot`** - normalized inputs to segment composition.
- **`IdeHealthFormattingUnit`** - pure string logic (unit-test without VM).
- **`IdeHealthSurfaceCompositor`** — order and composition of segments for the channel.
- **`IIdeHealthChannel` / `IdeHealthSnapshotUnit`** - collecting a snapshot from delegates and the environment.

That is, **convolution units already exist**, but the **named architectural layer** was not highlighted in the ADR glossary; new features risk dumping the fold again into the `MainWindowViewModel` or into transport 0094 “along the way”.

<a id="adr0097-analysis-gap"></a>

### 1.4 Hole in the wording

- There is no **uniform term** for the “LRU-like” module between ingestion and channel.
- There is no obvious **invariant**: “transport does not consider the meaning”, “CDS does not build laws from raw MSBuild”, “VM ideally orchestrates, and does not contain all the mathematics of the summary.”
- For **CCU** as a general layer, the full set of **CASCOPE*** is yet to come; for **IDE Health** **CASCOPE019** is already in effect (single point `IIdeHealthChannel.Build(...)` in `MainWindowViewModel.IdeHealth`, see [0099](0099-ide-databus-typed-events-and-projections.md)). For other channels, the risk remains partially **documented** until anti-patterns are formalized in analyzers ([§3 - direction for implementation](#adr0097-implementation-strangler)).

This ADR closes the wording and linkage with 0036 / 0094 / 0095 **without** the mandatory bulk type renaming.

---

<a id="adr0097-decision"></a>

## 2. Solution

<a id="adr0097-term"></a>

### 2.1 Term

The architectural (engineering) concept **cabin computing unit** is introduced - in the Russian text, **“computing unit”** is also acceptable as a colloquial tracing from English. **cockpit compute unit**, abbreviation **CCU** only as an **internal** label in comments and ADR. **Not** a product name for the user and **not** a replacement for the word “channel” in the sense of [0036](0036-cds-channel-compositor-surface-pipeline.md). The English canon of the layer name is **cockpit compute unit** (**CCU**), not *compute node*: this preserves the consonance with **U** in **LRU**.

**Definition:** a module with an **explicit input and output contract**, which from these sources (including after the layer [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md), if used) generates a **snapshot or DTO for the cabin channel** (or for the next step of the composition in the spirit [0036](0036-cds-channel-compositor-surface-pipeline.md) items 1-3), applying the convolution rules, priorities and - where appropriate - level tags from [0095](0095-workspace-solution-ide-health-stratification.md) (`stratum` and source discriminators).
**Specialization by stratum:** one CCU for the whole world is **not** mandatory and often harmful; typical decomposition - **several units** (for example, by levels A/B/C from [0095](0095-workspace-solution-ide-health-stratification.md#adr0095-stratum-ccu-examples): working names **WSCU / SSCU / ISCU**) plus **separate** composition into a single channel snapshot. More details and a disclaimer about **ISCU vs IDS** are in the same § example in 0095.

<a id="adr0097-invariants"></a>

### 2.2 Invariants

1. **The unit does not replace the delivery bus:** do not mix unbounded queue and “summary laws” in one type ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) remains a transport).
2. **The unit does not replace the CDS:** does not decide which region of the cabin the channel “has the right” to go to - this is the circuit [0036](0036-cds-channel-compositor-surface-pipeline.md) step 2.
3. **The unit is not an Avalonia surface:** does not own the control tree; publishing the result on the UI - through the existing VM/scheduler ([0004](0004-ui-thread-marshaling.md)).
4. **Output, if related to Health-like data,** when expanding contracts must be compatible with the discipline [0095](0095-workspace-solution-ide-health-stratification.md) (do not lump A/B/C into one unmarked field without conscious exception).
5. **Testability:** It is preferable to test the unit **without** loading the `MainWindow` and without the full UI tree (like the `IdeHealthFormattingUnit` and composer in the tests from the IDE Health blueprint).

<a id="adr0097-vm-orchestration"></a>

### 2.3 The role of `MainWindowViewModel` and orchestrators

Subscribing to `PropertyChanged`, calling `RebuildIdeHealth()` and binding delegates to the `IdeHealthSnapshotUnit` is **orchestration** (the glue between the IDE world and the channel). As the **rollup** rules grow, they should be **pushed down** into the `Cockpit/*` (or dedicated snapshot services), leaving the VM thinner - in the spirit of the already adopted split in the IDE Health blueprint.

<a id="adr0097-other-contours"></a>

### 2.4 Other circuits

Pipelines like [0055](0055-skia-instrument-composition-pipeline.md) (Intent → … → Render) - **separate family** compute for graphics; This ADR **doesn't** unify their API with `IdeHealth*`, but recognizes **the same architectural idea**: snapshot/model as input, structured result as output, tests without unnecessary UI.

---

<a id="adr0097-implementation-strangler"></a>

## 3. Direction for implementation (strangler)

1. New observability units and MCP/CDS extensions - design as a **chain of units** (possibly one unit per v1), with documented input/output.
2. An existing Health IDE is **not required** to rename classes to `*ComputeUnit` in the same PR; it is enough to **explicitly refer** to this ADR in the review as a canon of boundaries.
3. If the unit is powered by ingestion ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md)), the boundary: **at the ingestion output - typed events/chunks**; **at the output of the unit - a semantic snapshot/DTO of the channel**.
4. When the pattern of violations stabilizes (the convolution is “forgotten” in the transport type, prohibited `using` between CCU and Avalonia/`UiChrome`/ingestion, etc.) - create a rule in **[`CascadeIDE.ArchitectureAnalyzers`](../../CascadeIDE.ArchitectureAnalyzers/README.md)** (new ones or expansion of existing ones **CASCOPE***, with diagnostic tests based on the sample analyzer project). Loading restrictions for analyzers with **RoslynMcpWorkspace** - as in the README of the analyzers; **canon boundary checking** for human and CI - normal **`dotnet build`**.

---

<a id="adr0097-consequences"></a>

## 4. Consequences

- A **short word** appears for the review: “this should be in the **CCU** (unit), not in the transport” / “not in the CDS”.
- **Roslyn:** the same circuit that already gives **architectural rigor** for the cockpit and IDS (**CASCOPE*** in [`CascadeIDE.ArchitectureAnalyzers`](../../CascadeIDE.ArchitectureAnalyzers/README.md)), naturally expands to **CCU boundaries**: separate diagnostics increase the number of rules in the assembly, but **less silent drift** - both in your IDE and in the agent who edits the code without the full ADR context; Markdown remains the standard, **the compiler is the watchman**.
- Less risk of duplicating the summary for the UI and for the MCP in two places without a common **snapshot**.
- Additional documentation discipline: new observability ADRs may reference **0095 + 0097** together.

---

<a id="adr0097-implementation-status"></a>

## 5. Implementation state (standard in code)
**The IDE Health** channel already implements the CCU end-to-end pattern **without** the `*ComputeUnit` suffix in type names (see §3 ADR: strangler). The `ICockpitComputeUnit` and `ICockpitComputeUnitPayload` contracts are fixed in the code (`Cockpit/ComputingUnits/ICockpitComputeUnit.cs`). Filemap and stream: [`workspace-health-implementation-map-v1.md`](../../design/workspace-health-implementation-map-v1.md); short index: [`Cockpit/Channels/IdeHealth/README.md`](../../Cockpit/Channels/IdeHealth/README.md).

| Layer 0097 | Implementation |
|-----------|-----------|
| Login → snapshot | `IdeHealthInputSnapshot` (`ICockpitComputeUnitPayload`) / `IdeHealthSegmentInput` + `IdeHealthStratum` (ADR 0095) |
| Text convolution (pure) | `IdeHealthFormattingUnit` (`ICockpitComputeUnit`, `Default`) |
| Collecting snapshot from delegates/DAP | `IdeHealthSnapshotUnit` → `IIdeHealthChannel` (`ICockpitComputeUnit`) |
| Composition of segments for the channel | `IIdeHealthSurfaceCompositor` / `IdeHealthSurfaceCompositor` (`ICockpitComputeUnit`, `Cockpit/Composition/IdeHealth/`) |

New observability channels - according to the same discipline: single image, pure convolution, composition, **without** mixing with [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md). For IDE Health, **CASCOPE019** already fixes one of the invariant boundaries; **other** **CASCOPE** under CCU - [§3 - direction for implementation](#adr0097-implementation-strangler) (as long as there are stable anti-patterns).

---

<a id="adr0097-candidates-next"></a>

## 6. Candidates for the next CCU (practical shortlist)

Below is the engineering priority for the next steps after IDE Health. This is **not** a commitment to do everything at once, but rather the order in which CCU usually has the greatest effect.

<a id="adr0097-candidates-p1"></a>

### P1 (immediately after the current cycle)

- **Build/Test summary:** raw build/test event flow → `BuildStateSnapshot` (phase, progress, errors/warnings, duration, last-failure).
- **Debug session summary:** DAP events → `DebugSessionSnapshot` (attached/running/stopped, stop reason, current frame, breakpoint health).
- **Launch readiness:** `launchSettings` + startup project + env/fs checks → `LaunchReadinessSnapshot` (ready/not-ready + reasons).

<a id="adr0097-candidates-p2"></a>

### P2 (when we secure P1 with contracts)

- **Git workspace health:** status/branch/ahead-behind/submodule state → `RepoHealthSnapshot`.
- **LSP health:** state C#/Markdown language services → `LanguageServiceHealthSnapshot` (connected/degraded, diagnostics delta).
- **MCP health:** availability and degradation by tools → `McpHealthSnapshot` (availability, last error, latency buckets).

<a id="adr0097-candidates-p3"></a>

### P3 (after stabilization of the graph-backed circuit)

- **Semantic map input snapshot:** index/sources/invalidations → `SemanticMapInputSnapshot` as input to graph-backed surfaces.
- **Terminal attention snapshot:** stream of terminals → `TerminalAttentionSnapshot` (active command, failure streak, long-running suspicion).

<a id="adr0097-semantic-map-boundary"></a>

### Boundary for semantic map (what's in CCU and what's not)

- In **CCU**: normalization of sources, deduplication/prioritization of signals, calculation of derived fields, snapshot version/freshness.
- Outside **CCU**: graph interactions, layout, selection, navigation UX and specific surface logic ([0067](0067-graph-backed-surfaces-contract.md)).
- Default rule: if a module answers the question "**which semantic snapshot is now canon**", it is a CCU candidate; if "**how the user interacts with this screenshot**", it is not CCU.

---

<a id="adr0097-non-goals"></a>

## 7. Not goals

- Certification, DO-178, physical LRUs.
- Mandatory introduction of a new namespace or suffix in all `Cockpit/` types.
- Merging unit with **EICAS** ([0021](0021-pfd-mfd-cockpit-attention-model.md)): W/C/A alerts remain a separate data loop, as in IDE Health drawing §7.

---

<a id="adr0097-rejected-alternatives"></a>

## 8. Rejected alternatives

- **Calling any service a “unit”** is rejected: without a snapshot/DTO contract, the term is meaningless.
- **Extend 0094 so that it also builds Health reports** - rejected: mixes transport and meaning calculation.
- **Put all the math into the “composer” from 0036** - rejected: in 0036 the composer responds to the link between **slot and view** channel; pure reconciliation of raw materials into a snapshot is more logical as a **previous** or **nested** step (like `IdeHealthFormattingUnit` before `IdeHealthSurfaceCompositor`).
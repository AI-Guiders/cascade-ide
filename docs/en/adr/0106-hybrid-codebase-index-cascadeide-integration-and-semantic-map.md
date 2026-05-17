<!-- English translation of adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md. Canonical Russian: ../../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md -->

# ADR 0106: Hybrid Codebase Index - integration into CascadeIDE, freshness and Semantic Map

**Status:** Accepted · In progress  
**Date:** 2026-05-07 (updated 2026-05-08)  
## Related ADRs

| ADR | Role |
|-----|------|
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Basic kernel and MCP; this ADR is a CascadeIDE outline (DAL/CCU/DataBus, freshness, Semantic Map) |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer - boundary of external interfaces and adapters |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Cockpit Computing Units (CCU; analogue of LRU *Unit*) - a layer between transport, meaning and channel |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus - Typed Events and State Projections |
| [0098](0098-semantic-first-document-as-projection.md) | Semantics is primary; document and repository - projections (Semantic-First) |
| [0039](0039-workspace-navigation-affordances.md) | Workspace navigation - multiple views and "current file + related" |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD (control flow) |
| [0056](0056-semantic-map-pipeline-adoption.md) | Semantic Map adoption of Skia composition pipeline |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces - a general contract for a family of graph screens |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS (Ide Display System) - IDE overlay pipeline, orthogonal to CDS |

### Outside ADR

| Document | Role |
|----------|------|
| [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index) | Kernel and MCP Host Repository |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | in-proc orchestrator, UI settings, MFD HIS |

## Summary

- In-proc **HybridCodebaseIndex.Core** in CascadeIDE; exe MCP - for external hosts.
- Fitting into **DAL → Application → CCU → DataBus** ([0102](0102-data-acquisition-layer-boundary-and-contract.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0099](0099-ide-databus-typed-events-and-projections.md)).
- Index freshness, UI settings, border with **Semantic Map** ([0053](0053-semantic-map-control-flow-pfd.md)).

---
## Solution

The **`hybrid-codebase-index`** tool is designed as a **shared library** (`HybridCodebaseIndex.Core`) and a thin **MCP host** (`HybridCodebaseIndex.Mcp`) on top of it - the same template as **agent-notes** (`AgentNotes.Core` + exe). **CascadeIDE** primarily connects the **kernel in-proc** (`ProjectReference` to Core from the [`hybrid-codebase-index`](https://github.com/KarataevDmitry/hybrid-codebase-index) repository): one process with an editor, a cheap `reindex`/`search` call, a common `databasePath` with what the external MCP expects when working from Cursor. The separately published **exe MCP** remains for external hosts and for the isolation scenario; placement of I/O and life cycle - in the cabin layers below, without the “divine” `MainWindowViewModel`.

### Matching in the CascadeIDE loop

The built-in bundle (in-proc or child process raised by the IDE) must fit into the accepted architecture:

- **DAL** ([0102](0102-data-acquisition-layer-boundary-and-contract.md)): workspace bypass, reading files under the index, if necessary, network/processes for embeddings and other external I/O - in the spirit of `Features/<slice>/DataAcquisition/`, without throwing raw I/O into the VM.
- **Orchestrators `Application`**: scripts `reindex` / `search`, configuration from `settings.toml`, watcher bundle ↔ index core ↔ life cycle of SQLite files.
- **CCU** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)): collapsing the search result (FTS + vec, index version, hit metadata) into **stable DTOs** for channels and, if necessary, snapshots (top-N, explain) - without turning the CCU into a second “index engine”.
- File change/index progress and UI subscription events - within the meaning via **IDE DataBus** ([0099](0099-ide-databus-typed-events-and-projections.md)).

### Freshness in the IDE

With frequent saves, the index for `.cs` / `.axaml` and related file types should be updated **cheaply incrementally** (hash, rebuilding of affected chunks), without the UX lag of “full reindex for each keypress”. Semantics: either a scripted call with debounce from the orchestrator, or a single watcher thread agreed with the MCP contract where the agent watches the same `databasePath`.
For more information about motivation, see [ADR 0105 § watchouts freshness](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-watchouts-freshness) - **implementation in CIDE** is specified here.

### Semantic Map and layer B (border)

**Semantic Map** - graph-backed surface (intentions, control flow, Skia pipeline). The hybrid index (**layer B** in ADR 0105 terminology) **is not** a canonical Semantic Map graph and **does not replace** CFG/Roslyn symbolic truth in C#. It gives **orientation**: top hits, paths, ranges in files, index version and optional input for map declutter - with explicit `hit_kind` in DTO ([0105 § hit_kind](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-watchouts-hit-kind)).

In [0097 § P3](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-candidates-p3) the candidate **`SemanticMapInputSnapshot`** is recorded - layer B after normalization via **CCU** ([0097 § semantic border map](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-semantic-map-boundary)) specifies the content of such an input for graph-backed surfaces. More details about what HCI **does and doesn’t give** to the map (orientation vs graph, UI, non-goals) - **[ADR 0113](0113-hci-semantic-map-orientation-layer.md)**.

### Composition workflow in the product

Integration of the scenario **"Hybrid search → Roslyn accuracy"** (clause 5 of the roadmap [ADR 0105](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-rollout-plan)) in the UI/IDE orchestration: hints for the next step (`go-to-def`, usages, diagnostics), without mixing `hit_kind` with symbolic truth.

### Persistence and synchronization with the orchestrator

- Model: `CascadeIdeSettings.HybridIndex` → TOML **`[hybrid_index]`** (general CascadeIDE user settings file, see `SettingsService`; sample - `docs/samples/settings.toml`).
- **Clone / Is**: HCI section changes participate in the `Save to disk' detector (`SaveSettingsIfChanged`).
- After changing the parameters through the UI of the main window, **`ApplyHybridCodebaseIndexOrchestrationForCurrentSolution`** is called: enabling watcher, debounce, `scope_mode`, taking into account the **`mcp_only`** mode with `pause_when_mcp_stdio_host`, changing **`index_dir`** via in-proc rebuild `CodebaseIndexService` and reinstalling watchers.
- The **open/change solution** script additionally does **`Poke`** on `auto_reindex_on_solution_open` (as before on `SolutionPath`-change).
- Update **INDEX/HCI** page in MFD: `HybridIndexStateChanged` event in IDE DataBus; subscription when you first go to the page (see `MainWindowViewModel.EnvironmentReadiness`).

### Agent/Development Operational Notes

A short checklist (supported by the file `docs/agent-hci-cascadeide-notes-v1.md`): where TOML is located, how not to confuse the in-proc path of the database and the external MCP, also `index_dir`, how to confirm the “aliveness” of the index through MFD or MCP `codebase_index_status`.

---

## Rollout (sketch, CIDE only)

The order is indicative; the first steps fix the **library in the IDE**, then the life cycle and channels.

1. **Connect `HybridCodebaseIndex.Core` into the CascadeIDE solution**  
   `ProjectReference` on Core (submodule or NuGet when publishing a package - build solution). One index API per IDE process; The response fields contract is the same as that described for tools MCP ([0105 - layer B](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-layer-b), `hit_kind`, format version).

2. **Orchestrator + DAL**  
   Forwarding `workspace_root` and optionally the solution path - as in [0105 § area sketch](0105-hybrid-codebase-index-for-csharp-web.md#adr0105-impl-sketch-scope); file reading and kernel calls are outside the VM boundary, in the spirit of [0102](0102-data-acquisition-layer-boundary-and-contract.md). One SQLite per pair *(workspace, solution scope)*, the same directory that MCP uses with the same configuration.

3. **Freshness: saves → debounced incremental reindex**  
   Subscription to save documents (or a single watcher consistent with the IDE policy), debounce in the `Application` orchestrator, incremental reindex via Core. Progress events/"index updated" - in **IDE DataBus** ([0099](0099-ide-databus-typed-events-and-projections.md)).

4. **CCU and channels**  
   Rolling up the search result / index status into stable DTOs for **IDE Health** and, if necessary, a separate “Index / Orientation” channel - without duplicating the index logic in the VM ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)).
5. **(Optional, parallel or later)** Run the published **`HybridCodebaseIndex.Mcp.exe`** as a child process - parity with Cursor, restart isolation, or until Core is built into sln. Does not replace step 1 for the main UX editor.

6. **(Optional)** Link **`SemanticMapInputSnapshot`** after stabilizing the DTO and the CCU boundary ([0097 § semantic map](0097-cockpit-compute-units-transport-to-channel-dto.md#adr0097-semantic-map-boundary)).

---

## Consequences

- There is no duplication of index logic in the VM - CCU only packs; **one core** (Core), MCP - transport for external calls.
- The index format version (`indexFormatVersion` / status) must be consistent between the **Core build** built into CIDE and the **optional** MCP exe if the agent uses both loops to the same workspace - explicit check at startup or incompatibility.
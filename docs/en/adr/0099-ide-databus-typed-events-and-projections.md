<!-- English translation of adr/0099-ide-databus-typed-events-and-projections.md. Canonical Russian: ../../adr/0099-ide-databus-typed-events-and-projections.md -->

#ADR 0099: IDE DataBus - Typed Events and State Projections

**Status:** Accepted · Implemented  
**Date:** 2026-04-25

## Related ADRs

| ADR | Role |
|-----|------|
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | tire delivery and backpressure |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU: convolution in channel DTO |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → CDS → surface composition |
| [0095](0095-workspace-solution-ide-health-stratification.md) | Workspace/Solution/IDE levels |
| [0004](0004-ui-thread-marshaling.md) | UI marshaling |
| [0007](0007-signals-coupling-and-ui-backpressure.md) | signals and connectivity |
## Summary

- **IDE DataBus:** typed events in the IDE process.
- Decoupling of sources and projections; does not replace [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) and [0097](0097-cockpit-compute-units-transport-to-channel-dto.md).


---

<a id="adr0099-context"></a>

---
## Context

The code already has bus and convolution work elements:

- transport and output batching (`ADR 0094`);
- CCU computing units and channel composition (`ADR 0097`, `ADR 0036`).

But there is no single **application** contract “IDE domain event → subscribers → state projection” yet.  
Because of this, some signals continue to go through direct delegates and manual gluing in the `ViewModel`.

---

<a id="adr0099-decision"></a>

## Solution

Introduce the **IDE DataBus** layer into the architecture as an in-process typed event bus.

<a id="adr0099-p1"></a>

1. **Contract:** `IDataBus` with minimal API:
   - `Publish<TEvent>(TEvent evt)`
   - `Subscribe<TEvent>(Action<TEvent> handler)` (with disposable unsubscribe)

<a id="adr0099-p2"></a>

2. **Typed events** (not `object`/string):
   - `BuildStateChanged`
   - `TestsStateChanged`
   - `DebugStateChanged`
   - `GitStateChanged`
   - (as needed) `ScopeDecisionChanged`, etc.

<a id="adr0099-p3"></a>

3. **DataBus does not replace 0094:**  
   `Channel<T>`/ingestion remains the stream transport; DataBus is a distribution layer for **normalized domain events**.

<a id="adr0099-p4"></a>

4. **DataBus does not replace 0097/0036:**  
   CCU and compositor are still responsible for convolution/projection. DataBus delivers input events to the locations where snapshots/DTOs are built.

<a id="adr0099-p5"></a>

5. **Basic implementation v1:** synchronous in-memory bus in one IDE process, without external broker/IPC.

<a id="adr0099-p6"></a>

6. **Git in IDE Health:** one product path - after updating git lines in `UiChromeViewModel`, `AfterGitWorkspaceHealthSummaryApplied` is called (at the end of `RefreshGitSummaryAsync` on the UI thread), which in `MainWindowViewModel` is tied to `PublishGitToIdeDataBusAndRebuildIdeHealth` (publication `GitStateChanged` + `RebuildIdeHealth`). Initial state seed: `SeedIdeHealthDataBus()` in the constructor (startup + first `GitStateChanged`), without `PropertyChanged` on individual git fields.

<a id="adr0099-p7"></a>

7. **Channel Snapshot and UI:** `IdeHealthSnapshotUnit.Build` is called only from `MainWindowViewModel.IdeHealth` (`RebuildIdeHealth`); the result is cached in `_lastIdeHealthInputSnapshot`, row getters in `MainWindowViewModel.Presentation` read the cache. Roslyn **CASCOPE019** captures this boundary.

<a id="adr0099-p8"></a>

8. **Lifecycle:** `IdeHealthSnapshotUnit` implements `IDisposable` (bus unsubscribe); when closing the main window - `ReleaseWorkspaceHealthChannel()`.

<a id="adr0099-p9"></a>

9. **Order for IDE Health (implemented):** applied `InMemoryDataBus` main window - **synchronous** dispatch (`asynchronousDispatch: false`) so that subscribers of `IdeHealthSnapshotUnit` work before returning from `Publish`, and `RebuildIdeHealth()` reads the agreed snapshot. Build from the UI: first `BuildStateChanged` (start/finish), then `IsBuilding` - so that `NotifyPropertyChangedFor`→`RebuildIdeHealth` does not bypass the `_buildSnapshot` update. Publishing from the MCP background - via `UiScheduler.InvokeAsync` in `PublishToIdeDataBusAndRebuild` (same UI thread as fold).

---

<a id="adr0099-exchange-principles"></a>

## Exchange principles

1. **Non-blocking transport between layers:**  
   neither IDS, nor CDS, nor CCU should depend on each other's synchronous response in the runtime chain.  
   Publishing is performed as “fire-and-forward” to the appropriate channel/bus, processing is done when the consumer is ready.
2. **Strict message typing:**  
   no `object`/`dynamic` in domain channels.  
   Typed events and explicit message contracts are used (record/type hierarchy; discriminated-union style via pattern matching C#).

3. **Backpressure and loss policy by data class:**
   - for critical signals (errors, IDE vital status, safety/health) - lossless mode (unbounded, bounded+wait or a separate priority circuit);
   - for heavy/high-frequency signals (for example, graph slices for Skia) - `BoundedChannel` with a policy like `DropOldest`/“latest wins”, so as not to accumulate outdated frames.

4. **Domain isolation:**  
   The CCU receives input from its typed input stream (sensors/sources) and publishes a separate typed output stream (indication/projections).  
   Errors in the rendering/consuming loop should not crash the analysis/computation.

---

<a id="adr0099-boundaries"></a>

## Boundaries

- **You can:** use DataBus to decouple sources and projections (UI/MCP/cockpit snapshot).
- **You cannot:** mix transport mechanics (`Channel<T>`, backpressure) and business events in one type.
- **You cannot:** transfer rendering/UI logic to bus event handlers.

---

<a id="adr0099-strangler-plan"></a>

## Strangler-plan

1. Pilot vertical slice: `BuildStateChanged` from the build source to the IdeHealth snapshot.
2. Then `TestsStateChanged` and `DebugStateChanged`.
3. After stabilization, expand to Git/other domain signals.
4. Fix boundaries in `CascadeIDE.ArchitectureAnalyzers`: **CASCOPE019** - prohibit direct `_workspaceHealth.Build` outside `MainWindowViewModel.IdeHealth` (and previous pipeline rules for legacy APIs, see README of analyzers).

---

<a id="adr0099-consequences"></a>

## Consequences

- Less cohesion in `MainWindowViewModel`.
- It’s easier to test pieces based on events (publish → check projection).
- It’s easier to add new channels/images without cascading editing of existing services.
- The risk of “event spaghetti” appears with weak naming/boundary discipline - it is extinguished by typed events and ADR guidelines.

---

<a id="adr0099-non-goals"></a>

## Not goals

- External message broker, distributed bus or interprocess transport.
- Unification of all streams into one universal envelope in the first step.
- Mass migration of all existing signals into one commit.
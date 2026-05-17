<!-- English translation of adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md. Canonical Russian: ../../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md -->

# ADR 0094: Event delivery bus in UI (similar to AFDX) and `System.Threading.Channel<T>`

**Status:** Accepted  
**Date:** 2026-04-24  
## Related ADRs

| ADR | Role |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | product **channel** cabins → CDS → composer → surface |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | **CCU/Cockpit Computing Unit**: Convolution to DTO/Channel Snapshot - **not** this bus |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | **IDE Health** instead of the former *Workspace Health* - term and types like `IdeHealth*`; **not** this chat, but the solution recorded in the repo |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention zones; **EICAS** as a product circuit - according to the roadmap |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | channel string payload vs projection |
| [0007](0007-signals-coupling-and-ui-backpressure.md) | signals and UI load |
| [0004](0004-ui-thread-marshaling.md) | marshaling on UI thread |

## Summary

- **Delivery bus in UI** (analogous to AFDX): `Channel<T>`, batching, backpressure.
- Orthogonal to the CDS “channel” [0036](0036-cds-channel-compositor-surface-pipeline.md).


### Outside ADR

| Document | Role |
|----------|------|
| [cds-contract-v0.md](../../design/cds-contract-v0.md) | cds contract v0 |
---

<a id="adr0094-context"></a>

---
## Context

**Clarification on the placement of the UI (so as not to be confused with the VS-like “bottom panel” of the main window):** in the current `MainWindow` layout there is **not** a separate full-width grid line for “terminal / assembly”. Secondary circuit - **MFD column**: `MfdShellView` - top bar `WorkspaceChromeBandView` (**IDE Health** by [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md); backlog for **EICAS** semantics - see comment in `MfdShellView.axaml` and [0021](0021-pfd-mfd-cockpit-attention-model.md)), below is the host **`MfdContourStackHost`** with **`MfdShellPageStack`** (WORKSPACE pages / chat / **build log** / terminal / ...). This ADR is about **transporting text streams** to the appropriate VMs (e.g. `BuildOutputPanelViewModel`), not about the fact that the MSBuild log is already an EICAS strip.

Multiple **sources** (MSBuild/`dotnet`, child processes, LSP, agent, internal services) write text and statuses to these **secondary loop surfaces and adjacent VMs**. Today, the typical path is **many choppy** calls to the UI thread or **frequent** updates to one large buffer (`Build output`, terminal, etc.), which makes the thread feel **ragged** and the UI overloaded with small increments.

In avionics, **AFDX** (ARINC 664 Part 7, switched Ethernet) is an image of a **general deterministic bus**: several LRUs are connected to a **limited** transport with an access and priority policy, and not “each person pulls the wire into the device.”

Cascade **doesn't** implement AFDX as a protocol and **doesn't** confuse this ADR with certification; We transfer only **engineering meaning**: *normalized delivery + backpressure + one predictable path to the consumer*.

---

<a id="adr0094-decision"></a>

## Solution

An **optional ingestion** layer is introduced (working name in code/docs - by agreement, for example `IdeIngestion`, `BuildOutputIngestion`; this ADR **does not** fix the package name) for **transport** of events from producers to VMs of MFD pages and other consumers of long text:

<a id="adr0094-p1"></a>

1. **Product “channel” of the cabin** in the sense of [0036](0036-cds-channel-compositor-surface-pipeline.md) and **delivery bus** in the sense of this ADR are **different things**. The first one answers the question *what is in the cabin and where it makes sense*; the second is *how to bring pieces of text/events to binding to UI properties without races and an avalanche of updates*.

<a id="adr0094-p2"></a>

2. **Recommended implementation** in .NET: **`System.Threading.Channel<T>`** (often `BoundedChannelOptions` + explicit `FullMode`: wait, tail drop or product policy) for a queue of **typed** records (`BuildLogLine`, `TerminalChunk`, unified `IdeStreamEvent` - choice during implementation).

<a id="adr0094-p3"></a>

3. **One (or few) reader(s)** removes the channel and **batch** the application to the VM: for example, no more than N ms and/or no less than M characters per tick, then **one** `IUiScheduler`/`Post` to update the bound property (build log, terminal, ...) ([0004](0004-ui-thread-marshaling.md)).

<a id="adr0094-p4"></a>
4. **Strangler:** first vertical slice - **assembly log** (and if necessary **terminal**); expansion to other flows - after the pattern has stabilized. Existing calls do not have to migrate into the same commit.

<a id="adr0094-p5"></a>

5. **Observability:** snapshot of queue / drop counters (if policy allows) - as needed for debugging and MCP; the JSON form is **not** captured by this ADR (see [0008](0008-mcp-contracts-and-testable-infrastructure.md) when fields appear).

---

<a id="adr0094-cds-boundaries"></a>

## Boundaries with CDS and with “channel” in the glossary

- **CDS** and `Cockpit/Channels/**` remain the canon of **cockpit semantics** and data for instruments; this ADR **doesn't** add responsibility for "all MSBuild logs" to CDS.
- **The delivery bus** lives **below or next to** the VMs that feed **MFD pages** and similar long logs (build log, terminal), **not** replacing [0068](0068-deck-row-payload-and-presentation-projection.md): when the same meaning is already framed as **payload** **cockpit channel lines** (including future/current strip segments in the spirit of EICAS / **IDE Health** in CDS), it falls into the contour [0036](0036-cds-channel-compositor-surface-pipeline.md) according to existing rules - separately from the raw MSBuild stream in the TextBox of the “Build” page.
- **Convolution of raw materials into a channel snapshot/DTO** (aggregation, priorities, level A/B/C tags) is **not** the responsibility of the bus; see [0097](0097-cockpit-compute-units-transport-to-channel-dto.md).

---

<a id="adr0094-consequences"></a>

## Consequences

- Less “torn” UI and easier to **test** producers (write to a channel from a test, check batches).
- Explicit **backpressure** instead of unlimited line growth in memory during a log storm.
- Additional abstraction layer: implement **step by step**, with before/after measurement ([0054](0054-benchmarking-methodology-and-baselines.md) when scenarios appear).

<a id="adr0094-non-goals"></a>

## Not goals

- Implementation of **AFDX** / **ARINC 664** as a network stack.
- Replacing **CDS** or renaming the product term "channel" in the cockpit.
- Mandatory unification of **all** IDE signals in one `Channel<object>` without typing.

<a id="adr0094-rejected"></a>

## Rejected alternatives (briefly)

- **Rx only / manual queue only without policy:** locally acceptable; as **product default** a channel with explicit boundaries and a simple single-reader loop is preferable.
- **Push everything into CDS:** rejected - mixes cockpit and shell log transport ([0036](0036-cds-channel-compositor-surface-pipeline.md), [0066](0066-cockpit-ui-vs-ide-presentation-layer.md)).

---

<a id="adr0094-implementation"></a>

## Implementation status

**Build (UI and MCP), `dotnet format` (MCP) → Build Output panel:** single chain: `IDotnetCommandRunner.RunWithChunkWriterAsync` → `BuildLogIngestion.CreateBuildLogChannel` (**bounded**, `DefaultChannelChunkCapacity` ≈ 32, `FullMode = Wait` - backpressure to `stdout`/`stderr` pumps, without drop) → `BuildLogIngestion.DrainToAppendAsync` (batch `~8K` in `Append`, optional `onEachDequeuedChunk` in `OutputAccumulator` for full raw material in MCP response) → `BuildOutputPanelViewModel.Append` (phase 5: `Post` on UI).

**MCP** (`McpDotnetBuildTestService.BuildWithBinlogAsync`, `RunCodeCleanupAsync`): same transport; return string for JSON/parsers = accumulated in `OutputAccumulator` (and suffix `Exit code` on build error).

**Tests:** `CascadeIDE.Tests/BuildLogIngestionTests` (batch, `onEach`, bounded channel factory).

**Outside this ADR (intentional):** `dotnet test` / output **Instrumentation · tests** - still `RunAsync` and one insert into `InstrumentationPanel` (different surface; same pattern separately if needed). **Built-in terminal** (if/when) - the same strangler, does not block the *Accepted* degree of this ADR.
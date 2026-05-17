<!-- English translation of adr/0052-agent-contract-cli-and-snapshot-tests.md. Canonical Russian: ../../adr/0052-agent-contract-cli-and-snapshot-tests.md -->

# ADR 0052: CLI for agent contract (parity with MCP) and snapshot tests

**Status:** Accepted · Implemented  
**Date:** 2026-04-17  
**Implementation:** [`Services/AgentContract/AgentContractRunner.cs`](../../Services/AgentContract/AgentContractRunner.cs), [`Services/AgentContract/AgentContractHeadlessRuntime.cs`](../../Services/AgentContract/AgentContractHeadlessRuntime.cs), input `CascadeIDE.exe --agent-contract [--workspace <dir>] <command>` in [`Program.cs`](../../Program.cs); tests [`CascadeIDE.Tests/AgentContractRunnerTests.cs`](../../CascadeIDE.Tests/AgentContractRunnerTests.cs), [`CascadeIDE.Tests/AgentContractRunnerHeadlessTests.cs`](../../CascadeIDE.Tests/AgentContractRunnerHeadlessTests.cs) (including snapshot slice `cockpit_surface`: [`CascadeIDE.Tests/AgentContractCockpitContractSlice.cs`](../../CascadeIDE.Tests/AgentContractCockpitContractSlice.cs), [`CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json`](../../CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json)). Parity with MCP: `ide_get_ui_modes_diagnostics`, `ide_get_supported_editor_languages`, `ide_get_ide_state` (full summary - CLI command `get_ide_state`), `ide_get_cockpit_surface` / CDS only - `get_cockpit_surface` (same JSON as the `cockpit_surface` field in `get_ide_state`), `ide_git_*` (same JSON fields as in `MainWindowViewModel` / `GitCommandBuilder` + `GitCommandRunner`).  
## Related ADRs

| ADR | Role |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | parity “person ↔ agent” |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts and testable infrastructure |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport and parity |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Cabin tool (`Instrument`) - a handle to the slot composition, not `Control` |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | IDE/stdio MCP commands |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | `get_ui_modes_diagnostics`, `get_supported_editor_languages`, `get_solution_info`, `get_cockpit_surface`, `get_ide_state`, read-only `git_*` with `--workspace` |
| — | CI: `.gitlab-ci.yml` - `dotnet test` + smoke `--agent-contract` |
| — | **golden slice** CDS: `CascadeIDE.Tests/TestData/AgentContract/cockpit_surface_contract_slice.approved.json` + `AgentContractCockpitContractSlice` |

---
## Context

Today, Cascade IDE gives the agent **rich JSON** (layout, workspace state, diagnostics, cockpit/tools projection, etc.) through **MCP tools** (`ide_*`, see contract in [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md)). Some of the fields are **the same semantic layer** on which the UI is based (placement, visibility of slots, layout in the sense of CDS/host surface), and not “a parallel invention for the agent.” This is convenient interactively, but:

- **Regressions** in the response form are difficult to catch without running the IDE and the MCP script.
- **CI** wants to have **deterministic** contract checks: “after changing the snapshot collector, the response did not break.”
- A separate implementation “as a CLI” risks **diverging** from the real MCP - the tests will be green, the agent in Cursor will not.

## Solution

**Accept** a separate delivery - **headless CLI** (or a subcommand of an existing host), which:

1. **Calls the same** public/internal **data collectors and JSON serialization** as MCP handlers for the selected `ide_*` commands - **without a second copy of the logic** (general layer: services, `IdeMcpCommandExecutor` helpers, snapshot builders, etc., moved out so that they can be pulled from both the IDE process and the CLI).
2. Takes **explicit context** as input: at least `--workspace` (workspace root), if necessary, the path to the solution, flags “which tool to emulate” and args parameters (according to the MCP scheme).
3. Writes **to stdout** the same JSON (or the same **normalized** form) that would return the corresponding tool in MCP.
**CI:** in the root of the repository - [`.gitlab-ci.yml`](../../.gitlab-ci.yml): `dotnet build` / `dotnet test` and sequential smoke `dotnet run --project CascadeIDE -- --agent-contract ...` (expected **Windows** runner with .NET 10 SDK and tag `windows`; in a different environment - fix `default.tags`). Additionally, the repository accepts **`dotnet script`** (global `dotnet-script`, see `Financial/finplan/update-finplan-pdf.csx`, `agents-and-humans-book/update-agents-humans-pdf.csx`). To call `--agent-contract` from a pipeline with built `CascadeIDE.exe`: [`docs/samples/agent-contract-ci.csx`](../samples/agent-contract-ci.csx) - `ProcessStartInfo`, exit code, no surprises with `$LASTEXITCODE`. **PowerShell** alternative: [`docs/samples/agent-contract-ci.ps1`](../samples/agent-contract-ci.ps1) (`pwsh` 7+ or `Start-Process -PassThru.ExitCode` in Windows PowerShell 5.1).

**Tests:**

- **Integration / contract:** running CLI (or direct call to common API) on **fixture workspace** (minimal `.sln` / files are already in tests) → comparison with **expected JSON** or with **snapshot** with conscious update.
- **CDS (cockpit):** narrow **golden slice** - `schema_version` + `topology` + sorted `instruments` (`TestData/AgentContract/cockpit_surface_contract_slice.approved.json`); fields like `ui_mode` and `presentation_effective_line` are not included in the snapshot (they depend on the user's `settings.toml`). When changing the default placement or CDS scheme, update the approved file consciously.
- **Normalization for stability:** absolute paths → relative to fixture; cutting time/versions if necessary; or assert on a **subset of fields** (schema-shaped), if the full snapshot is too noisy.
- **“Rendering” at the data level:** for scenarios where MCP provides **cabin state / layout / tools** (what is consistent with what the UI draws), a snapshot of the same JSON in CI checks **representation regression** - not pixels or Skia/Avalonia-pipeline, but **what exactly should be in which slot and with what identifiers**. This is a meaningful "UI vs agent see the same thing" verification layer, without duplicating a separate "tests only" model.

**Negative:**

- **Pixels, fonts, visual regressions Skia/Avalonia** - still separate (screenshots, UI tests, manual review). CLI+MCP snapshots do not replace the render engine.
- Do not duplicate **business logic** “from scratch” - only a thin shell over the general code.

## Consequences

| Pros | Cons/risks |
|--------|----------------|
| Contract regressions in CI without GUI | You need to remove the common layer once and make sure that MCP and CLI go to the same entry point |
| Documenting “what exactly” gives the tool (through tests) | Fixtures and normalization - support when changing OS/paths |
| Quick feedback when refactoring JSON collectors | Scope on tools to grow in stages - otherwise there will be a big explosion of work |
| Checking the consistency of **cockpit/UI semantics** with what the agent sees (single source in JSON) | Risk of mixing layers: keep in snapshots fields that are actually shared with the UI, and not the entire response if it is noisy |

## Implementation stages (recommendation)

1. Select **one** tool with stable JSON (for example, a layout snapshot or one read-only script).
2. Create a **single** function “build payload” + “in JSON string”.
3. Add a minimal **CLI** (`dotnet run --project ...` or a separate `net10.0` tool).
4. Cover with **one** snapshot test; then expand the list of commands.

## Acceptance and open questions

**There are no open questions** - the direction is fixed for implementation.

Next: add commands to `AgentContractRunner.TryGetJson`, calling the same collectors as `IIdeMcpActions` / MCP; if there is a significant expansion, update the table here and in [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md).
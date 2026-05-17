<!-- English translation of adr/0019-shared-git-core-ide-and-git-mcp.md. Canonical Russian: ../../adr/0019-shared-git-core-ide-and-git-mcp.md -->

# ADR 0019: Common Git Core for Cascade IDE and git-mcp

**Status:** Accepted · Implemented  
**Date:** 2026-04-06  
## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Stable MCP contracts and testable infrastructure |
| [0002](0002-debug-human-agent-parity.md) | Single debug state layer for human and agent |

### Outside ADR

| Document | Role |
|----------|------|
| [git-and-submodules-v1.md](../../git-and-submodules-v1.md) | Git and Submodules Policy |
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | IDE MCP Protocol |

External repository: `git-mcp` (submodule `financial-open`).

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | `GitMcp.Core` is a common layer for `ide_git_*` in the IDE and git-mcp; parity argv |

---
## Context

1. **Two consumers of the same subject area:** Git operations for the agent are implemented both as **built-in IDE commands** (`ide_git_*`, `IIdeMcpActions` in `MainWindowViewModel.IdeMcpActions.Git.cs`), and as a separate process **git-mcp** (stdio MCP, tool directory, assembly of arguments and JSON responses in `Program.cs`).

2. **git-mcp** brought to a stable set of tools (including `fetch`, `pull`, `branch`, `show`, `submodule` next to `status`/`diff`/`commit`/`push`), with explicit rules for escaping arguments and a single response contract.

3. **Cascade IDE** now covers a **subset** of commands and keeps the logic **locally** (runner + argv collection + output trimming). At the target **parity in meaning** with git-mcp, the duplication will become noticeable and will diverge with any edits in one place.

4. **Precedent:** common layer **AgentNotes.Core** on **IDE** and **agent-notes-mcp** - package **[AIGuiders.AgentNotes.Core](https://www.nuget.org/packages/AIGuiders.AgentNotes.Core)** ([sources](https://github.com/KarataevDmitry/AIGuiders.AgentNotes.Core)).

## Solution (direction)

1. **Allocate the Git Core** library (working name, e.g. `GitMcp.Core` / `Cascade.Git.Core` - commit when deploying) in the same monorepo/submodule loop as git-mcp, similar to the separate **AgentNotes.Core** repo (NuGet package for two consumers):
   - building a list of `git` arguments for each supported operation;
   - uniform rules for **quotes/escaping** and restrictions (for example, incompatibility of `remote` and `all` for `fetch`);
   - if necessary - **normalized representation of the result** (success, exit code, stdout/stderr text, truncation policy), without reference to the MCP SDK or Avalonia.

2. **git-mcp** is translated to Core as **thin adapter**: parse JSON MCP arguments → call Core → return text/JSON to client.

3. **Cascade IDE** is transferred to Core in the **after** process abstraction layer ([0008](0008-mcp-contracts-and-testable-infrastructure.md)): `IGitCommandRunner` (or equivalent) remains the execution point; The VM/service collects the command via Core and sends it to the runner. The UI (Git panel, telemetry) is still subscribed to IDE events, not to Core directly.

4. **Parity:** the list of operations and semantics (`status`/`diff`/`commit`/`push` and extensions to the git-mcp level) are considered **the source of truth** in Core; `IdeCommands` and MCP-PROTOCOL are updated consciously when a tool is added (see [0013](0013-command-surface-and-discoverability.md), [0002](0002-debug-human-agent-parity.md)).

5. **Tests:** Core unit tests (arguments, edge cases, configuration error messages) common to both consumers; integration tests remain with everyone (MCP manifest / IDE dispatch).

## Consequences

- One PR to change git behavior for agent affects **Core**; IDE and git-mcp adapters are thinner.
- A **dependency** appears Cascade → Core: a consistent version policy is needed (submodule, package or relative `ProjectReference` - select during implementation).
- Documentation: Update [git-and-submodules-v1.md](../../git-and-submodules-v1.md) and [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) as `ide_git_*` is expanded.

## Rejected alternatives

- **Only document the contract** without a common library - cheaper in the short term, does not eliminate duplication and the risk of discrepancies with parity.
- **Consider git-mcp the only source and remove git from the IDE** - contradicts the built-in script (panel, telemetry, offline agent without external exe).

## Implementation (committed)
- **Source location:** submodule [`git-mcp-core/`](../../../git-mcp-core/) in the root of meta-repo `open` (next to `git-mcp`). **Canonical remote:** GitLab `Krawler/git-mcp-core`; public mirror and repo for **Trusted Publishing** NuGet - **[KarataevDmitry/git-mcp-core](https://github.com/KarataevDmitry/git-mcp-core)**. **Consumers** include the **`AIGuiders.GitMcp.Core`** package from nuget.org (`PackageReference`).
- **Building the library:** `GitMcp.Core.csproj`, `net10.0`, namespace `GitMcp.Core`, NuGet id **`AIGuiders.GitMcp.Core`**. There are no dependencies on `System.Text.Json` in Core - only primitives and argument lists; `GitArgsResult` for argv validation errors.
- **git-mcp:** `PackageReference` on **`AIGuiders.GitMcp.Core`**; calling `git` via `ProcessStartInfo.ArgumentList`. MCP server version **0.3.0**. `git_status` in MCP is a sequence from Core (`StatusMcpSequence`: `rev-parse` + `status`); in the IDE the panel is still `status --short --branch` (`StatusShortBranch`).
- **Cascade IDE:** `PackageReference` to the same package; expanded `ide_git_*` (log, fetch, pull, branch, show, submodule); for `ide_git_push` - `Push(..., defaultOriginWhenRemoteEmpty: false)` (without `origin` substitution when remote is empty), unlike MCP `git_push`.
- **Tests:** `GitMcp.Tests` via a link to `GitMcp.csproj` (transitively Core); `GitCommandBuilder` unit tests.
## Open questions (closed upon acceptance)

- ~~Location~~ - directory in `open`, not inside the `git-mcp` submodule.
- ~~Name~~ - `GitMcp.Core` / `GitMcp.Core`.
- ~~STJ in Core~~ - not used.
- ~~Migration order~~ - Core + both adapters in one change.
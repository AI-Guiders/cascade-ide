<!-- English translation of adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md. Canonical Russian: ../../adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md -->

# ADR 0089: Agent omnibus naming (`get_ide_state`) and **IDE Health** channel (instead of Workspace Health)

**Status:** Accepted  
**Date:** 2026-04-23  

## Related ADRs

| ADR | Role |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | debugging parity - **separate** solution; this ADR **doesn't** change the semantics of DAP/snapshot, only the names and term boundaries |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → CDS → composer → surface; **conveyor** same |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | contracts, tests |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | agent contract snapshots |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | MCP PROTOCOL |
**Summary:** remove confusion between **workspace = directory/repo** and **IDE snapshot** for agent; remove confusion between **Workspace Health** (observability strip) and “workspace on disk”. Solution: **rename** MCP omnibus `get_workspace_state` → **`get_ide_state`**; **rename** the product and in the code channel **Workspace Health** → **IDE Health** (specific identifiers are by repo agreement, e.g. `IdeHealth*`).

---

<a id="adr0089-context"></a>

## 1. Context

1. Today **`get_workspace_state`** actually gives **shot of the IDE handle** (solution, editor, breakpoints, `debug`, build, panels, `cockpit_surface`...), and not “just the folder path”. The name is **misleading** when discussed with agents and in documentation.
2. **Workspace Health** in the cockpit - about **building / tests / debugging / git** in the strip; the word *workspace* again carries the meaning of “project directory”, although we are talking about a **development environment in an IDE**.
3. The implementation of **a single debug-snapshot** ([0002](0002-debug-human-agent-parity.md)) is **orthogonal** to this ADR: first or in parallel, you can enter `DebugSnapshot`, reading it under the old omnibus name, until **this** ADR renames the tool.

<a id="adr0089-decision"></a>

## 2. Solution

1. **MCP / `IdeCommands`:** public name of the tool and `command_id` - **`get_ide_state`**, MCP tool - **`ide_get_ide_state`** (see [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); internal method - **`IIdeMcpActions.GetIdeStateAsync`**.
2. **Health channel:** rename **Workspace Health** → **IDE Health**: namespaces `Cockpit/Channels/…`, types `IWorkspaceHealthChannel` → `IIdeHealthChannel` (or other uniform name), provider, composer, UI strings, links in ADR/README. **The semantics of channel → CDS → compositor** from [0036](0036-cds-channel-compositor-surface-pipeline.md) **does not** change.
3. **Documentation and tests:** [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md), [architecture-migration.md](../architecture-migration.md) with a link to the tool, golden/approved JSON from [0052](0052-agent-contract-cli-and-snapshot-tests.md), if necessary - one line in [architecture-policy.md](../../architecture-policy.md).

<a id="adr0089-ui-scope"></a>

## 3. UI: what's included and what's not

- **Included (minimal):** anything the user **reads** as "Workspace Health" or the old omnibus name in tooltips/docs - **replace wording** with **IDE Health** / `get_ide_state` (ResX, lines in `Cockpit`, stripe captions, etc.). This is the **same** control/composition, **different name** (terminological correction, not a feature).
- **Not included:** new layout, change of PFD/MFD slots, new “design” of the health bar, debugging scripts in the UI - these are **other ADRs** (including [0002](0002-debug-human-agent-parity.md) for **debugging parity**: glyphs, panel, snapshot binding - **not** from 0089).

<a id="adr0089-non-goals"></a>

## 4. Boundaries (other things not included here)

- **Do not** duplicate [0002](0002-debug-human-agent-parity.md): do not describe DAP, `DebugSnapshot`, remove `show_debug_*` here - only **naming** and **readability** boundaries of "IDE vs workspace on disk".
- **Do not** change **CDS** and **topology** of regions; only **signatures/names**, where this is purely terminology.

<a id="adr0089-consequences"></a>

## 5. Consequences

- **Breaking change** for external MCP clients that called `ide_get_workspace_state` / `get_workspace_state`: update to `ide_get_ide_state` / `get_ide_state`; there are no aliases.
- Large, but **mechanical** refactoring in `Cockpit/Channels` and lines - if possible **separate logical commits** (MCP omnibus vs channel renaming vs docks).

<a id="adr0089-rejected"></a>

## 6. Rejected alternatives
- **Keep** `get_workspace_state` **without** the "IDE" synonym - rejected: accumulated confusion in CIDE discussions.
- **Rename only** omnibus **without** health channel - let's say as **phase 1**; complete alignment of terms is the goal of ADR.
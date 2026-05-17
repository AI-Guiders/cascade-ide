<!-- English translation of adr/0110-roslyn-refactor-intent-melody-bridge.md. Canonical Russian: ../../adr/0110-roslyn-refactor-intent-melody-bridge.md -->

#ADR 0110: Roslyn Range Refactorings - Intent Melody/IDE Bridge and Roslyn MCP

**Status:** Proposed  
**Date:** 2026-05-11  
## Related ADRs

| ADR | Role |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | parametric tail `:start:end`, §3 about refactorings |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | directory `[[melody_root]]`, build args in code |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | pairing an agent with Roslyn MCP |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id` |

### Outside ADR

| Document | Role |
|----------|------|
| [roslyn-mcp](../../../roslyn-mcp/README.md) | separate MCP server |

---
## Context

Full C# refactorings (Extract Method, Extract Interface, etc.) in the Cascade ecosystem are implemented **in the Roslyn MCP process**: `roslyn_get_code_actions` and `roslyn_apply_code_action` with an optional **range** (`end_line`, `end_column`) - see the tool diagrams and `ServiceLayer/CodeActions.cs` in the **roslyn-mcp** repository.

The **CascadeIDE** core does not duplicate the **Microsoft.CodeAnalysis.CSharp.Features** and MSBuildWorkspace stack for the same operations: the in-process editor uses simplified semantics ([`CSharpLanguageService`](../../Services/CSharp/CSharpLanguageService.cs)) without a full code actions pipeline.

Previously discussed mnemonic like `rmx` / `rix` ([0081](0081-parametric-intent-melodies-editor-line-ranges.md)); **do not store** stubs in the code without a real `command_id` - this is out of sync with the catalog and palette.

---

## Problem

1. The user expects **one line** `c:...` or **IdeCommands** leading to the same result as manually calling Roslyn MCP.
2. Duplicate implementation of refactorings inside **CascadeIDE.exe** is expensive and at odds with the single source of truth in **roslyn-mcp**.
3. We need an explicit **architectural place** for the future solution (bridge, delegation, settings), without dummy entries in TOML.

---

## Solution (direction)

1. **Canon of range operations in the IDE core** for today: already implemented layers - **select** (`select`), **replace text** (`apply_edit`), URL portal, etc. through the catalog [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md).
2. **Extract Method / Extract Interface and analogs** up to a separate ADR **Accepted** do not declare mandatory slugs in [`intent-melody-aliases.toml`](../../IntentMelody/intent-melody-aliases.toml). Options for the next iteration (mutually exclusive or combinable - decide upon implementation):
   - **Agent/external host** calls **Roslyn MCP** with the same solution/project path and range after the IDE has set the selection (`c:els:…` or equivalent).
   - **Optional bridge** in IDE: configurable path (localhost MCP, stdio, future in-proc host) and thin `command_id` that serializes intent + range and delegates to **roslyn-mcp** - see [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md).
   - **Rejection of individual mnemonics** `rmx`/`rix` in favor of the documented script “select range → code actions in Roslyn MCP”.

3. Catalog stubs **without** `command_id` in the bundle **do not use** - violate the invariant [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) (execution via `command_id` + args).

---

## Consequences

- Document **0081** with reference to this ADR fixes the border: **el\*** - in the core product zone; **r\*** and Roslyn refactorings - **after** an explicit bridge or only through an external MCP.
- Implementation of the bridge - separate commits: contract args, settings, tests, if necessary, new `IdeCommands` and regeneration of ProtocolDocGen.

---

## Rejected alternatives (briefly)

| Alternative | Why not now |
|--------------|----------------|
| Embed **CSharp.Features** + MSBuildWorkspace entirely in CascadeIDE | Duplication of **roslyn-mcp**, heavy pipeline, deployment size |
| Leave **rmx**/**rix** in TOML without execution | Confuses palette and chord ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)) |
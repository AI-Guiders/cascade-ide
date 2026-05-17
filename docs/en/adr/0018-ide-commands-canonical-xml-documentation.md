<!-- English translation of adr/0018-ide-commands-canonical-xml-documentation.md. Canonical Russian: ../../adr/0018-ide-commands-canonical-xml-documentation.md -->

# ADR 0018: Canonical XML docs for `IdeCommands` and ProtocolDocGen

**Status:** Accepted (partially: ProtocolDocGen + generated summaries; full XML on IdeCommands - on migration)  
**Date:** 2026-04-05  
## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | single registry `command_id` |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |

### Outside ADR

| Document | Role |
|----------|------|
| [ide-commands-protocol-docgen-contract.md](../dev/ide-commands-protocol-docgen-contract.md) | current parser contract |

---
## Context

The command registry **`IdeCommands`** (partial class, `Services/IdeCommands.cs` + `Services/IdeCommands.*.cs`) is supplied with XML comments; **`tools/CascadeIDE.ProtocolDocGen`** generates `IdeCommandsDoc`, `IdeCommandsArgs`, `IdeCommandsContract`, **`MCP-PROTOCOL.md`** fragment and lints the contract.

Currently, the machine-readable parts of the contract (**`returns:`**, **`args:`**, **`example:`**) are nested **in the text `<summary>`** - see [ide-commands-protocol-docgen-contract.md](../dev/ide-commands-protocol-docgen-contract.md). This simplified the first line parser, but:

- for **an open repository and new contributors** the **standard** structure of C# XML documentation (`<summary>`, `<param>`, `<returns>`, `<example>` / `<code>`) is more common;
- **IDE** and familiar tools expect a human description in the summary, and a return type and parameters in the designated tags.

## Solution (target state)

**Basic idea:** rely on **familiar** tags (`<summary>`, if necessary `<param>` / `<returns>` for a person), and express the **machine contract** for ProtocolDocGen **separately** - either inside standard tags by agreement, or through **your own (extended) XML tags**, without replacing the base ones with them.

### Option A - standard tags + conventions only

1. **`<summary>`** - brief description for the person; machine parts if necessary in **`<returns>`** and **`<param>`** by explicit rules (see option B below for mixing).

2. **`<param name="…">`** - human-readable description; for the `IdeCommandsArgs` scheme - **convention** (the first line is the `name:type` mini-grammar, the rest is text) **or** one line of the scheme in **`<remarks>`**.

3. **`<example>`** + **`<code language="json">`** - example for linting.

### Option B - standard tags + **custom** tags (preferably discussed)

Do not “break” the semantics of standard tags with a hard machine string, but **add** elements with **fixed names** under Cascade/ProtocolDocGen, for example (fix the names at the Accepted stage in the dev-doc):

- a separate tag for **argument schema** (one line in the old grammar or structured XML inside);
- a separate tag for the **return type** of the contract (`json` / `text` / `none`);
- if necessary, a separate tag for **JSON example** (if you don’t want to duplicate the logic with `<example>`).

**Why this is natural:** the C# compiler **preserves** non-standard tags in the output XML documentation; **IDE** by default shows in Quick Info mainly `<summary>` / `<param>` / `<returns>` - custom tags do not “break” tooltips, and **ProtocolDocGen** reads the full block and extracts both standard and custom elements. Many **SDKs and dock generators** do this (Sandcastle/DocFX have their own tags; the project has its own controlled set).

**Tag names:** It is better to have a **stable prefix** or a namespace-like scheme (`cascadeArgs`, `cascadeReturns` - or one consistent block name) so as not to interfere with future tool extensions.

### Common to both options

4. **Parser implementation:** parsing **XML document to symbol** (preferably **Roslyn** + full XML comment to `const`); gluing `IdeCommands*.cs` as it is now.

5. **Migration:** translate `IdeCommands.*.cs` to the selected template; if necessary, a short **compatibility** lint with the old format in `<summary>` or one major commit.

### Select A vs B

Leave for ADR finalization: if **B** - there are fewer compromises in the wording of standard tags and the “man/machine” boundary is clearer; price - **document the list of custom tags** and embed them into the parser once.

## Consequences
- **Pros:** closer to the C# ecosystem, easier onboarding; **option B** further separates "prose for people" from "fields for generator" without layering grammar on `<summary>`.
- **Disadvantages:** one-time **cost** of refactoring the parser and comments; for **A** - strict convention on the text inside `<param>` / `remarks`; for **B** - **catalog of custom tags** and support in ProtocolDocGen (but the contract is explicit).
- **Documentation:** update [ide-commands-protocol-docgen-contract.md](../dev/ide-commands-protocol-docgen-contract.md) after accepting the implementation (or in the same PR as the parser).

## Rejected alternatives

- **Keep only the minilanguage in `<summary>` forever** - rejected as a long-term goal for an open repository: works, but worse for contributors and standard expectations.

## Status

Before switching to **Accepted** - after choosing **option A or B**, exact tag names (including custom ones), agreement on args and parser implementation (and, if desired, a pilot on one file `IdeCommands.*.cs`).
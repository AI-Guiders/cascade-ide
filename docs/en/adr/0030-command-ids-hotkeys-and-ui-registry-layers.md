<!-- English translation of adr/0030-command-ids-hotkeys-and-ui-registry-layers.md. Canonical Russian: ../../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md -->

# ADR 0030: Layers of team identifiers, hotkeys and UI (without one “all-in-one” table for now)

**Status:** Accepted · Implemented (registry of v1 commands in the code)  
**Date:** 2026-04-08  
### Outside ADR  
**Implementation (current):** partial **`IdeCommandRegistry*.cs`** (palette + metadata of global window hotkeys), **`IdeCommandPaletteCatalog`** (projection), **`HotkeyTomlLoader`**, **`MainWindowHotkeyService`**, **`Hotkeys/hotkeys.toml`**; consistency tests - **`CascadeIDE.Tests/IdeCommandRegistryTests.cs`**. The full `IdeCommandUiMeta` blueprint from the blueprint is still separate iterations (§6 below).

## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | command surface, palette, `hotkeys.toml` |
| [0018](0018-ide-commands-canonical-xml-documentation.md) | canon XML for `IdeCommands` |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | path `%LocalAppData%\CascadeIDE\`, incl. custom `hotkeys.toml` |

### Outside ADR

| Document | Role |
|----------|------|
| [ide-command-registry-v1.md](../../design/ide-command-registry-v1.md) | ide command registry v1 |
**Implementation (current):** partial **`IdeCommandRegistry*.cs`** (palette + metadata of global window hotkeys), **`IdeCommandPaletteCatalog`** (projection), **`HotkeyTomlLoader`**, **`MainWindowHotkeyService`**, **`Hotkeys/hotkeys.toml`**; consistency tests - **`CascadeIDE.Tests/IdeCommandRegistryTests.cs`**. The full `IdeCommandUiMeta` blueprint from the blueprint is still separate iterations (§6 below).

---
## Context

[0013](0013-command-surface-and-discoverability.md) has already recorded the intention: **palette + parity with MCP**, **gestures in data files** (`Hotkeys/hotkeys.toml` + custom overlay), and in the consequences - **“unified command registry”** with a reference to the drawing [ide-command-registry-v1.md](../../design/ide-command-registry-v1.md).

In practice, there are now **several coordinated but separated layers**: there is no one structure in the code like “id → ICommand → gesture → menu item → line in the palette.” This raises questions during development (“where is the truth?”) and the risk of out of sync when adding commands.

It is necessary to **explicitly describe** what is considered canon at what level and what remains an **intentional bridge in the code** until the full catalog of UI metadata from the blueprint is implemented.

---

## Solution

### 1. Canon `command_id` for agent and MCP execution

- **`IdeCommands`** (partial class, `Services/IdeCommands*.cs`) - **sole source of string constants** for `ide_execute_command` / MCP, documentation ([MCP-PROTOCOL.md](../../MCP-PROTOCOL.md), ProtocolDocGen generation) and contract lint ([0018](0018-ide-commands-canonical-xml-documentation.md)).
- **`IdeMcpCommandExecutor`** - **executor** registry: `command_id` → handler.

### 2. Command Palette (subset + human metadata)

- **`IdeCommandPaletteCatalog`** - a static list **not of all** `IdeCommands`, but of commands that **should be discoverable through the palette** (title, category, optional args, rules by `UiModeFamily`).
- The identifier in the palette line matches **`command_id`** where the command goes in the MCP; individual `PaletteId` are valid as a row key, but **name parity** with `IdeCommands` is the target rule ([0013](0013-command-surface-and-discoverability.md)).

### 3. Gestures (strings of keys), not command logic

- **`Hotkeys/hotkeys.toml`** (spike) and **`%LocalAppData%\CascadeIDE\hotkeys.toml`** (overlay) - merge via **`HotkeyTomlLoader`**; result:
  - tooltips next to commands in the palette (**`HotkeyGestureMap`**, key = `command_id` where applicable);
  - **`Window.KeyBindings`**, menu items (**`HotKey`**) and **tunnel** for focus in the editor - **`MainWindowHotkeyService`**.
- **Source of truth for gesture string** - TOML, not literals in AXAML/C# (policy [0013 § Implementation Decisions](0013-command-surface-and-discoverability.md)).

### 4. Keys without `IdeCommands` (UI only)

Some gestures are bound to **RelayCommand in `MainWindowViewModel`**, for which **there is no** separate constant in `IdeCommands` (example: **`debug_start_or_continue`** - “Start or continue” in the debug menu). Such ids **are reserved in `hotkeys.toml`** and in **`MainWindowHotkeyService`** as stable UI string identifiers; in MCP they **don't** have to exist.

Similar to **`set_ui_mode_by_index_0` ... `_8`** - a parameterized family of gestures for UI modes, more than one `IdeCommands` command.

### 5. VM ↔ TOML bridge
- **`IdeCommandRegistry`** specifies which `command_id`/hotkeys require a **global** gesture on the main window; resolution in `ICommand` - in **`MainWindowHotkeyService`** (`ResolveWindowCommand` by `MainWindowHotkeyVmBinding`). Gestures are still only in TOML.

### 6. Target state (does not block current implementation)

The complete **glue** “id → UI metadata → execution → gesture” is described in the drawing **[ide-command-registry-v1.md](../../design/ide-command-registry-v1.md)** (`IdeCommandUiMeta` / single directory). The transition to it is **separate iterations** (refactoring the palette, validating the coverage, generating fragments from one source if desired). This ADR **does not cancel** the drawing and **does not require** immediate implementation of the entire list from §2 of the drawing.

---

## Consequences

- New command **for MCP**: add a constant to **`IdeCommands`**, a handler to **`IdeMcpCommandExecutor`**, if necessary a line to **`IdeCommandPaletteCatalog`**, a key to **`hotkeys.toml`** (if a gesture is needed), and an entry to **`MainWindowHotkeyService`** if the command is attached to the main window/tunnel/menu with the same id.
- Reviewers and feature authors are guided by this ADR + [0013](0013-command-surface-and-discoverability.md) + drawing [ide-command-registry-v1.md](../../design/ide-command-registry-v1.md) so as not to wait for **one class “registry of everything”** before it appears.
- When implementing **`IdeCommandUiMeta`** (or equivalent), this ADR can be qualified with a PR/migration reference or marked **Superseded** for §5-§6 without changing the meaning of [0013](0013-command-surface-and-discoverability.md).

## Rejected alternatives (as immediate requirement)

- **Declare existing `IdeCommands` as a complete "UI registry"** - rejected: there are no palette headers, categories, mode rules and gestures; This is an automation contract.
- **Consider `hotkeys.toml` the only command registry** - rejected: it has no execution and not all keys are MCP `command_id`.
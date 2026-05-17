<!-- English translation of adr/0107-blank-solution-creation-via-dotnet-new-sln.md. Canonical Russian: ../../adr/0107-blank-solution-creation-via-dotnet-new-sln.md -->

# ADR 0107: Creating an empty solution via `dotnet new sln` (workspace self-sufficiency)

**Status:** Accepted · Implemented  
**Date:** 2026-05-10

## Related ADRs

| ADR | Role |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | linked ADR |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | linked ADR |
| [0013](0013-command-surface-and-discoverability.md) | linked ADR |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | linked ADR |
| [architecture-migration.md](../architecture-migration.md) | outside numbered ADR |


## Summary

- Blank solution: **`dotnet new sln`**, menu/MCP, `BlankSolutionCreator`.
- `IDotnetCommandRunner` - general CLI runner.


---

## 1. Context

CascadeIDE already knew how to **open** a solution, project or folder as a workspace. For a "from scratch" scenario without an external IDE, the minimum step was missing: **create a new `.sln`** file to immediately load the tree and continue working inside the CIDE.

There is a separate ecosystem of **.NET templates** (including NuGet packages with `packageType: Template`): after `dotnet new install...` the same templates are visible in both Visual Studio and `dotnet new`. This is **not** a duplication of "VS magic" - a shared templating engine. In the first iteration, CIDE **doesn't** embed the template directory from NuGet and **doesn't** override package installation; Just rely on the **built-in SDK template** for a blank solution.

---

## 2. Solution

1. **Canonical operation v1:** create an empty solution with the command  
   `dotnet new sln -n <name> -o <parent_directory>`  
   where target path is the full path to the future `<name>.sln`, the directory is created if necessary, the file **not** must exist before the call.

2. **Logic placement (strangler, ADR 0006 / 0102):**
   - orchestration without UI - **`Features/Workspace/Application/BlankSolutionCreator`**;
   - CLI launch - only through the **`IDotnetCommandRunner`** port (DAL, do not duplicate `Process` in the VM);
   - UI: "Save As" dialog for `.sln`, then **`MainWindowViewModel.TryCreateBlankSolutionAtPathAsync`** and **`LoadSolution`** - in View/VM bridge as for "Open Solution".

3. **Command surface (ADR 0008 / 0013):**
   - constant **`IdeCommands.CreateNewSolutionDialog`** (`create_new_solution_dialog`);
   - menu item **File**, palette (`IdeCommandRegistry`), MCP handler next to the rest of the “File” dialogs;
   - description and contract - through the existing **ProtocolDocGen** (`IdeCommands` XML-doc).

4. **Tests:** unit tests with substitution of **`IDotnetCommandRunner`** (including simulating a successful `dotnet new sln` and checking arguments); failure if `.sln` already exists.

---

## 3. Consequences

- **`dotnet`** must be available on the user's machine with the `sln` template (typical SDK installation).
- Once created, the solution is **empty** (no projects): building/debugging is only meaningful after adding projects - this is expected and documented by product behavior, not an ADR error.
- Migration log captures slice (**v1.41m**): new Application file + tests.

---

## 4. Rejected / deferred alternatives

| Alternative | Why not v1 |
|--------------|--------------|
| Generate `.sln` manually (as text) without `dotnet` | Duplicate format, discrepancy with SDK and future versions. |
| Built-in solution + project wizard and template selection from NuGet | More UX and contracts; done in separate ADR/iterations after the empty `sln` has stabilized. |
| MCP only without menu | It is not enough to be “self-sufficient” without an external agent; menu - canonical parity with `open_*_dialog`. |

---

## 5. Extensions (outside the scope of the v1 implementation in this ADR)

Below is **target parity with VS** not as a copy of the UI, but as **same contract as the .NET SDK**: template engine + solution graph + links. Implementation in CIDE - in separate iterations; the guideline for commands is fixed in §6.

---

<a id="adr0107-vs-parity-cli"></a>

## 6. Parity with VS: `dotnet` surface (set for CIDE)

**Idea:** Visual Studio for the typical “new project / solution structure” relies on the **template engine** and the **solution + projects** model - the same thing the CLI provides. CIDE is not required to repeat every VS dialog; parity is in **possibilities** derived from the same primitives.

Canonical Microsoft documentation: [.NET CLI overview](https://learn.microsoft.com/dotnet/core/tools/), [`dotnet new`](https://learn.microsoft.com/dotnet/core/tools/dotnet-new), [`dotnet sln`](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln).
### 6.1. Templates and project "types" (as in VS after installing the template pack)

| Problem | CLI | Note for parity |
|--------|-----|---------------------|
| List of available templates (installed + built into SDK) | `dotnet new list` | With .NET 7 - `list` / `search` / `install` / `uninstall` / `update` subcommands. |
| Find templates in NuGet | `dotnet new search <string>` | An analogue of “look through the gallery” before installation. |
| Install a template package (including from NuGet or `.nupkg`) | `dotnet new install <PACKAGE_ID_or_path>` | After this, the templates are visible in both VS and `dotnet new` - one source. |
| Remove package | `dotnet new uninstall <PACKAGE_ID_or_path>` | Without an argument, a list of installed packages and uninstall commands. |
| Update installed packages | `dotnet new update` | Incl. `--check-only` for checking. |
| Create a project/item using a template | `dotnet new <shortName> -o <path> ...` | Parameters (`-f`, `--lang`, custom from `template.json`) - as in VS “Additional info”. |
| Element templates (files in the project) | `dotnet new <itemTemplate>` in project directory | VS “Add → Class”, etc. — the same mechanism, different short name. |

See also: [Custom templates for `dotnet new`](https://learn.microsoft.com/dotnet/core/tools/custom-templates).

### 6.2. Solution graph (Solution Explorer in CLI terms)

| Problem | CLI |
|--------|-----|
| List of projects in `.sln` / `.slnx` / `.slnf` | `dotnet sln [<file>] list` |
| Add project(s) | `dotnet sln [<file>] add <paths.csproj…>`; options `--in-root`, `-s\|--solution-folder` (solution folders, as in VS) |
| Remove project(s) | `dotnet sln [<file>] remove <path_or_project_name>` |
| Migration `.sln` → `.slnx` | `dotnet sln [<file>] migrate` |

Source: [`dotnet sln` (Microsoft Learn)](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln).

### 6.3. Links between projects (Project → Project)

| Problem | CLI |
|--------|-----|
| Add a link to another project | `dotnet reference add <path.csproj>` |
| List of links | `dotnet reference list` |
| Remove link | `dotnet reference remove` |

### 6.4. Links to NuGet packages (not to be confused with templates)

| Problem | CLI (current style - see docs for your SDK version) |
|--------|------------------------------------------|
| Add/remove/list package | `dotnet package add` / `dotnet package remove` / `dotnet package list` (+ `search`, `update`, ...) |

This is closer to a **NuGet/dependency** than a "new project type"; in VS there is a separate package manager.

### 6.5. What VS parity via CLI usually **not** includes (or second tier)

- **COM / Reference Manager / custom assemblies** - specifics of the .NET Framework and UI VS; for cross-platform CIDE - behind brackets or a separate mode.
- **`dotnet workload`** (MAUI, wasm-tools, ...) - if templates for workload are needed, this is a separate “SDK readiness” layer (see IDE Health / environment).
- **`dotnet msbuild` / custom goals** - powerful, but not a replacement for meaningful UX.

### 6.6. Recommended order of implementation in CIDE (to the roadmap)

1. **Already have:** empty solution (`dotnet new sln`) - §2.
2. **High win:** wizard or two steps “new project in current solution”: `dotnet new <template> -o ...` + `dotnet sln add` (+ optional `solution-folder`).
3. **Template maintenance:** wrappers over `dotnet new list/search/install/uninstall/update` (log in UI, without its own NuGet client in v1).
4. **References:** `reference` and `package` - from the decision tree/project context.

All calls are made through the same **`IDotnetCommandRunner`** (and, if necessary, parsing stdout for errors), without duplicating `Process` in the VM.
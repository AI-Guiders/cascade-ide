<!-- English translation of adr/0006-presentation-layers-and-feature-slices.md. Canonical Russian: ../../adr/0006-presentation-layers-and-feature-slices.md -->

# ADR 0006: Layers, vertical slices and the role of MainWindowViewModel

**Status:** Accepted  
**Date:** 2026-04-02  
## Related ADRs

### Outside ADR

| Document | Role |
|----------|------|
| [Features/README.md](../Features/README.md) | Features/README |
| [architecture-migration.md](../architecture-migration.md) | architecture migration |

---
## Context

We need a predictable structure of one desktop application (Avalonia + MVVM) without mandatory DDD or hexagonal for the entire code, but with clear boundaries so as not to get an unsupported God ViewModel and not to mix UI, scripts and access to disk and processes.

## Solution

### Horizontal layers

- **UI** - Views (AXAML), code-behind only for view tasks (`Views/`).
- **Presentation** — ViewModels, commands, bindings (`ViewModels/`).
- **Application / domain logic** - scripts not tied to Avalonia: parsers, coordination (`Services/`, part).
- **Infrastructure** - files, processes, git, HTTP, MCP transport (`Services/`, part; `IdeMcpServer`).

**Rule:** ViewModel does not contain direct details of running processes and reading files if this can be given to a service with an interface (or a narrow static helper with one responsibility).

### Vertical slices (features)

A large feature (Git panel, terminal, debugging, chat) is designed as a slice: base `Features/<Name>/` and namespace `CascadeIDE.Features.<Name>`; Other agreed placement is acceptable, the main thing is uniformity.

**Rule:** a new bottom or sidebar or large block is a separate `*ViewModel`, if possible a separate `*View`.

### MainWindowViewModel

**Composer:** creates child VMs, forwards dependencies, reacts to window-level events (change of solution, closing). Let's say a bridge to MCP.

**Do not** store here the voluminous logic of a new feature (dozens of git methods, parsing paths, searching for solutions) - export it to a panel VM and services.

###List Models and Modules

- List row models (`Models/GitStatusRow` and similar): data and flags for display, without heavy business logic.
- One new module - one clear namespace root (for example `CascadeIDE.Features.Git`).

## Consequences

New features are designed in layers and slices; migration of old code from a monolithic VM - gradually, see [architecture-migration.md](../architecture-migration.md).

## Rejected alternatives

- Introduce full DDD or mandatory hexagonal on all code by default - rejected as redundant for the current scale.
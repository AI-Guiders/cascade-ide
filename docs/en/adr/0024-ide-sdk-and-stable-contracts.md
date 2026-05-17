<!-- English translation of adr/0024-ide-sdk-and-stable-contracts.md. Canonical Russian: ../../adr/0024-ide-sdk-and-stable-contracts.md -->

# ADR 0024: SDK for CascadeIDE - stable contracts for internal extension and future plugins

**Status:** Proposed  
**Date:** 2026-04-08

## Related ADRs

| ADR | Role |
|-----|------|
| [0005](0005-defer-dynamic-plugins-mef.md) | deferred plugins |
| [0006](0006-presentation-layers-and-feature-slices.md) | layer/slice boundaries |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | contracts and testing infrastructure |
| [0013](0013-command-surface-and-discoverability.md) | command surface |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | common core as a precedent |
| [0023](0023-markdown-diagrams-language-tooling.md) | external instruments/LSPs as contracts |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | geometry preview Markdown in TOML |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | zones of attention PFD/MFD/… in SDK and capabilities |

## Summary

- **CascadeIDE SDK** - stable contracts for internal extension and future plugins.
- Versioning of public APIs; border with “everything internal”.
- Communication with attention zones - [0025](0025-sdk-attention-zones-and-capabilities.md).

---
## Context

CascadeIDE is actively developing “from the inside”: new panels, tools, diagnostic channels, UI modes and integration with external processes (LSP/DAP/MCP) are being added. In this case:

- Ease of development depends on the **mental model**: where are the boundaries, what dependencies are acceptable, what is considered “core”, what is a “feature”.
- `MainWindowViewModel` and UI-shell inevitably become a place where “everything is asked” if there are no explicit contracts.
- Plugins (dynamic loading of DLL/MEF) are **deferred** for now ([0005](0005-defer-dynamic-plugins-mef.md)), but when we return to them, we will need “where to insert”, otherwise the plugin-host will appear before the form of slots/contracts is stabilized.

We need an “SDK” - but first of all **for us**, so that:

- reduce coupling between features,
- ensure extensibility without chaos,
- prepare the base for future plugins without premature plugin-host.

---

## Solution

Adopt the approach: **SDK = stable IDE extension contracts** rather than a must-have plugin system today.

SDK v1 includes:

1. **Explicit public interfaces/contracts** between shell and features (and between features), designed as a separate layer (folder/project), with minimal dependencies.
   - Delivery format: **separate project** `CascadeIDE.Contracts` (or a similar name), not a folder inside `cascade-ide`.

2. **Capability model** (declaration of capabilities), so that a feature can “connect” to the shell and to each other without direct references to specific implementations.

3. **Capability registry - hybrid (follows from the current IDE logic):**
   - **Code-first registration** capabilities features at startup (without MEF/reflection “for growth”).
   - **Data overlay for presentation**: UI modes/layouts (TOML) can enable/disable or change presentation capabilities, but do not replace their semantics.
   - **Introspection**: shell can assemble a “capability map” for diagnostics/telemetry/agent.

4. **Command surface as a contract**: commands (and their discoverability) are formalized not through “knowing the insides” of a feature, but through a general registration/metadata contract (in the spirit of [0013](0013-command-surface-and-discoverability.md)).

5. **Out-of-proc as the norm for integrations** (LSP/DAP/MCP/CLI): protocols and DTOs are part of the “SDK” in the sense of stable contracts (see [0008](0008-mcp-contracts-and-testable-infrastructure.md)). In-proc extensions are not prohibited, but must be within the scope of contracts.

6. **Contract marking `Experimental`/`Stable` is for internal clarity, not a public promise.**  
   At the active-dev stage (far before alpha), the goal is not “forever compatibility”, but discipline of boundaries: by default everything is `Experimental`, and `Stable` appears only where the team consciously wants to rely on the contract. You can break `Experimental` freely; break `Stable` - consciously (via ADR/migration note), SemVer - when a product need appears.

7. **Deferred plugin-host**: dynamic loading of plugins remains deferred (see [0005](0005-defer-dynamic-plugins-mef.md)), but the future plugin-host must connect through the same SDK contracts as the “internal” features.

### Cockpit Attention Model (PFD/MFD/Forward/EICAS/HUD) and SDK
The semantics of zones is described in [0021](0021-pfd-mfd-cockpit-attention-model.md). **Explicit binding of UI‑capabilities to attention zones at the `CascadeIDE.Contracts` level** is a separate solution and phased implementation: [0025](0025-sdk-attention-zones-and-capabilities.md). So [0024](0024-ide-sdk-and-stable-contracts.md) remains about the general SDK, and the cockpit axis is not mixed with the registry/plugin discussion.

---

## Capability registry / capability-map (API and principles)

Goal: The capability layer should be strong enough to hold boundaries and a mental model, but simple enough to actually be used.

### What we consider capability

- **Service capability**: “I provide a service” (contract/interface + implementation).
- **Command capability**: “I provide a command” (discoverability, category, hotkeys, availability).
- **UI surface capability**: “I provide a UI slot/surface” (panel/page/tab), while the inclusion/layout remains overlay (TOML).

### Code-first module registration (no MEF/reflection)

- The register of modules is formed **explicitly by code** (manual list), without scanning assemblies.
- Each feature has one registration entry point, for example `ICascadeFeatureModule` → `Register(ICapabilityRegistry registry)`.

### Keys and dependencies

- Capabilities are identified by **string ids** (constants in the SDK/contracts), for example: `git.core`, `docs.markdown.exportExpanded`.
- The capability-descriptor supports **explicit dependencies** (`Requires`) for explainability (“why the capability is not active/visible”).

### Capability-map (introspection)

The recording layer is required to collect a “capability map” as an immutable description:

- list of service/command/ui capabilities with metadata (id, owner module, stability, tags, requires);
- the “available/enabled” state can be calculated by the shell taking into account overlay (for example UiMode TOML) and runtime conditions.

Capability-map is used for:

- diagnostics and explainability (“why the button/panel is missing”),
- telemetry and UI/agent snapshots (introspection),
- future plugin-host (the external module must be registered in the same way).

### TOML overlay (presentation)

UI modes/layouts (TOML) must refer to capabilities **by id**, controlling presentation (visibility/placement), but not replacing the semantics of capabilities.

---

## Minimum API (draft contracts)

Below is the minimum form of contracts for `CascadeIDE.Contracts` (without being tied to a DI container/framework):

- `ICascadeFeatureModule`
  - `string Id { get; }`
  - `void Register(ICapabilityRegistry registry)`

- `ICapabilityRegistry`
  - `void RegisterService(ServiceCapabilityDescriptor descriptor)`
  - `void RegisterCommand(CommandCapabilityDescriptor descriptor)`
  - `void RegisterUiSurface(UiSurfaceCapabilityDescriptor descriptor)` *(optional for MVP)*
  - `CapabilityMap BuildMap()` *(for introspection/diagnostics)*

- `CapabilityMap`
  - `IReadOnlyList<ServiceCapabilityDescriptor> Services`
  - `IReadOnlyList<CommandCapabilityDescriptor> Commands`
  - `IReadOnlyList<UiSurfaceCapabilityDescriptor> UiSurfaces`

- General descriptor fields:
  - `string Id` (strict identifier)
  - `string OwnerModuleId`
  - `ApiStability Stability` + `[ApiStability]`
  - `string[] Tags`
  - `string[] Requires`

Explanation: `CapabilityMap` describes “what is registered”, and “whether it is enabled” and “how it is allocated” is calculated by the shell taking into account TOML overlay and runtime conditions.

---

## What is NOT the goal (v1)

- Do not introduce the mandatory “plugins from DLL folder” mechanism now.
- Do not promise binary compatibility for third-party extensions “forever”.
- Do not turn the SDK into a “god API” without boundaries (on the contrary, the goal is to reduce the surface).

---

## Practical principles for SDK design

- **Minimal surface**: contracts should describe “what is needed”, not “how it is done.”
- **Dependencies are directed outwards**: features depend on contracts, not on specific features.
- **DTO is separate from UI**: portable data models do not depend on Avalonia controls.
- **Testability**: contracts are easy to mock; execution is transferred to services.
- **Security by default**: everything that runs code/processes/network requests is through explicit gateways and settings.

---

## How to reflect `Stable`/`Experimental` in code and documentation

Minimum contract (no bureaucracy, useful already in active-dev):
- **Namespace signal**: there are two “roots” in `CascadeIDE.Contracts`:
  - `CascadeIDE.Contracts.Experimental.*` - everything is new by default.
  - `CascadeIDE.Contracts.Stable.*` is what the team consciously relies on.
- **Marker attribute**: single `ApiStabilityAttribute` + enum (`Experimental`, `Stable`) for quick searches/analysis and future expansion (for example `Deprecated`).
- **Why both**: namespace gives “default visibility” (and simplifies dependency rules), the attribute gives a point label and a basis for future analyzers/reports.
- **Documentation**: in the ADR and/or in the README of the contract project, we fix the rule “by default Experimental”, the criteria for transferring to Stable and the expectation of migration during breaking-change.

---

## Consequences

- New features are added through clear slots/contracts, fewer “hidden” connections.
- Development is accelerated: it’s easier to understand where to add logic, easier to test, fewer regressions from random dependencies.
- A smooth base appears for future plugins: when the plugin-host ceases to be deferred, the “SDK form” will already exist.

---

## Rejected alternatives

- “SDK = immediately plugin-host (MEF/DLL loading)” - rejected as premature complication ([0005](0005-defer-dynamic-plugins-mef.md)).
- “No SDK, everything through direct links” - rejected: worsens the mental model, accelerates the growth of connectivity and size of the shell composer.

---

## Open questions

## Visualization and documentation capability-map (accepted direction)

Minimal path (v1), without separate UI:

- **The principle of “thin snapshots”** (applicable to both capability-maps and UI snapshots/other dumps):
  - by default we return **summary + hash** (and, if necessary, a short list of ids/counters);
  - “thick” data is obtained **by request** (filters/pagination) or through a **dump file**.
- **JSON dump file** capability-map is available in diagnostics/logs (for explainability and debugging of integrations), returning `path + hash`.
- Capability-map can be included in **MCP/UI snapshot** only in **summary** form; details - with a separate command or through a dump file (so as not to inflate the context).

Next step (when needed):

- **Capabilities** page/tab in Debug/Power (or a separate document in `docs/`) with grouping by `OwnerModuleId` and filter by `Stable/Experimental`.
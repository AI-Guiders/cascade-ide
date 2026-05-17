# CascadeIDE current architecture (v1)

This document is the **single entry point** for **how things are structured today** (not “why”).  
Decision context and alternatives — in **ADR**: [../adr/README.md](../adr/README.md).  
Policy and “where to look” — [../architecture-policy.md](../architecture-policy.md).

---

## 1. System model (one screen)

CascadeIDE is a desktop IDE (Avalonia + MVVM) where **cockpit semantics** (PFD / Forward / MFD) define the attention structure, and MCP makes the IDE agent-operable.

- **PFD**: primary attention zone, short situational summary and “command” indicators.
- **Forward**: work zone (editor/docs), main action stream.
- **MFD**: secondary loop — **long streams** (terminal/build/Git/…): **pages** on a stack.

Layout reference for the main window and region names for MCP:
- [../ui-ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md)

---

## 2. Layers and responsibility boundaries

Normative layers and “what lives where”:
- **ADR 0006**: layers, feature slices, role of `MainWindowViewModel` — [../adr/0006-presentation-layers-and-feature-slices.md](../adr/0006-presentation-layers-and-feature-slices.md)
- **ADR 0102**: DAL (external adapter boundary) — [../adr/0102-data-acquisition-layer-boundary-and-contract.md](../adr/0102-data-acquisition-layer-boundary-and-contract.md)
- **ADR 0097**: CCU (raw input → DTO/snapshot fold) — [../adr/0097-cockpit-compute-units-transport-to-channel-dto.md](../adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
- **ADR 0099**: IDE DataBus (typed events) — [../adr/0099-ide-databus-typed-events-and-projections.md](../adr/0099-ide-databus-typed-events-and-projections.md)
- **ADR 0036**: CDS → compositor → surface (cockpit as meaning domain) — [../adr/0036-cds-channel-compositor-surface-pipeline.md](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **ADR 0079**: IDS (IDE overlays) as separate domain from CDS — [../adr/0079-ide-display-system-ids-overlay-pipeline.md](../adr/0079-ide-display-system-ids-overlay-pipeline.md)

Practical mental model (top to bottom):

- **UI (Views)**: `Views/*.axaml` and related `*.axaml.cs`. Keep simple: layout, bindings, region naming.
- **VM (ViewModels)**: state composition, commands, links between attention zones.
- **Application / orchestration**: use-case coordination inside a feature (typically `Features/<Feature>/Application/*`).
- **DAL**: inbound/outbound to the outside world (processes, git, LSP, MCP clients, file system).
- **Transport / bus / batching**: event/line delivery to UI (bounded, backpressure).
- **CCU**: fold events/raw input into DTO suitable for UI and observability.

---

## 3. “Flight” UI architecture (fact, not concept)

The main window is **three columns** PFD | Forward | MFD. Long streams (Terminal/Build/Git/…) live as **MFD pages**, not a full-width bottom panel.

Detail:
- [../ui-ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md)
- Concept → code map (what is historical vs current): [../ui-ux/concept-to-implementation-map-v1.md](../ui-ux/concept-to-implementation-map-v1.md)

Key MFD elements (current names):
- `MfdShellView` + `MfdShellPageStack`
- stack host region (snapshots/theme/contracts): `MfdContourStackHost`

---

## 4. MCP: IDE as tool server

Contract and protocol:
- [../../MCP-PROTOCOL.md](../../MCP-PROTOCOL.md)
- **ADR 0008** (contracts and testable infrastructure): [../adr/0008-mcp-contracts-and-testable-infrastructure.md](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **ADR 0052** (contract CLI and snapshot tests): [../adr/0052-agent-contract-cli-and-snapshot-tests.md](../adr/0052-agent-contract-cli-and-snapshot-tests.md)

Remember:
- MCP targets **observability** (snapshots, diagnostics) and **control** (commands), not “secret” APIs.
- UI region keys exposed to the agent should be stable and documented (see layout doc above).

---

## 5. Hybrid index and code navigation (brief)

- **Hybrid index (FTS + vec)** as local context DB:  
  [../adr/0105-hybrid-codebase-index-for-csharp-web.md](../adr/0105-hybrid-codebase-index-for-csharp-web.md) (Accepted · Implemented)  
  [../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md](../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) (Proposed)
- Navigation MCP (`get_code_navigation_context`) and presets:  
  [../adr/0039-workspace-navigation-affordances.md](../adr/0039-workspace-navigation-affordances.md)

---

## 6. “Where to look in code” (anchors)

Not a full list — a “first ten” for orientation.

- **UI layout / regions**: `Views/MainWindow.axaml`, `Views/MfdShellView.axaml`
- **Main window VM**: `ViewModels/MainWindowViewModel.*.cs` (partials)
- **Hybrid Index orchestration**: `Features/HybridIndex/Application/*`
- **CCU / cockpit channels**: `Cockpit/ComputingUnits/*`, `Cockpit/Channels/*`, `Cockpit/Cds/*`, `Cockpit/Composition/*`, `Cockpit/Surface/*`
- **MCP tool catalog / protocol docs**: `Services/*` (see ADR 0008 and `MCP-PROTOCOL.md`)

For layer boundaries, Roslyn analyzers help:
- [../../../CascadeIDE.ArchitectureAnalyzers/README.md](../../../CascadeIDE.ArchitectureAnalyzers/README.md)

---

## 7. What to treat as historical (do not confuse with current)

The repo has docs and concepts describing older layouts (e.g. full-width “bottom panel”).  
See explicit “old topology” notes in:
- [../ui-ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md)
- [../../architecture-migration.md](../../architecture-migration.md)

---

## 8. How to update this document

Update when at least one of these changes:
- attention zone topology (PFD/Forward/MFD), key regions and their names;
- layer boundaries (DAL/CCU/DataBus/IDS/CDS) or the main “data path”;
- MCP contract (new tools, key/format changes).

Version: **v1** (current slice).  

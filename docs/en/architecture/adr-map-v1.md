# ADR map: how to read CascadeIDE architecture (v1)

This document answers: **which ADRs to read to understand the system** — by topic and by role.  
It does not replace the ADR index: [../adr/README.md](../adr/README.md).  
Current architecture “as is” — [current-architecture-v1.md](current-architecture-v1.md).

---

## 1. Where to start (fast route)

- **Layers and boundaries**: [`0006`](../adr/0006-presentation-layers-and-feature-slices.md)
- **Data flow and cockpit (CDS)**: [`0036`](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **IDE overlay domain (IDS), separate from CDS**: [`0079`](../adr/0079-ide-display-system-ids-overlay-pipeline.md)
- **MCP contracts and testable infrastructure**: [`0008`](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **Code navigation / navigation MCP**: [`0039`](../adr/0039-workspace-navigation-affordances.md)

If you are reading for “how Flight UI is laid out today”, start with the layout doc, not ADR:
- [../ui-ux/cascade-ide-ui-layout-v1.md](../ui-ux/cascade-ide-ui-layout-v1.md)

---

## 2. UI and attention model (PFD / Forward / MFD)

- **Attention model and PFD/MFD terminology**: [`0021`](../adr/0021-pfd-mfd-cockpit-attention-model.md) *(Proposed, but sets the language)*  
- **Layout invariants and `presentation` authority**: [`0046`](../adr/0046-presentation-layout-authority-and-cockpit-invariants.md) *(Accepted · Implemented)*
- **Multi-window and surfaces**: [`0017`](../adr/0017-multi-window-workspace-and-agent-surfaces.md) *(Accepted · Implemented)*
- **Remote operator surface from another device** (not mobile IDE): [`0117`](../adr/0117-remote-operator-surface-multidevice.md) *(Proposed)*
- **Agent Notes Core 2.0** (TOML, `knowledge_path`, MCP parity): [`0118`](../adr/0118-agent-notes-core-2-toml-and-knowledge-path.md) *(Accepted)*

---

## 3. MVVM, feature slices, and “what lives where”

- **Slices and layers**: [`0006`](../adr/0006-presentation-layers-and-feature-slices.md)
- **Strangler migration and exceptions**: [`0009`](../adr/0009-strangler-migration-and-exceptions.md)

---

## 4. Transport / backpressure / delivery to UI

- **Signals, coupling, backpressure**: [`0007`](../adr/0007-signals-coupling-and-ui-backpressure.md)
- **UI thread marshaling**: [`0004`](../adr/0004-ui-thread-marshaling.md)
- **Delivery bus (AFDX analogy)**: [`0094`](../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md)

---

## 5. DAL / CCU / DataBus (pipeline “raw → DTO → UI”)

- **DAL boundary**: [`0102`](../adr/0102-data-acquisition-layer-boundary-and-contract.md)
- **CCU as fold layer**: [`0097`](../adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
- **CDS (channel → compositor → surface)**: [`0036`](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **Graph-backed instruments — shared layer inside CDS**: [`0115`](../adr/0115-cds-graph-backed-shared-layer.md) *(Accepted)*
- **IDE DataBus**: [`0099`](../adr/0099-ide-databus-typed-events-and-projections.md)
- **Health stratification**: [`0095`](../adr/0095-workspace-solution-ide-health-stratification.md) *(if you need “what IDE/Solution/Workspace health means”)*  

---

## 6. MCP, agent, and contract testability

- **MCP contracts + testable infrastructure**: [`0008`](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **Agent contract CLI + snapshot tests**: [`0052`](../adr/0052-agent-contract-cli-and-snapshot-tests.md)
- **Visibility of reasoning / provider limits**: [`0020`](../adr/0020-agent-reasoning-visibility-and-provider-limits.md)
- **Intercom (channel, not “chatbot”)**: [`0080`](../adr/0080-intercom-naming-and-multi-party-channel-model.md)
- **Topic cards + spine (topic card index)**: [`0072`](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md), [`0096`](../adr/0096-intercom-topic-card-summary-and-product-spine.md)
- **Session tree, steer / follow-up**: [`0116`](../adr/0116-intercom-session-tree-and-agent-message-steering.md) *(Proposed)*
- **Chat persistence (event log)**: [`0045`](../adr/0045-agent-chat-persistence-event-log-and-projections.md)
- **Clarification batches and threads**: [`0031`](../adr/0031-agent-chat-clarification-batches-and-threading.md)

---

## 7. Code navigation and indexing

- **Workspace navigation affordances**: [`0039`](../adr/0039-workspace-navigation-affordances.md)
- **Hybrid codebase index core**: [`0105`](../adr/0105-hybrid-codebase-index-for-csharp-web.md) *(Accepted · Implemented)*
- **Hybrid index ↔ CascadeIDE integration**: [`0106`](../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)
- **HCI and Semantic Map (orientation, three axes)**: [`0113`](../adr/0113-hci-semantic-map-orientation-layer.md)
- **Edge relation type (`relation_kind`)**: [`0114`](../adr/0114-graph-edge-relation-kind-taxonomy.md)
- **Graph kinds (`graph_kind`) and instrument categories**: [`0065`](../adr/0065-instrument-categories-domain-taxonomy.md)
- **Graph-backed surfaces (graph family contract)**: [`0067`](../adr/0067-graph-backed-surfaces-contract.md)
- **Semantic map control flow (PFD)**: [`0053`](../adr/0053-semantic-map-control-flow-pfd.md) *(Accepted · Implemented)*

---

## 8. Commands and keyboard-first

- **Command surface & discoverability**: [`0013`](../adr/0013-command-surface-and-discoverability.md)
- **Command palette direct overlay**: [`0070`](../adr/0070-command-palette-direct-overlay-surface.md)
- **Chord stack (Ctrl+K) / FMS-style**: [`0060`](../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md)

---

## 9. Where to keep “current architecture” description

ADR records decisions and motivation. For “how it works now” use:
- `docs/en/architecture/current-architecture-v1.md` (canonical entry — this tree)
- `docs/en/ui-ux/cascade-ide-ui-layout-v1.md` (layout / region names reference)
- `docs/MCP-PROTOCOL.md` (MCP contract reference)

Version: **v1**.

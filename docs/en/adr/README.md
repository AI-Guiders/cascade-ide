<!-- English translation of adr/README.md. Canonical Russian: ../../adr/README.md -->

# Architecture Decision Records (ADR) - CascadeIDE

This is where the **architectural decisions taken** are recorded: context, choices, consequences and rejected alternatives. **[architecture-policy.md](../../architecture-policy.md)** - a short navigator and table of links here; step-by-step migration and MCP contracts - in [architecture-migration.md](../architecture-migration.md), [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md).

**Policy connection:** Major changes in direction will be a separate commit with a navigator update and a new entry here.

**Statuses and life cycle** (without ADR list): [status-lifecycle.md](status-lifecycle.md).

## Index


**Index status:** `Accepted` / `Proposed` / `Superseded` only. Details (`Implemented`, "direction", strangler) - in the ADR header and [status-lifecycle.md](status-lifecycle.md).

## Thematic clusters

Brief navigation; **full list** - [below](#adr-index-full). Large ADRs have a **## Summary** block at the beginning of the file.

### Debugging and IDE Health

| ID | ADR |
|----|-----|
| 0001–0004 | [hypotheses](0001-debug-hypotheses-json-storage.md) · [agent/human parity](0002-debug-human-agent-parity.md) · [Debug mode](0003-debug-ui-mode-separate-from-power.md) · [UI thread](0004-ui-thread-marshaling.md) |
| 0011 | [situational awareness](0011-debug-situational-awareness.md) |
| 0089, 0090–0091, 0095 | [IDE Health naming](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) · [launch profiles](0090-launch-profiles-and-debug-startup-configurations.md) · [PFD debug deck](0091-pfd-debug-situational-deck-hypothesis.md) · [stratification Health](0095-workspace-solution-ide-health-stratification.md) |

### Configuration and TOML

| ID | ADR |
|----|-----|
| 0010, 0028–0029 | [UI modes](0010-ui-modes-toml-configuration.md) · [settings.toml](0028-user-settings-toml-localappdata-and-secrets.md) · [TOML-first UI](0029-configuration-toml-canonical-ui-facade.md) |
| 0030, 0040, 0050–0051, 0083, 0086 | [commands/hotkeys](0030-command-ids-hotkeys-and-ui-registry-layers.md) · [LSP presets](0040-lsp-launch-line-settings-toml-presets-and-environment.md) · [tools in TOML](0050-declarative-instrument-zone-placement-toml.md) · [intent routing](0051-intent-based-attention-routing-toml.md) · [`[ai]`](0083-ai-mode-and-nested-settings-toml.md) · [topic UI](0086-ui-theme-toml-canonical-json-mcp-wire.md) |

### Cockpit, CDS and Computing Circuit

| ID | ADR |
|----|-----|
| 0021 | [**PFD/MFD - attention model**](0021-pfd-mfd-cockpit-attention-model.md) |
| 0036, 0046–0047, 0066 | [channel→CDS→surface](0036-cds-channel-compositor-surface-pipeline.md) · [layout authority](0046-presentation-layout-authority-and-cockpit-invariants.md) · [instrument descriptor](0047-cockpit-instrument-descriptor-and-slot-composition.md) · [cockpit vs presentation](0066-cockpit-ui-vs-ide-presentation-layer.md) |
| 0063–0065, 0068, 0097 | [instrument deck](0063-instrument-deck-named-composition-one-anchor.md) · [deck primitives](0064-deck-primitives-visual-language-render-layer-and-palette.md) · [taxonomy](0065-instrument-categories-domain-taxonomy.md) · [payload vs projection](0068-deck-row-payload-and-presentation-projection.md) · [**CCU**](0097-cockpit-compute-units-transport-to-channel-dto.md) |
| 0094, 0099 | [ingestion bus](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) · [IDE DataBus](0099-ide-databus-typed-events-and-projections.md) |

### Agent, MCP and indexing

| ID | ADR |
|----|-----|
| 0008, 0016, 0038 | [MCP contracts](0008-mcp-contracts-and-testable-infrastructure.md) · [ACP](0016-agent-client-protocol-external-agent.md) · [agent facade](0038-agent-facade-ai-provider-and-tool-orchestration.md) |
| 0031, 0045, 0048, 0082, 0087 | [chat](0031-agent-chat-clarification-batches-and-threading.md) · [persistence](0045-agent-chat-persistence-event-log-and-projections.md) · [Cursor parity](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) · [loopback MCP](0082-acp-ide-mcp-loopback-single-process.md) · [MAF](0087-microsoft-agent-framework-builtin-agent-orchestration.md) |
| 0105–0106, 0118 | [hybrid index](0105-hybrid-codebase-index-for-csharp-web.md) · [integration in CIDE](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) · [agent-notes 2.0](0118-agent-notes-core-2-toml-and-knowledge-path.md) |

### Graph, semantic map and navigation
| ID | ADR |
|----|-----|
| 0039, 0053, 0062 | [workspace navigation](0039-workspace-navigation-affordances.md) · [control flow PFD](0053-semantic-map-control-flow-pfd.md) · [GitMap](0062-git-submodules-semantic-map-subgraph.md) |
| 0067, 0113–0115 | [graph-backed contract](0067-graph-backed-surfaces-contract.md) · [HCI orientation](0113-hci-semantic-map-orientation-layer.md) · [`relation_kind`](0114-graph-edge-relation-kind-taxonomy.md) · [CDS graph layer](0115-cds-graph-backed-shared-layer.md) |

### Previews and documentation (Markdown)

| ID | ADR |
|----|-----|
| 0023, 0026→0069 | [language/diagrams](0023-markdown-diagrams-language-tooling.md) · [**0026 Superseded →**](0026-markdown-preview-surfaces-and-placement.md) [**0069**](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) |

<a id="adr-index-full"></a>

## Full index

| ID | Title | Status |
|----|----------|--------|
| [0001](0001-debug-hypotheses-json-storage.md) | Storing debugging hypotheses in one JSON file | Accepted |
| [0002](0002-debug-human-agent-parity.md) | Single debug state layer for human and agent | Accepted |
| [0003](0003-debug-ui-mode-separate-from-power.md) | Separate Debug UI mode (not Power cockpit) | Accepted |
| [0004](0004-ui-thread-marshaling.md) | Marshalling UI updates via `IUiScheduler` | Accepted |
| [0005](0005-defer-dynamic-plugins-mef.md) | Set aside dynamic plugins (MEF and similar) | Accepted |
| [0006](0006-presentation-layers-and-feature-slices.md) | Layers, feature slices, role `MainWindowViewModel` | Accepted |
| [0007](0007-signals-coupling-and-ui-backpressure.md) | Signals, connectivity, UI load | Accepted |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP Contracts and Testable Infrastructure | Accepted |
| [0009](0009-strangler-migration-and-exceptions.md) | Strangler migration and exceptions for spike | Accepted |
| [0010](0010-ui-modes-toml-configuration.md) | UI mode data in TOML | Accepted |
| [0011](0011-debug-situational-awareness.md) | Situational awareness in debugging (bar, hover; details in panel) | Accepted |
| [0012](0012-floating-workspace-chrome.md) | Floating and detachable chrome workspace (telemetry, stripes; not docks in v1) | Accepted |
| [0013](0013-command-surface-and-discoverability.md) | Command surface and discoverability (palette, minimal toolbar) | Accepted |
| [0014](0014-situational-checklists.md) | Situational checklists (model, triggers, UI; parent - 0013) | Accepted |
| [0015](0015-editor-toml-syntax-highlighting.md) | TOML highlighting in the editor (tapped TextMate package taplo; not LSP in v1) | Accepted |
| [0016](0016-agent-client-protocol-external-agent.md) | External agent for ACP (stdio, Cursor CLI); SDK vendor; UTF-8; orthogonal to MCP | Accepted |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Multiple application windows, screen areas, agent surfaces; MCP multi-root in scope | Accepted |
| [0018](0018-ide-commands-canonical-xml-documentation.md) | Canonical XML docs for `IdeCommands` and ProtocolDocGen (instead of mini-language only in summary) | Accepted |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Common Git Core for Cascade IDE and git-mcp (logic parity, agent-notes-core use case) | Accepted |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | Visibility of agent reasoning: layers (response, traces, optional log), fair restrictions of LLM providers | Proposed |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model | Accepted |
| [0022](0022-mfd-visual-design-surface-axaml-blazor.md) | Visual Surface UI Development (AXAML/Blazor), WinForms Benchmark, MFD Hosting | Proposed |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown + diagrams (Mermaid/PlantUML) - first-class experience through LSP and workflow | Proposed |
| [0024](0024-ide-sdk-and-stable-contracts.md) | SDK for CascadeIDE - stable contracts for internal extension and future plugins | Proposed |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | SDK: linking capabilities to attention zones (PFD/MFD/Forward/EICAS/HUD) | Proposed |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | Markdown preview: surfaces and placement (`workspace.toml`); internal links (peek by “point N”/anchors); not to be confused with language ADR 0023 | Superseded |
| [0027](0027-small-team-focus-vs-public-maturity.md) | Narrow team (person + assistant) vs maturity “for discovery”: two axes (boundaries vs queue) | Accepted |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | User settings: `settings.toml`, `%LocalAppData%\CascadeIDE\`, secrets in `ai-keys.toml` | Accepted |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | Configuration: TOML-first; holistic UI deferred settings; dot UI - canon façade | Accepted |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Layers `command_id`, hotkeys and UI: IdeCommands, palette, TOML, VM bridge; drawing of a unified catalog | Accepted |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Agent chat: clarification packages, more complex yes/no answers, threads; not to be confused with PFD confirmations | Proposed |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD above the editor: custom content, grammar like `presentation`; EBNF to ADR, parser - according to DSL complexity | Proposed |
| [0033](0033-internationalization-resx-avalonia.md) | i18n: ResX/.NET culture, Avalonia; UI strings are not in TOML as the main layer; pluralization - keys or library | Proposed |
| [0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) | Incapacitation, Emergency Mode; EICAS + PIC signals; liveness, contextual HUD, safety interlock; webcam/analysis MCP opt-in | Proposed |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Built-in browser in MFD (WebView2), external web LLM; trust boundary with MCP; hybrid via operator; bridge web↔MCP - beyond baseline | Proposed |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → surface composer → surface; CDS as routing in the attention model (Agent-first) | Accepted |
| [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) | PFD: surface invariants; Roslyn; canon `[PfdStrict]` / `PfdStrictControl` | Proposed |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | Agent facade: `AiProviderManager`, chat vs ACP vs standalone JSON loop; external MCPs; ideas for orchestration and tool-calling | Accepted |
| [0039](0039-workspace-navigation-affordances.md) | Workspace navigation; C# / .NET north-star (not polyglot v1); graph/semantic map; PFD/MFD; MCP: presets, `kind_filter`, subgraph | Accepted |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP C#/Markdown: command line in `settings.toml`, presets, optional keys; env - open in ADR | Accepted |
| [0041](0041-protobuf-for-agent-and-ide-messages.md) | Protobuf vs JSON for agent/IDE messages: boundaries, criteria, hybrid; entry point (Proposed) | Proposed |
| [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | Pre-flight briefing: Planned Changes (intent + SA) and Review Before Apply (preview, semantic layer, rejection without garbage); state machine; orthogonal to “line-by-line trust” | Proposed |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport recovery parity (human ↔ agent), host boundary (Cursor) vs IDE; orthogonal ADR 0002 | Proposed |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Avalonia as a host (fuselage), custom chat rendering (Skia - hypothesis); **model is primary**, spike follows; see ADR 0031 | Accepted |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Persistence of chat history: append-only NDJSON events + metadata and projections; UI/render is not the source of truth | Accepted |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | CDS: `CockpitPresentationLayoutPolicy` and P/F/M invariants; `presentation` as a source of truth for menus/MCP/modes/reactive layer | Accepted |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Cockpit Instrument (`Instrument`): slot descriptor (`CockpitInstrumentDescriptor`), not `Control`; SE vs intent map as examples | Accepted |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Chat via Cursor ACP in the IDE: `mcpServers`, auto IDE MCP; applications - body spaces, parsing `mcp.json` ↔ CIDE | Accepted |
| [0049](0049-skia-surface-rollout-over-avalonia-host.md) | Step-by-step rollout of Skia-surfaces via CIDE with Avalonia-host; wave migration and dual-path | Accepted |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | Declarative map "tool → zone/slot" in TOML | Accepted |
| [0051](0051-intent-based-attention-routing-toml.md) | Intent-based attention routing (TOML) | Accepted |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | CLI for agent contract (parity with MCP) and snapshot tests | Accepted |
| [0053](0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD (control flow, KISS, subgraph) | Accepted |
| [0054](0054-benchmarking-methodology-and-baselines.md) | Performance benchmarks and baseline metrics for CIDE | Proposed |
| [0055](0055-skia-instrument-composition-pipeline.md) | General composition pipeline of Skia tools (Intent -> Declutter -> Layout -> Render) | Accepted |
| [0056](0056-semantic-map-pipeline-adoption.md) | Intent map: implementing a common Skia pipeline (composer, controlFlow layout, cursor semantics) | Accepted |
| [0057](0057-chat-surface-pipeline-adoption.md) | Chat surface: adoption of common Skia pipeline (threads, confirmations, dual-path rollout) | Accepted |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Pairing the agent and Roslyn MCP in `settings.toml` (limits, types of nodes, timeouts, presets) | Proposed |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Roslyn Profiles MCP, Manager, Tactics (PFD) / EFB on MFD, Auto-Focus / Combat / Echelon | Proposed |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Chord layer (Ctrl+K), FMS-style, S/T, overlay; extension ADR 0013 | Accepted |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | ADR context map ↔ paths in `workspace.toml`, indicator on PFD, intent/tooltip, agent advisory (GPWS for docks) | Proposed |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | **GitMap:** submodules and git boundaries **separate** from WSNC/intent map; general Skia pipeline; own contract/MCP; [git-and-submodules-v1](../../git-and-submodules-v1.md) | Proposed |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **Instrument deck** + **`ContentRepresentation`**; taxonomy of primitives (including Readout, Trend, Gauge, Presence); **Presence/Activity vs Dark Cockpit**; `DedicatedPage` - Page mode for WH, not deck | Accepted |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | **Types of indicators** deck: single graphical implementation; **rendering library** + **semantic palette**; without unnecessary architectural layer; `DeckPrimitiveKind` = view directory | Accepted |
| [0065](0065-instrument-categories-domain-taxonomy.md) | **Tool categories** and **Graph types** (`graph_kind`): domain orthogonal to slot/`instrument_id`; "Intent Map" = **Code Intent Map**; see table `graph_kind` | Accepted |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | **Cockpit UI** vs **presentation IDE** (chrome, overlays, theme): two pillars; do not mix fixtures/deck with shell; `PrimitivesKit` vs `UiChrome` | Accepted |
| [0067](0067-graph-backed-surfaces-contract.md) | **Graph-backed surfaces:** general contract for a family of graph screens (data, interaction, navigation, layout, selection, sync); intent map, GitMap, future graphs | Accepted |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | **Channel Row Payload** vs **View Projection** vs **Slot**: table/strip ≠ cell type; v1 - one DTO; heterogeneity - discriminator and patterns | Accepted |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | **Markdown Preview:** MFD tool as primary surface, native Markdig renderer first, WebView as optional adapter; authoring extension orthogonal to preview | Accepted |
| [0070](0070-command-palette-direct-overlay-surface.md) | **Command Palette:** direct overlay surface to host, routing to active `TopLevel`; `ModalOverlay` is not canon for palette baseline | Accepted |
| [0071](0071-ai-assistance-sovereignty-locality-invisibility.md) | **AI / IDE assistant:** sovereignty, locality, invisibility; anti-pattern “cloud inline by default without control”; narrative - [cascadeide-philosophy-v1](../../design/cascadeide-philosophy-v1.md) | Proposed |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | **Chat:** topic cards, drill-in/back, adaptive default; intent-based Melody/Chords v1 for topic navigation; clarifies [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) in chat-domain | Accepted |
| [0073](0073-pfd-instrument-deck.md) | **PFD instrument deck:** catalog of composition options (SA, code metrics, intent map, ADR indicator...); criteria "PFD vs on demand"; live draft before preset selection | Proposed |
| [0074](0074-settings-ui-mfd-compact-layout-overflow.md) | **Settings:** more compact, anchor on **MFD**; lack of space in **P+F+M** - strategy table (scroll, min width, fallback window, ...) | Proposed |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | **UI:** topic index [`UI/README.md`](UI/README.md); MFD page conventions (payload vs projection, keyboard-first); does not replace flat index | Proposed |
| [0076](0076-ui-ux-principles-hub.md) | **UI/UX:** center of principles - coherent text from [`snippets/ui/`](snippets/ui/README.md) (attention/cockpit, product philosophy); does not replace the original ADR | Proposed |
| [0077](0077-tech-principles-hub.md) | **TECH:** principles center - coherent text from [`snippets/tech/`](snippets/tech/README.md) (boundaries/contracts, agent/debugging/observability); does not replace the original ADR | Proposed |
| [0078](0078-git-preflight-and-noise-control-for-cide.md) | **Git preflight:** noise control (EOL/BOM/whitespace), safe fixes, logical commit hints and post-push report | Accepted |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | **IDS (Ide Display System):** IDE overlay pipeline (intent → composer → snapshot → surface), orthogonal to CDS; single input host and slots - according to plan; Roslyn **CASCOPE013–016** | Accepted |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | **Intercom:** product name of the communication channel instead of the narrow “chat”; agent + command + system replicas; **external** command circuit vs one’s own “mountain”; discoverability/i18n; strangler for code | Accepted |
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | **Parametric Intent Melody:** suffix `:startLine:endLine` for operations on editor text; validation; range refactorings; UX (indicator, chord vs palette) | Accepted |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | **ACP + IDE MCP:** one copy of the process - loopback HTTP/SSE instead of the second `CascadeIDE --mcp-stdio`; localhost security; stdio for external host save | Proposed |
| [0083](0083-ai-mode-and-nested-settings-toml.md) | **`[ai]` in settings.toml:** `mode` = local \| acp\| mcp_only \| cloud; nested sections; no backwards compatibility with old `provider` | Accepted |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | **Agent's edits:** the only text in the editor; chat - intent/status; presence layer (cursor, “writes”); diff in chat is not the main one; without mandatory CRDT | Proposed |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | **Editor HUD:** inline layer in the editor (carriage, text, gutter) vs **HUD banner** (bar above the text); IDS overlays separately; banner config - [0032](0032-hud-banner-configuration-and-grammar.md) | Proposed |
| [0086](0086-ui-theme-toml-canonical-json-mcp-wire.md) | **UI theme:** brush canon in `settings.toml`; JSON for `ide_get/set_ui_theme` as transport; strangler `Themes/*.json` | Proposed |
| [0087](0087-microsoft-agent-framework-builtin-agent-orchestration.md) | **Microsoft Agent Framework (MAF):** a guide to orchestration of the built-in agent loop; track. step - PoC | Accepted |
| [0088](0088-host-slot-region-deck-cell-taxonomy.md) | **Host slot / region / deck cell** - taxonomy of levels; do not mix | Proposed |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | **`get_ide_state`** instead of `get_workspace_state`; **IDE Health** channel instead of Workspace Health; orthogonal ADR 0002 | Accepted |
| [0090](0090-launch-profiles-and-debug-startup-configurations.md) | Launch profiles / multiple debugging startup configurations (like launch profiles in VS), storage, MCP, migration from `startup-project.json` | Accepted |
| [0091](0091-pfd-debug-situational-deck-hypothesis.md) | Hypothesis: PFD **instrument deck** in debug mode - one Mfd page (DebugStack) may not be enough; PFD = Summary, Mfd = Details | Proposed |
| [0092](0092-visual-ui-designer-major-track.md) | Track **Visual UI** (markup designer): separate large program line; UX standard - [0022](0022-mfd-visual-design-surface-axaml-blazor.md); priority Avalonia → Blazor → (optional) Razor | Accepted |
| [0093](0093-mfd-embedded-browser-for-launch-url.md) | Extension [0090](0090-launch-profiles-and-debug-startup-configurations.md): **optional** built-in Kestrel URL view on MFD next to debug; the external browser remains default; WebView2 / cross-platform - in roadmap | Accepted |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | **Delivery bus in UI** (analogous to **AFDX**): `Channel<T>`, batching, backpressure; orthogonal to the CDS “channel”; strangler from build log | Proposed |
| [0095](0095-workspace-solution-ide-health-stratification.md) | Three levels of Health: **Workspace** (folders, Git) · **Solution** (build, tests) · **IDE** (LSP, MCP, environment); taxonomy for channels/CDS/MCP; strangler from current IDE Health | Accepted |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | **Intercom:** topic card = title + **summary** (card index); **spine** of the product line is orthogonal to the main thread (CIDE is an example); complements [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Accepted |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | **Cockpit Computing Units (CCU)**, analogue of LRU *Unit*: raw material convolution → DTO/channel snapshot; orthogonal to [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) (transport) and CDS; IDE Health in code - chain standard | Accepted |
| [0098](0098-semantic-first-document-as-projection.md) | **Semantic-first:** semantic map is primary; code/docs/git - projections; coordination with [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) on the edit session; strangler | Proposed |
| [0099](0099-ide-databus-typed-events-and-projections.md) | **IDE DataBus:** typed events in the IDE process; decoupling of sources and projections, without replacing 0094 (transport) and 0097 (CCU) | Accepted |
| [0100](0100-project-constitution.md) | **Constitution of the project:** long-term principles, architectural invariants, governance and order of changes of the foundation of the project | Proposed |
| [0101](0101-licensing-and-commercialization-strategy.md) | Licensing and commercialization: license matrix, dependency rules, copyleft guardrails and implementation plan | Proposed |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | **Data Acquisition Layer (DAL):** explicit external data acquisition boundary and contract DAL ↔ CCU ↔ UI | Proposed |
| [0103](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) | **Editor HUD Substrate:** `SemanticProjectionPipeline` / `EditorHudEngine` / `IEditorSurfaceAdapter`; DAL/CCU/DataBus; separate hi-freq bounded circuit; baselineAvaloniaEdit; comparison of hosts in `design/`, roadmap UI in `ui-ux/` | Accepted |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid codebase index: portable core + MCP for C#/Razor/AXAML (Roslyn true for C#); hybrid FTS+vec | Accepted |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Embedding hybrid index in CascadeIDE (DAL/CCU/DataBus/freshness) and Semantic Map border | Proposed |
| [0107](0107-blank-solution-creation-via-dotnet-new-sln.md) | Blank solution: `dotnet new sln`, menu/MCP, `BlankSolutionCreator` + `IDotnetCommandRunner` | Accepted |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Web portal for external web AI: WebView, Host Object → `IdeCommands`/MCP; allowlist, consent; PoC (Atlas/Search AI) | Accepted |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Intent Melody: target directory `[[melody_root]]` (`shape` + `tail_signature`); migration with `[aliases]`+`[[parametric]]`; args in code; plugins - processing by plugin | Accepted |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn refactorings by range: Intent Melody/IDE ↔ Roslyn MCP bridge; no duplicate Features in the core; `rmx`/`rix` is not in the bundle before the bridge | Proposed |
| [0111](0111-editor-linenumber-linerange-value-objects.md) | Editor: `LineNumber` / `LineRange` (1-based, Start ≤ End); `ParsedLineRange`; border to JSON - `int` in command args | Accepted |
| [0112](0112-command-palette-query-modes-strategy.md) | Palette (Ctrl+Q): line modes, strategies and workspace search backend contract (`t:`/`m:`/`x:`) with switching in settings | Accepted |
| [0113](0113-hci-semantic-map-orientation-layer.md) | HCI × Semantic Map: orientation; axes **`graph_kind`** / **provenance** / **`relation_kind`**; quick text referenced-by → Roslyn; `SemanticMapInputSnapshot` /CCU | Proposed |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Relation type on edge (`relation_kind`): "inherits", "refers to", partial peer, text match; orthogonal to `graph_kind` and provenance; link to `hit_kind` | Proposed |
| [0115](0115-cds-graph-backed-shared-layer.md) | CDS: general implementation layer of graph-backed **instruments** in the cockpit (not IDS); `IGraphDataSource` (v0); interface with [0036](0036-cds-channel-compositor-surface-pipeline.md) and [0067](0067-graph-backed-surfaces-contract.md) | Accepted |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Intercom: session tree (branching, rewind, bookmark) and **steer** / **follow-up** when the agent is running; [0045](0045-agent-chat-persistence-event-log-and-projections.md), [0080](0080-intercom-naming-and-multi-party-channel-model.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Proposed |
| [0117 (SkiaKit)](0117-ide-skia-kit.md) | **SkiaKit:** reusable Skia IDE primitives (sectioned cards, tile grid) | Accepted |
| [0117](0117-remote-operator-surface-multidevice.md) | Remote operator surface: **PWA**-remote from a phone/other PC, Operator Gateway; not mobile IDE; [0017](0017-multi-window-workspace-and-agent-surfaces.md), [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Proposed |
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | Agent Notes Core **2.0**: TOML in-proc, `config_path` SSOT with MCP, `knowledge_path` in `IdeCommands` | Accepted |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Chat as command line: Intercom (`/card`) + IDE (`/build run`, `/test run`, `/debug launch`); autocomplete, `command_id` | Proposed |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | `primary_work_surface`: Intercom or Editor in Forward anchor (analogous to Agent/Editor in Cursor) | Proposed |

## Assembly into one document (HTML, TXT, PDF)

Gluing together numbered ADRs and the same directives **INCLUDE** / **INCLUDE_MANIFEST** / **INCLUDE_GLOB** as in `resume/` (without DOCX). Run from this directory: `dotnet script build-adr.csx`. Dependencies and output paths are in [build/README.md](build/README.md).

## Agreements

- **Related ADRs:** not a continuous paragraph in the header - section [## Related ADRs](snippets/adr-related-links-convention.md) with the table `| ADR | Role |` (template and rules in snippets).
- **File name:** `NNNN-short-kebab-title.md`, four digits with leading zeros.
- **Statuses:** in the ADR header and in the “Status” column - see [status-lifecycle.md](status-lifecycle.md). Briefly: first tag (**Proposed** / **Accepted** / **Superseded** / **Deprecated**); for a solution **implemented in the code** - **`Accepted · Implemented`** (second tag via **` · `**). No subfolders by status - one `docs/adr/`.
- **Thematic subfolders** (not by status): optional index by topic - [0075](0075-ui-topic-index-and-mfd-page-conventions.md) and [`UI/README.md`](UI/README.md); **TECH** - [`TECH/README.md`](TECH/README.md) and [0077](0077-tech-principles-hub.md).
- The new ADR adds a row to the table above and, if necessary, a row to the table in [architecture-policy.md](../../architecture-policy.md).

<a id="adr-anchors-policy"></a>

### Internal anchors and references (so that “see paragraph N” work as links)

The canon of the text is [snippets/adr-anchors-policy.md](snippets/adr-anchors-policy.md) (in the `build-adr.csx` assembly you can connect via `{{ INCLUDE: snippets/adr-anchors-policy.md }}` from `adr-book.md`).
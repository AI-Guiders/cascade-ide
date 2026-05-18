---
hide:
  - toc
---

# Accepted

Accepted as norm; implementation not complete or intentionally phased.

[← ADR navigator](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0001](../../adr/0001-debug-hypotheses-json-storage.md) | Storing debugging hypotheses in a single JSON file | Accepted |
| [0002](../../adr/0002-debug-human-agent-parity.md) | Debug Human Agent Parity | Accepted |
| [0003](../../adr/0003-debug-ui-mode-separate-from-power.md) | Separate Debug UI Mode (Not the Power Cockpit) | Accepted (product direction); **implementation** per release plan |
| [0004](../../adr/0004-ui-thread-marshaling.md) | Marshalling UI updates via IUiScheduler | Accepted (implementation plan - strangler) |
| [0005](../../adr/0005-defer-dynamic-plugins-mef.md) | Non-target step - dynamic plugins (MEF and analogues) | Accepted |
| [0006](../../adr/0006-presentation-layers-and-feature-slices.md) | Layers, vertical slices and the role of MainWindowViewModel | Accepted |
| [0007](../../adr/0007-signals-coupling-and-ui-backpressure.md) | Signals Coupling And Ui Backpressure | Accepted (implementation - strangler) |
| [0009](../../adr/0009-strangler-migration-and-exceptions.md) | Strangler Migration And Exceptions | Accepted |
| [0011](../../adr/0011-debug-situational-awareness.md) | Situational Awareness in Debugging (Priority Over a “Full” Bottom Panel) | Accepted (direction; concrete screens and hotkeys per implementation iteration) |
| [0012](../../adr/0012-floating-workspace-chrome.md) | Floating and Detachable Workspace Chrome (Bottom Zone and Situational Awareness) | Accepted (direction; v1 scope and concrete controls per iterations) |
| [0013](../../adr/0013-command-surface-and-discoverability.md) | Command Surface and Discoverability (Palette, Minimal Toolbar) | Accepted (direction; command set and UI iterations separate) |
| [0014](../../adr/0014-situational-checklists.md) | Situational Checklists (Model, Triggers, UI) | Accepted |
| [0018](../../adr/0018-ide-commands-canonical-xml-documentation.md) | Canonical XML docs for `IdeCommands` and ProtocolDocGen | Accepted (partially: ProtocolDocGen + generated summaries; full XML on IdeCommands - on migration) |
| [0021](../../adr/0021-pfd-mfd-cockpit-attention-model.md) | PFD / MFD — Cascade IDE cockpit attention model | Accepted |
| [0022](../../adr/0022-workspace-health-lexicon.md) | Lexicon and canon of names - IDE Health (evolution of names; ADR file saved as 0022) | Accepted |
| [0027](../../adr/0027-small-team-focus-vs-public-maturity.md) | Small Team (Human + Assistant) vs “Open” Maturity — Two Axes, Not One Queue | Accepted |
| [0048](../../adr/0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Chat via Cursor ACP in IDE — Cursor host parity goal, tool surface, and MCP | Accepted (partial: Cursor ACP in IDE, auto-inject MCP; full parity — per ADR) |
| [0049](../../adr/0049-skia-surface-rollout-over-avalonia-host.md) | Phased rollout of Skia-surfaces with Avalonia-host (CIDE-wide) | Accepted (partially: chat surface, SkiaKit; other surfaces - by waves) |
| [0055](../../adr/0055-skia-instrument-composition-pipeline.md) | Skia instrument composition pipeline (Intent → Declutter → Layout → Render) | Accepted |
| [0060](../../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) | Chord layer (FMS-style), S/T, and overlay — keyboard-first extension (ADR 0013) | Accepted (partial: Intent Melody catalog + chord/palette v1; full FMS T/S overlay — per ADR) |
| [0063](../../adr/0063-instrument-deck-named-composition-one-anchor.md) | Instrument Deck — Named Composition of Instruments in a Single Field of Attention | Accepted |
| [0064](../../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) | Deck indicator kinds — visual language, render layer, and semantic palette | Accepted |
| [0065](../../adr/0065-instrument-categories-domain-taxonomy.md) | Instrument categories and graph types (orthogonal to slot and `instrument_id`) | Accepted |
| [0066](../../adr/0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI and presentation IDE layer - separate supports | Accepted |
| [0067](../../adr/0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces - a general contract for a family of graph screens | Accepted |
| [0068](../../adr/0068-deck-row-payload-and-presentation-projection.md) | Deck Row Payload And Presentation Projection | Accepted |
| [0076](../../adr/0076-ui-ux-principles-hub.md) | UI/UX - the center of principles (coherent text from the canon) | Accepted |
| [0079](../../adr/0079-ide-display-system-ids-overlay-pipeline.md) | IDS (Ide Display System) - IDE overlay pipeline, orthogonal to CDS | Accepted |
| [0080](../../adr/0080-intercom-naming-and-multi-party-channel-model.md) | Intercom — channel name and model (not only “chat with the agent”) | Accepted (strangler: Intercom in UI and docs v1; multi-party and external contour — on roadmap) |
| [0087](../../adr/0087-microsoft-agent-framework-builtin-agent-orchestration.md) | Microsoft Agent Framework (MAF) - a guide to the **embedded** agent framework orchestration layer | Accepted · **Next step: PoC** |
| [0089](../../adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | Agent omnibus naming (`get_ide_state`) and **IDE Health** channel (instead of Workspace Health) | Accepted |
| [0092](../../adr/0092-visual-ui-designer-major-track.md) | **Visual UI** track (layout designer) — separate major program line in CIDE | Accepted (direction) |
| [0094](../../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Event delivery bus in UI (similar to AFDX) and `System.Threading.Channel<T>` | Accepted |
| [0095](../../adr/0095-workspace-solution-ide-health-stratification.md) | Three Health levels — Workspace, Solution, IDE (channel taxonomy) | Accepted (partial: WorkspaceHealth MFD + channels; full three-level taxonomy — per ADR) |
| [0100](../../adr/0100-project-constitution.md) | Project constitution | Accepted |
| [0101](../../adr/0101-licensing-and-commercialization-strategy.md) | Licensing and commercialization strategy | Accepted |
| [0102](../../adr/0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer - boundary of external interfaces and adapters | Accepted |
| [0103](../../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) | Editor Hud Substrate Semantic Projection And Surface Adapter | Accepted (strangler) |
| [0115](../../adr/0115-cds-graph-backed-shared-layer.md) | CDS - common layer of graph-backed devices (implementation in the cockpit, not IDS) | Accepted |
| [0121](../../adr/0121-intent-oriented-programming-paradigm.md) | Intent-Oriented Programming (IOP) — conceptual foundation of Cascade IDE | Accepted |


---

_Generated by `tools/gen_adr_pages.py`. Do not edit by hand._

---
hide:
  - toc
---

# Proposed

Draft for discussion — not yet accepted.

[← ADR navigator](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0020](../../adr/0020-agent-reasoning-visibility-and-provider-limits.md) | Agent Reasoning Visibility and LLM Provider Limits | Proposed |
| [0022](../../adr/0022-mfd-visual-design-surface-axaml-blazor.md) | Mfd Visual Design Surface Axaml Blazor | Proposed |
| [0023](../../adr/0023-markdown-diagrams-language-tooling.md) | Markdown + diagrams (Mermaid/PlantUML) - first-class experience through LSP and workflow | Proposed |
| [0024](../../adr/0024-ide-sdk-and-stable-contracts.md) | SDK for CascadeIDE - stable contracts for internal extension and future plugins | Proposed |
| [0025](../../adr/0025-sdk-attention-zones-and-capabilities.md) | Sdk Attention Zones And Capabilities | Proposed |
| [0031](../../adr/0031-agent-chat-clarification-batches-and-threading.md) | Agent chat — clarification batches, answers beyond yes/no, threads (direction) | Proposed (direction draft until chat UI rework; protocol and screen details — iteration by iteration) |
| [0032](../../adr/0032-hud-banner-configuration-and-grammar.md) | HUD above editor - custom content and grammar (like `presentation`) | Proposed (intention fixed; implementation according to plan). |
| [0033](../../adr/0033-internationalization-resx-avalonia.md) | Internationalization (i18n) — .NET resources, Avalonia, orthogonal to TOML | Proposed (direction fixed; language scope and string migration — per plan). |
| [0034](../../adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) | Operator incapacitation, Emergency Mode, and optional presence sensing | Proposed (intent and boundaries fixed; implementation — separate roadmap). |
| [0035](../../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Embedded browser in MFD, external web LLMs, and MCP boundary | Proposed (intent and trust invariants; WebView and UX details — per roadmap). |
| [0037](../../adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md) | PFD — surface invariants and Roslyn enforcement (weight, input lock, channels) | Proposed |
| [0041](../../adr/0041-protobuf-for-agent-and-ide-messages.md) | Protocol Buffers — consideration for agent and IDE messages (entry point) | Proposed (discussion direction and criteria fixed; **not** a decision to migrate from JSON immediately) |
| [0042](../../adr/0042-pre-flight-planned-changes-and-review-before-apply.md) | Pre-flight briefing — Planned Changes and Review Before Apply | Proposed |
| [0043](../../adr/0043-mcp-transport-recovery-human-agent-parity.md) | MCP transport recovery parity (human ↔ agent) and host boundaries | Proposed |
| [0054](../../adr/0054-benchmarking-methodology-and-baselines.md) | Performance benchmarks and baseline metrics | Proposed |
| [0058](../../adr/0058-agent-roslyn-mcp-coupling-settings-toml.md) | Pairing the agent and Roslyn MCP in `settings.toml` (limits, node types, timeouts, presets) | Proposed |
| [0059](../../adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Roslyn MCP, Manager, Tactics/Strategy and EFB (MFD) Profiles | Proposed |
| [0061](../../adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md) | Context-aware ADR map for code paths (GPWS for documentation) and PFD indicator | Proposed (implementation deferred) |
| [0062](../../adr/0062-git-submodules-semantic-map-subgraph.md) | GitMap - a map of git boundaries (submodules) separate from the workspace navigation context | Proposed - draft for discussion; implementation is not fixed. |
| [0071](../../adr/0071-ai-assistance-sovereignty-locality-invisibility.md) | Principles of AI/assistant integration in IDE - sovereignty, locality, invisibility | Proposed |
| [0073](../../adr/0073-pfd-instrument-deck.md) | PFD instrument deck — catalog of composition variants and surfaces (SA) | Proposed |
| [0074](../../adr/0074-settings-ui-mfd-compact-layout-overflow.md) | Settings UI - more compact, anchored on MFD; lack of space in the P+F+M layout | Proposed |
| [0075](../../adr/0075-ui-topic-index-and-mfd-page-conventions.md) | UI Subject Index (`docs/adr/UI/`) and MFD Page Conventions | Proposed |
| [0076](../../adr/0076-ui-ux-principles-hub.md) | UI/UX - the center of principles (coherent text from the canon) | Proposed |
| [0077](../../adr/0077-tech-principles-hub.md) | TECH - the center of principles (connected text from the canon) | Proposed |
| [0082](../../adr/0082-acp-ide-mcp-loopback-single-process.md) | ACP and MCP IDE - one copy of the process (loopback HTTP/SSE instead of the second `CascadeIDE --mcp-stdio`) | Proposed |
| [0084](../../adr/0084-agent-edits-editor-source-of-truth-presence-channel.md) | Agent edits — editor as sole text source of truth; chat for intent/status; presence layer (GDocs-like, no mandatory CRDT) | Proposed |
| [0085](../../adr/0085-editor-hud-inline-layer-and-hud-banner.md) | Editor HUD - inline layer in the editor and difference from the HUD banner | Proposed |
| [0086](../../adr/0086-ui-theme-toml-canonical-json-mcp-wire.md) | UI theme - canon in TOML, JSON as MCP transport (strangler from `Themes/*.json`) | Proposed |
| [0088](../../adr/0088-host-slot-region-deck-cell-taxonomy.md) | Host slot, attention region and deck cell - taxonomy (do not mix) | Proposed |
| [0091](../../adr/0091-pfd-debug-situational-deck-hypothesis.md) | Hypothesis - PFD instrument deck in debug mode (MFD DebugStack does not exhaust) | Proposed |
| [0098](../../adr/0098-semantic-first-document-as-projection.md) | Semantics first; document and repository as projections (Semantic-First) | Proposed |
| [0104](../../adr/0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md) | Cognitive Decomposition Loop For Maf Prompt Orchestration | Proposed |
| [0110](../../adr/0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn Refactor Intent Melody Bridge | Proposed |
| [0113](../../adr/0113-hci-semantic-map-orientation-layer.md) | HCI and Semantic Map - orientation layer (not graph) | Proposed |
| [0114](../../adr/0114-graph-edge-relation-kind-taxonomy.md) | Relation type on graph edges (`relation_kind`) - connection semantics | Proposed |
| [0116](../../adr/0116-intercom-session-tree-and-agent-message-steering.md) | Intercom - session tree (branching) and steer / follow-up during agent operation | Proposed |
| [0117](../../adr/0117-remote-operator-surface-multidevice.md) | Remote operator surface - multi-device operator (remote control, not mobile IDE) | Proposed |
| [0119](../../adr/0119-chat-slash-commands-intercom-surface.md) | Slash commands in chat — unified command line (Intercom + IDE) | Proposed |
| [0120](../../adr/0120-primary-work-surface-intercom-or-editor.md) | Primary work surface — Intercom or Editor (Agent / Editor analogue) | Proposed |
| [0121](../../adr/0121-intent-oriented-programming-paradigm.md) | Intent-Oriented Programming (IOP) — conceptual foundation of Cascade IDE | Proposed |
| [0122](../../adr/0122-collaborative-iop-environment-and-shared-situational-display.md) | Collaborative IOP Environment — Workstations and Shared Situational Display | Proposed |


---

_Generated by `tools/gen_adr_pages.py`. Do not edit by hand._

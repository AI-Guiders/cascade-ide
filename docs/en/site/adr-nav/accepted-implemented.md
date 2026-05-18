---
hide:
  - toc
---

# Accepted · Implemented

Accepted and main delivery is in the codebase.

[← ADR navigator](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0008](../../adr/0008-mcp-contracts-and-testable-infrastructure.md) | Mcp Contracts And Testable Infrastructure | Accepted · Implemented |
| [0010](../../adr/0010-ui-modes-toml-configuration.md) | UI Mode Data (Focus / Balanced / …) in TOML | Accepted · Implemented |
| [0015](../../adr/0015-editor-toml-syntax-highlighting.md) | TOML highlighting in a text editor | Accepted · Implemented |
| [0016](../../adr/0016-agent-client-protocol-external-agent.md) | External agent using Agent Client Protocol (stdio, Cursor CLI) | Accepted · Implemented |
| [0017](../../adr/0017-multi-window-workspace-and-agent-surfaces.md) | Multiple Application Windows (Multi-Window), Screen Zones, and Agent Surfaces | Accepted · Implemented |
| [0019](../../adr/0019-shared-git-core-ide-and-git-mcp.md) | Common Git Core for Cascade IDE and git-mcp | Accepted · Implemented |
| [0023](../../adr/0023-environment-readiness-glance.md) | Glance channel - separate from IDE Health | Accepted (decision boundaries and signal selection; specific UI and types in the code - as implemented) |
| [0028](../../adr/0028-user-settings-toml-localappdata-and-secrets.md) | User settings - `settings.toml`, directory `%LocalAppData%\CascadeIDE\`, secrets separately | Accepted · Implemented |
| [0029](../../adr/0029-configuration-toml-canonical-ui-facade.md) | Configuration — **TOML-First** (On-Disk Canon); **Holistic** Settings UI — **Deferred**; Point UI — **Canon Facade**, Not a Second Truth | Accepted · Implemented (TOML-first on disk; holistic settings UI — deferred; point UI — facade) |
| [0030](../../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) | Layers of team identifiers, hotkeys and UI (without one “all-in-one” table for now) | Accepted · Implemented (registry of v1 commands in the code) |
| [0036](../../adr/0036-cds-channel-compositor-surface-pipeline.md) | Cds Channel Compositor Surface Pipeline | Accepted · Implemented |
| [0038](../../adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) | Agent Facade Ai Provider And Tool Orchestration | Accepted · Implemented (current code); section “Direction” - draft ideas, not obligations |
| [0039](../../adr/0039-workspace-navigation-affordances.md) | Workspace navigation — multiple views and “current file + related” | Accepted · Implemented |
| [0040](../../adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP (C# / Markdown) - command line in `settings.toml`: presets, optional keys, override via environment | Accepted · Implemented (as [§Solution](#solution) below) |
| [0044](../../adr/0044-avalonia-host-skia-agent-chat-surface.md) | Role split — Avalonia as host (“fuselage”), custom rendering for agent chat (Skia as hypothesis) | Accepted · Implemented |
| [0045](../../adr/0045-agent-chat-persistence-event-log-and-projections.md) | Persistence of chat history - append-only events + projections | Accepted · Implemented |
| [0046](../../adr/0046-presentation-layout-authority-and-cockpit-invariants.md) | Cockpit CDS - policy layouts (`CockpitPresentationLayoutPolicy`) and P/F/M invariants | Accepted · Implemented |
| [0047](../../adr/0047-cockpit-instrument-descriptor-and-slot-composition.md) | `Instrument` - slot composition handle, not `Control` | Accepted · Implemented |
| [0050](../../adr/0050-declarative-instrument-zone-placement-toml.md) | Declarative Instrument Zone Placement Toml | Accepted · Implemented |
| [0051](../../adr/0051-intent-based-attention-routing-toml.md) | Intent Based Attention Routing Toml | Accepted · Implemented |
| [0052](../../adr/0052-agent-contract-cli-and-snapshot-tests.md) | CLI for agent contract (parity with MCP) and snapshot tests | Accepted · Implemented |
| [0053](../../adr/0053-semantic-map-control-flow-pfd.md) | Intent map and control flow on PFD (control flow) | Accepted · Implemented |
| [0056](../../adr/0056-semantic-map-pipeline-adoption.md) | Semantic Map adoption of Skia composition pipeline | Accepted · Implemented |
| [0057](../../adr/0057-chat-surface-pipeline-adoption.md) | Chat surface adoption of Skia composition pipeline | Accepted · Implemented |
| [0069](../../adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | Markdown Preview - MFD tool, renderer-first decoupling and refusal of inline preview in the document | Accepted · Implemented |
| [0070](../../adr/0070-command-palette-direct-overlay-surface.md) | Command Palette as direct overlay surface, routed to active TopLevel | Accepted · Implemented |
| [0072](../../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Chat topic cards, drill-in/back and intent-based Melody/Chords for topic navigation | Accepted · Implemented |
| [0078](../../adr/0078-git-preflight-and-noise-control-for-cide.md) | Git preflight and noise control of changes in CIDE | Accepted · Implemented |
| [0081](../../adr/0081-parametric-intent-melodies-editor-line-ranges.md) | Parametric Intent Melody - Editor Line Ranges (`:start:end`) | Accepted · Implemented |
| [0083](../../adr/0083-ai-mode-and-nested-settings-toml.md) | `settings.toml` — `ai.mode` discriminant and nested sections (local / acp / mcp_only / cloud) | Accepted · Implemented |
| [0090](../../adr/0090-launch-profiles-and-debug-startup-configurations.md) | Launch profiles and multiple debug startup configurations (VS-style) | Accepted · Implemented |
| [0093](../../adr/0093-mfd-embedded-browser-for-launch-url.md) | Built-in launch URL viewing on MFD (extension to profiles and launchBrowser) | Accepted · Implemented |
| [0096](../../adr/0096-intercom-topic-card-summary-and-product-spine.md) | Intercom Topic Card Summary And Product Spine | Accepted · Implemented |
| [0097](../../adr/0097-cockpit-compute-units-transport-to-channel-dto.md) | Cockpit Computing Units (CCU; analogous to LRU *Unit*) - layer between transport, meaning and channel | Accepted · Implemented |
| [0099](../../adr/0099-ide-databus-typed-events-and-projections.md) | Ide Databus Typed Events And Projections | Accepted · Implemented |
| [0105](../../adr/0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid Codebase Index (core + MCP) for C# stacks with Roslyn truth | Accepted · Implemented |
| [0107](../../adr/0107-blank-solution-creation-via-dotnet-new-sln.md) | Creating an empty solution via `dotnet new sln` (workspace self-sufficiency) | Accepted · Implemented |
| [0108](../../adr/0108-web-ai-portal-host-object-tools-bridge.md) | Web Ai Portal Host Object Tools Bridge | Accepted · Implemented |
| [0109](../../adr/0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Unified declarative catalog of parametric Intent Melody (TOML + code binding of args) | Accepted · Implemented |
| [0112](../../adr/0112-command-palette-query-modes-strategy.md) | Command palette query modes (`f:` / `t:` / `m:` / `x:` / `c:`) — mode model, strategies, and workspace search **backends** | Accepted · Implemented |
| [0117](../../adr/0117-ide-skia-kit.md) | SkiaKit - reusable Skia IDE primitives | Accepted · Implemented |
| [0118](../../adr/0118-agent-notes-core-2-toml-and-knowledge-path.md) | Agent Notes Core 2.0 - TOML, `knowledge_path`, parity with agent-notes-mcp | Accepted · Implemented |
| [0119](../../adr/0119-chat-slash-commands-intercom-surface.md) | Slash commands in chat — unified command line (Intercom + IDE) | Accepted · Implemented |
| [0120](../../adr/0120-primary-work-surface-intercom-or-editor.md) | Primary work surface — Intercom or Editor (Agent / Editor analogue) | Accepted · Implemented |


---

_Generated by `tools/gen_adr_pages.py`. Do not edit by hand._

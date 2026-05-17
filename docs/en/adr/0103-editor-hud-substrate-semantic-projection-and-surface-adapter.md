<!-- English translation of adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md. Canonical Russian: ../../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md -->

#ADR 0103: Substrate Editor HUD - Semantic Projection, Surface Adapter, DAL/CCU/DataBus Borders

**Status:** Accepted (strangler)  
**Date:** 2026-04-26

## Related ADRs

| ADR | Role |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | Layers, vertical slices and the role of MainWindowViewModel |
| [0009](0009-strangler-migration-and-exceptions.md) | Strangler migration and when policy deviations are allowed |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD above editor - custom content and grammar (like `presentation`) |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Built-in browser in MFD, external web LLMs and border with MCP |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → surface composer → surface (Agent-first display) |
| [0039](0039-workspace-navigation-affordances.md) | Workspace navigation - multiple views and "current file + related" |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI and presentation IDE layer - separate supports |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces - a general contract for a family of graph screens |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Agent edits in the editor as the only textual source of truth; chat - intent and status; presence layer (GDocs-like, without mandatory CRDT) |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | Editor HUD - an inline layer in the editor and the difference from the HUD banner |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Event delivery bus in the UI (similar to AFDX) and `System.Threading.Channel<T>` |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | Cockpit Computing Units (CCU; analogue of LRU *Unit*) - a layer between transport, meaning and channel |
| [0098](0098-semantic-first-document-as-projection.md) | Semantics is primary; document and repository - projections (Semantic-First) |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus - Typed Events and State Projections |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer - boundary of external interfaces and adapters |

## Summary

- **Editor HUD Substrate:** `SemanticProjectionPipeline` / `EditorHudEngine` / `IEditorSurfaceAdapter`.
- DAL / CCU / DataBus; hi-freq bounded circuit separate from CDS.
- Baseline AvaloniaEdit; roadmap UI in `ui-ux/`, comparison of hosts in `design/`.

### Outside ADR

| Document | Role |
|----------|------|
| [design: data-acquisition-layer-boundaries-v1](../../design/data-acquisition-layer-boundaries-v1.md) | blueprint: data-acquisition-layer-boundaries-v1 |
| [drawing: comparison of editor surface candidates](../../design/editor-surface-candidates-comparison-v1.md) | drawing: comparison of editor surface candidates |
| [drawing: analyzer-rollout-dal-ccu-v1](../../design/analyzer-rollout-dal-ccu-v1.md) | drawing: analyzer-rollout-dal-ccu-v1 |
| [ux: Forward polishing roadmap](../ui-ux/editor-forward-ui-cleanup-roadmap-v1.md) | ux: polishing roadmap Forward |
**Implementation state (v1 layer is “closed” within strangler):** `Features/Editor` - `IEditorSurfaceAdapter` / `AvaloniaEditSurfaceAdapter`, hi-freq `EditorStabilizedInputThrottler` (one per window) + `EditorInputDelta` → guard by `CurrentFilePath` → **`EditorDocumentHudLayer`** (per document: `EditorHudEngine` + `EditorHudStabilizedContext`) → `SemanticProjectionPipeline` / `EditorSemanticSnapshot` from DAL stripes; **presentation** file-level **HUD banner** in `Application/Presentation/EditorHudBannerTextComposer` (0085: not to be confused with `EditorInlineHudLayer` - reserved inline); VM only orchestrates Roslyn entries and assigns `EditorHudBannerText`, with `DiagnosticsChanged` the snapshot is invalidated. **Outside v1:** transfer of inline render (squiggles, tooltips) from `DockDocumentView` to the same data layer - [editor-hud-inline-migration-inventory-v1](../../design/editor-hud-inline-migration-inventory-v1.md), banner/inline policy - [editor-hud-banner-inline-policy-v1](../../design/editor-hud-banner-inline-policy-v1.md), visual policy MFD/Forward - [editor-forward-ui-cleanup-roadmap-v1](../ui-ux/editor-forward-ui-cleanup-roadmap-v1.md).

---

<a id="adr0103-context"></a>

## 1. Context
`DockDocumentView` and associated view models already implement **Editor HUD** fragments (adorners, LSPs, tooltips) and **HUD banner** in the spirit of [0085](0085-editor-hud-inline-layer-and-hud-banner.md), but the **inline** experience is **not** framed as a single named substratum: Quick Info contracts, inlays, ghost text, gutter are scattered. In parallel, explicit layers are attached to the stack - **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md), **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md), **ingestion** [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) - and direction **semantic-first** [0098](0098-semantic-first-document-as-projection.md).

Without a single ADR, new HUD work risks:

- dump **LSP I/O and file reading** in view models or in CCU (conflict with [0102](0102-data-acquisition-layer-boundary-and-contract.md) and [analyzer-rollout-dal-ccu-v1](../../design/analyzer-rollout-dal-ccu-v1.md));
- publish **per-key traffic** to **DataBus** (conflict with [0099](0099-ide-databus-typed-events-and-projections.md): typed domain events, not a high-frequency input stream);
- **mix** transport ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md)) with application events and ad-hoc convolution in the UI.

---

<a id="adr0103-decision"></a>

## 2. Solution

<a id="adr0103-three-contours"></a>

### 2.1 Substrate name: three matched circuits

1. **`SemanticProjectionPipeline`** - aggregates **normalized** entities for the Forward editor zone: diagnostics, hover payload, symbol/link context, agent presence flags, **version/relevance** metadata. **Not** owns long-term LSP stdio, raw JSON as “home” and UX graph layout ([0067](0067-graph-backed-surfaces-contract.md) remains on graph-backed surfaces).

2. **`EditorHudEngine`** - **policy and composition**: *what* to show **inline**, what to show in **HUD banner**, what to give to PFD/MFD [0036](0036-cds-channel-compositor-surface-pipeline.md) / [0039](0039-workspace-navigation-affordances.md). Consumes **stabilized** or **throttled** inputs rather than unlimited "noise per symbol".

3. **`IEditorSurfaceAdapter`** (or equivalent name) - **implementation boundary** of the actual text control: **the main baseline** in this repository remains **AvaloniaEdit** (see [concept-to-implementation-map-v1](../ui-ux/concept-to-implementation-map-v1.md), [LANGUAGE-SERVICES-PLAN.md](../../LANGUAGE-SERVICES-PLAN.md)). The adapter provides the **document coordinates, caret, selection** and **affordances** of the host needed by the HUD engine, without spreading editor types across the application.

The terms [0085](0085-editor-hud-inline-layer-and-hud-banner.md) do not change: **Editor HUD** = inline + link to document; **HUD banner** = file level bar; **IDS** = global IDE overlays, not Editor HUD [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).

<a id="adr0103-layering-table"></a>

### 2.2 Layout: DAL, CCU, DataBus, high-frequency
| Question | Layer | Note |
|--------|------|-----------|
| LSP stdio, reading file/settings, starting processes | **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md) in `Features/*/DataAcquisition` | Not CCU, not thick `MainWindowViewModel` [0006](0006-presentation-layers-and-feature-slices.md) |
| Meaningful **snapshots** Editor HUD / Forward (dedup, priority error > warn, `DocumentId` + ranges, version) | **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) and/or `Features/.../Application` orchestrators | candidate names like `EditorSemanticSnapshot` / `ForwardHudSnapshot` |
| **Session** and **after debounce** domain facts (`CSharpLspRestarted`, `ActiveDocumentChanged`, scenario like “diagnostics have stabilized”) | **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md) | **Not** per keystroke |
| **Current, pointer, scroll** in frame/key scale | **Separate** in-process path: for example `System.Threading.Channels` **capacity 1** + `BoundedChannelFullMode.DropOldest` (or SPSC *latest slot*) | **Not** the second global “grocery bus”; **not** replacing [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) as an end-to-end transport. One consumer thins out and publishes **less frequently** to the DataBus/CCU inputs if necessary. |
| Ingestion / streams “as a log” in the UI | [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Orthogonal DataBus [0099](0099-ide-databus-typed-events-and-projections.md) |

<a id="adr0103-web-vs-native-forward"></a>

### 2.3 Web stack and native Forward editor

- **WebView2** in the Avalonia/Win32 host is **not** equal to “drag in **Electron**” (no Chromium+Node shell as an application). **Other** statement: built-in renderer, interop, trust.
- **Product baseline** for the **code editor** in Forward remains **native** (AvaloniaEdit). **Monaco in WebView2** (and similar) **not** silent default: **comparison** / **rejected** for Forward by product policy; see [editor-surface-candidates-comparison-v1](../../design/editor-surface-candidates-comparison-v1.md).
- Optional web surfaces **MFD** - by [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) / [0093](0093-mfd-embedded-browser-for-launch-url.md); **secondary** tools, not the “editor = browser” thesis.

<a id="adr0103-invariants"></a>

### 2.4 Invariants (do not break)

- [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md): when applying agent edits, **editor buffer** is the source of the applied truth; chat - intent/status; presence channel separately.
- [0085](0085-editor-hud-inline-layer-and-hud-banner.md): do not merge **inline** Editor HUD and **HUD banner** in one control without explicit names.
- [0098](0098-semantic-first-document-as-projection.md): PFD/MFD semantic paths remain leading; editor tips **do not** take over all navigation.
- [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) / [0102](0102-data-acquisition-layer-boundary-and-contract.md): **CASCOPE020/021** and rollout [analyzer-rollout-dal-ccu-v1](../../design/analyzer-rollout-dal-ccu-v1.md) as a “barrier” border.

<a id="adr0103-editor-core-criteria"></a>

### 2.5 Criteria for selecting the editor core (for the appendix drawing / future spike)

- Inline hints, ghost text, inlays, gutter; rich hover/Quick Info; performance on large buffers; **theme consistency**; Pointer/Caret/Document API; integration with **semantic-first** cockpit [0039](0039-workspace-navigation-affordances.md).

---

<a id="adr0103-strangler"></a>

## 3. Strangler / migration

1. **Unbind** the HUD presentation from the ad-hoc details `DockDocumentView`: introduce the three outlines above **without** the big-bang replacement `AvaloniaEdit`.
2. **Spike (default):** `AvaloniaEditSurfaceAdapter` + **bounded** high-frequency path + one **slice** `SemanticProjectionPipeline` / `EditorHudEngine` (for example diagnostics + one hover branch). More details - [§5 - spike volume](#adr0103-spike-scope).
3. **Second** host adapter (for example web) - only with **explicit** acceptance of the risk of the web stack in Forward; not required for the first spike.
4. After stabilization - if necessary, **graph-backed** surfaces [0067](0067-graph-backed-surfaces-contract.md) for *navigation*, which does not belong in a text host.

---

<a id="adr0103-non-goals"></a>

## 4. Not goals (v1 of this ADR)
- Fix the final **type names** and folder layout (strangler: interfaces next to `Features/Editor` or as agreed in the implementation).
- Replace **CDS** [0036](0036-cds-channel-compositor-surface-pipeline.md) or redefine **channel** in the cockpit sense.
- External message brokers or bringing all flows into one envelope.

---

<a id="adr0103-spike-scope"></a>

## 5. Technical spike volume (default)

| Item | Volume |
|-------|--------|
| **Adapter** | `IEditorSurfaceAdapter` for **AvaloniaEdit**; mapping caret/selection/offsets in a document for one document type (eg C#) |
| **Hi-freq** | Producer(s) on editor events → `Channel<EditorInputDelta>` (capacity 1, drop-oldest) → one consumer with **throttle** (e.g. 16–50 ms) before CCU/DataBus touches |
| **Pipeline** | One vertical slice: LSP/diagnostics from **DAL** → snapshot in the spirit of **CCU** or thin orchestrator in `Application` → **EditorHudEngine** → adapter |
| **Out of Spike** | full Monaco/WebView2 adapter; full CASCOPE coverage for all types of HUD |

**Success Criteria:** no new LSP I/O in `Cockpit/ComputingUnits/*`; no unlimited per-key `Publish` on `IDataBus`; banner vs inline [0085](0085-editor-hud-inline-layer-and-hud-banner.md) remains explicit.

---

<a id="adr0103-related-docs"></a>

## 6. Related documents

- **Host comparison (appendix):** [editor-surface-candidates-comparison-v1](../../design/editor-surface-candidates-comparison-v1.md)
- **Roadmap polishing UI (Forward, banner, tooltips, MFD):** [editor-forward-ui-cleanup-roadmap-v1](../ui-ux/editor-forward-ui-cleanup-roadmap-v1.md)

---

<a id="adr0103-consequences"></a>

## 7. Consequences

- **Plus:** single point of extension Quick Info, inlays, gutter without confusing DAL, DataBus and raw editor events; agreement with 0097/0102/0099/0094.
- **Minus:** preliminary development of the design and adapter before each cosmetic edit; discipline so that hi-freq does not go to the DataBus.
- **Risk if ignored:** fallback to **god-**`MainWindowViewModel`, **event spaghetti** on DataBus, CASCOPE violations between DAL and CCU.

---

<a id="adr0103-rejected-alternatives"></a>

## 8. Rejected alternatives (briefly)

- **The entire carriage/diagnostics flow via DataBus** - rejected: against the spirit of [0099](0099-ide-databus-typed-events-and-projections.md) and an avalanche of subscribers.
- **CCU reads LSP streams directly** - rejected: separation of [0102](0102-data-acquisition-layer-boundary-and-contract.md) and [0097](0097-cockpit-compute-units-transport-to-channel-dto.md).
- **Default web editor in Forward** without review - rejected for baseline; optional line only with an explicit product sign-off.
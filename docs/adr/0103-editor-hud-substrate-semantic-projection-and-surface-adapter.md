# ADR 0103: Editor HUD substrate — semantic projection, surface adapter, границы DAL / CCU / DataBus

**Status:** Proposed  
**Date:** 2026-04-26

**Related:** [0006](0006-presentation-layers-and-feature-slices.md), [0009](0009-strangler-migration-and-exceptions.md), [0021](0021-pfd-mfd-cockpit-attention-model.md) §9, [0032](0032-hud-banner-configuration-and-grammar.md), [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md), [0036](0036-cds-channel-compositor-surface-pipeline.md), [0039](0039-workspace-navigation-affordances.md), [0066](0066-cockpit-ui-vs-ide-presentation-layer.md), [0067](0067-graph-backed-surfaces-contract.md), [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md), [0085](0085-editor-hud-inline-layer-and-hud-banner.md), [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0098](0098-semantic-first-document-as-projection.md), [0099](0099-ide-databus-typed-events-and-projections.md), [0102](0102-data-acquisition-layer-boundary-and-contract.md), [design: data-acquisition-layer-boundaries-v1](../design/data-acquisition-layer-boundaries-v1.md), [design: editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md), [design: analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md), [ux: editor-forward-ui-cleanup-roadmap-v1](../ux/editor-forward-ui-cleanup-roadmap-v1.md)

---

## 1. Context

`DockDocumentView` and related view models already implement **Editor HUD** pieces (adorners, LSP, tooltips) and **HUD banner** per [0085](0085-editor-hud-inline-layer-and-hud-banner.md), but the **inline** experience is **not** a single named substrate: contracts for Quick Info, inlays, ghost text, and gutter are scattered. At the same time, the stack now has explicit layers — **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md), **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md), **ingestion** [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) — and **semantic-first** direction [0098](0098-semantic-first-document-as-projection.md).

Without a single ADR, new HUD work risks:

- piling **LSP I/O and file reads** into view models or CCU (conflicts with [0102](0102-data-acquisition-layer-boundary-and-contract.md) and [analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md));
- publishing **per-keystroke** traffic on **DataBus** (conflicts with [0099](0099-ide-databus-typed-events-and-projections.md) §principles: typed domain events, not high-frequency input spam);
- **mixing** transport ([0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md)) with **application** events and **UI** conflation.

---

## 2. Decision

### 2.1 Name the substrate (three cooperating concepts)

1. **`SemanticProjectionPipeline`** — aggregates **normalized** semantic entities for the Forward editor region: diagnostics, hover payload, symbol/related context, agent presence flags, and **versioning/freshness** metadata. It **does not** own LSP stdio, raw JSON parsing as a long-term home, or graph layout UX ([0067](0067-graph-backed-surfaces-contract.md) stays on graph-backed surfaces).

2. **`EditorHudEngine`** — **policy and composition** for *what* to show **inline** vs in **HUD banner** vs delegated to PFD/MFD [0036](0036-cds-channel-compositor-surface-pipeline.md) / [0039](0039-workspace-navigation-affordances.md). It consumes **stabilized** or **throttled** inputs, not unbounded per-character noise.

3. **`IEditorSurfaceAdapter`** (or equivalent name) — **implementation boundary** for the actual text control: **primary baseline** in this repository remains **AvaloniaEdit** (see [concept-to-implementation-map-v1](../ux/concept-to-implementation-map-v1.md), [LANGUAGE-SERVICES-PLAN.md](../LANGUAGE-SERVICES-PLAN.md)). The adapter exposes **document coordinates, caret, selection, and host affordances** needed by the HUD engine without spreading editor-specific types across the whole app.

[0085](0085-editor-hud-inline-layer-and-hud-banner.md) terminology is unchanged: **Editor HUD** = inline + document-attached; **HUD banner** = file-level strip; **IDS** = global IDE overlays, not editor HUD [0066](0066-cockpit-ui-vs-ide-presentation-layer.md).

### 2.2 Layering: DAL, CCU, DataBus, hi-freq

| Concern | Layer | Note |
|--------|--------|------|
| LSP stdio, file/settings reads, process launch | **DAL** [0102](0102-data-acquisition-layer-boundary-and-contract.md) in `Features/*/DataAcquisition` | Not CCU, not a fat `MainWindowViewModel` [0006](0006-presentation-layers-and-feature-slices.md) |
| Meaningful **Editor HUD** / Forward **snapshots** (dedup, priority error > warn, `DocumentId` + ranges, version) | **CCU** [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) and/or `Features/.../Application` orchestrators | e.g. `EditorSemanticSnapshot` / `ForwardHudSnapshot` as **candidates** for names |
| **Session** and **debounced** domain facts (`CSharpLspRestarted`, `ActiveDocumentChanged`, `DiagnosticsStabilized`-style) | **IDE DataBus** [0099](0099-ide-databus-typed-events-and-projections.md) | **Not** per-keystroke |
| **Caret, pointer, scroll** at frame/key rate | **Separate** in-process path: e.g. `System.Threading.Channels` **capacity 1** + `BoundedChannelFullMode.DropOldest` (or SPSC *latest slot*) | **Not** a second global “bus” product; **not** a substitute for [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) as cross-cutting transport. One consumer throttles and emits **rarefied** events to DataBus / CCU inputs when needed. |
| Ingestion / log-style streaming to UI | [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Orthogonal to DataBus [0099](0099-ide-databus-typed-events-and-projections.md) |

### 2.3 Web stack vs native Forward editor

- **WebView2** in an Avalonia/Win32 host is **not** “bundling **Electron**” (no Chromium+Node shell as the app). It is a **different** cost profile: embedded renderer, interop, trust.
- **Product baseline** for the **code editor** in Forward remains **native** (AvaloniaEdit). **Monaco in WebView2** (or similar) is **not** a silent default: treat as **comparative** / **rejected** for Forward per product policy; see [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md).
- **MFD** optional web surfaces stay under [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) / [0093](0093-mfd-embedded-browser-for-launch-url.md) — **secondary** tools, not “the editor = browser”.

### 2.4 Invariants (do not break)

- [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md): during agent apply, the **editor buffer** is the source of applied truth; chat is intent/status; presence channel stays separate.
- [0085](0085-editor-hud-inline-layer-and-hud-banner.md): do not merge **inline** Editor HUD and **HUD banner** concerns in one control without naming.
- [0098](0098-semantic-first-document-as-projection.md): PFD/MFD **semantic** paths stay primary; editor hints do not **own** all navigation.
- [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) / [0102](0102-data-acquisition-layer-boundary-and-contract.md): **CASCOPE020/021** and rollout [analyzer-rollout-dal-ccu-v1](../design/analyzer-rollout-dal-ccu-v1.md) for boundary gate.

### 2.5 Selection criteria for editor core (for appendix / future spike)

- Inline hints, ghost text, inlays, gutter affordances; rich hover / Quick Info; performance on large buffers; **theme parity**; pointer/caret/document API; **semantic-first** cockpit integration [0039](0039-workspace-navigation-affordances.md).

---

## 3. Strangler / migration

1. **Decouple** HUD presentation from ad-hoc `DockDocumentView` details: introduce the three concepts above **without** big-bang replacement of `AvaloniaEdit`.
2. **Spike (default):** `AvaloniaEditSurfaceAdapter` + hi-freq **bounded** path + a **single** `SemanticProjectionPipeline` / `EditorHudEngine` **slice** (e.g. diagnostics + one hover path). See §5.
3. A **second** host adapter (e.g. web) only if the team **explicitly** accepts Forward web-stack risk; not required for the first spike.
4. After stability, consider optional **graph-backed** surfaces [0067](0067-graph-backed-surfaces-contract.md) for *navigation* that must not live in the text host.

---

## 4. Non-goals (v1 of this ADR)

- Prescribing final **type names** or folder layout (strangler: ship interfaces near `Features/Editor` or as agreed in implementation).
- Replacing **CDS** [0036](0036-cds-channel-compositor-surface-pipeline.md) or redefining **channel** in the cockpit sense.
- **External** message brokers or unifying all streams into one envelope.

---

## 5. Technical spike scope (default)

| Item | Scope |
|------|--------|
| **Adapter** | `IEditorSurfaceAdapter` implemented for **AvaloniaEdit**; map caret/selection/document offsets for one document type (e.g. C#) |
| **Hi-freq** | Producer(s) on editor events → `Channel<EditorInputDelta>` (capacity 1, drop-oldest) → single consumer applying **throttle** (e.g. 16–50 ms) before touching CCU/DataBus |
| **Pipeline** | One vertical slice: LSP/diagnostics from **DAL** → **CCU**-shaped snapshot or thin orchestrator in `Application` → **EditorHudEngine** → adapter |
| **Out of scope for spike** | Full Monaco/WebView2 adapter; full CASCOPE coverage for all HUD types |

**Success criteria:** no new LSP I/O in `Cockpit/ComputingUnits/*`; no unbounded per-key `Publish` on `IDataBus`; banner vs inline [0085](0085-editor-hud-inline-layer-and-hud-banner.md) still explicit.

---

## 6. Companion documents

- **Comparative appendix (hosts):** [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md)
- **UI cleanup roadmap (Forward, banner, tooltips, MFD):** [editor-forward-ui-cleanup-roadmap-v1](../ux/editor-forward-ui-cleanup-roadmap-v1.md)

---

## 7. Consequences

- **Positive:** one place to extend Quick Info, inlays, and gutter without entangling DAL, DataBus, and raw editor events; alignment with 0097/0102/0099/0094.
- **Negative:** up-front design and adapter work before every cosmetic tweak; need discipline to keep hi-freq out of DataBus.
- **Risks if ignored:** return to a **god** `MainWindowViewModel` path, **event spaghetti** on DataBus, and CASCOPE violations between DAL and CCU.

---

## 8. Rejected alternatives (summary)

- **Drive all caret/diagnostics through DataBus** — rejected: violates [0099](0099-ide-databus-typed-events-and-projections.md) spirit and creates subscriber storms.
- **Make CCU read LSP streams directly** — rejected: [0102](0102-data-acquisition-layer-boundary-and-contract.md) + [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) split.
- **Adopt web editor as default Forward** without review — rejected for baseline; optional future track only with explicit product sign-off.

# Editor surface candidates — comparison (v1)

**Status:** design companion to [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md)  
**Date:** 2026-04-26

**Related:** [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), [0098](../adr/0098-semantic-first-document-as-projection.md), [0035](../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md), [concept-to-implementation-map-v1](../ux/concept-to-implementation-map-v1.md), [LANGUAGE-SERVICES-PLAN.md](../LANGUAGE-SERVICES-PLAN.md)

This document is a **comparative appendix** for the **Forward** document editor host. It does **not** change product baseline: **AvaloniaEdit** remains the default stack in this repository.

---

## 1. Current stack (baseline)

| Dimension | AvaloniaEdit (today) |
|-----------|------------------------|
| **Integration** | Native Avalonia; `DockDocumentView` host; TextMate via AvaloniaEdit.TextMate |
| **LSP / semantics** | Wired in app code (DAL direction per [0102](../adr/0102-data-acquisition-layer-boundary-and-contract.md)); not free from glue code |
| **Inline HUD** | Adorners, custom renderers, tooltips — feasible; full VS-class inlays/ghost need ongoing work |
| **Performance** | Good for many files; very large files depend on host usage |
| **Theming** | Can track app theme; parity with MFD/cockpit requires explicit work ([0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md)) |
| **Hi-freq** | Direct events; must still route through [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) hi-freq **bounded** path, not DataBus |
| **Risk** | Lower platform variance on Windows; no embedded browser in Forward |

---

## 2. Web editor in WebView2 (e.g. Monaco)

| Dimension | Monaco (or similar) in WebView2 |
|-----------|----------------------------------|
| **What it is** | Embedded **Edge** webview in a **native** process — **not** an Electron app shell. Different tradeoffs (interop, C++/WinRT, two heaps) from Chromium+Node bundling. |
| **Inline HUD** | Rich ecosystem (Monaco: decorations, codelens patterns); work remains for **C#-specific** LSP alignment and interop **IPC** to DAL/IDE |
| **Theming** | Two theme systems: web CSS vs Avalonia `PrimitivesKit` / cockpit — **duplication** risk |
| **Performance** | Can be strong; large-doc and **alloc** profile depends on integration |
| **Platform** | WebView2 is Windows-first; cross-platform MFD already calls out [0093](../adr/0093-mfd-embedded-browser-for-launch-url.md) for secondary surfaces, not a mandate for the **code** editor |
| **Policy** | [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md): **not** baseline Forward; optional research track only. WebView2 as **MFD** tool stays [0035](../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md). |

---

## 3. Other native or hybrid options (strawman)

| Option | When to consider |
|--------|------------------|
| **Heavier custom Avalonia** (more adorners, Skia, custom text layout) | Stay native; invest if AvaloniaEdit limits are **measurable** in target scenarios (large files, specific hint density). |
| **Separate process editor host** (IPC) | Out of scope for v1; increases ops and sync with [0084](../adr/0084-agent-edits-editor-source-of-truth-presence-channel.md) buffer truth. |
| **Roslyn/VS platform**-style | Long-term; not implied by 0103 spike. |

---

## 4. How this feeds `IEditorSurfaceAdapter`

The adapter contract should be **host-agnostic** so that the **same** `SemanticProjectionPipeline` / `EditorHudEngine` can drive AvaloniaEdit first; a web host would re-implement the **same** coordinate, caret, and **semantic display** port surface without duplicating DAL or CCU.

**Minimum port surface (conceptual):**

- `DocumentId` + text snapshot or change ranges for LSP
- Caret/selection in document offsets; optional visual line/column
- API to request **stabilized** hover/diagnostic presentation from **engine** (not raw LSP in view)

---

## 5. Decision summary

| Use case | Direction |
|----------|-----------|
| **Default Forward code editor** | **AvaloniaEdit** + [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) layers |
| **MFD / launch URL / external LLM** | WebView2 per **0035 / 0093** — not a stand-in for core editor without ADR |
| **“Replacement-first” research** | Optional spike **after** first AvaloniaEdit vertical slice; explicit risk sign-off |

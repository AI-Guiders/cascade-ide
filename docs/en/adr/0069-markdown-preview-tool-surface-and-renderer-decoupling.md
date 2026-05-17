<!-- English translation of adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md. Canonical Russian: ../../adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md -->

# ADR 0069: Markdown Preview - MFD tool, renderer-first decoupling and refusal of inline preview in the document

**Status:** Accepted · Implemented  
**Date:** 2026-04-19  

**Replaces surface/placement architecture:** [0026](0026-markdown-preview-surfaces-and-placement.md) (historical canon `markdown_preview_placement` / `forward_split`).

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD vs MFD; long texts and secondary streams - in MFD |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown authoring, diagrams, export expanded |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | former placement canon; this ADR **superseded** on surface/placement architecture |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | WebView on MFD as a separate trusted/restricted layer, not the base renderer |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | channel → surface |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | tool vs chrome IDE |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | payload/projection/slot division |

## Summary

- Markdown Preview - **MFD tool**, optional part of the document tab.
- **Renderer-first:** native Markdig by default; WebView - optional adapter ([0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md)).
- **`forward_split` removed** from canon; replaces [0026](0026-markdown-preview-surfaces-and-placement.md).
- Authoring ([0023](0023-markdown-diagrams-language-tooling.md)) is orthogonal to preview.

---
## Context

The current implementation of preview Markdown grew up as an **widget within a document** (`DockDocumentView`, `forward_split`) with an additional window and an incomplete `mfd` route from [0026](0026-markdown-preview-surfaces-and-placement.md). In practice, this resulted in incorrect connectivity:

1. **The document depends on the preview-renderer.** A crash or binary incompatibility of the preview library breaks the opening of the file as such.
2. **Placement and renderer are mixed.** Inline preview in the document simultaneously decides *where* to show and *what* to render.
3. **`forward_split` contradicts the attention model.** For long Markdown/docos, the preview in the frontal editor takes up space from the primary work surface, although according to [0021](0021-pfd-mfd-cockpit-attention-model.md) reading long secondary materials is more natural for **MFD**, and not for **PFD** and not for the primary forward surface.
4. The product already has an **authoring Markdown line**: include, Kroki/diagram expansion, export expanded, potential authoring expansion on top of Markdown ([0023](0023-markdown-diagrams-language-tooling.md)). Preview should be a **consumer of the result of the authoring pipeline**, and not the architectural master of the Markdown file.

Additional trigger: inline preview, strictly dependent on the third-party Avalonia Markdown-renderer, became a source of runtime crash when opening a document. This confirmed that preview should be isolated as a **tool** and not as a required part of the document tab lifecycle.

---

## Solution

<a id="adr0069-p1"></a>

### 1. Preview Markdown no longer lives inside the `DockDocumentView`

The `DockDocumentView` and document editor **should not** contain a required preview-renderer in the default visual tree.  

**Invariant:** the Markdown file is opened and edited **even if** no preview renderer is available or temporarily crashes.

`forward_split` as a basic architectural form is **removed from the canon**. We are not introducing deprecated mode for the sake of compatibility: the product is not yet required to preserve the old UX topology.

<a id="adr0069-p2"></a>

### 2. Preview Markdown becomes a separate tool (tool surface)

Preview is treated as a **separate tool/secondary surface** and not as “part of the document”. Basic target placement:

- **Primary:** `mfd_tool`
- **Secondary:** `separate_window`

PFD is not used for preview: according to [0021](0021-pfd-mfd-cockpit-attention-model.md) PFD keeps situational awareness and current context, and not long texts/docs.

<a id="adr0069-p3"></a>

### 3. Placement and renderer - two independent axes

You need to explicitly separate:

| Axis | Question |
|----------|--------|
| **Placement** | Where to show preview: MFD tool, separate window, in the future another secondary surface |
| **Renderer** | How to render markdown: native renderer, WebView renderer, fallback stub |
**Invariant:** changing the renderer should not change the semantics of the surface, and changing the placement should not require a different preview data model.

<a id="adr0069-p4"></a>

### 4. Canon of renderers: native first, WebView optional

Basic canon:

- **`native_markdig`** — target main renderer.
- **`webview_html`** - a separate renderer/adaptor, valid **only** as a secondary MFD-oriented surface or a separate window; not the base path.
- **`disabled` / `unavailable`** - explicit fallback without the document crashing.

**Why `native_markdig`:**

- better controlled from Avalonia-host without strict dependence on the web stack;
- easier to keep surfaces/tools inside models;
- easier to integrate into the authoring pipeline and subject to product restrictions;
- does not break PFD/MFD semantics and does not make preview a “small default browser”.

**Why `webview_html` is still needed:**

- it is already planned by [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md);
- it is useful for richer HTML-like rendering, when the MFD deliberately acts as a secondary rich surface.

But **WebView does not become a canon preview as such**: it is only one renderer-adaptor.

<a id="adr0069-p5"></a>

### 5. Preview consumes the authoring pipeline rather than owning it

The Preview layer receives an already prepared **source payload**:

- raw markdown,
- expanded markdown (after include / diagram expansion),
- metadata (title, source file, origin, possibly link-map / anchor map),
- renderer options.

The Authoring Markdown extension remains a separate product line:

- it is responsible for authoring affordances;
- preview is responsible only for the **presentation** of the already generated payload;
- publication contract / `export expanded markdown` from [0023](0023-markdown-diagrams-language-tooling.md) remains a priority over “another beautiful runtime renderer”.

<a id="adr0069-p6"></a>

### 6. Recommended model in code

An explicit abstraction like:

- `MarkdownPreviewSource` / `MarkdownPreviewPayload`
- `IMarkdownPreviewRenderer`
- `MarkdownPreviewToolViewModel`
- `MarkdownPreviewToolView`

Minimum renderer-contract:

1. accept payload preview,
2. or return Avalonia `Control`,
3. or update an existing host,
4. on failure, degrade to “preview unavailable”, rather than throwing an exception out into the document/tool host.

---

## Consequences

### Positive

- Opening a document no longer depends on the preview renderer.
- The architecture better matches [0021](0021-pfd-mfd-cockpit-attention-model.md): long reading and rich preview - in MFD, not in PFD and not in forward core.
- It's easier to add multiple renderers without changing the entire UI topology.
- Authoring extension Markdown can evolve separately from the visual preview.

### Cost

- You will have to remove the old `forward_split` path and reconnect the preview UX commands.
- We need a new tool/page in the secondary shell.
- Need your own native renderer on top of `Markdig` instead of relying on random third party Avalonia Markdown control as a foundation.

---

## Non-targets

- Don’t make a full-fledged WYSIWYG Markdown editor right now.
- Do not promise pixel compatibility with GitHub/VS Code preview.
- Do not turn preview into a common browser runtime for any rich surfaces.
- Do not transfer preview to PFD.

---

## Canon change from ADR 0026

This ADR is **superseded** [0026](0026-markdown-preview-surfaces-and-placement.md) in part:

- `forward_split` as canonical placement,
- embedding preview inside `DockDocumentView`,
- implicit coupling between placement and renderer.

From [0026](0026-markdown-preview-surfaces-and-placement.md) retain the meaning, but are readable through a new layer:

- preview as a secondary surface, and not part of the authoring semantics;
- internal links/peek as a UX preview feature;
- a separate window as an acceptable secondary placement.

---

## Alternatives

| Option | Why rejected |
|--------|------------------|
| Leave inline preview in the document and just change the library | Doesn't solve architectural coherence: document still depends on preview |
| Make WebView the base renderer for everything preview | Too heavy and intruding center of gravity; worse for the native surface model |
| Keep only a separate window and do not make an MFD tool | Contradicts the MFD's intended role as a secondary instrument surface |
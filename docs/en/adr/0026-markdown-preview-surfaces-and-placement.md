<!-- English translation of adr/0026-markdown-preview-surfaces-and-placement.md. Canonical Russian: ../../adr/0026-markdown-preview-surfaces-and-placement.md -->

# ADR 0026: Markdown — Preview Surfaces and Placement (`workspace.toml`)

**Status:** Superseded  
**Date:** 2026-04-08  
**Updated:** 2026-04-11 — subsection “Internal references”; orthogonal to [0023](0023-markdown-diagrams-language-tooling.md). Details — [§ History](#adr0026-history).

> **Superseded — current canon:** [ADR 0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) (MFD tool surface, renderer-first, no `forward_split`). Below — **history** of placement via `workspace.toml` and internal references; for new decisions rely only on **0069**.

## Related ADRs

| ADR | Role |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | `workspace.toml`, merge of shipped `UiModes/` and repo overlay `.cascade/workspace.toml` |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Preview as secondary surface vs forward editing |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Separate window as second `TopLevel` |
| [0022](0022-mfd-visual-design-surface-axaml-blazor.md) | MFD tab/region perspective |
| [0023](0023-markdown-diagrams-language-tooling.md) | LSP, diagrams, Kroki, export — **orthogonal** to preview widget placement |

## Summary

- **Historical** canon for Markdown preview placement (`workspace.toml`, `forward_split`).
- **Do not use for new decisions** — see callout and [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md).

## Replacing earlier wording

- **This ADR is superseded** by the new canon in [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md): preview is now a separate tool surface with renderer/placement decoupling; `forward_split` is removed from canon.
- **Historical canon for Markdown preview placement** — this ADR; current surface/renderer canon — [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md).
- The only “Markdown preview” mention in UX context in [0023](0023-markdown-diagrams-language-tooling.md) § “UX details” was **removed from canon there** and **replaced with a link here** ([0023](0023-markdown-diagrams-language-tooling.md) remains language experience and diagrams).
- [0023](0023-markdown-diagrams-language-tooling.md) is **not** superseded as a whole — only the “where to show preview” overlap.

---

## Context

We need one source of truth for **where** rendered Markdown mounts in the UI: beside text in **forward** (editor), in a **separate window**, or in **MFD** (secondary attention). This is **chrome and widget topology**, not LSP, include, or Kroki.

Configuration fits **`workspace.toml`** with other global chrome metrics ([0010](0010-ui-modes-toml-configuration.md)): one merge layer with the bundle and repo overlay, without writing dynamic resize back into shipped files.

---

## Decision

1. **TOML key:** `markdown_preview_placement` at the root of merged **`UiWorkspaceToml`** (model and snake_case → PascalCase like other `workspace.toml` fields).

2. **Allowed string values** (case-insensitive for users; code normalizes):
   - **`forward_split`** — second column beside the active document (`EditorContentGrid` in `DockDocumentView`), inline `MarkdownScrollViewer`. Inactive tabs keep preview column width **zero** so layout does not “stick” in the background.
   - **`separate_window`** or alias **`window`** — existing `MarkdownPreviewWindow` (secondary `TopLevel` per [0017](0017-multi-window-workspace-and-agent-surfaces.md), without requiring the full multi-window roadmap).
   - **`mfd`** — **target** placement in MFD tab/region ([0021](0021-pfd-mfd-cockpit-attention-model.md), cross-ref [0022](0022-mfd-visual-design-surface-axaml-blazor.md)). Until a dedicated MFD preview tab exists, behavior is an **explicit fallback** to the separate window (as in code and comments), not silent “same as forward”.

3. **Default** before merged TOML load and in test reset: **`forward_split`** (`MarkdownPreviewPlacementRuntime`).

4. **Link to attention model [0021](0021-pfd-mfd-cockpit-attention-model.md):** preview stays a **secondary** surface vs editing in forward; `markdown_preview_placement` only changes **mount geometry**, not PFD/MFD/EICAS semantics.

5. **Do not confuse** with a future key for **general** multi-`TopLevel` presentation topology (discussion in [0010](0010-ui-modes-toml-configuration.md) / [0017](0017-multi-window-workspace-and-agent-surfaces.md)): `markdown_preview_placement` is a **narrow** key for Markdown preview only.

### Preview depth (v1 intent)

IDE preview is an **auxiliary** surface ([0021](0021-pfd-mfd-cockpit-attention-model.md)): target bar is **“good enough to trust draft and diagrams”** (structure, code blocks, links, diagram render per privacy/Kroki rules), not a **Typora/GitHub-class** product.

**Higher priority** than “prettier preview panel” is the **publication contract**: assembled/deployed document for external readers — [0023](0023-markdown-diagrams-language-tooling.md) (export expanded, include, external Markdown consistency).

**Not v1 goals** (until explicit pain): pixel-perfect GitHub, full WYSIWYG instead of editor, heavy sync scroll with block highlight “preview-only editors”. Narrow **forward_split** deliberately steals editor width — users who refuse to split forward choose **`separate_window`** / **`window`** or (when available) **`mfd`**.

### Internal references in long documents (peek / “Show Definition”)

**Problem:** ADRs and long specs often say **“see §6 above”** / **“§6”** without anchor links — readers scroll to recall the section.

**Intent (not mixed with language layer [0023](0023-markdown-diagrams-language-tooling.md)):** in the **preview widget** (same host as `markdown_preview_placement`) — Peek Definition–style UX: hover or gesture on an **internal** reference in the current file shows a **short overlay** with the target paragraph (or navigate on click), **without** mandatory editor scroll.

**Target resolution:**

- **Preferred in authoring:** normal Markdown links — `[see §6](#adrNNNN-p6)` to anchor `<a id="adrNNNN-p6"></a>` in the same ADR; anchor naming — [snippets/adr-anchors-policy.md](../../adr/snippets/adr-anchors-policy.md) (short ref: [README ADR](README.md#adr-anchors-policy)).
- **Heuristic (optional):** recognize “§6” / “section 6” against **numbered lists** in “Decision” and similar — only if maintenance pays off; on **ambiguity** — **user choice** (pick candidate or go to anchor), no mandatory “best screen” magic.

**Priority:** below baseline preview quality and **publication contract** ([0023](0023-markdown-diagrams-language-tooling.md), export); may land in a **later** iteration after stable render and placement per this ADR.

## Implementation (code orientation)

- Model: `UiWorkspaceToml.MarkdownPreviewPlacement`, merge: `UiWorkspaceTomlMerger`.
- Runtime: `MarkdownPreviewPlacement`, `MarkdownPreviewPlacementParser`, `MarkdownPreviewPlacementRuntime`; wired when loading mode catalog — `UiModeCatalog` (with `UiWorkspaceLayoutRuntimeMetrics`, `AttentionZonePanelRuntime`).
- UI: `DockDocumentView` — editor grid and inline preview; `MainWindow` — preview commands branch on `MarkdownPreviewPlacementRuntime.Current`.

---

## Consequences

- Agent/MCP docs: changing placement changes UI snapshot regions; multi-root contract ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) applies to the preview window.
- Extending **`mfd`** without key change: separate delivery — MFD shell tab/host and removing fallback.
- **Internal references** (subsection above): parser/preview host or layer on render; if needed — author conventions for ADRs (anchors vs “§ N”) in [ADR README](README.md), no new ADR.

---

## Rejected alternatives

- **Preview placement only in user `settings.toml`** — rejected for “project preset”: team should fix behavior in repo overlay beside other `workspace.toml`.
- **Merge with [0023](0023-markdown-diagrams-language-tooling.md) in one ADR** — rejected: mixes language experience and UI geometry, harder to evolve independently.

---

## Change history

<a id="adr0026-history"></a>

| Date | Change |
|------|--------|
| 2026-04-08 | Subsection “Preview depth”: v1 target bar, non-goals, export priority from [0023](0023-markdown-diagrams-language-tooling.md). |
| 2026-04-11 | Subsection **“Internal references”** (hover/peek for “see § N” and anchors); orthogonal to [0023](0023-markdown-diagrams-language-tooling.md). |

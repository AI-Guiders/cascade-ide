<!-- English translation of adr/0023-markdown-diagrams-language-tooling.md. Canonical Russian: ../../adr/0023-markdown-diagrams-language-tooling.md -->

# ADR 0023: Markdown + diagrams (Mermaid/PlantUML) - first-class experience through LSP and workflow

**Status:** Proposed  
**Date:** 2026-04-08

## Related ADRs

| ADR | Role |
|-----|------|
| [0015](0015-editor-toml-syntax-highlighting.md) | TextMate highlighting as a fast baseline |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | external processes and testable abstractions |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | cockpit attention model |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | **where** the Markdown preview widget is mounted - `workspace.toml`; canon for posting previews |

## Summary

- Markdown + **Mermaid/PlantUML** - first-class via LSP and workflow.
- Kroki, export expanded, authoring - **orthogonal** preview ([0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md)).

---
## Context

There are a lot of `*.md` in repo and production scripts, as well as Mermaid/PlantUML diagrams (often right in fenced blocks). The comfort of working with them is usually worse than with C# (diagnostics, completion, navigation, rename links, quick cycle “edit → see the result”).

Important restrictions:

- Markdown is a container: “nested languages” live inside fenced blocks, but LSP usually works per file, not per range.
- “Make it like C#” for fenced blocks requires a complex infrastructure: virtual documents, two-way mapping of coordinates/diagnostics, synchronization of edits.
- There are different rendering backends for diagrams (locally, PlantUML Server, Kroki). For some commands (for example, Kroki/PlantUML server), the diagram source goes to an external server - this affects privacy.

---

## Solution

Adopt the incremental path “80% value fast”:

1. **Markdown becomes first-class via Markdown LSP (`marksman`) as an external process.**  
   Goal: links/anchors/wikilinks, go-to-definition, references, rename, basic diagnostics - on the entire `*.md` array.

2. **PlantUML receives LSP at the individual file level (`*.puml` / `*.plantuml`) via `plantuml-lsp` as an external process.**  
   Goal: completion/hover/diagnostics in a “real” PlantUML document, without Markdown injection on v1.

3. **Mermaid (and PlantUML inside Markdown) on v1 is provided via:**
   - baseline: **TextMate highlighting**, snippets/templates (if necessary),
   - **preview/render** in Markdown (built-in workflow IDE),
   - **workflow commands** for extracting diagrams from Markdown into separate `*.mmd`/`*.puml` files when “language” experience is needed (LSP, navigation, diagnostics).

4. **LSP injection into fenced Markdown blocks (virtual documents) - deferred** as a separate ADR/phase after:
   - Markdown LSP is stable,
   - UX rules are highlighted (how to show diagnostics inside Markdown, how to do quick-fix),
   - solved privacy/offline issues for diagram rendering.

---

## Diagram storage canon + include directive

**The canonical format for storing diagrams is separate files** next to the documentation:

- Mermaid: `*.mmd` (or `*.mermaid`)
- PlantUML: `*.puml` / `*.plantuml`

Markdown (`*.md`) uses fenced blocks **optional**, but prefers **inlining** for:

- chart revision,
- cleaner diffs,
- enabling LSP at the “real file” level (`plantuml-lsp` for `*.puml`),
- “like C# rename” capabilities (updating links/paths).

**Syntax include (one line, compatible with the pattern from the `resume` repo):**```mermaid
{{ INCLUDE: example.mmd }}
```

```plantuml
{{ INCLUDE: example.puml }}
```
Optional (not necessary for MVP, but useful for large sections): `INCLUDE_MANIFEST`, `INCLUDE_GLOB` - as in `resume`.

Rules (v1, benchmark):

- **case-insensitive** directive (`INCLUDE`/`include`) and allows spaces;
- the default path is considered **relative to the `*.md`** file in which the fenced block is located (as opposed to `resume`, where the path is from the root of the repo);
- limit the include depth (for example, max depth 5) and detect cycles;
- include errors (file not found, loop, too deep) should be displayed in the preview as a clear diagnostic.

---

## Publishing (so as not to “break external Markdown”)

Include directives (`{{ INCLUDE: ... }}` and variants) are **not a Markdown standard** and outside of CascadeIDE they can break the rendering of diagrams.

Principle:

- **Inside CascadeIDE / in the sources** you can keep an include structure (authoring convenience).
- **Publish/rummage outside** (GitHub/GitLab/Confluence/…): only **assembled/expanded** Markdown variants, where include has already been replaced with the actual text of diagrams/fragments.

Consequence (product requirement):

- The IDE should provide an explicit path “**Export / Build expanded Markdown**” (command/operation) so that the user can get a portable version of the document to publish.

---

## UX details (guideline)

- **Placement of the Markdown preview widget** (forward / window / MFD, TOML) - **[0026](0026-markdown-preview-surfaces-and-placement.md)**; here - only language, diagrams and workflow commands.
- For diagrams there is an explicit “confidence threshold”:
  - if the rendering is done through a server (Kroki/PlantUML server), there is a switch and URL in the settings;
  - offline/private mode - through your own instance (or disabling network rendering).
- **Workflow commands (MVP):**
  - “Extract diagram to file...” (from fenced `mermaid`/`plantuml` → `*.mmd`/`*.puml` + link/include back),
  - “Open diagram in dedicated editor” (as a quick transition to a separate file or to a virtual document in the future),
  - “Render diagram” (update preview/image on demand, without constant polling).

---

## Consequences

- Markdown will become more comfortable immediately (LSP at the file level).
- PlantUML will get “like a language” experience where it is really needed - in `*.puml`.
- There will be a gap for fenced blocks (there is no full-fledged LSP inside Markdown), but there will be a quick way to “take it out and live normally.”
- An infrastructure of external LSP processes will appear, which can be extended to other languages.

---

## Rejected alternatives

- **Immediately do LSP injection into Markdown fenced blocks** - rejected as too much volume/risk for v1 (complicated synchronization and mapping of diagnostics).
- **Rely only on the preview of diagrams without LSP** - rejected: the goal is not only to “see the picture”, but also to “type like a language”.
- **Embedding LSP as an in-process library** - rejected: easier to update/replace external LSP servers, and lower risk of IDE dependency bloat.

---

## Open questions

- Where to store and how to configure paths to `marksman` / `plantuml-lsp` (system PATH vs built-in tool manager).
- Support for `.mmd` as a separate document: is a separate “Mermaid mode” (highlighting/snippets/validation) needed without a full LSP.
- What is the format of the link from Markdown to the rendered diagram (regular link to a file, embed image, include-pragma).
<!-- English translation of adr/0076-ui-ux-principles-hub.md. Canonical Russian: ../../adr/0076-ui-ux-principles-hub.md -->

# ADR 0076: UI/UX - the center of principles (coherent text from the canon)

**Status:** Proposed  
**Date:** 2026-04-20  

**Purpose:** one **input** ADR for the reader: **how we think about the interface and experience**, without going through dozens of records. Details, tables of terms and exceptions are in the original ADR and in [cascadeide-philosophy-v1.md](../../design/cascadeide-philosophy-v1.md).

**Canon of text** below - files in [`snippets/ui/`](snippets/ui/README.md); wording changes are made there, this ADR sets the structure and **Proposed/Accepted** status for the UX “center”.

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | cockpit, areas |
| [0071](0071-ai-assistance-sovereignty-locality-invisibility.md) | Principles of AI/assistant integration in the IDE - sovereignty, locality, invisibility |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Chord layer (FMS-style), S/T and overlay - keyboard-first extension (ADR 0013) |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | pointer `UI/` |

### Outside ADR

| Document | Role |
|----------|------|
| [UI/principles.md](UI/principles.md) | UI/principles |
**Build:** in GitHub raw `{{ INCLUDE }}` are not expanded - for reading “like a book”: `dotnet script build-adr.csx` (expanded `0076` will go into the general workbook) or collect HTML from the root `docs/adr` point by point after including 0076 in your `adr-book.md`.

---

## Introduction

The goal is **not a “second VS/Rider” in the sense of the UX profile**: not endless panels and not cloud inline by default everywhere, but **a consistent contour of attention** (cockpit) and **transparency** regarding the code and repository. Below are two blocks: **attention model**, then **product philosophy** in a condensed form.

{{ INCLUDE: snippets/ui/0076-attention-and-zones.md }}

{{ INCLUDE: snippets/ui/0076-philosophy-not-second-vscode.md }}

---

## Consequences

- Onboarding and external reviews can link **"start with [0076](0076-ui-ux-principles-hub.md)"**, then follow the links to full ADRs.
- Expansion of the “center” - new sections in `snippets/ui/` + new `INCLUDE` here; There is no need to duplicate long text in several ADRs unnecessarily.

---

## Rejected alternatives

- **Link table only** - not enough for a reader who wants **one coherent text** (see [UI/principles.md](UI/principles.md) as a map, not as a replacement for this ADR).
- **Duplicate the entire standard** from 0021/0071 in this file - out of sync; canon remains in the original ADR+ snippets.
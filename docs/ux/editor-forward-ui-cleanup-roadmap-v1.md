# Editor Forward — UI cleanup roadmap (v1)

**Status:** design / roadmap companion to [ADR 0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md)  
**Date:** 2026-04-26

**Related:** [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) §9, [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), [architecture-migration.md](../architecture-migration.md), [editor-surface-candidates-comparison-v1](../design/editor-surface-candidates-comparison-v1.md)

Goal: a **visually coherent** Forward region (document chrome, **HUD banner**, **inline** Editor HUD, tooltips) and **aligned** MFD/Problems-style surfaces, without conflating **cockpit deck** language with **IDE presentation** shell [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md).

---

## Roadmap (ordered for strangler)

1. **Align with [architecture-migration.md](../architecture-migration.md):** when extracting editor HUD, **do not** grow DAL inside view models. LSP/files/settings → DAL; UI snapshots → CCU or `Features/*/Application` + thin orchestrator.

2. **Shared semantic presenters** from `DockDocumentView` and related VMs: one pattern for `hover`, `diagnostics` presentation model, `inline` hints, `agent` presence (consume **stabilized** input from 0103 pipeline, not raw events).

3. **Split data vs rendering:**
   - **Data:** `WorkspaceDiagnostics`, LSP/Roslyn payloads, navigation graph, future code actions.
   - **Presentation:** HUD **banner**, inline hint, gutter, tooltip/popover, semantic **chips** (mapping only at the edge, not in DAL).

4. **Visual hierarchy on Forward surface:**
   - Unify **document** chrome, **header** tab area, **banner** strip, and **editor** content margins.
   - Define **priority** and **weight** for `error / warn / info / agent / semantic` signals so the Forward region does not look like unrelated overlays.

5. **MFD and Problems (and similar):** same **visual language** (density, color tokens, iconography) as Forward where they show the *same* diagnostic family — avoid “tech demo” inconsistency between editor squiggles and MFD list rows **when** both are on screen.

6. **Tooltips and popovers:** one interaction model: delay, **pointer vs keyboard** (accessibility), dismissal, and no overlap with [0079](../adr/0079-ide-display-system-ids-overlay-pipeline.md) global IDS except intentional hierarchy.

7. **After** [0103](../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) vertical slice: revisit spacing/typography **tokens** in one pass (not per-feature drift).

---

## Out of scope (this roadmap doc)

- Full redesign of PFD **instrument deck** (see deck ADRs) — only **consistency** where Forward and MFD show shared semantic signals.
- i18n string audit — [0033](../adr/0033-internationalization-resx-avalonia.md) remains separate.

---

## Success (UX)

- User can read **one** file’s problems without **competing** loud chrome; inline and banner are **intentional** duplicates or complements per [0085](../adr/0085-editor-hud-inline-layer-and-hud-banner.md), not accident.
- **Semantic-first** [0098](../adr/0098-semantic-first-document-as-projection.md): MFD/PFD stay credible **navigation** home; the editor does not become the only busy surface.

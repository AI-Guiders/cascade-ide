<!-- English translation of adr/0066-cockpit-ui-vs-ide-presentation-layer.md. Canonical Russian: ../../adr/0066-cockpit-ui-vs-ide-presentation-layer.md -->

# ADR 0066: Cockpit UI and presentation IDE layer - separate supports

**Status:** Accepted  
**Date:** 2026-04-19

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Attention model, EICAS |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | Policy `presentation` |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | `PrimitivesKit`, cabin palette |
| [0065](0065-instrument-categories-domain-taxonomy.md) | Categories / `graph_kind` |
| [0013](0013-command-surface-and-discoverability.md) | Command Palette |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Command registry, hotkeys |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS - shell overlays (**not** cockpit UI) |

**Code:** `Cockpit/PrimitivesKit/`, `Features/UiChrome/`, `Themes/*.json`.

---
## Context

The product simultaneously contains:

1. **Instrumental layer of the cockpit** - deck, PFD/MFD/Forward zones, instruments, lamps, semantic map as a visual tool, role palette in the sense of EICAS/annunciator ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)).
2. **IDE shell** - menu, window, command palette, modal overlays without “device” semantics, standard fields and indents for settings and dialogs, theme tokens for **regular** UI.

Without an explicit separation, discussions and code reviews mix **Cockpit UI** and **presentation layer IDE** (conventionally “UI kit” of Chrome): they drag cockpit primitives into dialogs or, conversely, duplicate overlays and indents inside `PrimitivesKit`. This breaks the semantic boundary and makes it difficult for the theme and cockpit to evolve independently.

---

## Solution

Fix **two supports** (two design contexts), not two mandatory namespaces for each line of code:

| Support | Meaning | Typical place in code/artifacts |
|-------|--------|---------------------------------------|
| **Cockpit UI** | Visual language of **instruments and deck** in the cockpit metaphor: types of indicators, drawing of instruments, semantic colors of the cockpit (`CockpitPrimitivesPalette`), Skia-scenes of instruments, Dark Cockpit rules for **this** layer. | `Cockpit/PrimitivesKit/`, cockpit palette; ADR [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md), [0063](0063-instrument-deck-named-composition-one-anchor.md), [0065](0065-instrument-categories-domain-taxonomy.md); connection with [0021](0021-pfd-mfd-cockpit-attention-model.md). |
| **IDE presentation (chrome)** | App-wide **non-device** things: window shell, command palette, reusable **modal overlays**, consistent padding/typography for chrome, `CascadeTheme` theme tokens/JSON for **shell**. | `Features/UiChrome/`; `Views/` for specific screens; themes in `Themes/`; commands and palette - [0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md). |

**Default rule:** if a widget makes sense **without** the deck / attention zones / device metaphor - it does not belong to **Cockpit UI**; if the meaning is “show the state in the deck cell / on the device / in the cockpit strip” - do not mix it with the general overlay layer and “just IDE”.

**Invariant:** the **cabinet semantic palette** ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)) is not the only color directory for the entire application: **theme** and **chrome** can set tokens for menus, editor and modals; hex matches are allowed only as a conscious agreement, not as a mandatory dependency of the cockpit on the shell.

---

## Consequences

- Reviews and discussions clearly indicate the context: **Cockpit** vs **chrome IDE**; controversial cases are resolved by the default rule from the table above.
- New **reused** non-modal chrome primitives (overlays, standard settings cards) are being developed in the **`Features/UiChrome`** zone (or nearby in `Views`, without transferring to `Cockpit/`).
- **Cockpit** remains responsible for the consistency of instruments, deck and Skia rendering according to [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md); do not duplicate the menu palette there “for the sake of file unity.”

### Testing in assembly (Roslyn)

The **imports** border between `Features/UiChrome` and `Cockpit/PrimitivesKit` is fixed by the `CascadeIDE.ArchitectureAnalyzers` analyzer:

- **CASCOPE011** - `using CascadeIDE.Cockpit.PrimitivesKit` is prohibited in `Features/UiChrome/`.
- **CASCOPE012** - `using CascadeIDE.Features.UiChrome` is prohibited in `Cockpit/PrimitivesKit/`.
Details and Constraints (MCP/`RoslynMcpWorkspace`) - [CascadeIDE.ArchitectureAnalyzers/README.md](../../CascadeIDE.ArchitectureAnalyzers/README.md). The full list of CASCOPE* is there.

---

## Non-targets (current phase)

- Introduce a separate "UIKit" build or rename folders in one commit without the need.
- A comprehensive catalog of all theme tokens and components (this is a live guide and code, not duplicated in ADR).
- Disallow exceptions: a local prototype in a feature is possible, but does not define a second canon without revising the ADR.

---

## Alternatives (briefly)

| Option | Minus |
|--------|--------|
| One “UI kit for everything”, including the cockpit | Mixing semantics; the cabin pulls the shell and vice versa |
| Verbal agreement only | No stable link for review and onboarding |
| New ADR for each control (button, field) | Noise; the layer boundary is sufficient at the level of this ADR |
<!-- English translation of adr/0046-presentation-layout-authority-and-cockpit-invariants.md. Canonical Russian: ../../adr/0046-presentation-layout-authority-and-cockpit-invariants.md -->

# ADR 0046: Cockpit CDS - policy layouts (`CockpitPresentationLayoutPolicy`) and P/F/M invariants

**Status:** Accepted · Implemented  
**Date:** 2026-04-14

## Related ADRs

| ADR | Role |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation`, multi-window |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD Attention Model |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | UI is not the source of truth in meaning |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Instrument, slots |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS → composer → surface |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | P/F/M and coercion intent invariants in VM; `CockpitPresentationLayoutPolicy` / CASCOPE003 - see § "Solution" |

---
## Context

`presentation` in `settings.toml` specifies not only the decorative geometry, but also the semantic layout of the cockpit: where the PFD, Forward and MFD are located.

Problem before fixing the rules:

- different inputs (menu, MCP, changing UI mode, reactive changes) changed the visibility of panels differently;
- it was possible to get a state that contradicts the anchors of the first screen;
- closing `MfdHostWindow` visually affected the layout as if the preset had changed.

This broke the cockpit model: the user saw a “floating UI” rather than a stable P/F/M scheme.

## Solution

<a id="adr0046-p1"></a>

1. The canon of rules is placed in the CDS layer: `CockpitPresentationLayoutPolicy` (`CascadeIDE.Cockpit.Cds`).
2. The source of truth for the rules is the parsed `PresentationParseResult` (first screen). Static `PresentationLayoutAuthority` from `Services/Presentation` **cleared**; at the shell boundary, only a thin intent record remains (partial `MainWindowViewModel`, `Apply*` methods - “I want” semantics, no duplication of policy).
3. For the first screen, the following invariants apply:
   - if there is an anchor `P`, you cannot hide the left column (`IsSolutionExplorerVisible`);
   - if there is an anchor `M`, you cannot collapse the right column of the MFD to zero (`IsChatPanelExpanded = false`);
   - `Forward` is interpreted as a mandatory central zone; a separate explicit toggle to disable forward is not introduced.
4. Any way to change the layout must go through the coercion policy; displaying main grid columns - through the surface composer (`MainWindowShellSurfaceCompositor`, ADR 0036 p.3):
   - relay commands;
   - MCP visibility commands;
   - application of UI mode;
   - reactive-callbacks properties to intercept direct assignments.
5. Closing/opening the second `TopLevel` (`MfdHostWindow`) only changes the surface of the M-content placement, but does not recalculate the `presentation` semantics.

## Consequences

- The behavior of UI, MCP and modes becomes deterministic and consistent.
- "Impossible" states do not accumulate: policy returns them to the valid area.
- Any new commands that affect P/F/M must be consistent with the **same** coercion policy in CDS and not bypass the composer surface.
- On the assembly: Roslyn **CASCOPE003** (`CascadeIDE.ArchitectureAnalyzers`) - direct assignments `IsSolutionExplorerVisible` / `IsChatPanelExpanded` (and backing fields) outside the [white list of files](../../CascadeIDE.ArchitectureAnalyzers/README.md); new points - via `Apply*` / expanding the list in the analyzer.

## Rejected alternatives

- Allow each input (menu/MCP/mode) to have its own rules: causes logic drift.
- Consider `presentation` only a "layout hint", and not an invariant: contradicts the cockpit model.
- Fix only UI commands, without reactive coercion: direct assignments still bypass the rules.
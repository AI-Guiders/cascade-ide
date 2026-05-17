<!-- English translation of adr/0070-command-palette-direct-overlay-surface.md. Canonical Russian: ../../adr/0070-command-palette-direct-overlay-surface.md -->

# ADR 0070: Command Palette as direct overlay surface, routed to active TopLevel

**Status:** Accepted · Implemented  
**Date:** 2026-04-19

## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | palette and discoverability |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | several `TopLevel` and focus |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention model |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | keyboard-first and overlay-tips |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | shell chrome vs cockpit UI |

## Summary

- Command Palette - **direct overlay** in host, not `ModalOverlay` baseline.
- Routing to active `TopLevel`; communication with [0112](0112-command-palette-query-modes-strategy.md).


---
## Context

The command palette was already a canonical surface in [0013](0013-command-surface-and-discoverability.md), but its specific render-host remained unfixed. Practice has shown that composition through a common `ModalOverlay` turned out to be fragile for multi-window topology:

1. overlay might not materialize in the desired `TopLevel`, although the hotkey and VM state have already worked;
2. with multi-windows, the palette could visually appear in the wrong host or be duplicated;
3. keyboard-first UX broke down precisely at the critical point of discoverability: the user got the feeling that `Ctrl+Q` does nothing.

The problem turned out to be not in the palette itself as a product idea, but in the foundation of the surface: the general modal compose-path was too implicit for the keyboard-first overlay, which must be bound to the active window and focus.

---

## Solution

<a id="adr0070-p1"></a>

### 1. Basic surface palette

The Command Palette is rendered as a **direct overlay surface** within a specific host view, and not as a mandatory composition through a generic `ModalOverlay`.

This means:

- the root visual of the palette itself contains a dimmer/panel;
- the palette itself controls visibility, focus and keyboard routing;
- `ModalOverlay` remains a common UI framework, but is no longer **considered the canonical foundation** for the palette.

<a id="adr0070-p2"></a>

### 2. Routing to active TopLevel

When opened, the palette should be visible **in exactly one active host window**:

- `MainWindow`
- `PfdHostWindow`
- `MfdHostWindow`

The source of truth for host selection is the shell/VM state, reflecting the active `TopLevel` and the source of the last keyboard entry. Overlay should not “guess” from the global static state outside the current window.

<a id="adr0070-p3"></a>

### 3. Keyboard-first invariants

The following invariants are required for the palette:

- opening by hotkey should work from child controls and the editor;
- when opened, focus goes to the search bar;
- when closing, focus returns to the previous element of the current host;
- `Esc`, `Enter`, `Up/Down`, `PageUp/PageDown` are processed at the level of the palette itself;
- overlay should not depend on whether a particular layout has additional chrome/composition wrappers.

<a id="adr0070-p4"></a>

### 4. Decision boundary

This ADR captures only the baseline surface for the **Command Palette**.

It **doesn't** mean that:

- any modal/dimmer in the IDE must be rewritten to fit the same scheme;
- `ModalOverlay` is prohibited;
- overlay chord system hints from [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) must use the same visual tree.

But for the palette the default rule is now unambiguous: **direct overlay in the desired host**.

---

## Consequences

- The behavior of the palette becomes predictable in multi-window topology.
- Tests must cover not only hotkey routing, but also host routing: the palette is visible only in the expected `TopLevel`.
- Documentation for keyboard-first can now rely on a specific baseline, rather than the “historical” implementation via `ModalOverlay`.

---

## Rejected alternatives

1. **Keep `ModalOverlay` canon and fix private bugs around it.**  
   Rejected: the cause of the failure is not local, but architectural for multi-window keyboard-first surface.

2. **Global singleton-overlay on top of all windows.**  
   Rejected: violates the focus and attention model from [0017](0017-multi-window-workspace-and-agent-surfaces.md) and [0021](0021-pfd-mfd-cockpit-attention-model.md).

3. **Bind the palette only to `MainWindow`, and force the host window to proxy input back.**  
   Rejected: The user expects the discoverability surface to appear exactly where it is currently working.
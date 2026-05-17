<!-- English translation of adr/0117-ide-skia-kit.md. Canonical Russian: ../../adr/0117-ide-skia-kit.md -->

# ADR 0117: SkiaKit - reusable Skia IDE primitives

**Status:** Accepted · Implemented  
**Date:** 2026-05-17

## Related ADRs

| ADR | Role |
|-----|------|
| [0055](0055-skia-instrument-composition-pipeline.md) | Pipeline of cockpit tools (do not mix with SkiaKit) |
| [0057](0057-chat-surface-pipeline-adoption.md) | First consumer - chat surface |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | PrimitivesKit cockpits vs IDE |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces - target consumers |

## Solution

Enter the directory **`Views/SkiaKit/`** - primitive rendering library for IDE-Skia surfaces (analogous to `Views/UiKit/` for Avalonia).

- `SkiaSectionedCard` - card with compartments (signature + separator + lines).
- `SkiaTileGridLayout` — a grid of tiles.
- `SkiaKitThemeBridge` - CascadeTheme → `SkiaKitPaintTheme`.
- `SkiaTextLayout` - line wrapping.

Features (chat, semantic map cards, ...) **assemble the scene** from the kit; the domain and pipeline stages remain in Features/Services.

**Border:** SkiaKit does not import ViewModels, navigation pipeline stages, Cockpit PrimitivesKit.

## Code

`Views/SkiaKit/README.md`
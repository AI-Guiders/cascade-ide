# ADR 0117: SkiaKit — переиспользуемые Skia-примитивы IDE

**Статус:** Accepted  
**Дата:** 2026-05-17

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0055](0055-skia-instrument-composition-pipeline.md) | Pipeline инструментов кабины (не смешивать с SkiaKit) |
| [0057](0057-chat-surface-pipeline-adoption.md) | Первый consumer — chat surface |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | PrimitivesKit кабины vs IDE |
| [0067](0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces — целевые consumers |

## Решение

Ввести каталог **`Views/SkiaKit/`** — библиотека отрисовки примитивов для IDE-Skia surfaces (аналог `Views/UiKit/` для Avalonia).

- `SkiaSectionedCard` — карточка с отсеками (подпись + разделитель + строки).
- `SkiaTileGridLayout` — сетка плиток.
- `SkiaKitThemeBridge` — CascadeTheme → `SkiaKitPaintTheme`.
- `SkiaTextLayout` — перенос строк.

Фичи (чат, semantic map cards, …) **собирают сцену** из kit; домен и pipeline stages остаются в Features/Services.

**Граница:** SkiaKit не импортирует ViewModels, navigation pipeline stages, Cockpit PrimitivesKit.

## Код

`Views/SkiaKit/README.md`

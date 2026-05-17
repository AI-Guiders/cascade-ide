# SkiaKit (IDE)

Переиспользуемые **Skia-примитивы** для IDE-surfaces: чат ([0057](../../docs/adr/0057-chat-surface-pipeline-adoption.md)), graph-backed surfaces ([0067](../../docs/adr/0067-graph-backed-surfaces-contract.md)), будущие MFD-карточки.

**Не путать с:**

| Слой | Путь | Назначение |
|------|------|------------|
| **UiKit** | `Views/UiKit/` | Avalonia-контролы (Ecam*) |
| **PrimitivesKit** | `Cockpit/PrimitivesKit/` | Индикаторы кабины (Lamp, Bar, …) — [0064](../../docs/adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) |
| **SkiaKit** | `Views/SkiaKit/` | Отрисовка примитивов IDE на Skia (без доменной логики) |

## Правила

- Kit **не импортирует** Features/ViewModels/Services pipeline stages.
- Только: `model + ISkiaKitPaintTheme + bounds → Measure/Draw`.
- Цвета — через `SkiaKitThemeBridge.ResolveIdeSurface` или расширения (напр. `SkiaChatTheme`).

## Состав

- `SkiaSectionedCard` — карточка с отсеками (заголовок секции + разделитель + строки).
- `SkiaTileGridLayout` — сетка плиток по ширине viewport.
- `SkiaTextLayout.Wrap` — перенос строк.
- `SkiaKitColor` / `SkiaKitThemeBridge` — мост CascadeTheme → SKColor.

Фичи (`Views/Chat/Skia/`, semantic map render, …) собирают **сцену** из kit + свой layout/compositor.

# IDE chrome design tokens v1

**Статус:** v1 (код: `CascadeTheme.*` в `App.axaml`, `Themes/*.json`, [ADR 0086](../adr/0086-ui-theme-toml-canonical-json-mcp-wire.md) — wire позже).  
**Связь:** [ADR 0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), [Views/UiKit/README.md](../../Views/UiKit/README.md).

Семантические ключи **IDE chrome** (не палитра кабины [0064](../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md)).

## Поверхности

| Ключ | Назначение |
|------|------------|
| `CascadeTheme.ChatPanelBackground` | фон панели / вложенных полей |
| `CascadeTheme.ChatMessageBubbleBackground` | elevated section (карточка-секция) |
| `CascadeTheme.PanelBackgroundBrush` | MFD-карточки, readout (alias elevated в теме) |
| `CascadeTheme.InsetSurfaceBackground` | вложенные блоки (уточнения, nested list) |
| `CascadeTheme.InsetSurfaceBorderBrush` | рамка inset |

## Текст

| Ключ | Назначение |
|------|------------|
| `CascadeTheme.ChatMessageContentForeground` | основной текст |
| `CascadeTheme.ChatLabelForeground` | подписи, meta, placeholder |

## Акцент и рамки

| Ключ | Назначение |
|------|------------|
| `CascadeTheme.EditorColumnBorderBrush` | рамки секций и popup |
| `CascadeTheme.PanelTitleAccentBrush` | focus ring, акцент |

## Статус (chip)

| Ключ | Назначение |
|------|------------|
| `CascadeTheme.StatusChipBackground` | фон «идёт загрузка» |
| `CascadeTheme.StatusChipBorderBrush` | рамка chip |
| `CascadeTheme.StatusChipForeground` | текст chip |

## Отступы и радиусы (конвенция)

См. [flat-chrome-dark-cockpit-v1.md](flat-chrome-dark-cockpit-v1.md) (Flight).

- Section: `Padding=10`, `CornerRadius=4` — класс `cascadeSection` в `App.axaml`
- Inset: `CornerRadius=4` — `cascadeInset`
- Control: `CornerRadius=6`, `Padding=8,6`
- Status chip: `CornerRadius=4`, `Padding=8,3` — только при активном ходе работы (Dark Cockpit)
- IDE Health (non-Power): `ideHealthStrip` + `ideHealthSegment` — без `modeCard` на сегмент

## Правило для разметки

Новые AXAML: **`DynamicResource CascadeTheme.*`** и классы `cascadeSection` / `cascadeInset` / компоненты `Views/UiKit/`. Сырые `#RRGGBB` — только в JSON тем и `App.axaml` defaults.

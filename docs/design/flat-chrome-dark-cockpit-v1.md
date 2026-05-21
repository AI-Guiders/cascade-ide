# Flat chrome + Dark Cockpit v1

**Статус:** v1 (реализация в темах и `App.axaml`, Flight / `CursorLike`).  
**Связь:** [0021 §6](../adr/0021-pfd-mfd-cockpit-attention-model.md), [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), [ide-chrome-tokens-v1.md](ide-chrome-tokens-v1.md).

## Две оси (не путать)

| Ось | Смысл |
|-----|--------|
| **Flat chrome** | Геометрия IDE presentation: 1px границы, малые радиусы, без теней и градиентных «островов» в штатном Flight. |
| **Dark Cockpit** | Политика внимания: в норме тихо; цвет/акцент/карточка — **по отклонению** (W/C/A, EICAS, активный чеклист). |

Flat **не** означает «серый пустой экран». Означает: не тратить salience на декор.

## Правила flat chrome (Flight)

1. **Поверхности:** `main_window` ≈ `editor` ≈ колонки; панели (`chat_panel`, `toolbar`) — на один шаг светлее/темнее, не отдельный «остров».
2. **Разделители:** `workspace_layout.border_brush` / `editor_column.border_brush` — единая линия `#333841`, без glow.
3. **Радиусы:** колонки и секции **4px**; popup/палитра **6–8px**; без 10–14px у рабочих колонок.
4. **Тени:** убраны у палитры, slash-popup (Skia), Flight health; **Power**-семейство (`Classes.power`) — legacy glow, не активно при `UiModeFamily.Flight`.
5. **IDE Health (Flight):** одна плоская полоса — текст сегментов, без вложенных `modeCard` на каждый badge.
6. **Якоря layout-lab:** контур PFD/Forward/MFD — **1px**, фон прозрачный (не заливка 8–20% opacity).

## Dark Cockpit — что сохраняем

- EICAS / health: **появление** при активных оповещениях, не постоянная цветная полоса.
- Акцент Intercom: **редко** (inset/рамка), не на каждой строке.
- Status chip: только при **ходе работы** (сборка, загрузка), не декор.
- Эскалация Warning/Caution — **заметна** (цвет, контраст); flat не ослабляет W/C/A.

## Вкладки документов (Dock)

- Имя файла и `*` / `[P]` — **только** в полоске вкладок `Dock.Avalonia` (`DisplayTitle`).
- Внутри `DockDocumentView` **нет** второго `PanelChromeHeader` с полным путём (дубль убран); полный путь — ToolTip на области редактора.

## Вне scope v1

- Ситуационные чеклисты (0014), semantic tint сегментов health по severity.
- Полный отказ от `message_bubble` в Skia-ленте (0123).
- Перепись Fluent theme / Dock глобально (VS-like скруглённые вкладки — backlog).

## Файлы

| Артефакт | Изменение |
|----------|-----------|
| `Themes/cursor-like-theme.json`, `dark-theme.json` | Плоские поверхности, тише chip |
| `App.axaml` | `cascadeSection`/`cascadeInset`, `ideHealthStrip`, TreeView selection |
| `IdeHealthStripView.axaml` | Flight: flat strip |
| `SolutionExplorerView`, `DocumentsDockView` | CornerRadius 4 (non-Power) |
| `CommandPaletteView.axaml` | border вместо shadow |
| `SkiaPopupList.cs` | без drop shadow |
| `MainWindow.axaml` | тихие якоря зон |

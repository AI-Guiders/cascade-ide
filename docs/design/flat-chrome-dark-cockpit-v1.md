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

## Dark Cockpit — agent attention (parallel human)

Та же политика внимания, что у человека (тихо в норме; salience только по отклонению), но каналы агента — не пиксели, а **alert / SA / next[] / pulse / eQRH / Autoi charge / pressure notify**.

Полная формулировка и DoD для рантайма CDP: sibling repo [`cdp-mcp/docs/design/dark-cockpit-agent-v1.md`](../../../cdp-mcp/docs/design/dark-cockpit-agent-v1.md). Краткая норма ниже — канон-зеркало.

| Ось | Human | Agent |
|-----|-------|-------|
| **Норма** | тихие лампы, center тёмный | `alert.level=clear`, без WARN/ECL на здоровой sit |
| **Salience** | цвет только W/C/A | шум/токены только на **реальном** отклонении |
| **Пустота** | center off ≠ поломка | intentional plateau / no focus после ship ≠ failure |

**Контракт агента (обязан соблюдать):**

1. Не эскалировать clear sit. Если toolchain OK, ship intentional, focus null по плану — не трактовать `sa WARN · ecl · plateau` как работу; это нарушение DC со стороны продукта/агента.
2. Soft ≠ hard: Stage `@phase` affinity vs session phase — advisory, не WARN, пока каталог инструментов не ломается.
3. `next[]` — бюджет actionable deviations; evergreen tourism (onboard/goto «на всякий») не маскировать под W/C.
4. Autoi: charge/re-arm только при реальном TM focus / authorized work; blind plateau re-arm запрещён.
5. Pressure L1 — stash quietly; не превращать notify в ритуал export в чат.

**Антипример (dogfood):** leftover AC+DoD ship → focus null → `sa WARN · ecl · plateau` + `n-alert` — Dark Cockpit violation.

### Agent Scan Pattern (кратко)

Human SP = geography `P→Forward→M`. Agent SP **шире**: `board → sa → next → drill` — geography остаётся якорем; sit/steer/meta-слои не обязаны сводиться к трём seat-строкам. SSOT: [`dark-cockpit-agent-v1.md`](../../../cdp-mcp/docs/design/dark-cockpit-agent-v1.md) § Agent Scan Pattern.

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

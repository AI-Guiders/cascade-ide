# ADR 0066: Cockpit UI и слой presentation IDE — раздельные опоры

**Статус:** Accepted  
**Дата:** 2026-04-19  
**Принято:** 2026-04-19  

**Связь:** [0021](0021-pfd-mfd-cockpit-attention-model.md) (модель внимания, зоны, EICAS), [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) (политика представления), [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) (виды индикаторов deck, `PrimitivesKit`, семантическая палитра кабины), [0065](0065-instrument-categories-domain-taxonomy.md) (категории инструментов / `graph_kind`), [0013](0013-command-surface-and-discoverability.md) / [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) (палитра, команды). Реализация: `Cockpit/PrimitivesKit/`, `CockpitPrimitivesPalette`; общий хром — `Features/UiChrome/` (например `ModalOverlay`), темы `Themes/*.json`.

---

## Контекст

В продукте одновременно существуют:

1. **Инструментальный слой кабины** — deck, зоны PFD/MFD/Forward, приборы, лампы, semantic map как визуальный инструмент, палитра ролей в смысле EICAS/annunciator ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)).
2. **Оболочка IDE** — меню, окно, палитра команд, модальные оверлеи без семантики «прибор», типовые поля и отступы для настроек и диалогов, токены темы для **обычного** UI.

Без явного разделения обсуждения и код ревью смешивают **Cockpit UI** и **presentation-слой IDE** (условно «UI kit» хрома): тащат примитивы кокпита в диалоги или, наоборот, дублируют оверлеи и отступы внутри `PrimitivesKit`. Это ломает смысловую границу и усложняет эволюцию темы и кокпита независимо.

---

## Решение

Зафиксировать **две опоры** (два контекста проектирования), не два обязательных неймспейса на каждую строчку кода:

| Опора | Смысл | Типичное место в коде / артефактах |
|-------|--------|-------------------------------------|
| **Cockpit UI** | Визуальный язык **инструментов и deck** в метафоре кабины: виды индикаторов, отрисовка приборов, семантические цвета кабины (`CockpitPrimitivesPalette`), Skia-сцены инструментов, правила Dark Cockpit для **этого** слоя. | `Cockpit/PrimitivesKit/`, палитра кокпита; ADR [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md), [0063](0063-instrument-deck-named-composition-one-anchor.md), [0065](0065-instrument-categories-domain-taxonomy.md); связь с [0021](0021-pfd-mfd-cockpit-attention-model.md). |
| **IDE presentation (хром)** | Общие для приложения **не-приборные** вещи: оболочка окна, палитра команд, переиспользуемые **модальные оверлеи**, согласованные отступы/типографика для хрома, токены темы `CascadeTheme` / JSON для **shell**. | `Features/UiChrome/`; `Views/` для конкретных экранов; темы в `Themes/`; команды и палитра — [0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md). |

**Правило по умолчанию:** если виджет имеет смысл **без** метафоры deck / зон внимания / прибора — он не относится к **Cockpit UI**; если смысл — «показать состояние в ячейке deck / на приборе / в кокпитной полосе» — не смешивать с общим слоем оверлеев и «просто IDE».

**Инвариант:** семантическая палитра **кабины** ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md)) не является единственным каталогом цветов для всего приложения: **тема** и **хром** могут задавать токены для меню, редактора и модалок; совпадения по hex допустимы только как сознательное согласование, не как обязательная зависимость кокпита от shell.

---

## Последствия

- Ревью и обсуждения явно указывают контекст: **Cockpit** vs **хром IDE**; спорные случаи решаются правилом по умолчанию из таблицы выше.
- Новые **переиспользуемые** не-модальные примитивы хрома (оверлеи, типовые карточки настроек) развиваются в зоне **`Features/UiChrome`** (или рядом в `Views`, без переноса в `Cockpit/`).
- **Cockpit** остаётся ответственным за согласованность приборов, deck и Skia-отрисовки по [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md); не дублировать туда палитру меню «ради единства файла».

### Проверка в сборке (Roslyn)

Граница **импортов** между `Features/UiChrome` и `Cockpit/PrimitivesKit` закреплена анализатором `CascadeIDE.ArchitectureAnalyzers`:

- **CASCOPE011** — в `Features/UiChrome/` запрещён `using CascadeIDE.Cockpit.PrimitivesKit`.
- **CASCOPE012** — в `Cockpit/PrimitivesKit/` запрещён `using CascadeIDE.Features.UiChrome`.

Подробности и ограничения (MCP / `RoslynMcpWorkspace`) — [CascadeIDE.ArchitectureAnalyzers/README.md](../../CascadeIDE.ArchitectureAnalyzers/README.md). Полный список CASCOPE* — там же.

---

## Не-цели (текущая фаза)

- Ввести отдельную сборку «UIKit» или переименовать папки в одном коммите без потребности.
- Исчерпывающий каталог всех токенов темы и компонентов (это живой гайд и код, не дублирование в ADR).
- Запретить исключения: локальный прототип в фиче возможен, но не задаёт второй канон без пересмотра ADR.

---

## Альтернативы (кратко)

| Вариант | Минус |
|--------|--------|
| Один «UI kit на всё», включая кокпит | Смешение семантик; кабина тянет за собой shell и наоборот |
| Только устная договорённость | Нет стабильной ссылки для ревью и онбординга |
| Новый ADR на каждый контроль (кнопка, поле) | Шум; граница слоёв достаточна на уровне этого ADR |

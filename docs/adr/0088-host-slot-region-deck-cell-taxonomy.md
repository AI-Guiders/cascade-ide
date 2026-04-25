# ADR 0088: Host slot, регион внимания и ячейка deck — таксономия (не смешивать)

**Статус:** Proposed  
**Дата:** 2026-04-22  

**Связь:** [0017](0017-multi-window-workspace-and-agent-surfaces.md) (зона **Mfd** vs **страницы** `MfdShellView`), [0021](0021-pfd-mfd-cockpit-attention-model.md) (якоря PFD/MFD, география vs телеметрия), [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) (`CockpitInstrumentDescriptor`, `slot_id`), [0050](0050-declarative-instrument-zone-placement-toml.md) (`[instrument_routing]`, `pfd_primary` / `mfd_primary`), [0063](0063-instrument-deck-named-composition-one-anchor.md) (**instrument deck**, `InstrumentDeckDescriptor`, `SemanticAnchorId`), [0068](0068-deck-row-payload-and-presentation-projection.md) (полезная нагрузка строки vs проекция vs слот), [0073](0073-pfd-instrument-deck.md) (каталог кандидатов на PFD), композиция кадра хоста: `Cockpit/Composition/HostSurface/MainWindowHostSurfaceCompositor.cs`.

**Резюме:** в обсуждениях **слот** называли и **всю колонку PFD**, и **малый прямоугольник** в сетке приборов. Этот ADR фиксирует **разведение уровней**: что такое **host slot** в wire хоста, что такое **регион/якорь внимания**, что такое **ячейка instrument deck** — и **что не смешивать**. Реализация **мульти-инструмента** на PFD (композитор deck на поверхности) **не** обязана менять смысл `CockpitSlotIds.Pfd` без отдельного согласования (см. §4).

---

## 1. Контекст: откуда путаница

1. В коде **`CockpitSlotIds.Pfd` / `Mfd`** — идентификаторы для **`MainWindowHostSurfaceCompositor`**: в кадр попадает **не больше одного** `CockpitInstrumentDescriptor` на **каждый** такой идентификатор; **географически** это **вся** видимая колонка PFD (или MFD) в `DockedGrid`, а не «одна плитка» внутри неё.
2. В продуктовой речи **PFD** — **зона первичного внимания** (целая колонка, отдельное окно P+M и т.д.).
3. **Instrument deck** [0063] вводит **именованную композицию** «несколько сущностей **в одном якоре**» с **`SemanticAnchorId`** = например `pfd` — то же слово, что и `slot_id`, но **другой уровень абстракции** (`InstrumentDeckDescriptor` vs кадр хоста).
4. В [0068](0068-deck-row-payload-and-presentation-projection.md) уже разделены **строка таблицы/полоса** и **тип ячейки**; остаётся явно связать это с **хостовым** слотом [0047].

Без фиксированных имён снова возникает вопрос: *«Слот — это вся PFD или ячейка в deck?»*

---

## 2. Решение: канонические уровни

Ниже — **рекомендуемые** термины в документации, ADR, ревью; в коде идентификаторы (`CockpitSlotIds`, поля TOML) **не обязаны** дословно совпадать с этими русскими/английскими именами, но **смысл** должен маппиться.

**Грубая «лестница» сверху вниз (разные ветки):** *топология окон / пресентация* ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) → *регион внимания* (PFD, MFD) → **страница оболочки** (`MfdShellPage`, §2.0) — для вторичного контура; *параллельно* на стороне PFD — **host slot** (§2.1) и далее deck / внутренние блоки прибора. **Page** сидит **выше** маршрута «какой кабинный `instrument_id` в `pfd`», иначе путали бы **чат** с **прибором**.

<a id="adr0088-shell-page"></a>

### 2.0. Страница оболочки (Page, `MfdShellPage`)

- **Определение:** **навигация внутри `MfdShellView`** — какой **цельный** режим занимает **колонку** вторичного контура: Chat, Terminal, Solution Explorer, Build, … (`Models/MfdShellPage.cs`, команда `set_mfd_shell_page` в [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)).
- **Уровень:** **выше** §2.1–2.4: это **не** `CockpitSlotIds`, **не** `CockpitInstrumentDescriptor`, **не** ячейка instrument deck. Смена страницы **ортогональна** выбору **PFD**-прибора в кадре хоста.
- **Связь с [0017](0017-multi-window-workspace-and-agent-surfaces.md):** *зона внимания Mfd* (колонка/окно) **≠** *страница*; чат — **страница**, а не «ещё одна зона» рядом с PFD.
- **PFD-ветка:** в v1 **аналога** `MfdShellPage` для колонки PFD как **переключаемого набора несвязанных UIs** нет: там в основном **один** смонтированный кабинный прибор (§2.1), его внутренняя мозаика — §2.4. Для Pfd называть навигационный **layout**, не *page stack*: контракт `IPfdLayout` + `PfdLayouts` в `Models/Shell/`. Для Mfd — `IShellPage` / `IMfdShellPage` + `MfdShellPageDescriptor` (тот же `MfdShellPage` в VM).

<a id="adr0088-level-1"></a>

### 2.1. Host slot (хостовый слот, `slot_id` в `CockpitInstrumentDescriptor`)

- **Определение:** единица **маршрутизации инструмента в кадре хоста** главного/вспомогательного окна: *куда* композитор кладёт **релевантный** `instrument_id` для отображения в **данной** **геометрической** зоне колонки.
- **Сегодня (v1):** один `instrument_id` на один host slot; пример: `CockpitSlotIds.Pfd` + `CockpitStandardInstrumentIds.WorkspaceNavigationMap`.
- **Карта:** `MainWindowHostSurfaceCompositor.BuildInstruments` → список `CockpitInstrumentDescriptor(InstrumentId, SlotId)`.
- **Не путать с:** мелким глифом, строкой в таблице WH, sub-slot внутри Avalonia-разметки одного `UserControl`.

**Инвариант v1:** host slot **не** дробится на несколько `slot_id` **в той же** модели кадра, пока [0047] не расширен явно (см. §4).

<a id="adr0088-level-2"></a>

### 2.2. Регион / якорь внимания (PFD, MFD, …)

- **Определение:** **семантическая зона кокпита** по [0021] — *зачем* эта часть экрана (первичный скан, вторичный контур, …).
- **Связь с host slot:** для колонок primary/docked **география** «колонка PFD» совпадает с host slot’ом `pfd` **как площадь экрана**; **якорь** объясняет *роль* зоны, **host slot** — *точка подключения* в DTO кадра и MCP-нарративе «смонтировать инструмент X в Y».

<a id="adr0088-level-3"></a>

### 2.3. Instrument deck и ячейка deck (deck cell)

- **Определение [0063]:** **именованная** композиция: упорядоченный набор **ячеек** (сетка, стек, полоса) под **`SemanticAnchorId`**.
- **Ячейка deck (deck cell):** **одна** позиция в раскладке: стабильный **`cell_id`** (как `EnvironmentReadinessInstrumentDeck.OrderedCellIds`, `IdeHealthInstrumentDeck.OrderedSegmentIds` для **канала** IDE Health — аналог по **роли**).
- **Критично:** ячейка deck **не** является `CockpitSlotIds` **в текущем** контракте [0047], пока не введён явный **mapping** «ячейка → host slot / вложенный маршрут».
- **Назначение:** **несколько** индикаторов/инструментов **внутри** географии одного PFD-региона (при product decision) — через **другой** слой: либо **составной** `UserControl` (внутренняя раскладка = не deck в смысле host), либо будущий **deck host** / **PFD surface deck compositor** (см. §3).

<a id="adr0088-instrument-internal-blocks"></a>

### 2.4. Прибор целиком и внутренние блоки (ячейки прибора)

Ортогонально §2.3: **один** `instrument_id` в host slot (например карта намерений в Pfd) **сам** может быть **составным** — пилот воспринимает **один** прибор, а внутри — **несколько блоков** (ячеек), в которых лежат **конкретные** индикаторы, подграфы, легенда, хром, подписи ([0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) — виды примитивов).

- **Внутренняя ячейка / блок** — единица раскладки **внутри** одного кабинного инструмента; стабильные id и политика композиции задаются **композитором этого прибора** (для мини-карты — pipeline карты намерений, см. [0055](0055-skia-instrument-composition-pipeline.md), [0056](0056-semantic-map-pipeline-adoption.md)), **не** путать с `cell_id` **instrument deck** на уровне региона (§2.3).
- **Смысл для UX:** «прибор = композиция блоков» — тогда, например, **граф** и **легенда** к нему — **два** таких блока с явной укладкой (резерв под легенду, независимое масштабирование и т.д. по продукту), а не одна неразличимая каша.
- **Реализация (карта PFD v1, продукт «Карта намерений»):** разбиение граф/легенда — `CodeNavigationMapInstrumentBlockCompositor` + `CodeNavigationMapInstrumentBlockDescriptor` / `CodeNavigationMapInstrumentBlockIds` в `Services/CodeNavigation/` (стабильные id `code_navigation.pfd_instrument.block.*`); альтернативный композитор с id `code_navigation.workspace_instrument.block.*` — `CodeNavigationMapWorkspaceInstrumentBlockCompositor` в `Services/Navigation/`. Итог пайплайна — `CodeNavigationMapCompositionResult.CodeNavigationMapInstrumentBlocks` (после `CodeNavigationMapLayoutStage` в `CodeNavigationMapCompositor`). Отрисовка единая (`CodeNavigationMapSceneDrawing`), прямоугольники согласованы с `LegendColumnLeft` / `LegendBlockTopY`.

**Инвариант терминов:** *deck* на PFD = **колода из нескольких (кабинных) инструментов** в регионе; *блок* = **часть одного** прибора. В разговоре «ячейка» уточнять: *deck cell* (регион) vs *внутренняя ячейка прибора*.

---

## 3. Направление реализации (без фиксации срока)

1. **Композитор набора инструментов** на **одной** геометрии PFD: принимает `InstrumentDeckDescriptor` (или PFD-специфичный вариант) и **проецирует** `instrument_id` **в ячейки**; **внешний** кадр хоста по-прежнему может содержать **один** дескриптор на `pfd` с `instrument_id` = *deck host*, либо — при расширении модели — иначе; выбор **отдельной** итерации после прототипа.
2. **CDS / MCP / snapshot:** что считать «атомом» в кадре (один host instrument vs N внутренних) — **контрактная** граница; обновлять [0008] / `cds-contract` **вместе** с внедрением, не деградировать «один `instrument_id` в `pfd`» незаметно.

---

## 4. Правило различения: «что называть слотом в разговоре»

| Вопрос | Канонический ответ (этот ADR) |
|--------|------------------------------|
| «Слот — вся PFD?» | **Host slot** `pfd` **в смысле [0047] / кадр хоста** — **да,** это **маршрут до колонки** (один выбранный инструмент на эту площадь в v1). |
| «Слот — маленькая ячейка в сетке приборов?» | **Нет** для `CockpitSlotIds`; называть **ячейка deck** / **`cell_id`**. |
| «`SemanticAnchorId` = `pfd` в `InstrumentDeckDescriptor` — то же, что `slot_id`?** | **Тот же якорь внимания / регион**, **не** дубликат **отдельного** host slot в DTO, пока deck не **поднят** в кадр как отдельный уровень; избегать «двух `pfd` в одном кадре» в речи без оговорки. |

---

## 5. Последствия

- Документация, новые ADR, ревью: использовать **host slot** vs **deck cell** vs **регион PFD** по таблице §4.
- [0073](0073-pfd-instrument-deck.md) (каталог PFD) и [0063](0063-instrument-deck-named-composition-one-anchor.md) остаются: этот ADR **уточняет** термины, **не** заменяет их.
- Код: **немедленная** смена имён `CockpitSlotIds` **не** требуется; при появлении PFD multi-instrument — либо **внутренний** deck без нового `slot_id`, либо явное **расширение** 0047 + запись в changelog MCP.

---

## 6. Отклонённые альтернативы

- **Считать «слот» только микро-ячейку** — ломает текущий язык MCP/«смонтировать в `pfd`» и [0047] без миграции.
- **Переименовать `CockpitSlotIds.Pfd` в `PfdColumn`** и ввести `PfdCell_0`… — возможно в будущем, но **высокая** цена; до консенсуса пользуемся §2–4.
- **Смешивать** строку WH и host slot — уже раскрыто в [0068]; не повторяем.

---

## 7. Открытые вопросы (не блокируют Proposed)

- Точный wire: один `instrument_id` «PFD deck host» vs массив дескрипторов **в** `MainWindowHostSurfaceFrame` — **по итогам** PoC композитора.
- Стабильные `cell_id` для PFD-деска vs пресетов TOML — эволюция [0050] / [0063 § deck в пресетах].

---

<a id="adr0088-glossary"></a>

## Краткий глоссарий (для копипаста)

| Термин | Смысл |
|--------|--------|
| **Страница оболочки (Page)** | `MfdShellPage` — **какой** контент **вторичного контура** в колонке Mfd (чат, терминал, …); **выше** host slot [0047]; [0017](0017-multi-window-workspace-and-agent-surfaces.md). |
| **Host slot** | `slot_id` в `CockpitInstrumentDescriptor` / `CockpitSlotIds` — **точка крепления в кадре хоста**; v1: **вся** колонка PFD (или MFD), один выбранный `instrument_id`. |
| **Регион / якорь внимания** | PFD, MFD — **зона кокпита** [0021]; география часто 1:1 с host slot’ом, семантика — «роль зоны». |
| **Instrument deck** | Именованная композиция **внутри** якоря; `SemanticAnchorId` = регион. |
| **Ячейка deck (cell)** | Позиция в раскладке **регионального** deck; **не** `CockpitSlotIds` в v1. |
| **Блок / внутренняя ячейка прибора** | Единица **внутри** одного `instrument_id` (граф, легенда, полоса индикаторов); композитор **прибора**, не host deck. |

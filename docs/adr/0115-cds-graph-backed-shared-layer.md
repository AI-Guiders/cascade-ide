# ADR 0115: CDS — общий слой graph-backed приборов (реализация в кабине, не IDS)

**Статус:** Accepted  
**Дата:** 2026-05-14  
**Обновлено:** 2026-05-14 — в коде: `IGraphDataSource`, адаптер workspace navigation map. Подробности — [§ История](#adr0115-history).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Канал → CDS → композитор → поверхность |
| [0067](0067-graph-backed-surfaces-contract.md) | Контракт семейства graph-backed поверхностей |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS — оверлеи оболочки (**не** путать с CDS) |
| [0055](0055-skia-instrument-composition-pipeline.md) | Skia-этапы композиции |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Слоты и дескрипторы приборов |
| [0065](0065-instrument-categories-domain-taxonomy.md) | Ось `graph_kind` |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Ось `relation_kind` |
| [0113](0113-hci-semantic-map-orientation-layer.md) | HCI, `edge_provenance` |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU → DTO канала (§4 в тексте) |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | `SemanticMapInputSnapshot` и интеграция индекса |

## Проблема

**[0067](0067-graph-backed-surfaces-contract.md)** задаёт **измерения контракта** (модель, взаимодействие, provenance, `relation_kind`, синхронизация с workspace, …) для *всех* graph-backed поверхностей, но **не фиксирует явно**, в каком **продуктовом контуре** живёт общая реализация: кабина (**CDS**) vs оверлей IDE (**IDS**). В дискуссии легко появляется третий «DisplaySystem» (условный *GraphDisplaySystem*), который **размывает** границу **[0079 § CDS vs IDS](0079-ide-display-system-ids-overlay-pipeline.md#adr0079-cds-vs-ids)**.

Нужен ADR уровня **размещения**: где в архитектуре собирается **переиспользуемый код** общих частей graph-backed приборов и как он стыкуется с уже принятой цепочкой **0036**.

---

## Решение

### 1. Инвариант размещения

**Общий слой graph-backed приборов** — это **подсистема внутри контура кабины (CDS)**, а не параллель **IDS** и не отдельный верхнеуровневый «Display System».

- Графовые экраны продукта (Semantic Map, GitMap, будущие dependency graphs и т.д.) остаются **приборами / регионами поверхности** в смысле **[0021](0021-pfd-mfd-cockpit-attention-model.md)** и проходят ту же **логическую** цепочку **[0036](0036-cds-channel-compositor-surface-pipeline.md)**: данные и намерение — в **канале** / связанных сервисах; маршрутизация по зонам — **CDS**; свёртка в разметку слота — **композитор**; **поверхность** — Avalonia/Skia-хост в `Cockpit/Surface` (или эквивалент), без импорта `IdeDisplay` из `Cockpit/` (**CASCOPE016**).

### 2. Что относится к «общему слою» (целевое разделение)

**Общий слой** (имя в коде — решение реализации; рабочее пространство имён в духе `Cockpit.*` + суффикс `Graph` / `GraphSurface` / `GraphBacked`):

- общие протоколы **документа графа** (узел/ребро, ключи сессии, опциональные поля **`graph_kind`**, **`edge_provenance`**, **`relation_kind`** — см. **0065**, **0113**, **0114**);
- **абстракция источника данных** для поверхности: **`IGraphDataSource`** в `CascadeIDE.Cockpit.Graph` — метод `BuildNavigationJson` принимает **`CodeNavigationMapJsonRequest`** (v0: wire JSON карты намерений / workspace navigation); конкретные провайдеры — **адаптеры** (сейчас `WorkspaceNavigationMapContextJsonDataSource` в `Features/WorkspaceNavigation/Application`) без привязки generic-каркаса к Roslyn в VM;
- **повторяемые** куски **interaction** (пан/зум/hit-test policy, лимиты Dark Cockpit), где домен не уникален;
- **точка подключения** к **[0055](0055-skia-instrument-composition-pipeline.md)** (Intent / Declutter / Layout / Render), не дублируя доменную загрузку графа;
- соглашения по **command routing** и **observability** для агента, совместимые с таблицей измерений **0067**.

**Остаётся доменно-специфичным** у каждого `graph_kind` / инструмента:

- **реализация** источника (реализация `IGraphDataSource` / композиция нескольких источников: Roslyn, Git, HCI-кандидаты, …);
- выбор **layout engine** и визуальная семантика узла (иконки, подписи, цвет по домену);
- обработчики команд «перейти к исходнику» там, где семантика **не** выводится только из **`relation_kind`** + стандартной таблицы действий.

**Нюанс «поверхности пофиг»:** каркасу действительно **безразличен** конкретный backend, пока документ графа и метаданные рёбер удовлетворяют контракту. Но **не** на 100%: доверительный UX (подписи/иконки по **`edge_provenance`**), дефолтные действия по **`relation_kind`**, ограничения навигации — часто оформляются **в общем слое** политиками и подсказками, а не только внутри адаптера; иначе каждый адаптер дублирует одно и то же.

### 4. Связь с CCU и внешними входами

Свёртка сырья в DTO канала **до** или **вокруг** CDS остаётся по **[0097](0097-cockpit-compute-units-transport-to-channel-dto.md)**. Входы вроде **`SemanticMapInputSnapshot`** ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md), [0113](0113-hci-semantic-map-orientation-layer.md)) **питают** канал/композитор graph-backed прибора; **не** заменяют graph-backed interaction layer.

### 5. Явное «не делаем»

- **Не** вводим **IdeDisplay.Graph*** и не смешиваем граф кабины с **IDS**, пока нет отдельного продукта «граф только как глобальный оверлей» (тогда — отдельный ADR и осознанное исключение).
- **Не** дублируем текст **0067**: этот ADR про **где живёт код общего слоя**, а не про повторение таблицы измерений.

---

## Последствия

- Ревью новых графовых фич: вопрос «куда кладём общий код?» → **внутри CDS/Cockpit**, ссылка на **0115**; «какие измерения соблюсти?» → **0067**.
- Поиск дублирования viewer между Semantic Map и GitMap → вынос в общий пакет **в границах Cockpit**, а не в `IdeDisplay`.

---

## Rollout (эскиз)

1. Документ (этот ADR) как стабильная ссылка для дизайн-ревью.  
2. Strangler: **v0** — `IGraphDataSource` + адаптер на существующий `WorkspaceNavigationMapContextJsonBuilder`; refresh PFD через интерфейс. Далее — вынос общих частей композитора/политик по мере второго потребителя (**0067**).  
3. По мере стабилизации — уточнить неймспейс и CASCOPE-правила в `CascadeIDE.ArchitectureAnalyzers` при необходимости (отдельный мини-ADR или правка существующих guardrails).

---

## История изменений

<a id="adr0115-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-05-14 | абстракция источника графа (`IGraphDataSource` / эквивалент) в общем слое; адаптеры — доменно. |
| 2026-05-14 | в коде: `CascadeIDE.Cockpit.Graph.IGraphDataSource`, `CodeNavigationMapJsonRequest`, адаптер `WorkspaceNavigationMapContextJsonDataSource`; `MainWindowViewModel` берёт JSON через интерфейс. |

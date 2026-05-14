# ADR 0115: CDS — общий слой graph-backed приборов (реализация в кабине, не IDS)

**Статус:** Proposed  
**Дата:** 2026-05-14  

**Связь:** цепочка кабины **[0036](0036-cds-channel-compositor-surface-pipeline.md)** (канал → CDS → композитор → поверхность). Контракт семейства графов **[0067](0067-graph-backed-surfaces-contract.md)**. **Не** путать с **[0079](0079-ide-display-system-ids-overlay-pipeline.md)** (IDS — оверлеи оболочки). Skia-этапы **[0055](0055-skia-instrument-composition-pipeline.md)**. Слоты и дескрипторы **[0047](0047-cockpit-instrument-descriptor-and-slot-composition.md)**. Оси данных **[0065](0065-instrument-categories-domain-taxonomy.md)** (`graph_kind`), **[0114](0114-graph-edge-relation-kind-taxonomy.md)** (`relation_kind`), **[0113](0113-hci-semantic-map-orientation-layer.md)** (`edge_provenance` / HCI).

---

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
- **повторяемые** куски **interaction** (пан/зум/hit-test policy, лимиты Dark Cockpit), где домен не уникален;
- **точка подключения** к **[0055](0055-skia-instrument-composition-pipeline.md)** (Intent / Declutter / Layout / Render), не дублируя доменную загрузку графа;
- соглашения по **command routing** и **observability** для агента, совместимые с таблицей измерений **0067**.

**Остаётся доменно-специфичным** у каждого `graph_kind` / инструмента:

- источник узлов и рёбер (Roslyn, Git, HCI-кандидаты, …);
- выбор layout engine и визуальная семантика узла;
- конкретные команды «перейти к исходнику» в терминах домена.

### 3. Связь с CCU и внешними входами

Свёртка сырья в DTO канала **до** или **вокруг** CDS остаётся по **[0097](0097-cockpit-compute-units-transport-to-channel-dto.md)**. Входы вроде **`SemanticMapInputSnapshot`** ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md), [0113](0113-hci-semantic-map-orientation-layer.md)) **питают** канал/композитор graph-backed прибора; **не** заменяют graph-backed interaction layer.

### 4. Явное «не делаем»

- **Не** вводим **IdeDisplay.Graph*** и не смешиваем граф кабины с **IDS**, пока нет отдельного продукта «граф только как глобальный оверлей» (тогда — отдельный ADR и осознанное исключение).
- **Не** дублируем текст **0067**: этот ADR про **где живёт код общего слоя**, а не про повторение таблицы измерений.

---

## Последствия

- Ревью новых графовых фич: вопрос «куда кладём общий код?» → **внутри CDS/Cockpit**, ссылка на **0115**; «какие измерения соблюсти?» → **0067**.
- Поиск дублирования viewer между Semantic Map и GitMap → вынос в общий пакет **в границах Cockpit**, а не в `IdeDisplay`.

---

## Rollout (эскиз)

1. Документ (этот ADR) как стабильная ссылка для дизайн-ревью.  
2. Strangler: первый выделенный модуль общего слоя при появлении второго потребителя, уже отвечающего **0067**.  
3. По мере стабилизации — уточнить неймспейс и CASCOPE-правила в `CascadeIDE.ArchitectureAnalyzers` при необходимости (отдельный мини-ADR или правка существующих guardrails).

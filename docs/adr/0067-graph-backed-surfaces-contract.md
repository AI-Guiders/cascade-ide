# ADR 0067: Graph-backed surfaces — общий контракт для семейства графовых экранов

**Статус:** Accepted  
**Дата:** 2026-04-19  
**Принято:** 2026-04-19  
**Обновлено:** 2026-04-19 — ключевая мысль «операционная surface», ограничения, доп. требования, seed для агента (EN). 2026-05-14 — измерения **Edge / node provenance** ([0113 § оси](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)) и **Relation kind** ([0114](0114-graph-edge-relation-kind-taxonomy.md)); обе ортогональны `graph_kind`. 2026-05-14 — ссылка на размещение общего слоя реализации в **CDS** ([0115](0115-cds-graph-backed-shared-layer.md)), не IDS.

**Связь:** [0065](0065-instrument-categories-domain-taxonomy.md) (ось `graph_kind`, категории инструментов, Semantic Map vs другие графы), [0113](0113-hci-semantic-map-orientation-layer.md) (оси **provenance** и сводка **трёх** осей с `relation_kind`), [0114](0114-graph-edge-relation-kind-taxonomy.md) (каталог **`relation_kind`**: наследует, ссылается на, …), [0115](0115-cds-graph-backed-shared-layer.md) (**где** в коде кабины живёт общий слой реализации graph-backed приборов — **CDS**, не IDS), [0062](0062-git-submodules-semantic-map-subgraph.md) (GitMap — отдельный домен данных, общий пайплайн отрисовки), [0053](0053-semantic-map-control-flow-pfd.md) / [0056](0056-semantic-map-pipeline-adoption.md) (Semantic Map, внедрение pipeline), [0055](0055-skia-instrument-composition-pipeline.md) (Intent → Declutter → Layout → Render), [0039](0039-workspace-navigation-affordances.md) (навигация, MCP, subgraph), [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) (инструмент, слот, поверхность), [0021](0021-pfd-mfd-cockpit-attention-model.md) (зоны внимания). [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) — хром IDE vs кокпит: контракт ниже про **поверхность инструмента** в кабине, не про `ModalOverlay`.

---

## Контекст

В IDE появляется **не один** графовый экран, а **семейство** поверхностей, где пользователь и агент работают с **графом** как с главным объектом: Semantic Map (намерения кода / control flow), GitMap / submodules ([0062](0062-git-submodules-semantic-map-subgraph.md)), возможные будущие графы (зависимости, топология сервисов и т.д.).

Если каждый сценарий вести **отдельным ad-hoc viewer**, неизбежно дублируются:

- модель данных и идентичность узла/ребра в workspace;
- взаимодействие (масштаб, прокрутка, hit-test, жесты);
- семантика навигации («перейти к символу», «раскрыть subgraph», «синхронизировать с редактором»);
- абстракция раскладки (чтобы не копировать layout между доменами);
- выделение, фокус, клавиатурный контур;
- согласование с остальным workspace (дерево решения, открытые файлы, MCP, инвалидация при смене ветки/git).

Нужен **общий архитектурный класс UI** — **graph-backed surface** — и явный **контракт** по измерениям ниже; **различаются домен графа и источник данных**, а не заново изобрётся колесо для каждого use case.

Задача уровня **архитектуры платформы**, а не просьба «нарисуй узлы и рёбра».

<a id="adr0067-key"></a>

### Ключевая мысль: граф — не только визуализация

**Graph** в этой IDE — это **не** синоним картинки для отчёта.

**Graph-backed surface** — это **навигационная поверхность IDE** на тех же правах, что **редактор**, **терминал**, **диагностики**, **обозреватель решения**: не декоративная диаграмма и не export-only preview, а **операционный surface**, через который и человек, и агент могут исследовать структуру, выбирать узлы, переходить к исходникам, фильтровать, фокусироваться и выполнять действия. Визуализация — следствие модели и контракта, а не наоборот.

---

## Решение

Зафиксировать понятие **graph-backed surface** (рабочее имя): **инструмент или фрагмент UI**, в котором **первична** работа с **ориентированным (или помеченным) графом** как с объектом навигации и действий, согласованная с кокпитом и workspace по единым правилам; отрисовка — один из слоёв, не определение поверхности. **Размещение общей реализации** переиспользуемых частей этого класса в продукте — в контуре **CDS / Cockpit**, не в **IDS**; см. **[0115](0115-cds-graph-backed-shared-layer.md)**.

<a id="adr0067-not"></a>

### Ограничения: что это не является

Чтобы реализация и внешние агенты не «уезжали» в одноразовый viewer:

- это **не** «diagram control» как полный ответ (диаграмма без модели навигации и синхронизации с workspace);
- это **не** слой **только** rendering;
- это **не** фича под **один** CFG / один use case;
- это **расширяемая платформа** для **нескольких** graph-backed инструментов (Semantic Map, GitMap, будущие графы зависимостей / связей и т.д.).

<a id="adr0067-extra"></a>

### Дополнительные требования к платформе

Помимо [таблицы измерений](#adr0067-dimensions), к целевому контракту относятся:

- **Единая абстракция документа графа** (например **GraphDocument** или эквивалент): узлы/рёбра, метаданные, привязка к домену и `graph_kind` ([0065](0065-instrument-categories-domain-taxonomy.md)).
- **Единая семантика навигационных команд**: из узла — к **исходникам**, **деталям**, **связанному подграфу** / related graph (формулировки могут отличаться по домену, **канал действий** — один).
- **Сериализуемое состояние** surface: выделение, viewport, фильтры — для восстановления сессии, тестов и согласования с остальными панелями.
- **Agent introspection**: состояние surface **читаемо** агентом и командами (MCP / `ide_*`), без единственной опоры на пиксели.
- **Разные layout engines** как подключаемые стратегии **без** поломки модели документа (см. Layout abstraction и [0055](0055-skia-instrument-composition-pipeline.md)).

**Инвариант:** Semantic Map, GitMap и последующие графовые экраны — **представления одного класса** в смысле контракта; они **не обязаны** делить один `instrument_id` или один JSON wire-формат, но **обязаны** быть сопоставимы по измерениям [§2](#adr0067-dimensions), чтобы команда и агент могли переносить ожидания между экранами.

Реализация допускается **поэтапно** (strangler): сначала два потребителя (например Semantic Map + GitMap) выявляют общий минимум; контракт в коде (`interface` / набор протоколов) расширяется без нарушения ADR.

<a id="adr0067-dimensions"></a>

### Измерения контракта (что явно согласовывается)

| Измерение | Вопрос, на который отвечает слой | Примечание |
|-----------|-----------------------------------|------------|
| **Data model** | Что такое узел и ребро в **этом** домене; стабильный **ключ** в пределах сессии; связь с `graph_kind` и категорией инструмента ([0065](0065-instrument-categories-domain-taxonomy.md)). | Домены ортогональны: код vs git-топология — разные графы, один класс поверхности. |
| **Edge / node provenance** | На каком **источнике правды** держатся связи и узлы для этого экрана: символьная модель (Roslyn), эвристика workspace (MSBuild), полнотекст/vec по корпусу (HCI), композит (например HCI → Roslyn). | Ортогонально **`graph_kind`**: тот же вид карты может сочетать слои с разным provenance; таблица и имена — **[0113 § оси](0113-hci-semantic-map-orientation-layer.md#adr0113-axes)**. |
| **Relation kind** | Какое **отношение** между сущностями утверждает ребро (наследует, ссылается на, partial peer, текстовое совпадение, …). | **[0114](0114-graph-edge-relation-kind-taxonomy.md)**; ортогонально **`graph_kind`** и **provenance**. |
| **Interaction model** | Пан, зум, «перетаскивание» вида, hit-test, ограничения FPS/Dark Cockpit ([0021](0021-pfd-mfd-cockpit-attention-model.md) §6). | Общие паттерны; детали могут отличаться по инструменту. |
| **Navigation semantics** | Что значит «перейти», «открыть», «запросить subgraph», как это стыкуется с MCP и агентом ([0039](0039-workspace-navigation-affordances.md)). | Семантика **действий**, не только отрисовка. |
| **Layout abstraction** | Где граница между данными графа и геометрией: этапы [0055](0055-skia-instrument-composition-pipeline.md); сменяемые **layout engines** под вид графа без дублирования Render. | GitMap и CFG не обязаны иметь один layout engine; обязаны иметь **одинаковую точку подключения** в pipeline. |
| **Selection / focus model** | Один или несколько выбранных узлов; фокус клавиатуры; связь с «текущим» узлом для агента и UI. | Нужна согласованность с остальным кокпитом. |
| **Command routing** | Команды IDE, контекстное меню, хоткеи доходят до surface предсказуемо; не дублировать разрозненные обработчики без политики. | Связь с [0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md). |
| **Deep-linking / воспроизводимость** | Переход по ссылке на узел/фильтр/view state; согласование с сериализуемым состоянием. | Не обязательно в v1 полностью; направление зафиксировано. |
| **Sync with workspace** | Связь выделения с редактором, деревом решения, git-состоянием; инвалидация при смене файла, ветки, решения; без второго источника правды. | Явные события или снимки, не скрытые глобалы. |
| **Observability (agent)** | Снимок состояния surface для агента и автоматизации; не только «что нарисовано», но и выделение, фокус, доменные ключи узлов. | См. [доп. требования](#adr0067-extra). |

Контракт **не** требует одного общего типа `Graph` в памяти для всех доменов — требует **сопоставимости** протоколов и **отсутствия** несогласованных one-off viewer без обоснования.

<a id="adr0067-agent-prompt"></a>

### Стартовый prompt для агента (English)

Текст ниже можно использовать как единую отправную точку для проектирования и ревью (Cursor и др.):

We need to design a **reusable graph-surface architecture** for the IDE, not a one-off graph viewer. Existing and upcoming features such as Semantic Map (CFG), Git submodules, and future dependency / relationship graphs should all fit the **same conceptual model**.

A graph in this IDE is **not** just a visualization; it is an **interactive workspace surface** with navigation, focus, selection, commands, synchronization with other panes, and **agent-readable state** — on par with the editor, terminal, diagnostics, and solution explorer.

**This is not:** only a diagram control; only a rendering layer; a single feature for one CFG use case. **It should be** an **extensible platform** for multiple graph-backed tools.

Please propose an architecture for a generic graph-surface framework, including where appropriate:

- graph **document** model and node/edge metadata;
- **layout** abstraction and pluggable layout engines without breaking the document model;
- **interaction** contract (pan/zoom/hit-test as needed);
- **command routing** and unified navigation semantics (e.g. node → source / details / related graph);
- **selection/focus** and synchronization with the rest of the IDE workspace;
- **deep-linking** and **serializable** surface state;
- **agent introspection** over surface state.

Cross-check with ADR **0067** (graph-backed surfaces) and related ADRs on `graph_kind`, Semantic Map, GitMap, and the Skia pipeline.

---

## Последствия

- Новые графовые фичи проходят проверку: **какое измерение** уже покрыто общим слоем, **что** доменно-специфично.
- Документация и ревью могут ссылаться на **graph-backed surface** и таблицу измерений вместо «ещё одна карта».
- Пайплайн [0055](0055-skia-instrument-composition-pipeline.md) остаётся **общим местом** для Layout/Render; источники графа подключаются как **адаптеры**, а не форки viewer.

---

## Не-цели (текущая фаза)

- Единый **универсальный** layout для всех видов графов в одном релизе.
- Полная реализация всех измерений в коде до появления второго и третьего потребителя контракта — допустим **минимальный v0** и расширение.
- Замена [0062](0062-git-submodules-semantic-map-subgraph.md) или [0065](0065-instrument-categories-domain-taxonomy.md): они уточняют **домен**; этот ADR уточняет **класс UI**.

---

## Альтернативы (кратко)

| Вариант | Минус |
|--------|--------|
| Отдельный viewer на каждый граф | Дублирование, расхождение навигации и синхронизации с workspace |
| Один жёсткий `GraphView` control на все данные | Не гнётся под разные домены и layout; тормозит эволюцию |
| Только гайд в Markdown без ADR | Нет стабильной ссылки для ревью и онбординга |

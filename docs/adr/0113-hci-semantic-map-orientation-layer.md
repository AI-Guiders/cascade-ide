# ADR 0113: HCI и Semantic Map — слой ориентации (не граф)

**Статус:** Proposed  
**Дата:** 2026-05-14  
**Обновлено:** 2026-05-14 — оси **`graph_kind`**, **provenance** и **`relation_kind`** ([0114](0114-graph-edge-relation-kind-taxonomy.md)); линза «б… Подробности — [§ История](#adr0113-history).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Граница Semantic Map / слой B; интеграция IDE |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid index, MCP |
| [0039](0039-workspace-navigation-affordances.md) | Карта как поверхность навигации |
| [0053](0053-semantic-map-control-flow-pfd.md) | Control flow на PFD |
| [0067](0067-graph-backed-surfaces-contract.md) | Контракт graph-backed поверхностей |
| [0065 §6](0065-instrument-categories-domain-taxonomy.md#adr0065-p6) | Ось `graph_kind` |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Семантика `relation_kind` на ребре |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | `SemanticMapInputSnapshot` в CCU |

## Проблема

В **[0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** в одном документе смешаны: подключение Core, оркестратор, свежесть, DataBus, MFD INDEX — и краткая формулировка роли HCI рядом с **Semantic Map**. Читатель нередко читает это как «HCI обогащает **граф** карты», хотя ADR уже противопоставляет слой B и канонический граф; явного **контракта ориентации** (что именно показываем, чего не делаем, какие DTO) в одном месте не хватает.

---

## Решение

### 1. Роль HCI относительно Semantic Map

**Hybrid Codebase Index (HCI)** в связке с Semantic Map даёт только **слой ориентации по кодовой базе**:

- топовые **текстовые** (FTS) и при включении — **векторные** попадания с путями, сниппетами и явным **`hit_kind`** ([0105](0105-hybrid-codebase-index-for-csharp-web.md));
- **метаданные индекса** (готовность, версия формата, scope) как контекст доверия к строке ориентации;
- опционально в перспективе — **вход для declutter** подсветки карты (фильтрация «шума» в UI), **без** подмены символьной топологии графа.

HCI **не** является источником рёбер CFG, **не** заменяет **Roslyn** для go-to-definition / символьной связности и **не** смешивает `hit_kind` с фактами графа.

<a id="adr0113-axes"></a>

### 1a. Три ортогональные оси (`graph_kind`, `edge_provenance`, `relation_kind`)

Чтобы не смешивать **какой граф рисуем**, **на чём основаны рёбра** и **что ребро означает**, держим разводку явно (третья ось — **[0114](0114-graph-edge-relation-kind-taxonomy.md)**):

| Ось | Вопрос | Где зафиксировано | Примеры |
|-----|--------|-------------------|---------|
| **Тип / домен графа** (`graph_kind`) | Какой **смысловой** подграф в кабине: намерения кода, связанные файлы, дерево модулей Git, … | **[0065 §6](0065-instrument-categories-domain-taxonomy.md#adr0065-p6)** | `code_intent_code_navigation_map`, `related_files`, `repository_module_tree` |
| **Происхождение связей** (`edge_provenance`) | **Откуда** взята доказуемость «узел A связан с B»: символьная модель, эвристика workspace, полнотекст / vec по корпусу, цепочка из нескольких источников | **0113** + измерение в **[0067](0067-graph-backed-surfaces-contract.md#adr0067-dimensions)** | `symbolic_roslyn`, `workspace_navigation_msbuild`, `hci_fulltext`, `hci_vector`, `composite_hci_then_roslyn` |
| **Тип отношения** (`relation_kind`) | **Какое отношение** между сущностями мы показываем: наследует, ссылается на тип, partial peer, текстовое совпадение, … | **[0114](0114-graph-edge-relation-kind-taxonomy.md#adr0114-three-axes)** | `inherits`, `references_type`, `partial_peer`, `textual_name_match`, … |

**Зачем:** один и тот же `graph_kind` (например карта намерений) может **визуально** соседствовать с данными разного происхождения: CFG/Roslyn — канон для control flow; HCI — быстрый **корпусный** слой («где в репо всплывает имя типа / фрагмент» → черновой **referenced-by по тексту**), затем уточнение через Roslyn. При этом **смысл** связи для UX и агента задаётся **`relation_kind`** (например `references_type` у Roslyn vs `textual_name_match` или `candidate_symbol_reference` у HCI), а не только `edge_provenance`. Отдельный инструмент «граф только из HCI» — по-прежнему отдельное решение и согласование с [0067](0067-graph-backed-surfaces-contract.md).

### 2. Продуктовый контур «карта + HCI»

- **PFD / MFD:** краткая **строка ориентации** (или отдельный микро-канал), согласованная с тем же scope и `databasePath`, что оркестратор HCI ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)).
- **Следующий шаг для пользователя/агента:** из попадания HCI — осмысленные действия уровня **Roslyn** (definition, usages, diagnostics), как в эскизе **«Hybrid search → Roslyn точность»** в 0106 / дорожной карте 0105 — без объявления попадания HCI «истиной символа». Продуктовая линза **«быстро referenced-by по корпусу»** укладывается в **`hci_fulltext` / `hci_vector`** на оси provenance; символьный **referenced-by** — **`symbolic_roslyn`**; **смысл** связи при этом размечается **`relation_kind`** ([0114](0114-graph-edge-relation-kind-taxonomy.md)), чтобы не путать `references_type` с `textual_name_match`.

### 3. DTO и CCU (целевое состояние)

Нормализованный вход для graph-backed поверхностей после стабилизации границы CCU — **`SemanticMapInputSnapshot`** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)): состав полей (топ hits, версия индекса, ошибки запроса, флаги declutter) фиксируется отдельно при внедрении; **0113** задаёт семантику слоя, а не замену 0097.

---

## Не делаем (явные non-goals)

- Не объявляем узлы/рёбра Semantic Map «проиндексированными HCI» без отдельного ADR и согласования с **[0067](0067-graph-backed-surfaces-contract.md)**.
- Не подменяем палитру **`t:`/`m:`** полным HCI без политики из **[0112](0112-command-palette-query-modes-strategy.md)** (там уже разведены ripgrep, FTS и semantic).

---

## Последствия

- **[0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** остаётся точкой входа по **интеграции HCI в CascadeIDE**; деталь контракта «карта ↔ HCI» для читателей и ревью — **0113**.
- Изменения в UI-строке ориентации или в `SemanticMapInputSnapshot` ссылаются на **0113** (и при необходимости на 0097), а не раздувают 0106.

---

## Rollout (эскиз)

1. Зафиксировать в коде/UX только **ориентацию** и ссылки на scope/HCI errors — без подмешивания в граф Roslyn.  
2. По готовности DTO — один PR с `SemanticMapInputSnapshot` + тесты границы CCU; обновить этот ADR до **Accepted · Implemented** для соответствующих пунктов.

---

## История изменений

<a id="adr0113-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-05-14 | оси **`graph_kind`**, **provenance** и **`relation_kind`** ([0114](0114-graph-edge-relation-kind-taxonomy.md)); линза «быстрый referenced-by по корпусу». |

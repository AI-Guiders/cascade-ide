# ADR 0113: HCI и Semantic Map — слой ориентации (не граф)

**Статус:** Proposed  
**Дата:** 2026-05-14  

**Связь:** расширяет и уточняет границу **Semantic Map и слой B** в **[0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)** — без дублирования интеграционного контура IDE. Базовый индекс и MCP: **[0105](0105-hybrid-codebase-index-for-csharp-web.md)**. Карта как поверхность: **[0039](0039-workspace-navigation-affordances.md)**, **[0053](0053-semantic-map-control-flow-pfd.md)**, **[0067](0067-graph-backed-surfaces-contract.md)**. Снимок входа в CCU: **[0097](0097-cockpit-compute-units-transport-to-channel-dto.md)** (`SemanticMapInputSnapshot`).

---

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

### 2. Продуктовый контур «карта + HCI»

- **PFD / MFD:** краткая **строка ориентации** (или отдельный микро-канал), согласованная с тем же scope и `databasePath`, что оркестратор HCI ([0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)).
- **Следующий шаг для пользователя/агента:** из попадания HCI — осмысленные действия уровня **Roslyn** (definition, usages, diagnostics), как в эскизе **«Hybrid search → Roslyn точность»** в 0106 / дорожной карте 0105 — без объявления попадания HCI «истиной символа».

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

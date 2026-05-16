# ADR 0057: Chat surface adoption of Skia composition pipeline

**Статус:** Accepted  
**Дата:** 2026-04-17

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | пакеты уточнений, threading |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | ранняя гипотеза host/render split |
| [0055](0055-skia-instrument-composition-pipeline.md) | общий pipeline |
| [0056](0056-semantic-map-pipeline-adoption.md) | первый внедрённый consumer |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | overview/detail layout и keyboard-first intent для тем поверх pipeline |
---

## Контекст

После внедрения pipeline в Semantic Map ([0056](0056-semantic-map-pipeline-adoption.md)) следующий запланированный consumer — chat surface:

- треды,
- подтверждения (confirmations),
- приоритизация и declutter в режиме высокой плотности событий.

Чат в `MfdShellView` остаётся продуктовой MFD-поверхностью, но канонический surface теперь строится вокруг Skia pipeline snapshot, а не вокруг Avalonia list/tree.  
Новый слой Skia нужен не для "замены ради замены", а для сценариев, где линейная лента не даёт достаточной ситуационной читаемости и не показывает ширину ветвления.

---

## Решение

<a id="adr0057-p1"></a>

### 1) Принять chat surface как следующий pipeline-consumer

Чат переводится на тот же composition-подход из [0055](0055-skia-instrument-composition-pipeline.md):

1. **Intent**: построение модели текущего состояния диалога (треды, pending confirmations, активные ветви).
2. **Declutter**: приоритизация сообщений/подтверждений и компрессия шумовых элементов.
3. **Layout**: раскладка узлов разговора и карточек подтверждений.
4. **Render**: Skia-отрисовка сцены.

<a id="adr0057-p2"></a>

### 2) Зафиксировать single product path через Skia surface

После появления snapshot-композитора:

- `ChatPanelView` остаётся host-контейнером и формой ввода, но не альтернативной лентой;
- продуктовый рендер чата идёт через единый Skia surface;
- legacy Avalonia list-path не считается обязательным fallback.

Avalonia остаётся shell/host-слоем, а не параллельной продуктовой реализацией chat scene.

<a id="adr0057-p3"></a>

### 3) Выделить чатовые intent-единицы как first-class

Минимальный набор доменных сущностей для v1:

- `ThreadNode`,
- `MessageNode`,
- `ConfirmationNode`,
- `DecisionEdge` (`ask`, `confirm`, `resolve`, `supersede`).

Слой layout не должен вычислять эти сущности из UI-дерева; он получает их из Intent-stage.

---

## Последствия

### Плюсы

- Чат становится консистентным с общей моделью Skia-инструментов (0055).
- Треды/подтверждения получают явно управляемую композицию, а не "плоскую ленту с костылями".
- Reuse pipeline-практик, уже проверенных на Semantic Map.

### Минусы

- Увеличивается сложность chat-подсистемы.
- Требуется держать строгие snapshot/contract tests, потому что surface больше не дублируется вторым UI-путём.

---

## Не-цели

- Не возвращать параллельный Avalonia list-path как "страховочный" baseline без новой ADR.
- Не фиксировать здесь итоговый visual language (цвета, типографика, анимации) — это отдельные UX-итерации.
- Не менять MCP-контракты чата в этой ADR без отдельной контрактной фиксации.

---

## План внедрения (минимум)

1. Ввести каркас `ChatSurfaceCompositor` и stage-контракты (`Intent/Declutter/Layout`) в стиле 0055.
2. Поднять intent-слой: `ThreadNode` / `MessageNode` / `ConfirmationNode` / `DecisionEdge` поверх канонической истории диалога.
3. Подключить `ClarificationBatch` / `ClarificationResponse` к реальному chat flow и MCP entrypoints, без строкового схлопывания как единственной правды.
4. Добавить snapshot/contract-тесты композиции и threading/clarification сценариев.

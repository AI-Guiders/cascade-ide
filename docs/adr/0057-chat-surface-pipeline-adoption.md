# ADR 0057: Chat surface adoption of Skia composition pipeline

**Статус:** Accepted  
**Дата:** 2026-04-17

**Связь:** [0031](0031-agent-chat-clarification-batches-and-threading.md) (пакеты уточнений, threading), [0044](0044-avalonia-host-skia-agent-chat-surface.md) (Skia в чате как направление), [0049](0049-skia-surface-rollout-over-avalonia-host.md) (поэтапный rollout), [0055](0055-skia-instrument-composition-pipeline.md) (общий pipeline), [0056](0056-semantic-map-pipeline-adoption.md) (первый внедрённый consumer).

---

## Контекст

После внедрения pipeline в Semantic Map ([0056](0056-semantic-map-pipeline-adoption.md)) следующий запланированный consumer — chat surface:

- треды,
- подтверждения (confirmations),
- приоритизация и declutter в режиме высокой плотности событий.

Текущий чат в `MfdShellView` построен на Avalonia control-tree (`ChatPanelView`) и остаётся рабочим baseline.  
Новый слой Skia нужен не для "замены ради замены", а для сценариев, где линейная лента не даёт достаточной ситуационной читаемости.

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
### 2) Сохранить dual-path на этапе rollout

До стабилизации:

- Avalonia `ChatPanelView` остаётся fallback/каноническим путём;
- Skia chat surface внедряется волнами и включается по флагам rollout.

Это соответствует [0049](0049-skia-surface-rollout-over-avalonia-host.md): host остаётся Avalonia, новый surface-слой подключается постепенно.

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
- Нужен период dual-path поддержки (Avalonia + Skia) до стабилизации UX.

---

## Не-цели

- Не удалять текущий Avalonia-чат немедленно.
- Не фиксировать здесь итоговый visual language (цвета, типографика, анимации) — это отдельные UX-итерации.
- Не менять MCP-контракты чата в этой ADR без отдельной контрактной фиксации.

---

## План внедрения (минимум)

1. Ввести каркас `ChatSurfaceCompositor` и stage-контракты (`Intent/Declutter/Layout`) в стиле 0055.
2. Поднять минимальный intent-слой: thread + confirmation graph из текущей модели чата.
3. Добавить snapshot/contract-тесты композиции (стабильность сцены при одинаковом входе).
4. Включить feature-flag rollout и сравнение с Avalonia baseline на типовых сценариях.


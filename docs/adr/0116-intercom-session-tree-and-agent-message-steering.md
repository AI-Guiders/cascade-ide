# ADR 0116: Intercom — дерево сессии (ветвление) и steer / follow-up при работе агента

**Статус:** Proposed  
**Дата:** 2026-05-15

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom как канал |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Картотека тем, product spine |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Topic cards, overview/detail |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Append-only события чата |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Треды, пакеты уточнений |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | Цикл агента, tool-run |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Присутствие агента в редакторе |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP/MCP, контекст |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Снапшоты для тестов |

**Вне ADR:** [Pi](https://pi.dev/) — KB `kb-open-source-agents-patterns-landscape-v1.md`, секция `pi-dev-coding-agent`.

## Проблема

1. **Линейная лента** Intercom/чата плохо переносит реальную работу: ответвление на идею, возврат к старой линии, «а что если сделать иначе с того места» — без **ветвления** остаётся только глубокий скролл ([0031](0031-agent-chat-clarification-batches-and-threading.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md)).
2. При **долгом tool-run** агента оператору нужно либо **перехватить** ход (новый приоритет), либо **добавить** уточнение **после** текущего шага — без двух режимов всё смешивается в «ещё одно сообщение в ленту», что непредсказуемо для оркестратора ([0038](0038-agent-facade-ai-provider-and-tool-orchestration.md)).
3. Внешние harness (напр. [Pi](https://pi.dev/)) уже разделяют **steer** и **follow-up**; в CIDE это пока не зафиксировано в каноне — риск несовместимых реализаций в UI, ACP и автономном цикле.

---

## Решение (направление)

### 1. Дерево сессии Intercom (session tree)

**Сессия** Intercom хранится и отображается как **дерево** событий/сообщений, а не только как одна хронологическая лента.

| Понятие | Смысл |
|---------|--------|
| **Узел** | Сообщение, системная реплика, граница tool-run, открытие/закрытие clarification batch — по политике [0045](0045-agent-chat-persistence-event-log-and-projections.md) |
| **Ребро** | «продолжение от» — ответ на родителя; при ветвлении у дочерней ветки свой **head** |
| **Ветка (branch)** | Линия от выбранного узла; смена head = «продолжаем с этой точки» |
| **Закладка (bookmark)** | Метка узла для навигации в UI (`/tree`-аналог); не обязана быть head |
| **Rewind / continue from** | Оператор выбирает узел → **новые** события пишутся как потомки **этого** узла (история «справа» от точки ветвления не переписывается) |

**Ортогональность topic cards ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md)):**

- **Topic / thread** — продуктовая «нить» с заголовком и summary на карточке.
- **Session tree** — **физическое** хранение и навигация по всей сессии; тема может ссылаться на **поддерево** или head ветки, но **не заменяет** дерево целиком.

**Persistence (направление, согласовать с [0045](0045-agent-chat-persistence-event-log-and-projections.md)):**

- В payload событий (или `meta.json`) — стабильные `node_id`, `parent_id`, опционально `branch_id`, `kind` (user / assistant / system / tool_boundary).
- Новые типы событий v2+ (эскиз): `branch_created`, `head_moved`, `node_bookmarked` — только при необходимости; минимум v1 — достаточно `parent_id` на `message_added` и политики head в проекции.
- **Экспорт / share:** как у Pi — HTML или gist — **не** цель v1; заложить в модель идентификаторы, чтобы export был возможен позже.

**UI (эскиз):**

- Команда или панель **«дерево сессии»** в Intercom (не PFD): список/граф веток, переход к узлу, «продолжить отсюда».
- Overview topic cards ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)) может показывать **активную ветку** и краткий путь (breadcrumb).

### 2. Steer vs follow-up во время работы агента

Зафиксировать **два режима** исходящего сообщения оператора, пока агент в состоянии **running** (tool-run / стриминг):

| Режим | Семантика | Когда доставляется | Влияние на tool-run |
|-------|-----------|-------------------|---------------------|
| **Steer** (перехват) | Новый приоритет **сейчас** | После **текущего** tool-вызова (или безопасной точки отмены), **прерывает** оставшиеся tools в текущем ходе агента | Оставшиеся запланированные tools **не** выполняются; оркестратор переходит к новому пользовательскому intent |
| **Follow-up** (очередь) | Уточнение **после** завершения текущего хода | Когда агент **закончил** текущий цикл (все tools / ответ) | Текущий ход **не** прерывается; сообщение в очереди на следующий turn |

**UI (ориентир Pi):** `Enter` → steer по умолчанию; `Alt+Enter` (или явный переключатель) → follow-up — **конкретные клавиши** не фиксируются этим ADR, только семантика.

**Инварианты:**

- Режим **виден** в UI (иконка, подпись поля ввода, tooltip) — оператор не должен гадать.
- В **event log** сохраняется `delivery_mode: steer | follow_up | normal` (имя в wire — при реализации).
- **Steer** не подменяет системные **подтверждения** PFD ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) — опасные действия по-прежнему через политику кабины.
- Внешний агент (ACP/Cursor) получает ту же семантику, где транспорт позволяет; иначе — документированное ограничение ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md)).

**Связь с presence ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md)):** steer может сопровождаться сбросом «агент пишет» / отменой pending edit — детали в ADR реализации, не здесь.

### 3. Наблюдаемость (агент и MCP)

- Снапшот Intercom для MCP/тестов ([0008](0008-mcp-contracts-and-testable-infrastructure.md)) включает: `active_branch_id`, `head_node_id`, опционально список bookmark, **очередь follow-up** (если есть).
- При **steer** в снапшоте фиксируется факт прерывания хода (для воспроизводимости в тестах).

---

## Последствия

- [0045](0045-agent-chat-persistence-event-log-and-projections.md) при реализации ветвления — расширить схему событий и проекций, не второй «файл правды».
- [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) / [0096](0096-intercom-topic-card-summary-and-product-spine.md) — карточки тем ссылаются на узлы дерева, summary не дублирует всё дерево.
- [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) — оркестратор обязан понимать steer (отмена оставшихся tools).
- Референс Pi остаётся в KB; канон продукта — этот ADR.

## Не цели (v1)

- Полный графовый редактор дерева в стиле git log --graph.
- Синхронизация веток между несколькими пользователями (мультиплеер Intercom).
- Обязательный export/share в v1.
- Замена **topic cards** деревом — только дополнение.

## Rollout (эскиз)

1. **Документ (этот ADR)** + ссылка из [0080 § идеи развития](0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities) при необходимости.
2. **Persistence:** `parent_id` + head в проекции; UI «продолжить от узла» без полного `/tree`.
3. **Steer/follow-up:** поле ввода + event `delivery_mode` + минимальная поддержка в одном оркестраторе ([0038](0038-agent-facade-ai-provider-and-tool-orchestration.md)).
4. MCP snapshot полей ветки — после стабилизации модели.

## Открытые вопросы

1. Ветвление на уровне **всей сессии** vs отдельное дерево **на тему** — v1 предлагает **сессию**; per-topic деревья — если появится запрос.
2. Steer при **незавершённом** streaming-токене ответа — политика обрезки partial message (отдельная спецификация).

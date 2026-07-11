# ADR 0172: Session graph habitat — Intercom как рабочая память сессии

**Статус:** Proposed (концепт / north-star)  
**Дата:** 2026-07-10  
**Обновлено:** 2026-07-11 — worklines, spin-off, agent materialization; design thesis; API boundary (stateless FM)

## Резюме

Часть операторов живёт **почти целиком в Intercom**; редактор — по attach/reveal. Их stance — **conversation-first** [0120](0120-primary-work-surface-intercom-or-editor.md).

**Моат CIDE — не «ещё один чат»**, а **нелинейная сессия**: темы, ветки, rewind, scope на экране [0031](0031-agent-chat-clarification-batches-and-threading.md), [0116](0116-intercom-session-tree-and-agent-message-steering.md). Линейная flat feed — **проекция одной ветки**, не единственная правда.

[0171](0171-presentation-tiers-compact-vs-cockpit.md) задаёт tier (compact/cockpit). **0172** задаёт **habitat** для conversation-first: **Session graph canvas** в Forward, код on demand.

**Принято направление (концепт):**

1. **`habitat = session-graph`** (или `conversation`) — Forward = Intercom canvas (scope + worklines + tree/timeline).
2. **Topics = worklines index** — параллельные `ThreadNode` [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md); **не** аналог Cursor New Chat.
3. **Detail default** — scope strip + **Tree | Timeline**; flat feed только для **выбранной ветки** [0170](0170-intercom-feed-readability-mlp.md).
4. **Composer modes:** continue · **steer** · **follow-up** [0116](0116-intercom-session-tree-and-agent-message-steering.md).
5. **Harness неизменен** [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md).

## Design thesis

Классические IDE заточены под **написание кода**: редактор в центре, а сопутствующее — решения, intent, история рассуждения, «почему так» — размазано по боковым панелям, файлам в репо и внешним тредам, и **читается плохо**. В agentic-цикле **писать код в основном берёт на себя агент**; работа человека смещается к **пониманию, направлению и фиксации решений**. Редактор не исчезает — он для проверки и точечных правок, когда нужно руками.

Session graph habitat — ответ на эту ось: Intercom — не «ещё один чат», а **инфраструктура восприятия сессии** (ветки, scope, worklines, batches). Moat — удобство **читать и управлять** нелинейной работой, а не паритет линейной ленты с Cursor.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Пакеты уточнений; обзор размаха; ветвления |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Append-only события; проекции |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Topic overview/detail/back |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Сводка; spine ортогонален main |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Session tree; rewind; steer/follow-up |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | `primary_work_surface = intercom` |
| [0126](0126-intercom-inspect-slash-and-compact-chrome-status.md) | `/topic tree`, inspect |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Harness ≠ habitat |
| [0170](0170-intercom-feed-readability-mlp.md) | Comfortable flat feed внутри ветки |
| [0171](0171-presentation-tiers-compact-vs-cockpit.md) | Compact ≠ session-graph default |

---

## Дифференциация от линейного agent chat (Cursor и аналоги)

| | Linear agent chat | Session graph (CIDE) |
|--|-----------------|----------------------|
| Единица «нового» | New Chat / New Agent | **Fork branch** / новая **workline** в той же сессии |
| История | Хронология | **Дерево** + хронология как проекция |
| Восстановление контекста | Скролл | **Scope strip** + tree |
| Уточнения плана | Одна строка | **Clarification batch** [0031](0031-agent-chat-clarification-batches-and-threading.md) |
| Долгий tool-run | Сообщение в ленту | **Steer** vs **follow-up** [0116](0116-intercom-session-tree-and-agent-message-steering.md) |
| Продуктовая нить | — | **Spine** [0096](0096-intercom-topic-card-summary-and-product-spine.md) |

**Anti-pattern moat loss:** topics как список чатов + только flat feed в detail → **паритет с Cursor без нелинейности**.

---

## Контекст

### Три оси (не смешивать)

```text
Tier (пространство)   : compact | cockpit
Forward [0120]        : intercom | editor
Stance                : session-graph-first | code-first
```

### Оператор session-graph-first

| ~99% | Intercom canvas: worklines, scope, tree/timeline, composer |
| Редко | Editor reveal из attach |
| Фон | Solution warmup; **не** SE tree вместо чата после `load_solution` |

---

## Решение

### 1. Session graph canvas (не «lanes + feed»)

```text
┌─ Scope: N branches · open batch · last decision @msg ─────────┐
│ Worklines │  [ Tree ] [ Timeline ]                              │
│  index    │   graph of selected topic / branch                   │
│  (rows)   │   + flat feed (THIS branch only, measure cap)        │
├───────────┴──────────────────────────────────────────────────────┤
│ Composer: Continue | Steer | Follow-up  +  slash                 │
└──────────────────────────────────────────────────────────────────┘
              Code · Terminal — on demand (MFD / mon2 / reveal)
```

### 2. Слои UI

| Слой | Назначение | Cursor-like? |
|------|------------|--------------|
| **Worklines index** | Параллельные темы; строка + branch count + open Q | Внешне как channel list |
| **Scope strip** | Снимок сессии/темы без скролла | **Нет** |
| **Tree view** | Ветки, rewind, continue from | **Нет** |
| **Timeline view** | Flat feed выбранной ветки | Да (гигиена читаемости) |
| **Spine** | Ортогональная продуктовая линия | **Нет** |

### 3. Инварианты

| # | Инвариант |
|---|-----------|
| S1 | Канон — **event log / session tree** [0045](0045-agent-chat-persistence-event-log-and-projections.md); лента — проекция |
| S2 | Forward default = **Intercom** (`primary_work_surface = intercom`) |
| S3 | Overview worklines = **строки** (title + branches + summary), не hero cards |
| S4 | Detail **не** только timeline; есть **Tree** и scope |
| S5 | **Continue from here** на узле → новые события как потомки [0116](0116-intercom-session-tree-and-agent-message-steering.md) |
| S6 | `load_solution` не переключает на SE/Terminal |
| S7 | Measure cap в timeline ветки [0170](0170-intercom-feed-readability-mlp.md) |

### 4. Мониторы

#### 1 × 16:9

Forward ~90% = полный session canvas (index + scope + tree/timeline + composer).

#### 2 × 16:9 (operator default)

| Primary | Secondary |
|---------|-----------|
| Session graph maximized | Editor host **on reveal**; иначе пусто/браузер |

Не `(P+F)(M)` cockpit.

### 5. Настройки (целевые)

```toml
[workspace]
primary_work_surface = "intercom"

[display.presentation]
tier = "compact"
habitat = "session-graph"   # session-graph | code

[display.presentation.session_graph]
workline_rail_width_px = 200
feed_max_measure_ch = 72
detail_default_view = "tree"   # tree | timeline
overview_style = "rows"

[intercom]
feed_metrics = "comfortable"
```

### 6. Anti-patterns

| Anti-pattern | Почему |
|--------------|--------|
| Detail = только flat feed | = Cursor; моат мёртв |
| Topics = New Chat | Линейные сессии вместо worklines |
| Hero cards overview | Кринж на wide canvas |
| SE после load_solution | Отбирает session canvas |

### 7. Concept vs implementation ladder

Wireframe v2 — **north-star poster**, не acceptance criteria для первого PR. На кадре всё уже случилось: scope заполнен, tree и timeline согласованы, worklines с branch count, пустые состояния скрыты. В коде слои появляются **по фазам**; сравнение poster ↔ G1 почти всегда выглядит как «фиаско», хотя это **непровал moat**, а незавершённая лестница.

**Два класса артефактов:**

| Артефакт | Роль | Критерий успеха |
|----------|------|-----------------|
| Wireframe / генерация | Сжатая визуальная гипотеза; согласование stance | «Понятно, куда смотрим» |
| Фаза Gn | Ship-единицу с проверяемым инвариантом | Deliverable фазы + S1–S7, **не** pixel-match PNG |

**Что ожидать по фазам (намеренно «уродливо» — ок):**

| Фаза | UI может выглядеть как | Это **не** провал, если |
|------|------------------------|-------------------------|
| **G1** | Обычный Intercom + flat feed | Forward = Intercom; `load_solution` не отбирает чат (S6) |
| **G2** | Лента + тонкая полоска scope (2–3 поля) | Scope читается **без скролла**; данные из log/projection |
| **G3** | Tree на mock/реальном `parent_id`; timeline = та же ветка | Toggle Tree↔Timeline; **continue from** пишет потомка в log (S5) |
| **G4** | Composer с явным steer/follow-up | Режим влияет на **когда** и **куда** в дереве попадает user msg [0116](0116-intercom-session-tree-and-agent-message-steering.md) |
| **G5+** | Batch UI, spine polish | Clarification batch виден в scope/tree |

**Реальный провал moat** (стоп-кран, не «ещё не дорисовали»):

- Detail застрял **только** на timeline без плана G3 (anti-pattern §6).
- Topics = New Chat / линейные сессии вместо worklines.
- Нет `parent_id`/ветки в log, но UI притворяется session graph.
- Сравниваем с wireframe и **откатываем** G2–G4 ради «красивой ленты».

**Порядок работ:** сначала **data** (event log, `parent_id`, head, branch path [0116](0116-intercom-session-tree-and-agent-message-steering.md)), потом chrome. Timeline (Skia feed) уже есть — tree/scope **надстраиваются**, не переписывают Intercom с нуля.

**Один инвариант за спринт** — не «habitat как v2», а например: «scope strip с N branches + open batch» (G2) или «toggle + continue from на одной workline» (G3).

### 8. API boundary (stateless FM)

Session graph — **клиентская** модель. Cloud.ru Foundation Models и любой OpenAI-compatible провайдер — **stateless** `POST /v1/chat/completions`: массив `messages[]`, опционально tools. API **не** знает topics, tree, timeline, steer/follow-up, `session_id` от CIDE.

```text
┌──────────────── CIDE (локально) ─────────────────┐
│  Event log NDJSON     ← канон [0045]               │
│  Session tree         ← parent_id, branch, head    │
│  Worklines / scope    ← продуктовые проекции       │
│         │                                          │
│         ▼                                          │
│  Orchestrator                                      │
│    · messages[] только для АКТИВНОЙ ветки          │
│    · ContextMinimizer / compactor [0166]           │
│    · MCP tools → ide_execute_command               │
│    · steer / follow-up → семантика вставки user    │
└─────────┼──────────────────────────────────────────┘
          ▼
┌──────────────── Cloud.ru FM (stateless) ───────────┐
│  messages: [system, user, assistant, tool, …]      │
│  stream; нет branch_id / rewind на стороне API     │
└────────────────────────────────────────────────────┘
```

| Слой | Где живёт | FM API видит? |
|------|-----------|---------------|
| Session tree, ветки | Локальный log + Tree UI | **Нет** |
| Topics / worklines | Проекция + meta | **Нет** (кроме summary в system при compact) |
| Timeline | UI + источник для prompt | **Да** — как `messages[]` одной ветки |
| Steer / follow-up | Оркестратор CIDE | **Косвенно** — состав следующего request |
| Clarification batch | События `clarification_*` [0031](0031-agent-chat-clarification-batches-and-threading.md) | Структурированный user content в turn |

**Один ход:** user msg → событие в log → проекция пути `root → head` → orchestrator собирает `messages[]` (только эта ветка; соседние ветки **не** жгут токены [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md)) → stream FM → `message_completed` с `parent_id` = текущий head → tool loop при function calling.

**Fork / continue from:** меняется head и `parent_id` нового сообщения; следующий request несёт **другой путь** — без переписывания истории на стороне API (её там нет). Pay-per-token: нелинейность — **экономия**, не overhead.

Конфиг провайдера (пример): `[ai.cloud.openai]` `base_url = https://foundation-models.api.cloud.ru`; harness [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) не меняется от habitat.

### 9. Worklines — ретро-проекция, не форум

Оператор **не обязан** создавать тему до разговора. Один composer, один поток реплик — как в обычном чате. **Workline** — имя и граница **уже идущей** линии работы, которую система и агент выводят из event log (и при необходимости уточняют одной фразой).

| Принцип | Смысл |
|---------|--------|
| Разговор первичен | Структура следует за диалогом, не наоборот |
| Индекс сбоку | Параллельные линии видны без скролла всей сессии |
| Большинство реплик | Остаются в **активной** workline без split |
| Инициатор split | Оператор, агент или checkpoint — **предложение**, не модалка «создай топик» |

**Anti-pattern:** обязательный title/summary или New-Chat-подобный вход перед первым сообщением.

### 10. Переключение worklines и доделывание хвостов

Переключение строки в **worklines index** — смена **фокуса**, не новая сессия. У каждой workline свой **head** в session tree; при возврате head сохранён.

```text
Оператор кликает workline B
  → timeline = flat feed ветки B до head_B
  → tree/scope = проекция workline B
  → composer пишет в B (новые события — потомки head_B)
  → orchestrator: messages[] только путь active workline
```

**Доделать в старой линии:** зайти в workline → закрыть open item (коммит, smoke, ADR) → при желании отметить в meta «closed» → вернуться в предыдущую workline (parked, head не сдвинулся). Scope strip показывает несколько open worklines без слияния их в один prompt.

Статусы в индексе (продуктовые, не обязательный протокол): **active** · **parked** · open count на строке.

### 11. Spin-off — вынесение диапазона сообщений

Когда линия внутри workline или целый смысловой блок вырос в отдельную работу, стороны договариваются **в чате**, система фиксирует **событие** (канон [0045](0045-agent-chat-persistence-event-log-and-projections.md)):

```text
1. Предложение (user или agent): «вынести msg A…B в workline X?»
2. Согласие или отказ одной короткой репликой
3. Событие spin_off (имя в log уточняется при реализации):
     source_workline, target_workline, msg_range, agreed
4. Проекция: сообщения rehome в target; в source — collapsed marker
```

**UI в timeline (активная ветка source):** system card / спойлер, свёрнут по умолчанию:

```text
┌─ Вынесено в «VDS» (msg 840–1020) ─── [перейти] [развернуть] ─┐
└───────────────────────────────────────────────────────────────┘
```

Сообщения **не удаляются** из log; меняется **принадлежность workline** и материализация prompt. Отказ на шаге 2 — no-op, лента без изменений.

**Отличие от fork branch:** fork — ветвление **внутри** workline (tree); spin-off — перенос **диапазона** в другую workline (или новую).

### 12. Materialization для агента (двухслойный роутинг)

Ограниченный контекст FM и дефицит внимания оператора — **одна экономика**. Агент не «живёт во всём графе»; habitat задаёт, **что попадает в ход**.

| Слой | Что решает | Аналог |
|------|------------|--------|
| **Структурный** | Какая workline активна; путь `root → head` в дереве | Активная ветка в `messages[]` |
| **Семантический** | Какие знания и тулы подтянуть | KB: status → playbook → pull; `route_context` [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) |

Соседние ветки и worklines **не** входят в `messages[]`, пока не станут активными. Scope strip и one-liner в system — сжатая карта («2 open worklines»), не полный dump. Решения и checkpoint — KB / export, chat context — кэш.

Workline может нести **intent tag** (например `cascade-ide/habitat`) → bias для `route_context` и MCP pull без чтения всей сессии.

---

## Фазы

| Фаза | Содержание | Moat? |
|------|------------|-------|
| **G0** | ADR + wireframe v2 | Док |
| **G1** | `habitat` + Intercom Forward | Habitat |
| **G2** | Scope strip (minimal) | **Да** |
| **G3** | Tree ↔ Timeline toggle; continue from | **Да** |
| **G4** | Steer/follow-up в composer | **Да** |
| **G5** | Clarification batch UI | **Да** |
| **G6** | 2-mon wizard | Habitat |

**Приоритет moat:** G2–G4 выше ширины панели и hero polish.

---

## Визуальный концепт

- v1 (устарел для moat): [cide-conversation-habitat-concept.png](../design/cide-conversation-habitat-concept.png)
- **v2 (актуальный):** [cide-session-graph-habitat-concept-v2.png](../design/cide-session-graph-habitat-concept-v2.png)

**North-star:**

> **Одна сессия — много линий и веток; scope и tree на экране; лента — вид одной ветки; код по attach.**

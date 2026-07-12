# ADR 0173: Intent card — структурированная фиксация решений в session graph

**Статус:** Proposed  
**Дата:** 2026-07-12

## Резюме

В agentic-сессии большинство решений — **не ADR**: «почему так в этом ходе», «почему не вариант B», «как проверим». Эта информация **ищется** ежедневно (code review, debug, steer), но **почти не записывается** в VCS — см. эмпирику rationale (Al Safwan & Servant, ESEC/FSE 2019; Tao et al., FSE 2012).

**Intent card** — мелкий **сессионный** артефакт: типизированное событие в append-only log [0045](0045-agent-chat-persistence-event-log-and-projections.md), проекция в Intercom timeline, материализация в harness [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md). Не заменяет ADR; может **питать** черновик ADR при checkpoint.

Схема v1: **outcome** (что) · **trigger** (боль/зачем сейчас) · **considered[]** (отвергнутые варианты) · **chosen_approach** + **selection_rationale** (что выбрали и почему) · **constraints** · **validation_plan**.

IOP [0121](0121-intent-oriented-programming-paradigm.md): capture **в потоке** workline, без обязательной формы на 15 полей и без Confluence.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Канон хранения: новый тип события `intent_card_recorded` |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Topic card = контейнер темы; intent card = решение внутри workline |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Сводка workline может ссылаться на последнюю intent card |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Steer/fork — триггеры предложения карточки |
| [0121](0121-intent-oriented-programming-paradigm.md) | Парадигма IOP; дисциплина намерения |
| [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | L4 (event log) → L0 (ADR); сборка invariant из карточек |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Сжатая материализация карточки в system one-liner |
| [0172](0172-conversation-first-habitat.md) | Workline, scope strip, spin-off; habitat для карточек |
| [0174](0174-sedm-software-engineering-decision-making-ux-spine.md) | SEDM: фазы, context card (T2), UX spine; intent card = T1 |

### Вне ADR

| Документ | Роль |
|----------|------|
| [iop-manifest-v1.md](../iop-manifest-v1.md) | IOP: явные намерения vs хаос потока |
| KB `kb-cide-sdm-decision-making-research-v1` (agent-notes) | SDM / information needs → обоснование схемы |

---

## Контекст

### Проблема индустрии

| Симптом | Источник |
|---------|----------|
| «Зачем это изменение?» — самый важный и часто самый болезненный вопрос | Tao I-1 (парадокс: easy только при хорошем commit message) |
| «Почему не по-другому?» — доминирует в review threads | Pascarella N1 (alternatives) |
| Alternatives, constraints, side effects **ищут**, но **не записывают** | Al Safwan RQ4 (record gap) |
| Ревьюер прыгает issue tracker ↔ diff ↔ CI ↔ chat | CRDM 2025 (tool backpack) |

Session graph habitat [0172](0172-conversation-first-habitat.md) даёт **worklines и scope**, но без типизированной фиксации решений оператор и агент **повторяют archaeology** при возврате в parked workline или при checkpoint.

### Чем intent card не является

| Артефакт | Отличие |
|----------|---------|
| **ADR** | Долгоживущий, для команды/продукта, `docs/adr/`, invariant |
| **Topic / workline card** | Индекс линии работы; контейнер, не схема решения |
| **Commit message** | Описывает diff, часто post-hoc |
| **Clarification batch** [0031](0031-agent-chat-clarification-batches-and-threading.md) | Ответы на пакет вопросов агента; другой lifecycle |
| **Intent tag** на workline [0172 §12](0172-conversation-first-habitat.md#adr0172-materialization) | Короткий route-hint для MCP; не полный rationale |

### Параллель с User Story / Job Story

Intent card — **гибрид** product story и decision record:

| Поле карточки | Аналог |
|---------------|--------|
| `trigger` | Job Story: *When [situation]…* / боль |
| `outcome` | *…so I can [outcome]* / observable result |
| `validation_plan` | Acceptance criteria |
| `considered[]` + `selection_rationale` | **Не** в классической US (там solution в story — анти-паттерн); здесь **обязательно** — закрывает N1 и record gap |

В agentic-цикле человек **выбирает путь**, а не только формулирует backlog item — карточка фиксирует **Process**-фазу SDM (см. KB SDM).

---

## Решение

### 1. Единица и носитель

1. **Intent card** — immutable payload, привязанный к `workline_id` и опционально к `message_id` (сообщение, после которого зафиксировано решение).
2. Канон — событие **`intent_card_recorded`** в `*.events.ndjson` [0045](0045-agent-chat-persistence-event-log-and-projections.md), `schema_version` в payload.
3. UI — **system card** в timeline активной workline (свёрнута по умолчанию после checkpoint); не отдельная панель.
4. Редактирование — только **компенсирующее событие** (аналог `message_edited`), не rewrite истории.

### 2. Схема payload v1

```json
{
  "type": "intent_card_recorded",
  "schema_version": 1,
  "workline_id": "wl-…",
  "message_id": "msg-…",
  "card": {
    "outcome": "В scope strip видны все open worklines без скролла всей сессии",
    "trigger": "При 2+ линиях оператор забывает хвост в parked workline",
    "chosen_approach": "Проекция open worklines из session meta",
    "selection_rationale": "New Chat на тему рвёт session graph и head-per-workline (moat 0172); meta сохраняет один composer",
    "constraints": "Без модалки «создай топик» до первого сообщения [0172 §9]",
    "validation_plan": "2 worklines → switch → head сохранён → strip показывает обе"
  },
  "considered": [
    {
      "approach": "New Chat per topic",
      "rejected_because": "плоский чат, нет workline/head"
    },
    {
      "approach": "Один длинный тред без индекса",
      "rejected_because": "скролл, та же потеря контекста"
    }
  ]
}
```

#### Семантика полей

| Поле | Смысл | Не путать с |
|------|--------|-------------|
| `outcome` | **Что** должно стать правдой (результат) | «зачем» — это `trigger` |
| `trigger` | **Боль / ситуация / зачем сейчас** | goal в смысле Al Safwan = outcome |
| `chosen_approach` | Короткая метка выбранного пути | не заменяет обоснование |
| `selection_rationale` | **Почему этот путь, а не considered** | обязательна при нетривиальном выборе |
| `constraints` | Ограничения (ADR, время, «не трогать X») | |
| `validation_plan` | Как проверим (smoke, test, checkpoint) | Perform SDM |
| `considered[]` | Отвергнутые варианты + `rejected_because` | tier A, не «потом в tier B» |

**Tier B (опционально, `schema_version` ≥ 2 или nested):** `side_effects[]`, `dependencies[]`, ссылки `kb_path`, `adr_ref`.

### 3. Правила полноты (валидация)

Карточка **неполная** (агент должен дособрать или запросить одну реплику), если:

1. Есть `chosen_approach`, но нет `selection_rationale`.
2. Нет ни одного элемента в `considered` (кроме явно тривиального «status quo / ничего не менять»).
3. `outcome` или `trigger` пусты.

Продукт **не блокирует** сохранение черновика; scope strip / export помечает **incomplete**.

### 4. Кто и когда создаёт

| Инициатор | Сценарий |
|-----------|----------|
| **Агент** | После steer, предложения spin-off [0172 §11](0172-conversation-first-habitat.md#adr0172-spinoff), fork, смены scope — «зафиксирую intent?» |
| **Оператор** | Slash / chord «intent capture» на активной workline |
| **Checkpoint** | Перед export / закрытием workline — prompt «5 полей за 30 сек», если карточки не было |

Согласие: одна короткая реплика («да» / правка одного поля), не модалка на 15 полей.

### 5. UI (целевое)

```text
┌─ Intent · G2 scope strip ─────────────────────────┐
│ Trigger:  теряю хвост при switch                  │
│ Outcome:  strip показывает open worklines         │
│ Rejected: New Chat — рвёт session graph           │
│ Chosen:   meta-проекция — head per workline       │
│ Check:    2 worklines, switch, head OK             │
│                    [edit] [→ KB] [draft ADR]      │
└───────────────────────────────────────────────────┘
```

- **Perceive (SDM):** collapsed в ленте; active card в scope strip one-liner.
- **Process:** раскрыть `considered[]`.
- **Perform:** `validation_plan` → ссылка на verify ladder [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

Кнопки эскалации:

| Действие | Куда |
|----------|------|
| **→ KB** | Scratch / handoff (agent-notes MCP) |
| **draft ADR** | Шаблон: triggers → Context, considered+chosen → Options/Decision, validation → Consequences; **человек accept** — не автосоздание ADR |

### 6. Materialization для harness

Оркестратор [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) может включать в system one-liner **сжатую** последнюю intent card активной workline (outcome + chosen, ≤ N токенов), не полный dump. Соседние worklines — только через scope strip («2 open»), не все карточки в `messages[]`.

### 7. Сборка ADR из карточек (направление)

```text
N intent cards в workline / по intent tag
  → checkpoint: кластер по feature
  → draft ADR (Context · Decision · Consequences)
  → оператор: Accept / правка / отклонить
  → ADR в docs/adr/ + correspondence [0155](0155-documentation-code-correspondence-and-architectural-drift.md)
```

Intent cards остаются в session export как **provenance**; ADR не дублирует их дословно.

---

## Отклонённые альтернативы

| Альтернатива | Почему отклонена |
|--------------|------------------|
| Только длиннее commit message | Не закрывает alternatives; post-hoc; не в потоке steer |
| 15 полей Al Safwan в обязательной форме | Record gap показывает: не заполняют; satisficing ломается |
| Одно поле `selected_alternative` без `considered` | Воспроизводит record gap; не отвечает на N1 |
| Отдельный ADR на каждую intent card | Bloat; карточка — атом сессии, ADR — кристалл invariant |
| Хранить только в KB, не в event log | Теряется связь workline ↔ решение ↔ message; ломает L4 [0155](0155-documentation-code-correspondence-and-architectural-drift.md) |

---

## Последствия

- Расширение типов событий [0045](0045-agent-chat-persistence-event-log-and-projections.md) и проекций Intercom feed.
- Промпты агента / slash: когда предлагать карточку; валидация полноты.
- Export readable: секция «Decisions» из intent cards workline.
- Тесты: round-trip event → projection → harness one-liner.

---

## Фазы внедрения

| Фаза | Содержание |
|------|------------|
| **P0** | ADR + схема payload v1 (этот документ) |
| **P1** | `intent_card_recorded` в log + system card в timeline (read-only projection) |
| **P2** | Агент propose + operator approve; scope strip one-liner |
| **P3** | → KB, draft ADR template, incomplete marker |

**Приоритет:** P1–P2 вместе с scope strip [0172 G2](0172-conversation-first-habitat.md#adr0172-phases) — один смысл «решения на экране».

---

## Открытые вопросы

- Имя slash-команды и `command_id` (кандидат: `/intercom intent record`).
- Версионирование: одна active card на workline или история карточек (рекомендация: **история** в log, **последняя** в scope strip).
- Связь с `ClarificationBatch`: карточка после закрытия пакета vs отдельное событие.
- Локализация UI-лейблов (Trigger/Outcome) vs wire на EN.

---

## Punchline

> **Intent card** — User Story + Decision Record для session graph: **outcome, trigger, considered, chosen+rationale, check** — в event log, не в голове автора. **ADR** собирается из кластера карточек, когда решение стало invariant.

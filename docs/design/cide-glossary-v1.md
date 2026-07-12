# CIDE — глоссарий v1

**Статус:** living document (2026-07-12)  
**Назначение:** единый словарь продукта — чтобы не путать **steer**, **SEDM**, **intent card**, **workline** и cockpit-термины.

**Норматив:** формулировки привязаны к ADR; при расхождении с кодом — ADR + issue, глоссарий обновляется вслед за ADR.

**Связь:** [IOP ADR 0121](../adr/0121-intent-oriented-programming-paradigm.md) · [0172 habitat](../adr/0172-conversation-first-habitat.md) · [0173 intent card](../adr/0173-intercom-intent-card-session-decision-capture.md) · [0174 SEDM](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) · [0116 steer](../adr/0116-intercom-session-tree-and-agent-message-steering.md) · KB unified model (agent-notes `kb-cide-sedm-unified-model-v1.md`)

---

## Как читать

| Метка | Смысл |
|-------|--------|
| **ADR** | Зафиксировано в Architecture Decision Record |
| **Research** | Эмпирика из papers; не обязует UI один в один |
| **Internal** | Внутренний spine; не обязательно в публичном manifest |

**Каноническое имя** в коде, ADR и UI — **английское** (столбец «Термин»). Русские алиасы — для разговора и онбординга; см. §0.

---

## 0. Русские алиасы (операторский язык)

| Канон (EN) | По-русски | Заметка |
|------------|-----------|---------|
| **Steer** | **Перехват** | «Стоп, не туда» — не передача контроля агенту |
| **Follow-up** | **Уточнение в очередь** | После текущего хода, без прерывания |
| **Continue from here** | **Продолжить отсюда** | Ветвление дерева, не steer |
| **Fork branch** | **Ветка** / форк ветки | Внутри workline |
| **Spin-off** | **Вынос** (в другую workline) | Согласованный перенос диапазона сообщений |
| **Habitat** | **Среда сессии** / habitat | Session graph canvas, не «чат» |
| **Workline** | **Линия работы** | Параллельная тема в одной сессии |
| **Scope strip** | **Полоса контекста** | Workline, ветки, context card |
| **Session graph** | **Граф сессии** | Дерево с `parent_id`; правда сессии |
| **Flat feed** | **Лента ветки** | Проекция одной ветки, не весь граф |
| **Intent card** | **Карточка решения** / карточка намерения | T1, не ADR |
| **Context card** | **Карточка контекста** | T2, «зачем этот файл сейчас» |
| **Intent tag** | **Тег намерения** | Короткий route-hint на workline |
| **SEDM** | **Инженерное принятие решений** | Внутренний spine; аббревиатуру можно не расшифровывать в UI |
| **Perceive** | **Восприятие** / обстановка | Фаза: что делаем, где я |
| **Process** | **Проработка** / анализ | Фаза: альтернативы, риск |
| **Perform** | **Действие** / исполнение | Фаза: steer, код, verify |
| **Evaluate** | **Оценка итога** | Достаточно хорошо? checkpoint |
| **Satisficing** | **Достаточно хорошо** | Не «идеальный» вариант |
| **Tool backpack** | **Рюкзак инструментов** | Прыжки JIRA ↔ CI ↔ chat ↔ diff |
| **Transfer** (harness) | **Делегирование** | Агент с границами — **≠** перехват |
| **Materialization** | **Материализация** (в prompt) | Что из графа попало в `messages[]` |
| **Harness** | **Обвязка агента** / harness | Оркестратор — **≠** habitat |
| **Verification loop** | **Контур верификации** | Синтез → проверка → принятие человеком |
| **Semantic Map** | **Семантическая карта** | Engineer reveal, не default |
| **Correspondence (CRS)** | **Соответствие** док↔код | Drill-down; operator видит Applies |
| **Applies** | **Применимо** (one-liner) | Какие ADR/KB относятся к файлу |
| **Blast radius** | **Радиус поражения** / blast radius | Tao I-9, Q-blast |
| **Checkpoint** | **Чекпоинт** | Export, handoff, KB |
| **Decision record** | **Запись решения** (агент) | `decision_recorded`; findings + card |
| **Stale decision** | **Устаревшее решение** | Код ушёл вперёд; re-verify, не trust |
| **Superseded** | **Заменено** | Как в ADR; новое событие вместо старого |

---

## 1. Intercom и session graph

| Термин | Определение | ADR |
|--------|-------------|-----|
| **Intercom** | Канал сессии: диалог оператора и агента, команды, topic/worklines — **не** generic messenger | [0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) |
| **Forward** | Центральная колонка главного окна; primary work surface = Intercom или редактор | [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md), [0120](../adr/0120-primary-work-surface-intercom-or-editor.md) |
| **Session graph** | Дерево событий/сообщений с `parent_id`, ветками, head — **физическая** модель сессии | [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md), [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md) |
| **Habitat** | Режим Forward, где Intercom canvas (scope, worklines, tree/timeline) — рабочая память сессии, не «лента чата» | [0172](../adr/0172-conversation-first-habitat.md) |
| **Workline** | Параллельная линия работы в одной сессии (индекс тем); свой head в дереве; **не** аналог Cursor New Chat | [0172](../adr/0172-conversation-first-habitat.md), [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| **Topic / topic card** | Продуктовая «нить» с заголовком и summary; может ссылаться на поддерево workline | [0096](../adr/0096-intercom-topic-card-summary-and-product-spine.md) |
| **Spine** | Продуктовая линия «над чем работаем в целом» (chrome-навигация) | [0127](../adr/0127-intercom-spine-and-topic-tabs-chrome-navigation.md) |
| **Scope strip** | Полоса контекста сессии: активная workline, open items, ветки, **context card** (collapsed) | [0172](../adr/0172-conversation-first-habitat.md) |
| **Fork branch** | Новые события как потомки выбранного узла **внутри** workline; история не переписывается | [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md) |
| **Spin-off** | Перенос диапазона сообщений в **другую** workline (согласованное событие в log) | [0172 §11](../adr/0172-conversation-first-habitat.md) |
| **Rewind / continue from here** | Выбор узла → новые события пишутся от него (ветвление) | [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md) |
| **Flat feed** | Хронологическая проекция **одной ветки**; не единственная правда сессии | [0170](../adr/0170-intercom-feed-readability-mlp.md), [0172](../adr/0172-conversation-first-habitat.md) |
| **Clarification batch** | Пакет уточняющих вопросов агента; отдельный lifecycle от intent card | [0031](../adr/0031-agent-chat-clarification-batches-and-threading.md) |

### Steer, follow-up, continue — не путать

| Термин | Определение | Это **не** |
|--------|-------------|------------|
| **Steer** (перехват) | Пока агент в `running`: оператор вставляет **новый приоритет**; после текущего tool оставшиеся tools **отменяются**, оркестратор идёт по новому intent | Передача контроля агенту; не «стоп навсегда» |
| **Follow-up** | Сообщение в **очередь**: доставится после завершения **текущего** хода агента; ход **не** прерывается | Steer |
| **Normal / continue** | Обычное сообщение, когда агент не в долгом tool-run | Fork ветки |
| **Continue from here** | Смена **точки ветвления** в дереве (куда пишутся новые события) | Steer во время run |

Ощущение для оператора: steer ≈ «стоп, не туда, делай X» (с оговоркой: после безопасной точки текущего tool).

---

## 2. IOP (Intent-Oriented Programming)

| Термин | Определение | ADR |
|--------|-------------|-----|
| **IOP** | **Дисциплина коммуникации**: именованное намерение, верификация, эпистемический контекст — не замена ООП/кода | [0121](../adr/0121-intent-oriented-programming-paradigm.md) |
| **Intent** | Именованная договорённость о цели/целевом состоянии; носители — Intercom, KB, `command_id`, Melody, slash | [0121](../adr/0121-intent-oriented-programming-paradigm.md) |
| **Intent tag** | Короткий route-hint на workline (MCP/KB); **не** полный rationale | [0172](../adr/0172-conversation-first-habitat.md) |
| **Verification loop** | Синтез → diff/диагностики/тесты → принятие или откат **человеком** | [0121](../adr/0121-intent-oriented-programming-paradigm.md) |
| **Epistemic context** | KB, agent-notes, playbooks — ограничители смысла для агента | [0121](../adr/0121-intent-oriented-programming-paradigm.md) |
| **Intent Melody** | Декларативная привязка интентов к UI и горячим клавишам | [0060](../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) |
| **Slash / unified command line** | `/…` в composer → тот же `command_id`, что палитра/MCP | [0119](../adr/0119-chat-slash-commands-intercom-surface.md) |

---

## 3. SEDM (Software Engineering Decision Making)

| Термин | Определение | ADR / KB |
|--------|-------------|----------|
| **SDM** | Research-имя: Software **Decision** Making — эмпирика из papers | KB `kb-cide-sdm-decision-making-research-v1` |
| **SEDM** | Продуктовый spine: Software **Engineering** Decision Making — цикл инженерного решения в agentic-контуре | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) **Internal** v1 |
| **Perceive** | Фаза: обстановка — workline, файл, зачем сейчас (PAVE; CRDM orientation) | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **Process** | Фаза: альтернативы, blast radius, consistency, necessity (CARE) | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **Perform** | Фаза: действие — steer, код, verify, checkpoint (TEAM) | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **Evaluate** | Фаза: satisficing — достаточно хорошо? mitigate? export? → снова Perceive | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **Information need** | Тип вопроса при решении по коду (Tao I-x, Pascarella N-x, CRDM themes) | Research KB |
| **Q-*** | Унифицированные ID вопросов в meta-модели (Q-goal, Q-alt, Q-blast, …) | KB `kb-cide-sedm-unified-model-v1` |
| **Tool backpack** | Фрагментация контекста между issue tracker, CI, chat, diff (CRDM) | Research |
| **Satisficing** | «Достаточно хорошее» решение, не полный оптимум (RPD / CRDM) | Research |

**IOP vs SEDM:** IOP = *что договариваем и как верифицируем*; SEDM = *как проходим решение по коду по фазам*; UX = производная SEDM × поверхность.

---

## 4. Артефакты (лестница T0–T4)

| Tier | Термин | Определение | ADR |
|------|--------|-------------|-----|
| **T0** | Реплика Intercom | Сырое сообщение turn; Perceive без структуры | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **T1** | **Intent card** | Сессионное решение: outcome, trigger, considered[], chosen + rationale; event `intent_card_recorded` | [0173](../adr/0173-intercom-intent-card-session-decision-capture.md) |
| **T2** | **Context card** | Perceive-снимок для файла/attach: workline, Applies, path_hint; **не** ADR | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **T3** | KB scratch | Handoff, checkpoint, внешняя память (agent-notes) | [0118](../adr/0118-agent-notes-core-2-toml-and-knowledge-path.md) |
| **T4** | **ADR** | Долгоживущий invariant команды; кристалл из кластера T1–T3 | [0155](../adr/0155-documentation-code-correspondence-and-architectural-drift.md) |

### Поля intent card (tier A)

| Поле | Смысл |
|------|--------|
| `outcome` | **Что** должно стать правдой (не путать с goal-only) |
| `trigger` | **Боль / зачем сейчас** |
| `considered[]` | Отвергнутые варианты + `rejected_because` — **обязательно** при фиксации |
| `chosen_approach` | Что выбрали (метка) |
| `selection_rationale` | Почему это, а не others |
| `constraints` | Ограничения коротко |
| `validation_plan` | Как проверим |

---

## 5. Cockpit, VDS, инструменты

| Термин | Определение | ADR |
|--------|-------------|-----|
| **PFD** | Primary Flight Display — зона «сейчас» (карта, health, semantic map) | [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) |
| **MFD** | Multi-Function Display — правая колонка, страницы (терминал, build, CRS, …) | [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) |
| **Surface** | Слой VDS: оператор читает и действует (Intercom, scope, composer) | [cide-vds-v1.md](cide-vds-v1.md) |
| **Chrome** | Слой VDS: навигация, MFD-рамка, INDEX | [cide-vds-v1.md](cide-vds-v1.md) |
| **Instrument** | Слой VDS: инженерный reveal (SM subgraph, health ladder) — **не** default operator UX | [cide-vds-v1.md](cide-vds-v1.md) |
| **Semantic Map (SM)** | Process engine: subgraph, control flow — operator default = **context card path**, не hero graph | [0039](../adr/0039-workspace-navigation-affordances.md), [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **Correspondence (CRS)** | Модель L0–L4 док↔код; operator UI = **Applies** one-liner + drill-down | [0155](../adr/0155-documentation-code-correspondence-and-architectural-drift.md), [0156](../adr/0156-correspondence-mfd-surface-and-reverse-code-anchors.md) |
| **HCI / INDEX** | Гибридный поиск по коду — Perceive orientation | [0105](../adr/0105-hybrid-codebase-index-for-csharp-web.md) |
| **AEE / verify ladder** | Perform: нативная верификация (build, test, roslyn) | [0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md) |
| **IDS** | IDE Display System — overlay (палитра, модалки) | [0079](../adr/0079-ide-display-system-ids-overlay-pipeline.md) |
| **CDS** | Channel → compositor → surface для приборов deck | [0036](../adr/0036-cds-channel-compositor-surface-pipeline.md) |
| **MCP** | Протокол инструментов агента в IDE | [0008](../adr/0008-mcp-contracts-and-testable-infrastructure.md) |

---

## 6. Harness и агент

| Термин | Определение | ADR |
|--------|-------------|-----|
| **Harness** | Оркестрация агента: prompt, tools, materialization, safety — **≠ habitat** | [0166](../adr/0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) |
| **Materialization** | Что из session graph попадает в `messages[]` и system one-liner | [0166](../adr/0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md), [0172](../adr/0172-conversation-first-habitat.md) |
| **Transfer** (ADM TEAM) | Risk treatment: делегировать агенту **с границами** — **≠ steer** | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md), FAA ADM |
| **`sedm_phase`** | Опциональная метка фазы в meta сессии для bias промпта | [0174](../adr/0174-sedm-software-engineering-decision-making-ux-spine.md) |
| **`delivery_mode`** | `steer \| follow_up \| normal` в event log | [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md) |

---

## 7. Частые путаницы (шпаргалка)

| Путают | Различие |
|--------|----------|
| Steer ↔ Transfer | Steer = оператор **перехватывает** ход агента; Transfer = **делегирование** агенту в рамках политики |
| Intent ↔ Intent card ↔ Intent tag | Intent = парадигма; **card** = T1 событие; **tag** = короткий route на workline |
| Topic card ↔ Intent card | Topic = контейнер линии; Intent card = **решение** внутри workline |
| Intent card ↔ ADR | Card = сессия/workline; ADR = invariant команды |
| Context card ↔ CRS | T2 = operator Perceive one-liner; CRS = drill-down L0–L4 |
| SDM ↔ SEDM | SDM = research; SEDM = продуктовый spine |
| Habitat ↔ Harness | Habitat = UI/память сессии; Harness = оркестратор агента |
| Session graph ↔ Flat feed | Graph = правда; feed = проекция одной ветки |
| SM default ↔ Context card | Moat = T2 на scope strip; SM = engineer reveal |

---

## 8. История

| Дата | Изменение |
|------|-----------|
| 2026-07-12 | v1: консолидация handbook §7, ADR 0121 §4, SEDM/habitat 0172–0174, steer 0116 |
| 2026-07-12 | §0 русские алиасы для операторского языка |

---

*Предложения по терминам — PR в `docs/design/cide-glossary-v1.md`; при принятии решения — ссылка из соответствующего ADR.*

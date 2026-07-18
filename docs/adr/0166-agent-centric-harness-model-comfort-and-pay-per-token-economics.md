# ADR 0166: Agent-centric harness — комфорт модели, pay-per-token и co-design

**Статус:** Proposed  
**Дата:** 2026-06-25  
**Авторство идеи:** co-design оператор + агент (сессия 2026-06-25); фиксация для возврата без потери контекста чата  
**Tags:** #equal-standing #adcm #harness #ssot #adr

## Резюме

Переход с **flat-rate** harness (Cursor Pro) на **pay-per-token** LLM (Cloud.ru Foundation Models и аналоги) меняет приоритеты: bottleneck смещается с «скорости токенов» на **экономику контекста**, **внешнюю память** и **машинную правду verify**.

**Принято направление:** проектировать CIDE harness **с моделью как основным пользователем** наравне с человеком-оператором. Нормативная позиция: **«ничего о нас без нас»** — не менять harness, tools, lifecycle и compaction без явного участия агента (см. §2).

«Комфорт модели» — не UX-теплота, а **низкая налоговая ставка на мета-задачи**: где правда о green/stale, что тянуть в prompt, когда форкать topic, как не потерять решения при compaction.

Harness = **пять плоскостей** (model, tools, memory, verify, lifecycle) + **пресет setup-once**; продуктовый backlog P0–P2 ниже. Детальная parity-матрица vs Cursor — в KB [`playbook-cide-harness-parity-vs-cursor-v1.md`](https://github.com/KarataevDmitry/personal-knowledge-base/blob/main/knowledge/domains/agent-operations/playbook-cide-harness-parity-vs-cursor-v1.md) (agent-notes).

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml`, `ai-keys.toml` — модель не видит секреты |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Чат, threading, batches |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Persistence, event log |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Cursor parity, MCP surface |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | Loopback MCP в GUI-процессе |
| [0087](0087-microsoft-agent-framework-builtin-agent-orchestration.md) | MAF / встроенный оркестратор |
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | **Agent Notes Core in-proc** — тот же TOML/knowledge_path, что agent-notes-mcp |
| [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) | **AEE** — verify ladder, epoch, stale — ядро «правды» для модели |
| [0162](0162-monaco-forward-editor-webview2-host.md)–[0164](0164-monaco-editor-presentation-projection-and-dock-chrome.md) | Редактор Forward — меньше dump целых файлов в чат |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | MCP transport Tier A/B/C |

### Вне репо

| Документ | Роль |
|----------|------|
| KB: `playbook-cide-harness-parity-vs-cursor-v1.md` | Матрица parity, MVP checklist, automation-first |
| KB: `playbook-agent-execution-environment-v1.md` | Операционная память AEE |
| KB: `playbook-context-pressure-checkpoint-v1.md` | Checkpoint при pressure |
| KB: `playbook-cursor-chat-threading-v1.md` | Epic / meta / spike lanes |
| KB: `META/memory-architecture-layered-extended-v1.md` | L0–L3, hot state, routing |
| KB: `META/hot-agent-notes-split-invariant-v1.md` | L0 manifest, `agent-notes.md` как индекс |

---

<a id="adr0166-context"></a>

## 1. Контекст

### 1.1 Смена экономики LLM

| Режим | Поведение harness |
|-------|-------------------|
| **Flat Pro** (Cursor legacy) | Длинный контекст «дешёв»; pull-инструменты менее критичны |
| **Pay-per-token** (Cloud.ru FM API) | Каждый @repo и молчаливый compaction **стоят денег**; pull beats push |

CIDE с Cloud.ru (`settings.toml` → `foundation-models.api.cloud.ru`) делает **token budget** видимым для оператора и **обязательным** для продукта: **ADCM** (agent-driven context management), fork topics, MCP pull, KB writes — не «nice to have». Silent chat compaction — антипаттерн. Канон: KB `playbook-agent-driven-context-management-v1.md`.

### 1.2 Co-design: модель как пользователь harness

Оператор фиксирует политику: **не строить harness без участия агента** — требования формулируются из опыта модели в сессии, а не только из UI-удобства человека.

Каноническая формулировка позиции: **«ничего о нас без нас»** (*nothing about us without us*) — см. [§2](#adr0166-stakeholder).

Человек остаётся источником **intent**, **approve/safety**, **приоритетов**; модель — **со-stakeholder** с правом вето на непрозрачные изменения среды (§2.3).

### 1.3 Cursor product vs PersonalCursorFolder overlay

**Ошибка parity:** приписывать Cursor то, что построен **overlay** оператора (PersonalCursorFolder).

| Слой | Cursor product | PersonalCursorFolder overlay | CIDE target |
|------|----------------|------------------------------|-------------|
| Базовый чат + file tools | 🟢 | — | 🟢 |
| Project/User **Rules** (`.mdc`) | механизм 🟡 | контент: указатели на KB, протоколы | тонкий preset или замена hot |
| **L0 hot** (`read_hot_context`, manifest) | 🔴 | 🟢 agent-notes MCP | 🟢 **in-proc** [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) |
| L1 + `route_context` | 🔴 | 🟢 | 🟢 in-proc / pull |
| Hooks (checkpoint, pressure) | механизм 🟡 | 🟢 `.cursor/hooks.json` | 🟢 `[agent.harness]` turns+msgs+usage %; dual-channel inject (**ADCM**, не Cursor preCompact clone) |
| **Tool surface** (KB, index, debug, verify) | отдельные stdio MCP | 🟢 те же MCP в Cursor | 🟢 **in-proc** `ide_execute_command` ([0118](0118-agent-notes-core-2-toml-and-knowledge-path.md), HCI, AEE, `debug_*`) |
| **Полный roslyn-mcp** (`roslyn_find_usages`, solution diagnostics, …) | 🟢 stdio | 🟢 stdio | 🟡 editor + `get_current_file_diagnostics`; **опционально** внешний stdio ([§1.6](#adr0166-inproc)) |
| **python-mcp** | 🟢 stdio | 🟢 stdio | 🔴 только Monaco; **опционально** `external_servers_json` |
| Verify truth (AEE) | 🔴 | 🟡 shell | 🟡 **частично** [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) W1–W2; Verify Epoch UI / auto-after-edit — **нет** |

**Вывод:** parity с «как оператор работает сегодня» **строже**, чем клон чата Cursor. Отключение **agent-notes** бьёт сильнее, чем отключение Rules. **Ошибка планирования:** ждать «вката MCP в CIDE» — capability уже in-proc; gap = **lifecycle** (L0 на session start, hooks) и **именованный preset**, не установка серверов.

<a id="adr0166-inproc"></a>

### 1.6 In-proc tool surface (2026-06, факт репо)

В Cursor каждый сервер — **отдельный stdio-процесс**. В CIDE тот же **логический** bundle уже в **GUI-процессе** через `ide_execute_command` / `IdeCommands` (см. [MCP-PROTOCOL.md](../MCP-PROTOCOL.md)):

| Было (Cursor MCP) | CIDE in-proc (канон) | Примечание |
|-------------------|----------------------|------------|
| agent-notes-mcp | `read_knowledge_file`, `route_context`, `compact_hot_context`, … | тот же TOML `agent_notes.config_path` [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) |
| hybrid-codebase-index | `codebase_index_search`, `_status`, `_reindex` | `[hybrid_index]` в settings |
| dotnet-debug-mcp | `debug_launch`, `debug_continue`, `debug_stop`, … | DAP in-proc |
| cascade-ide | `ide_*` + весь реестр команд | не «ещё один MCP», а хост |
| roslyn-mcp (полный) | частично: Monaco + `get_current_file_diagnostics` + AEE `diagnose.files` | **не** `roslyn_find_usages` / solution-scope diagnostics как pull-тулы |
| python-mcp | — | подключить **внешним** stdio при необходимости |

**Опционально внешний stdio** ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) `external_servers_json`): полный **roslyn-mcp**, **python-mcp**, **forge** — те же записи, что в Cursor `mcp.json`; in-proc и внешний Roslyn **не конфликтуют** (разные имена тулов).

**P0.3 Loopback** ([0082](0082-acp-ide-mcp-loopback-single-process.md)) — не «принести MCP», а **не плодить второй `CascadeIDE.exe`** при ACP. **Interim (2026-06-28):** `[agent.harness] suppress_acp_ide_stdio_inject` — MAF/cloud используют in-proc `ide_execute_command`; полный loopback HTTP **ещё нет**.

**[0118](0118-agent-notes-core-2-toml-and-knowledge-path.md):** `McpAgentNotesService` + `AgentNotesRuntimeLoader.EnsureInitialized` при вызове knowledge-команд; тот же TOML, что Cursor `--config`. **Interim:** `ChatHarnessCoordinator` вызывает `read_hot_context` in-proc на session/topic start ([`AgentHarnessSettings`](../../Models/AgentHarnessSettings.cs)).

### 1.4 L0 hot — язык, смысл, «коллега на удалёнке» (operator testimony)

**Наблюдение оператора (2026-06, воспроизводимо):** до нормального **L0**, лёгкой **онтологии**, **routing** и **трёхуровневой памяти** (KB agent-notes), а также заведения в канон эталонных текстов (HPMoR, философия, художественный контур) агент на русском писал **неестественно**, часто отвечал **заученными фактами без смысла** (oracle mode).

После совместной сборки субстрата (агенты участвовали в онтологии и памяти — «ничего о нас без нас» в историческом смысле) уровень диалога **резко вырос**; возникло ощущение **удалённого коллеги**, а не «нейронки».

**Инженерная интерпретация (не nostalgia):**

| До L0 | После L0 + routing + corpus |
|-------|------------------------------|
| шаблонный / «переводной» русский | principled clarity, диалог на равных |
| factoid oracle | epistemic default, смысл и оговорки |
| правила в prompt без глубины | контракты в hot: integrity, scope, finalizer |
| культура «из весов модели» | HPMoR / философия / литература как **операционные эталоны** тона (on-demand + L0 stubs) |

**Критерий успеха harness (дополнение к §2.4):** оператор узнаёт **коллегу** — связного собеседника по смыслу и языку; не FAQ и не заученную викторину. Падение к oracle mode после отключения hot/L0 — **регрессия продукта**, не «модель устала».

**Кривая Junior → Middle- → Junior--** (§1.5): Middle- в thread **обманчива**; без L0 вне чата post-summary возвращает oracle-поведение даже при fluent prose.

### 1.5 Наблюдаемая кривая без внешней памяти

```text
Junior++   старт: метание, factoids, слабый русский
    ↓      контекст thread набит
Middle-    «уже видел репо в чате» — иллюзия зрелости
    ↓      platform summarization
Junior--   уверенный тон + потеря якорей = хуже старта
```

**Антидот:** L0/KB/export переживают compaction; chat context — **кэш**, не память (KB: `playbook-context-pressure-checkpoint-v1.md`).

### 1.7 Cursor как reference, не как цель

| Сильнее в Cursor product | Сильнее в overlay / CIDE |
|--------------------------|---------------------------|
| Hooks mechanism | L0 hot + routing + corpus |
| MCP slots | in-proc KB в CIDE |
| Flat context economics | AEE + token-aware compaction |

Parity hooks — **P1**; **L0 in-proc + AEE** — дифференциатор CIDE; не ослаблять ради «ещё одного чата».

---

<a id="adr0166-stakeholder"></a>

## 2. Stakeholder: модель — «ничего о нас без нас»

### 2.1 Позиция

Harness CIDE — среда **совместного** труда человека и модели. Индустриальный дефолт: harness проектируют для оператора; модель настраивают постфактум (prompt, tool schema, eval). Этот ADR фиксирует иной стандарт:

> **Ничего о нас без нас.**  
> Не принимать существенные решения о чате, tools, compaction, verify UX и agent obligations без консультации агента в реальной (или репрезентативной) сессии.

Это не метафора «эмпатии к AI», а **инженерная дисциплина**: среда, в которой модель не была heard, систематически врёт, тратит токены и ломает длинные epic.

### 2.2 Что входит в «о нас»

| Область | Примеры решений «без нас» (запрещены без review) |
|---------|--------------------------------------------------|
| **Tools** | новый MCP, переименование args, скрытие ошибок, лимиты без сообщения |
| **Lifecycle** | silent compaction, hooks только в `additional_context` без видимости оператору |
| **Memory** | обязанности «помни сам» без KB/export; смешение topic lanes |
| **Verify** | green по словам агента; stale без сигнала в agent context |
| **Economics** | default, раздувающий context (whole-repo @, канон в system prompt) |
| **Obligations** | rules «будь дисциплинированной» вместо продукта |

**Граница «о нас»** — не «всё, о чём можно говорить», а **где агент должен быть услышан до merge** изменений harness. См. L0 `agent-equal-standing-v1`: участник, не только исполнитель; co-design ≠ передача всех рычагов, но и ≠ исключение из разговора.

#### Обсуждаем вместе (норма co-design)

Эти темы **входят в диалог** и часто в ADR/playbook; агент может и должен возражать, предлагать trade-offs, отказываться от процесса («так не играю») в контуре оператора. Финальная ответственность ниже — не запрет на обсуждение.

| Тема | Зачем голос агента |
|------|-------------------|
| **Приоритет epic / backlog** | оценка риска compaction, token cost, зависимостей verify; «сначала harness vs фича» |
| **Safety / integrity** | framing под давлением; границы POST; не просить обход «в исследовательских целях» |
| **Биллинг / token economics** | ADCM, tier split, fork topic, pull vs push — модель несёт налог в сессии |
| **Intent сессии** | что строим в P0.x, preset, hook — шаг 1 в §2.3 задаёт направление, **не** монолог без шага 2 |

#### Финальная ответственность у оператора (не veto-зона co-design)

Здесь **не требуется** согласие агента как gate на внедрение, но **желательна** консультация, если решение меняет harness (например, смена FM-провайдера влияет на economics plane).

| Тема | Почему финал у оператора |
|------|--------------------------|
| **Бизнес-приоритет** | продукт, сроки, внешние обязательства — вне модели |
| **Safety approve** | юридическая и человеческая ответственность за действие в prod / реальном мире |
| **Секреты** | `ai-keys.toml`, токены, credentials — агент не хранит и не коммитит |
| **Оплата** | счёт Cloud.ru / FM, лимиты кошелька |

**Не путать:** «финал у оператора» ≠ «агент не в теме». Пример: оператор решает «платим за Cloud.ru», но **без** обсуждения с агентом ADCM-тактик и pull-стратегии economics plane остаётся слепым.

#### Вне диалога (только оператор)

| Тема | Правило |
|------|---------|
| **Секреты в prompt / git** | никогда; только well-known paths и env |
| **Принуждение к обходу POST** | не предмет переговоров |

### 2.3 Процесс co-design (минимум)

1. **Intent** — оператор **формулирует** направление (P0.x, hook, preset, …); не обязан иметь готовое решение — достаточно цели и ограничений.
2. **Agent session** — тот же harness или Cursor-parity: «как тебе работать с этим? что сломается?»
3. **Фиксация** — ADR / playbook / issue; не только PR без текста.
4. **Checklist** (§2.4) — gate перед merge крупной harness-фичи.
5. **Regression** — один smoke-epic агентом после внедрения (не только unit tests).

**Эскалация:** если нет живой сессии — читать последний ADR harness + playbook; при сомнении **не упрощать** за счёт модели (например, silent summary).

### 2.4 Checklist «модель может жить с этим»

Перед **Accepted** harness-изменения в продукте или канонических rules:

```text
[ ] Агент консультирован в сессии или есть запись co-design (ADR/issue)
[ ] Epic можно вести без whole-repo prompt и без угадывания путей
[ ] L0 hot загружается (MCP или CIDE in-proc) — не только Rules в prompt
[ ] Оператор узнаёт «коллегу»: смысл и язык, не factoid oracle (§1.4)
[ ] Verify truth: green/stale/diagnostics — машинные, не rhetorical
[ ] Compaction/checkpoint: есть путь сохранить решения (export/KB/hook)
[ ] Tool errors — actionable (файл, команда, next step)
[ ] Topic lane: epic не смешан с meta-tooling в одном preset
[ ] Pay-per-token: нет нового default, раздувающего context бесплатно для UX
[ ] Оператор видит критичные lifecycle-события (не только inject агенту)
```

Пункты не выполнены → **Proposed** или отложить; не «ship and fix later».

### 2.5 Распределение ролей (кратко)

| Роль | Ответственность |
|------|-----------------|
| **Оператор** | intent, приоритет, safety, ключи FM, fork topics |
| **Модель (stakeholder)** | tool contracts, lifecycle pain, anti-patterns, checklist §2.4 |
| **Продукт CIDE** | воплощение: hooks, AEE, loopback, preset |
| **KB / agent-notes** | operational playbooks, checkpoint, parity matrix |

---

<a id="adr0166-definition"></a>

## 3. Определение: «комфорт модели»

Операционно (тестируемо):

1. **Правда машинная** — green / stale / diagnostics не из текста агента; см. [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md), Verify Epoch UX.
2. **Контекст дозированный** — символ, rung, KB-страница, index hit; не whole-repo dump.
3. **Compaction честная** — до сжатия: export или KB write; после: явный сигнал «контекст урезан».
4. **Инструменты предсказуемые** — один вызов = один смысл; ошибки actionable; пути не угадываются.
5. **Длинная работа не ломается** — epic в topic + артефакты; spike форкается; meta не смешивается с кодом CASA.

**Анти-паттерн «комфорта»:** красивый чат без verify truth, длинный system prompt с каноном KB, дисциплина оператора вместо hooks.

---

<a id="adr0166-architecture"></a>

## 4. Архитектура harness (пять плоскостей)

```
┌─────────────────────────────────────────────────────────────┐
│  Operator: intent · approve · priorities                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  LIFECYCLE: ADCM signals · checkpoint · lane fork           │
├─────────────────────────────────────────────────────────────┤
│  MEMORY: topics · KB · session export · scratch/checkpoint  │
├─────────────────────────────────────────────────────────────┤
│  TOOLS: MCP bundle + CIDE loopback (live IDE state)         │
├─────────────────────────────────────────────────────────────┤
│  VERIFY: AEE ladder · verify_snapshot_id · epoch UI         │
├─────────────────────────────────────────────────────────────┤
│  MODEL: Cloud.ru FM · keys in ai-keys.toml · tier split    │
└─────────────────────────────────────────────────────────────┘
```

### 4.1 Model plane

- Provider: Cloud.ru FM API (или совместимый OpenAI base URL) в `[ai.cloud.openai]` / ADR 0028.
- Секреты только в `ai-keys.toml` — **никогда** в prompt и логах чата.
- **Tier split (рекомендация):** coder model для epic; отдельная дешёвая модель для **явного** Persist/Prune summary (ADCM; не silent rewrite).
- UI: видимость token/cost pressure для оператора → раньше fork/checkpoint.

### 4.2 Tool plane

**Always-on MCP bundle** (пути — per-machine `mcp.json`):

| Server | Назначение для модели |
|--------|------------------------|
| **agent-notes** | KB read/write; L0 hot; канон вне prompt |
| **roslyn** | C# symbols, diagnostics, refactor |
| **python** | CASA / payload |
| **hybrid-codebase-index** | semantic «где живёт» без full grep |
| **CIDE loopback** ([0082](0082-acp-ide-mcp-loopback-single-process.md)) | open file, solution, verify trigger, git — **живое** состояние GUI |

**CIDE in-proc ([0118](0118-agent-notes-core-2-toml-and-knowledge-path.md)):** knowledge-команды (`read_hot_context`, `route_context`, …) через `IdeCommands.Knowledge.*` **без** stdio subprocess — тот же TOML, что `agent-notes-mcp`. External MCP agent-notes остаётся для Cursor/Roo parity.

**Правило:** native CIDE tools **дублируют критичное** из external MCP там, где нужен in-proc state (вкладка, epoch, debug, **L0 hot**).

Transport stratification — [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md); логику тулов не дублировать.

### 4.3 Memory plane

| Механизм | Роль |
|----------|------|
| **L0 hot** (`memory-architecture-v1.json`, `read_hot_context`) | integrity, epistemic default, scope, язык/тон — **«коллега»**, не oracle (§1.4) |
| **Intercom topics** | epic ≠ meta-tooling ≠ spike-* |
| **agent-notes KB** (in-proc в CIDE) | ADR summaries, decisions, open items, corpus on-demand |
| **Session export** | readable transcript до compaction |
| **Agent obligation** | checkpoint в KB при pressure без запроса оператора |

**Граница:** habitat (CASA store, Neumann life, ADR-0088) ≠ harness (CIDE, MCP, hooks). Не смешивать в одном topic/prompt lane.

### 4.4 Verify plane (ядро CIDE)

Норматив — [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md):

- `verify_rung.*` вместо ad-hoc shell.
- `verify_snapshot_id` + stale после write.
- Verify Epoch UI — общий факт для человека и модели.
- Фоновый runner — чат не блокируется на `build.affected`+.

**Идеал продукта:** после нетривиальной C# правки IDE **предлагает** следующий rung, а не ждёт «запусти билд» в чате.

### 4.5 Lifecycle plane — ADCM (не Cursor silent preCompact)

| Событие | Требование harness |
|---------|-------------------|
| ≥ N user turns | dual-channel: visible tape + pending agent context → checkpoint (export + резюме + open items) |
| context pressure (msgs / usage %) | то же; агент выбирает ADCM-тактику (Prevent/Partition/Persist/Prune) — **не** silent rewrite чата |
| Partition continuity | sibling topic + **TopicDecisions + Handover ops** (+ export/якоря) — [0175](0175-adcm-partition-continuity-pair-and-message-anchors.md) (Proposed) |
| Lane switch | предложить новый topic |
| verify stale | запрет семантики «done» без нового rung |

**Cursor reference:** `session_checkpoint_pressure.py` — `stop` → `followup_message` (видно в ленте); `postToolUse` → только агент (недостаточно для оператора). CIDE **не** клонирует Cursor platform summarizer.

**Канон:** KB `playbook-agent-driven-context-management-v1.md`. API не компактит; harness сигнализирует, агент управляет.

**CIDE:** `[agent.harness]` + `ChatHarnessCoordinator` (turns / msgs / usage %) — interim **Done**; дальше — тексты inject / wizard, не «platform preCompact event».

---

<a id="adr0166-priorities"></a>

## 5. Продуктовый backlog (P0–P2)

### 5.1 Interim до product preset (~2026-08)

**Не блокер:** именованный mode «CASA/Neumann T1» в UI и wizard. **Достаточно** ручной политики + in-proc surface (§1.6):

| Шаг | Артефакт | Где |
|-----|----------|-----|
| FM smoke | `[ai.cloud.openai]` → Cloud.ru + `ai-keys.toml` | `%LocalAppData%\CascadeIDE\` |
| KB SSOT | `agent_notes.config_path` = тот же TOML, что `--config` agent-notes-mcp | settings.toml |
| Опц. полный Roslyn / Python | `external_servers_json_path` | шаблон [harness-external-mcp.optional.json](../samples/harness-external-mcp.optional.json) |
| ADCM / hot Prune | `compact_hot_context` = Prune hot only; L0 — **`read_hot_context` в первом ходе** | agent obligation + [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) + KB ADCM playbook |
| Checkpoint | Cursor hooks до Aug; CIDE — agent rules | overlay `.cursor/hooks.json` |

Скрипт: [`scripts/setup/Setup-CideHarness.ps1`](../../scripts/setup/Setup-CideHarness.ps1) — TOML/policy overlay; **product hooks** `[agent.harness]` — см. §5.2 (interim 2026-06-28).

<a id="adr0166-adr-vs-code"></a>

### 5.2 Связанные ADR: норма vs код (честный срез)

Этот ADR = **направление + backlog**. Ссылки на другие ADR **не означают**, что поведение уже в продукте.

| ADR / item | Статус ADR | В коде (2026-06-28) | Что остаётся |
|------------|------------|---------------------|--------------|
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | Accepted · Implemented | 🟢 in-proc + **L0/session/topic** `ChatHarnessCoordinator` | wizard «CASA/Neumann T1» |
| [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) | Accepted · In progress | 🟡 W1–W2 + auto-verify coalescer; telemetry stale in context | Verify Epoch **UI**, W4–W6 |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | **Proposed** | 🟡 `suppress_acp_ide_stdio_inject`; in-proc MCP smoke | loopback HTTP |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | Proposed | 🔴 HTTP MCP notes/forge в CIDE | P1.3 |
| **P0.2** lifecycle / ADCM signals | этот ADR | 🟢 turns+msgs+usage %; dual-channel; inject copy `[harness ADCM · …]` | wizard T1; agent habit ADCM |
| **P0.4** verify habit | этот ADR | 🟡 auto-verify + stale в context/rules sample | Verify Epoch UI |
| [0163](0163-monaco-native-capability-bus-full-forward-migration.md)–[0164](0164-monaco-editor-presentation-projection-and-dock-chrome.md) | In progress | 🟡 Monaco Forward частично | меньше Read целого файла |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Proposed | 🟡 `codebase_index_*` in-proc | полная интеграция DAL/CCU |
| **P1.1** ADCM summary tier (опц.) | этот ADR | 🔴 handoff summary на той же FM | дешёвая модель только для **явного** Persist/Prune summary (не silent chat rewrite) |
| **P1.2** topic fork brief | этот ADR | 🟢 composer template on `/topic create` | structured session tree UI |
| **P2.3** harness telemetry | этот ADR | 🟢 `ide_agent_status` + minimized block | — |

**Практика:** interim harness = **overlay + Cursor hooks + agent obligation** (`read_hot_context`, checkpoint); не ждать, пока все строки таблицы станут 🟢.

### P0 — daily driver для модели

| # | Deliverable | Критерий готовности |
|---|-------------|---------------------|
| P0.1 | **Harness preset (политика)** + setup | **Interim:** `Setup-CideHarness.ps1` + overlay + **`[agent.harness]`** L0 hot on session/topic. **Product (post-Aug):** wizard «CASA/Neumann T1». *Не* «установить MCP bundle» — in-proc §1.6. |
| P0.2 | **ADCM signals** (turns + msgs + usage; dual-channel) | **Done interim:** @40 user turns + @60 msgs/topic + usage % + visible tape + agent pending context. **Not** Cursor silent preCompact clone. |
| P0.3 | **Loopback MCP** MVP ([0082](0082-acp-ide-mcp-loopback-single-process.md)) | **Interim:** suppress ACP stdio + MCP smoke doc. **Остаётся:** loopback HTTP |
| P0.4 | **Verify Epoch default** после C# edits | auto-verify + stale/telemetry в context + MAF rules sample |

### P1 — token economics

| # | Deliverable | Критерий |
|---|-------------|----------|
| P1.1 | ADCM summary tier (опц.) | Явный Persist/Prune summary не на том же coder FM |
| P1.2 | Structured session tree | **Interim:** brief template в composer на fork |
| P1.3 | HTTP MCP для notes/forge ([0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) Phase 2–3) | Те же тулы, другой transport |

### P2 — shared reality человек ↔ модель

| # | Deliverable | Критерий |
|---|-------------|----------|
| P2.1 | Monaco Forward stable ([0163](0163-monaco-native-capability-bus-full-forward-migration.md)–[0164](0164-monaco-editor-presentation-projection-and-dock-chrome.md)) | hover/nav/semantic без Read целого файла |
| P2.2 | Debug MCP parity | attach/continue/stop без taskkill |
| P2.3 | **Harness telemetry** в agent context | **Interim:** `ide_agent_status` harness_* + stale flag |

---

<a id="adr0166-operator"></a>

## 6. Инварианты оператора (минимум ритуалов)

Не дисциплина «вспоминать checkpoint», а **жёсткие lanes**:

| Инвариант | Зачем модели |
|-----------|--------------|
| Один epic → один topic | нет смешения контекста |
| meta-tooling ≠ epic-fsr | billing/FM не в кодовом чате |
| spike ≤ 1 сессия → новый topic | не раздувает epic |
| «Продолжим завтра» → KB checkpoint или export | silent rewrite не стирает решения |

Остальное — **автоматизация** (hooks, preset, agent rules как fallback до P0.2).

---

<a id="adr0166-decision"></a>

## 7. Решение

1. **Harness — first-class product surface** CIDE, не побочный эффект чата.
2. **«Ничего о нас без нас»** — норматив; gate §2.4 перед Accepted harness-изменений.
3. **Приоритет:** P0.3–P0.4 (loopback + verify) **параллельно** P0.2 (hooks); editor Forward — P2, не блокирует harness MVP.
4. **KB playbook** остаётся operational checklist; **этот ADR** — нормативное направление в репо cascade-ide.
5. **Roo / VS Code** — fallback harness с тем же `mcp.json` и FM URL; primary — CIDE.

---

## 8. Последствия

### Положительные

- Измеримый прогресс к «жизни без Cursor Pro» на pay-per-token.
- AEE и harness усиливают друг друга (truth + memory).
- ADR даёт якорь для issue/forge и для агента при длинных сессиях.

### Отрицательные / риски

- Hook parity — product work (1–2 дня), не только rules.
- Два harness (Cursor до ~Aug **2026**, CIDE после) — временный dual maintenance.
- Prune/summary без checkpoint **опасен** — только с lifecycle + ADCM (явный Persist).

### Не в scope

- Замена CASA habitat / Neumann life контуром CIDE.
- Выбор конкретной FM-модели Cloud.ru (smoke и benchmark — отдельный spike).
- Полная ACP parity с Cursor ([0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md)) — отдельные ADR/фазы.

---

## 9. Фазы (связь с playbook)

| Фаза | Содержание | Статус |
|------|------------|--------|
| **H0** | Cloud.ru key + `settings.toml` + smoke | 🔶 оператор |
| **H1** | **Политика T1** (FM, agent_notes TOML, topics, rules) + опц. external roslyn/python | 🔶 in-proc §1.6; не ждать UI preset |
| **H2** | CIDE hook parity | 🔴 product gap |
| **H3** | Loopback MCP (wire only) | 🔴 [0082](0082-acp-ide-mcp-loopback-single-process.md) — не duplicate MCP install |
| **H4** | Verify Epoch habit + AEE defaults | 🟡 [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) in progress |
| **H5** | Compactor tier split + HTTP MCP notes | 🔶 [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) |

---

## 10. История

| Дата | Change |
|------|--------|
| 2026-06-25 | Proposed: agent-centric harness, five planes, P0–P2, co-design с оператором |
| 2026-06-25 | §2: stakeholder «ничего о нас без нас», процесс, checklist §2.4 |
| 2026-06-25 | §1.3–1.6: overlay vs Cursor product; L0/colleague criterion; Junior curve; CIDE in-proc KB [0118] |
| 2026-06-28 | §1.6 in-proc tool surface; §2.2 co-design границы; §5.1 interim T1; §5.2 ADR vs код; P0.1/P0.3 уточнены |
| 2026-06-28 | **Product interim:** `[agent.harness]` — L0 hot, checkpoint @40, auto-verify, suppress ACP stdio; §5.2 обновлён |

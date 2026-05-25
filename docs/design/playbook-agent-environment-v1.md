# Playbook: agent execution environment v1

ADR: [0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)  
KB (agent-notes MCP): `knowledge/work/projects/door-to-singularity/cascade-ide/playbook-agent-execution-environment-v1.md` в каноне (`AGENT_NOTES_CANON_PATH`, типично `D:\Experiments\agent-notes`).  
Связь: [0141](../adr/0141-solution-scoped-warmup-orchestration.md) (warm substrate), [0038](../adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) (фасад), [0138](../adr/0138-cockpit-command-line-and-parametric-ranges.md) (CCL/slash).

Полная норма: ADR [0148 §2.2–2.3, §5.1](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

## Культура сессии

*Читай ADR → спорь с нормой → дописывай историю.* — не «лычка senior» в промпте, а работа с **памятью команды** (ADR, playbook, rejected, history). Развёрнуто: [cascadeide-philosophy-v1 §8.1](../design/cascadeide-philosophy-v1.md#81-память-команды-не-лычка-в-промпте).

## Тезис: два такта

| Такт | Что ограничивает | Типичный dev tooling |
|------|------------------|----------------------|
| **Когнитивный** | Скорость мышления (человек, агент ×10–×100) | Не учитывается |
| **Среды** | Build, test, shell, I/O, DB lock, cold start | **Единственный**, на котором всё «висит» |

Проблема не в том, что «агент медленный», а в том, что **среда не успевает за мыслью**. Cursor/MCP/shell-first цикл усиливает это: каждый шаг — round-trip + subprocess + ожидание человека на verify.

CascadeIDE standalone должна **сопоставить bandwidth среды с bandwidth мышления**: параллельный verify, push вместо poll, in-process Roslyn/build/test, прозрачный учёт времени.

## Открытый .NET stack (кратко)

CLI (`dotnet build|test|format`) — оболочка над **MIT/Apache** библиотеками. AEE **не форкает SDK**; L0 Roslyn in-proc; L1+ — **supervised build host** ([0148 §5.2](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)). Subprocess — E-tier.

| Rung | MLP |
|------|-----|
| L0 | Roslyn diagnostics + format (in-proc; SG-aware) |
| L1–L2 | Supervised build host |
| L3 | Supervised test host |

Детали, границы (VS IDE, Code Coverage license, VMR): [0148 §2.3](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

## Verification ladder (кратко)

| Уровень | Действие | Цель |
|---------|----------|------|
| **L0** | Roslyn diagnostics по затронутым файлам | Секунды, без shell |
| **L1** | Incremental build (project/slice) | Минуты → секунды на warm solution |
| **L2** | Targeted test filter | Только релевантные тесты |
| **L3** | Full test / integration | Перед merge / по запросу |
| **L4** | CI parity (ideal) | Вне IDE |

Агент **не блокирует чат** на L2+ — runner + DataBus события + `/agent status|cancel`.

## W1 (первая поставка кода)

1. **`EnvironmentTaskRunner`** — очередь, cancel, progress, id; **одна** verify-цепочка; implicit cancel predecessor.
2. **`verify_snapshot_id`** + stale на write в `in_verification` set (DataBus `AgentVerifyEpochStale`).
3. **События в DataBus** — `environment.task.*` + `AgentEnvironmentTaskDied` ([0099](../adr/0099-databus-event-fabric-and-cross-feature-notifications.md)).
4. **Time accounting в чате** — wall / active / environment / **blocked** (не `idle_user` — W3+).
5. **Slash** — `/agent verify`, `/agent cancel`, `/agent status` ([0138](../adr/0138-cockpit-command-line-and-parametric-ranges.md)).
6. **Ephemeral sandbox** — substrate bundle (WitDB + ports + temp); fresh per L3+ task.

Не в W1: worktree sandbox, batch native tools, L4, PFD instrument deck, gutter dim для epoch, `idle_user`.

## Матрица согласованности среды (W1–MLP)

Сводка для runner / UI / orchestrator — норма [0148 §8](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md):

| Зона | MLP | Не в MLP |
|------|-----|----------|
| Stale context | `verify_snapshot_id` + coalesce 1.5 s; green **S** ≠ **S′** | Auto-rollback tree; worktree на каждый verify |
| State bleeding | Fresh **substrate bundle** per L3+ task | Rollback-only в тестах |
| Metrics | reasoning \| environment \| blocked | `idle_user` (W3+) |
| Source generators | SG-aware L0 → L1 host | Отдельный GeneratorDriver в CIDE |

## Слепые зоны (концептуальный аудит, review Orion 2026-05-25)

Четыре темы, которые легко недооценить при W2–W4. Нормативная деталь — [0148 §8.1](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

### 1. Stale context (параллельный когнитивный и средовой такт)

Пока runner гоняет L2/L3, агент/оператор может **писать дальше**. Падение verify через 40 с не откатывает «мысль за эти 40 с» автоматически.

| Подход | MLP | Идеал |
|--------|-----|-------|
| **Verify epoch** — каждый run привязан к `snapshot_id` (git HEAD + dirty set) | ✓ в метаданных run | UI показывает «verify устарел» |
| **Implicit cancel predecessor** — новый verify отменяет старый на L1+ | ✓ W1 | |
| **Soft lock** — предупреждение при правке файлов из `in_verification` set | ✓ stale event; gutter dim W3+ | |
| **Environment branch** — когнитивный такт агента в `agent_worktree` до green | W4/W6 | default для autonomous long runs |
| Hard rollback operator workspace к pre-verify | ✗ default | опасно сотрёт работу человека |

**Не путать** с sandbox DB: это про **согласованность кода и гипотезы «билд зелёный»**.

### 2. Идемпотентность ephemeral DB (state bleeding)

L3/L4 **пишут** в БД. Повторный прогон на той же temp DB — грязное состояние.

| Подход | Когда |
|--------|-------|
| **Fresh substrate per ladder jump** — новый **bundle** (data dir + ports + temp) перед каждым L3+ task | MLP для integration |
| **CoW / snapshot** data dir (SQLite file copy, volume snapshot) | ideal |
| **Rollback transaction** на test host после run | если тесты поддерживают shared fixture — осторожно |

Связь с §6 ADR: ephemeral ≠ «одна БД на всю сессию агента».

### 3. Metrics dilution (blocked vs idle human)

`Blocked Time` = ожидание **среды**. Если оператор ушёл за кофе — это не fault tooling.

| Фаза | Смысл |
|------|-------|
| `reasoning` | Модель / orchestrator |
| `environment` | Runner / build host |
| `idle_user` | Нет фокуса CIDE / нет ввода N сек (опционально W3+) |
| `blocked` | Только когда run **активен** и среда не отвечает |

Иначе аналитика «эффективности агента» смешивает Slack и MSBuild.

### 4. Roslyn L0 vs Source Generators

L0 diagnostics **без** сгенерированных документов даёт ложные CS1061 при зелёном `dotnet build` (см. roslyn-mcp / [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md)).

| Подход | Владелец |
|--------|----------|
| `GetSourceGeneratedDocumentsAsync` + актуальный `CurrentSolution` | L0 path (Roslyn in-proc) |
| Warm item `agent.source_generators` / reuse 0141 HCI sidecar | [0141](../adr/0141-solution-scoped-warmup-orchestration.md) P2+ |
| Если SG не прогрет — **не обещать** «мгновенный L0»; поднимать rung до L1 build host | ladder policy |

**Правило:** L0 green **не** auto-climb на L1, если известны SG в проекте и generated docs stale.

## Операционные инварианты (Orion round 2, 2026-05-25)

Норма: [0148 §8.1.1, §8.1.5](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

### Verify coalesce + cancel

- После окна coalesce новый verify **отменяет** предыдущий на L1+ (implicit cancel predecessor).
- Две параллельные verify-цепочки в одной session — **запрещены**.

### Human parity (оператор в контуре)

- Те же stale rules, что для агента: write в файл epoch → `AgentVerifyEpochStale` сразу.
- W3+: визуальное «заморожение»/тускнение файлов из dirty set epoch (gutter / status).

### Supervised host death

- Crash/OOM host → `AgentEnvironmentTaskDied`, не ждать timeout.
- Orchestrator: alert + restart host; не assume green.

## Для быстрого оператора (человек)

- Те же runner и ladder — не «только для агента».
- Keyboard/slash первичны; mouse — для обзора, не для unblock.
- Warm-up ([0141](../adr/0141-solution-scoped-warmup-orchestration.md)) снижает L1 latency до открытия solution.

## Анти-паттерны

- Default `dotnet test` без filter после каждой правки.
- Shell как единственный verify path для C#.
- Блокирующий чат до зелёного CI локально.
- Скрытое время среды (агент «думает», пока идёт build).
- Писать код «как будто verify уже зелёный», не глядя на `verify epoch` / stale run.
- Переиспользовать одну ephemeral БД на несколько L3+ без refresh substrate.
- Две параллельные `/agent verify` без отмены первой.
- Считать green завершённого verify после правки файлов из epoch dirty set.

## Открытые вопросы (из ADR)

Orchestrator ownership, ACP parity, auto test filters, CASCOPE для raw shell, cross-platform process supervise — см. [0148 § Open questions](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

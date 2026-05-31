# Playbook: agent execution environment v1

ADR: [0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)  
Naming: [naming-layers-v1.md](naming-layers-v1.md) · UX: [agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md)  
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

CLI (`dotnet build|test|format`) — оболочка над **MIT/Apache** библиотеками. AEE **не форкает SDK**; `diagnose.files` Roslyn in-proc; `compile.project`+ — **supervised build host** ([0148 §5.2](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)). Subprocess — E-tier.

| verify_rung | MLP |
|-------------|-----|
| `diagnose.files` | Roslyn diagnostics + format (in-proc; SG-aware) |
| `compile.project` | Semantic compile affected projects (build host) |
| `build.affected` | Incremental project build (build host) |
| `test.scoped` | Supervised test host, filter |
| `test.full` | Full suite (ideal / ci_parity) |

Детали, границы (VS IDE, Code Coverage license, VMR): [0148 §2.3](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

## Verification ladder (кратко)

| verify_rung | Действие | Цель |
|-------------|----------|------|
| **`diagnose.files`** | Roslyn diagnostics по затронутым файлам | Секунды, без shell |
| **`compile.project`** | Compile / semantic model affected projects | Cross-file types без full build |
| **`build.affected`** | Incremental build project(s) | NuGet/refs, MSBuild graph |
| **`test.scoped`** | Filtered tests | Поведение, unit scope |
| **`test.full`** | Full suite / integration | Merge gate, ci_parity |

Legacy `L0–L4` — **не использовать** в UI и новых docs ([naming-layers-v1.md](naming-layers-v1.md)).

Политики: `minimal` | `standard` (default) | `strict` | `ci_parity`. Агент **не блокирует чат** на `build.affected`+ — runner + DataBus + Verify Epoch UI ([agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md)).

## W1 (первая поставка кода)

1. **`EnvironmentTaskRunner`** — очередь, cancel, progress, id; **одна** verify-цепочка; implicit cancel predecessor.
2. **`verify_snapshot_id`** + stale на write в `in_verification` set (DataBus `AgentVerifyEpochStale`).
3. **События в DataBus** — `environment.task.*` + `AgentEnvironmentTaskDied` ([0099](../adr/0099-databus-event-fabric-and-cross-feature-notifications.md)).
4. **Time accounting в чате** — wall / active / environment / **blocked** (не `idle_user` — W3+).
5. **Slash** — `/agent verify`, `/agent cancel`, `/agent status` ([0138](../adr/0138-cockpit-command-line-and-parametric-ranges.md)).
6. **Ephemeral sandbox** — substrate bundle (WitDB + ports + temp); fresh per `test.scoped`+ task.

На rung `test.scoped` переменные **`CASCADE_AGENT_SUBSTRATE_WIT_DB`** и **`CASCADE_AGENT_SUBSTRATE_DEV_PORT`** добавляются в среду процесса `dotnet test`.

Не в W1: worktree sandbox, batch native tools, PFD Verify Epoch instrument, gutter dim для epoch, `idle_user`.

## Матрица согласованности среды (W1–MLP)

Сводка для runner / UI / orchestrator — норма [0148 §8](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md):

| Зона | MLP | Не в MLP |
|------|-----|----------|
| Stale context | `verify_snapshot_id` + coalesce 1.5 s; green **S** ≠ **S′** | Auto-rollback tree; worktree на каждый verify |
| State bleeding | Fresh **substrate bundle** per `test.scoped`+ task | Rollback-only в тестах |
| Metrics | reasoning \| environment \| blocked | `idle_user` (W3+) |
| Source generators | SG-aware `diagnose.files` → `compile.project` host | Отдельный GeneratorDriver в CIDE |

## Слепые зоны (концептуальный аудит, review Orion 2026-05-25)

Четыре темы, которые легко недооценить при W2–W4. Нормативная деталь — [0148 §8.1](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

### 1. Stale context (параллельный когнитивный и средовой такт)

Пока runner гоняет `build.affected`/`test.scoped`, агент/оператор может **писать дальше**. Падение verify через 40 с не откатывает «мысль за эти 40 с» автоматически.

| Подход | MLP | Идеал |
|--------|-----|-------|
| **Verify epoch** — каждый run привязан к `snapshot_id` (git HEAD + dirty set) | ✓ в метаданных run | UI Verify Epoch ([agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md)) |
| **Implicit cancel predecessor** — новый verify отменяет старый на `compile.project`+ | ✓ W1 | |
| **Soft lock** — предупреждение при правке файлов из `in_verification` set | ✓ stale event; gutter dim W3+ | |
| **Environment branch** — когнитивный такт агента в `agent_worktree` до green | W4/W6 | default для autonomous long runs |
| Hard rollback operator workspace к pre-verify | ✗ default | опасно сотрёт работу человека |

**Не путать** с sandbox DB: это про **согласованность кода и гипотезы «билд зелёный»**.

### 2. Идемпотентность ephemeral DB (state bleeding)

`test.scoped`/`test.full` **пишут** в БД. Повторный прогон на той же temp DB — грязное состояние.

| Подход | Когда |
|--------|-------|
| **Fresh substrate per ladder jump** — новый **bundle** (data dir + ports + temp) перед каждым `test.scoped`+ task | MLP для integration |
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

### 4. Roslyn `diagnose.files` vs Source Generators

Diagnostics **без** сгенерированных документов даёт ложные CS1061 при зелёном `dotnet build` (см. roslyn-mcp / [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md)).

| Подход | Владелец |
|--------|----------|
| `GetSourceGeneratedDocumentsAsync` + актуальный `CurrentSolution` | `diagnose.files` path (Roslyn in-proc) |
| Warm item `agent.source_generators` / reuse 0141 HCI sidecar | [0141](../adr/0141-solution-scoped-warmup-orchestration.md) P2+ |
| Если SG не прогрет — **не обещать** мгновенный `diagnose.files`; поднимать rung до `compile.project` | ladder policy |

**Правило:** `diagnose.files` green **не** auto-climb на `compile.project`, если SG stale.

## Операционные инварианты (Orion round 2, 2026-05-25)

Норма: [0148 §8.1.1, §8.1.5](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

### Verify coalesce + cancel

- После окна coalesce новый verify **отменяет** предыдущий на `compile.project`+ (implicit cancel predecessor).
- Две параллельные verify-цепочки в одной session — **запрещены**.

### Human parity (оператор в контуре)

- Те же stale rules, что для агента: write в файл epoch → `AgentVerifyEpochStale` сразу.
- W3+: Verify Epoch UI + gutter dim ([agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md)).

### Supervised host death

- Crash/OOM host → `AgentEnvironmentTaskDied`, не ждать timeout.
- Orchestrator: alert + restart host; не assume green.

## Для быстрого оператора (человек)

- Те же runner и ladder — не «только для агента».
- Keyboard/slash первичны; mouse — для обзора, не для unblock.
- Warm-up ([0141](../adr/0141-solution-scoped-warmup-orchestration.md)) снижает latency `compile.project` до открытия solution.

## Анти-паттерны

- Default `dotnet test` без filter после каждой правки.
- Shell как единственный verify path для C#.
- Блокирующий чат до зелёного CI локально.
- Скрытое время среды (агент «думает», пока идёт build).
- «Агент сказал green» без Verify Epoch на **текущем** snapshot (Cursor anti-pattern).
- Писать код «как будто verify уже зелёный», не глядя на verify epoch / stale run.
- Переиспользовать одну ephemeral БД на несколько `test.scoped`+ без refresh substrate.
- Две параллельные `/agent verify` без отмены первой.
- Считать green завершённого verify после правки файлов из epoch dirty set.
- Голые «L2» в verify-контексте — использовать `verify_rung` ([naming-layers-v1.md](naming-layers-v1.md)).

## Локальный test-drive (Orion)

- Док: [aee-orion-local-test-drive-v1.md](aee-orion-local-test-drive-v1.md)
- Скрипт: `scripts/aee/orion-test-drive.ps1`
- xUnit: `Category=AgentEnvironment` → `AgentEnvironmentOrionStressTests`

## Открытые вопросы (из ADR)

Orchestrator ownership, ACP parity, auto test filters, CASCOPE для raw shell, cross-platform process supervise — см. [0148 § Open questions](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

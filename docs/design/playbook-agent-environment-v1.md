# Playbook: agent execution environment v1

ADR: [0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)  
Связь: [0141](../adr/0141-solution-scoped-warmup-orchestration.md) (warm substrate), [0038](../adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) (фасад), [0138](../adr/0138-cockpit-command-line-and-parametric-ranges.md) (CCL/slash).

Полная норма: ADR [0148 §2.2–2.3, §5.1](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

## Тезис: два такта

| Такт | Что ограничивает | Типичный dev tooling |
|------|------------------|----------------------|
| **Когнитивный** | Скорость мышления (человек, агент ×10–×100) | Не учитывается |
| **Среды** | Build, test, shell, I/O, DB lock, cold start | **Единственный**, на котором всё «висит» |

Проблема не в том, что «агент медленный», а в том, что **среда не успевает за мыслью**. Cursor/MCP/shell-first цикл усиливает это: каждый шаг — round-trip + subprocess + ожидание человека на verify.

CascadeIDE standalone должна **сопоставить bandwidth среды с bandwidth мышления**: параллельный verify, push вместо poll, in-process Roslyn/build/test, прозрачный учёт времени.

## Открытый .NET stack (кратко)

CLI (`dotnet build|test|format`) — оболочка над **MIT/Apache** библиотеками. AEE **не форкает SDK**; default — **in-process** (`IAeeTool` → Roslyn / MSBuild / VSTest). Subprocess — E-tier и текущий техдолг (`McpDotnetBuildTestService`) до W2.

| Rung | In-process |
|------|------------|
| L0 | Roslyn diagnostics + `Formatter` |
| L1–L2 | MSBuild incremental |
| L3 | VSTest filter |

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

1. **`EnvironmentTaskRunner`** — очередь задач среды, cancel, progress, id.
2. **События в DataBus** — `environment.task.started|progress|completed|failed|cancelled` (см. [0099](../adr/0099-databus-event-fabric-and-cross-feature-notifications.md)).
3. **Time accounting в чате** — wall vs active vs blocked (ожидание среды).
4. **Slash** — `/agent verify`, `/agent cancel`, `/agent status` ([0138](../adr/0138-cockpit-command-line-and-parametric-ranges.md)).
5. **Ephemeral sandbox** — изолированный WitDB / temp scope (урок intercom-service tests).

Не в W1: worktree sandbox, batch native tools, L4, PFD instrument deck.

## Для быстрого оператора (человек)

- Те же runner и ladder — не «только для агента».
- Keyboard/slash первичны; mouse — для обзора, не для unblock.
- Warm-up ([0141](../adr/0141-solution-scoped-warmup-orchestration.md)) снижает L1 latency до открытия solution.

## Анти-паттерны

- Default `dotnet test` без filter после каждой правки.
- Shell как единственный verify path для C#.
- Блокирующий чат до зелёного CI локально.
- Скрытое время среды (агент «думает», пока идёт build).

## Открытые вопросы (из ADR)

Orchestrator ownership, ACP parity, auto test filters, CASCOPE для raw shell, cross-platform process supervise — см. [0148 § Open questions](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md).

# AEE Orion local test-drive (v1)

Сценарии от ревью Orion, привязка к **MLP** (текущая реализация) и автоматизация.

## Быстрый запуск

```powershell
pwsh -File scripts/aee/orion-test-drive.ps1
pwsh -File scripts/aee/orion-test-drive.ps1 -Ui   # + чеклист в IDE
```

Фильтр xUnit: `Category=AgentEnvironment` → класс `AgentEnvironmentOrionStressTests`.

## 1. Stale context & epochs (выносливость)

| Orion | MLP сейчас | Как проверить |
|-------|------------|---------------|
| Микро-правки 500 ms + `/agent verify` | `AgentVerifyEpochTracker` → `AgentVerifyEpochStale`; новый verify **отменяет** предыдущий (`superseded`) | `Stress_MicroWrites_*`, `Stress_RapidVerify_*`, `Stress_RapidVerifyAndMicroWrites_*` |
| Coalesce 1.5 s схлопывает запросы | **Только L2 build** внутри одного climb (`EnvironmentTaskDedup`), не очередь slash-verify | `Stress_Dedup_*` |
| Очередь CancellationToken | Один `_activeCts` на run; при новом verify — cancel + dispose | `Stress_RapidVerify_SupersedesPredecessor_*` |

**В IDE:** открыть solution → `/agent verify standard` → править `.cs` каждые ~500 ms → `/agent status` (не должен зависать).

## 2. Substrate bundle (глухая изоляция)

| Orion | MLP сейчас | Как проверить |
|-------|------------|---------------|
| `_cide_sandbox/{run_id}` | `%LocalAppData%/CascadeIDE/agent-runs/{run_id}/substrate/` | `/agent sandbox agent_ephemeral` |
| Порты без `SocketException` | `AgentSandboxSubstrate.ReserveFreeTcpPort()` на run | `Stress_ParallelSubstrate_*` |
| БД не протекает между run | `wit.db` + `owner.txt` per run; `RecreateSubstrateBeforeTests` перед L3 | тот же тест |

## 3. Supervised host death

| Orion | MLP сейчас | Как проверить |
|-------|------------|---------------|
| `FailFast` / kill MSBuild worker | Host kind `supervised-inproc`; смерть job → `AgentEnvironmentTaskDied` | `Stress_HostDeath_*` (`TestJobStatusFactory` → null status) |
| UI glyph Died | DataBus → `RefreshPfdBackgroundStatusBar` (нет отдельного glyph W3+) | UI: убить dotnet во время verify (ручной краш) |
| Чат не зависает | `AgentRunCompleted` / cancel; chat projection async | UI: `/agent cancel` после сбоя |

**Идеал (не MLP gate):** отдельный supervised MSBuild process + auto-restart (ADR §8.1.5 open Q #10).

## Ограничения тестов без IDE

- Интеграция с реальным `CascadeIDE.sln` — тесты ищут solution вверх от `bin/`; если нет — часть тестов no-op `return`.
- `Environment.FailFast` в процессе теста убьёт весь test host — используем симуляцию через `job not found` → `TaskDied`.

## Связанные документы

- [ADR 0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)
- [playbook-agent-environment-v1.md](playbook-agent-environment-v1.md)

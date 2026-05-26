# Out-of-process verify worker

Текущее состояние (CIDE + AEE):

| `build_verify_host` | Поведение |
|---------------------|-----------|
| `supervised-inproc` (default) | `BuildTestJobCoordinator` в IDE; `dotnet build`/`test` в дочерних процессах |
| `supervised-worker-process` | One-shot: `dotnet exec BuildVerifyWorker.dll build\|test …` на каждый job |
| `supervised-worker-daemon` (opt-in) | Long-lived: `dotnet exec … serve`, JSON-lines IPC (enqueue / status / wait / cancel) |

Долгая цель ADR 0148: изоляция coordinator и нагрузки от UI; daemon снижает latency повторных verify.

## Артефакт

`tools/CascadeIDE.BuildVerifyWorker` — консоль **net10.0**. Копируется рядом с `CascadeIDE.exe` (`CopyBuildVerifyWorker`).

### Настройки (`[agent.environment]`)

```toml
build_verify_host = "supervised-inproc"
# build_verify_worker_assembly_path = ""
```

### CLI one-shot

```bash
dotnet exec CascadeIDE.BuildVerifyWorker.dll build Path/To/Solution.sln
dotnet exec CascadeIDE.BuildVerifyWorker.dll test Path/To/Solution.sln --filter FullyQualifiedName~SomeTests
```

### Daemon IPC (`serve`)

Запуск: `dotnet exec CascadeIDE.BuildVerifyWorker.dll serve`

- Первая строка stdout: `{"id":"0","ok":true,"ready":true,"protocol":1}`
- Далее: одна JSON-строка на запрос (stdin) и одна на ответ (stdout)

Операции:

| `op` | Поля | Ответ |
|------|------|--------|
| `ping` | — | `pong: true` |
| `enqueue` | `kind` build\|test, `path`, `filter?`, `timeout_seconds?`, `include_raw_output?` | `accepted`, `job_id` |
| `get_status` | `job_id` | `status` (как у coordinator) |
| `wait` | `job_id` | `result_json` |
| `cancel` | `job_id` | `cancelled` |
| `shutdown` | — | завершение процесса |

Закрытие stdin воркера → graceful exit.

Клиент в IDE: `BuildVerifyWorkerDaemonClient` + `DaemonBuildVerifyWorkerBackend`.

## L0 + warmup

При `l0_include_warmup_cs = true` L0 добавляет `.cs` из solution warm-up (см. defaults `agent.environment.ladder`).

## Следующий шаг

- Auto-restart daemon при падении (policy в settings).
- Semantic L0 / affected-project graph (0141+).

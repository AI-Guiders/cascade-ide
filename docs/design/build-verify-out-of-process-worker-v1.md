# Out-of-process verify worker (scaffold)

Текущее состояние (CIDE + AEE):

- `BuildTestJobCoordinator` живёт **в процессе CascadeIDE.** Каждая job вызывает **`DotnetProcessRunner`**: `dotnet build` / `dotnet test` уже в **отдельном OS-процессе** dotnet — т.е. типичный MSBuild **не** выполняется «внутри» VM Avalonia, а в дочернем `dotnet`.
- Долгая цель из [ADR 0148](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md): **полный** вынес очереди/coordinator в supervised worker (единый долгоживущий процесс, RPC/stdio), чтобы локализовать нагрузку и сбои ещё до уровня IDE.

## Артефакт

`tools/CascadeIDE.BuildVerifyWorker` — консоль **net10.0**, которая создаёт свой `BuildTestJobCoordinator` в **отдельном от CIDE** процессе и синхронно ждёт результат (одна команда `build` или `test`). Тот же стек, что и в IDE (`DotNetBuildTest.Core`).

### Примеры

```bash
dotnet run --project tools/CascadeIDE.BuildVerifyWorker/CascadeIDE.BuildVerifyWorker.csproj -- build Path/To/Solution.sln
dotnet run --project tools/CascadeIDE.BuildVerifyWorker/CascadeIDE.BuildVerifyWorker.csproj -- test Path/To/Solution.sln --filter FullyQualifiedName~SomeTests
```

Выход: structured JSON в stdout; exit code 0 при `success: true`, иначе 1 (или 11 при `busy` очереди координатора внутри воркера).

## Следующий шаг (не в этом срезе)

- IPC / long-lived worker + маршрутизация `CancelJobsForRun` и `job_id` между CIDE и процессом (см. `EnvironmentTaskRunner`).
- Опционально: deploy layout (копировать `BuildVerifyWorker` рядом с CIDE и вызывать по абсолютному пути из настроек).

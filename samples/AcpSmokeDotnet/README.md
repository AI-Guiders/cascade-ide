# AcpSmokeDotnet — тот же ACP-smoke, что `samples/AcpSmoke`, на .NET

Зачем: проверить цепочку **initialize → new_session → prompt** с тем же эталонным `echo_agent.py`, но клиентом на C# (**тот же** вендор `AgentClientProtocol`, что и IDE: `ProjectReference` на [`externals/acp-csharp`](../../externals/acp-csharp) — неофициальный SDK [nuskey8/acp-csharp](https://github.com/nuskey8/acp-csharp); пакет на [nuget.org](https://www.nuget.org/packages/AgentClientProtocol) может отставать по сигнатурам).

## Требования

- .NET 10 SDK (как у решения CascadeIDE).
- Python 3.10+ и зависимости для агента: из каталога `samples/AcpSmoke` выполни `pip install -r requirements.txt` (нужен только запуск `echo_agent.py`).

## Запуск

Из корня репозитория `cascade-ide`:

```bash
dotnet run --project samples/AcpSmokeDotnet/AcpSmokeDotnet.csproj
```

Или из `samples/AcpSmokeDotnet`:

```bash
dotnet run
```

Опционально: путь к `echo_agent.py` первым аргументом или переменная окружения `ACP_ECHO_AGENT_PATH`; интерпретатор Python — `ACP_PYTHON` (по умолчанию `python`).

Ожидается вывод с `initialize`, `session`, `session_update`, `ACP smoke (.NET) OK`.

## Связь с Python-smoke

Логика совпадает с `samples/AcpSmoke/smoke_client.py`: subprocess → stdio → `ClientSideConnection`. Официального ACP SDK для .NET от авторов протокола нет — используется community-реализация из `externals/acp-csharp` (как в `CascadeIDE.csproj`).

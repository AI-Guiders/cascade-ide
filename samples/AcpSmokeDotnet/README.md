# AcpSmokeDotnet — тот же ACP-smoke, что `samples/AcpSmoke`, на .NET

Зачем: проверить цепочку **initialize → new_session → prompt** с тем же эталонным `echo_agent.py`, но клиентом на C# ([NuGet `AgentClientProtocol`](https://www.nuget.org/packages/AgentClientProtocol) — неофициальный SDK [nuskey8/acp-csharp](https://github.com/nuskey8/acp-csharp)).

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

Логика совпадает с `samples/AcpSmoke/smoke_client.py`: subprocess → stdio → `ClientSideConnection`. Официального ACP SDK для .NET от авторов протокола нет — используется community-пакет; версию см. в `AcpSmokeDotnet.csproj`.

# AcpSmoke — локальная проверка Agent Client Protocol (ACP)

Зачем: убедиться, что **клиентская** сторона ACP (как будущий Cascade) может поднять subprocess-агента по stdio и пройти `initialize` → `new_session` → `prompt`. Это не интеграция с Cursor, а **эталонный echo-агент** из официального [python-sdk](https://github.com/agentclientprotocol/python-sdk) (`examples/echo_agent.py`).

## Требования

- Python 3.10+ (см. `echo_agent.py`, PEP 723 deps)
- `pip` или `uv`

## Запуск

```bash
cd samples/AcpSmoke
pip install -r requirements.txt
python smoke_client.py
```

Ожидается вывод вида `session_update: ...` и строка `ACP smoke OK`.

## Файлы

| Файл | Назначение |
|------|------------|
| `echo_agent.py` | Копия из upstream `examples/echo_agent.py` (обновляй при смене API). |
| `smoke_client.py` | Минимальный клиент через `spawn_agent_process` из пакета `acp`. |
| `requirements.txt` | Зависимость `agent-client-protocol`. |

## Вариант на .NET

Тот же сценарий с тем же `echo_agent.py`: [`samples/AcpSmokeDotnet`](../AcpSmokeDotnet/README.md) (`dotnet run`, пакет `AgentClientProtocol`).

## Cursor и Cascade

Подключение **Cursor как ACP-агента** к JetBrains описано у Cursor ([блог](https://cursor.com/blog/jetbrains-acp)); для Cascade см. заметку [`docs/ui-ux/note-acp-cascade-cursor-v1.md`](../../docs/ui-ux/note-acp-cascade-cursor-v1.md). Этот smoke **не** подставляет бинарник Cursor — только проверяет протокол на референс-агенте.

## Лицензия `echo_agent.py`

Исходный пример из репозитория Agent Client Protocol / python-sdk; при обновлении файла сохраняй соответствие лицензии upstream.

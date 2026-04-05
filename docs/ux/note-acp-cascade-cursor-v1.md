# Заметка: Agent Client Protocol (ACP), Cascade и Cursor

**Статус:** рабочая заметка (не ADR). Фиксирует терминологию, ссылки и направление интеграции; детали протокола — в [официальной спецификации](https://agentclientprotocol.com/).

---

## 1. Терминология: не путать два «ACP»

| Название | Суть | Где смотреть |
| -------- | ---- | ------------ |
| **Agent Client Protocol** | Связь **редактор/IDE ↔ агент**, JSON-RPC; по духу близко к LSP. Cascade в перспективе — **клиент**, внешний Cursor/другой агент — **агент** (или наоборот, если Cascade экспортирует агента). | [agentclientprotocol.com](https://agentclientprotocol.com/), [обзор транспортов](https://agentclientprotocol.com/protocol/transports.md) |
| **Agent Communication Protocol** (другой проект) | Межагентский REST, BeeAI / A2A и т.д. | [agentcommunicationprotocol.dev](https://agentcommunicationprotocol.dev/) — **не то же самое**, хотя аббревиатура совпадает. |

В разговоре часто говорят «Agent Context Protocol» — у Cursor в документации и у JetBrains фигурирует именно **Agent Client Protocol** ([пост Cursor про JetBrains](https://cursor.com/blog/jetbrains-acp)).

---

## 2. Зачем это Cascade

- **Развязка:** IDE не обязана встраивать весь UI чужого агента — достаточно ACP-транспорта (часто **stdio**: клиент поднимает процесс агента, сообщения — JSON-RPC по строкам, без мусора в stdout).
- **Совместимость:** агенты с ACP могут подключаться к разным клиентам; клиент с ACP — к разным агентам ([реестр](https://agentclientprotocol.com/get-started/registry.md)).
- **Связь с UX:** внешний агент и отдельное окно Cursor логично считать **отдельным «дисплеем»**, не смешивая с внутренним PFD/MFD — см. [`concept-pfd-mfd-cascade-v1.md`](concept-pfd-mfd-cascade-v1.md) §8.

---

## 3. Cursor как агент

- Cursor поставляет ACP-совместимого агента для сторонних IDE (например JetBrains): установка из реестра ACP, авторизация аккаунтом Cursor ([доки Cursor](https://cursor.com/docs/cli/acp#ide-integrations)).
- Точная команда запуска и флаги — по актуальной документации Cursor; в этой заметке не фиксируем (могут меняться).

---

## 4. Что делать в Cascade (направления, без обязательств)

1. Спроектировать **роль клиента ACP**: запуск процесса агента, stdio, жизненный цикл сессий, отображение потока обновлений (diff, tool calls — см. спеку).
2. Отделить **встроенный чат/MCP** от **сессии ACP** в UI (разные поверхности или явные режимы).
3. Опционально: **MCP-over-ACP** ([RFD](https://agentclientprotocol.com/rfds/mcp-over-acp.md)) — если нужен единый контур с уже существующими MCP-инструментами.

---

## 5. Локальный smoke (репозиторий)

В [`samples/AcpSmoke/README.md`](../../samples/AcpSmoke/README.md) — минимальный Python-клиент + эталонный `echo_agent` из upstream: проверка, что цепочка `initialize` → `new_session` → `prompt` работает на машине разработчика. Это **не** замена интеграционного теста с Cursor.

---

## 6. Полезные ссылки

- [Introduction](https://agentclientprotocol.com/get-started/introduction.md)
- [Initialization](https://agentclientprotocol.com/protocol/initialization.md), [Prompt turn](https://agentclientprotocol.com/protocol/prompt-turn.md)
- [Python SDK](https://agentclientprotocol.com/libraries/python.md), [TypeScript SDK](https://agentclientprotocol.com/libraries/typescript.md)
- Список [клиентов и редакторов](https://agentclientprotocol.com/get-started/clients.md)

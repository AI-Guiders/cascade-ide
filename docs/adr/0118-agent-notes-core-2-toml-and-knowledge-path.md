# ADR 0118: Agent Notes Core 2.0 — TOML, `knowledge_path`, паритет с agent-notes-mcp

**Статус:** Proposed  
**Дата:** 2026-05-16  

**Связь:** [0019](0019-shared-git-core-ide-and-git-mcp.md) (прецедент общего Core), [0028](0028-user-settings-toml-localappdata-and-secrets.md) / [0029](0029-configuration-toml-canonical-ui-facade.md) (TOML в IDE), [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) (поверхность `IdeCommands`), [0008](0008-mcp-contracts-and-testable-infrastructure.md). Вне репо: **agent-notes-mcp** [ADR 014](https://github.com/AI-Guiders/agent-notes-mcp/blob/develop/docs/adr/014-agent-notes-local-settings-toml-v1.md) (релиз **2.0**, `--config`), **AIGuiders.AgentNotes.Core** 2.x (NuGet).

---

## Контекст

**agent-notes-mcp 2.0** (ветка `develop`, 2026-05):

- Обязательный **`--config`** → TOML (`[knowledge]`, `[workspace]`, `[status]`).
- Тулы MCP: аргумент **`knowledge_path`** (вместо `canon_path`).
- **AgentNotes.Core** 2.0: `LocalSettingsLoader`, `AgentNotesRuntime`; без чтения `knowledge/META/mcp-resolve-paths-v1.json` и без **`AGENT_NOTES_CANON_PATH`** в supported path.

**Cascade IDE** сейчас:

- NuGet **`AIGuiders.AgentNotes.Core` 1.0.0** (`CascadeIDE.csproj`).
- In-proc **`McpAgentNotesService`** → **`NotesStorage`**; knowledge-команды **`IdeCommands`** с опциональным **`canon_path`** в JSON args.
- Разрешение канона: **`AGENT_NOTES_CANON_PATH`**, embedded **kb-base-cide**, overlay **`AgentNotesSettings.KbBaseOverlayPath`** — отдельно от TOML MCP.
- Environment readiness и подсказки в UI всё ещё ссылаются на **`AGENT_NOTES_CANON_PATH`**.

Расхождение ломает единый контракт «primary knowledge root» и дублирует устаревшие имена после выноса Core в пакет ([0019](0019-shared-git-core-ide-and-git-mcp.md)).

---

## Решение

1. **Поднять зависимость** до **`AIGuiders.AgentNotes.Core` ≥ 2.0.0** (NuGet после публикации пакета с `develop` agent-notes-core).

2. **Инициализация runtime in-proc** (аналог `Program.cs` MCP, без отдельного exe):
   - Путь к TOML: **`%LocalAppData%\CascadeIDE\agent-notes.toml`** (рабочее имя; схема **`version = 1`** — та же, что у MCP) **или** секция в существующем **`settings.toml`** / отдельный файл по [0028](0028-user-settings-toml-localappdata-and-secrets.md) — выбор при реализации; в ADR зафиксирован **отдельный файл** рядом с настройками IDE для паритета с `mcp.json` `--config`.
   - При старте IDE (или первом обращении к `McpAgentNotesService`): `LocalSettingsLoader.Load` → **`AgentNotesRuntime.Initialize`**.
   - Fail fast при битом TOML — сообщение в environment readiness / лог, не молчаливый fallback на env.

3. **Переименовать контракт IDE-команд** (breaking для JSON args внешнего агента):
   - **`canon_path` → `knowledge_path`** в `IdeCommands.Knowledge.*`, generated args/doc, `IdeMcpCommandExecutor.Handlers.AgentNotes`, `IIdeMcpActions` XML.
   - Семантика: корень репозитория с каталогом **`knowledge/`**; если не передан — primary root из загруженного TOML.

4. **KB-Base overlay** оставить для сценария «чтение без полного канона на диске» ([`McpAgentNotesService`](../../Services/McpAgentNotesService.cs) embedded zip + overlay) — **ортогонально** primary root из TOML; порядок при list/read без `knowledge_path` описать в коде и [MCP-PROTOCOL.md](../MCP-PROTOCOL.md), не дублировать env `AGENT_NOTES_CANON_PATH`.

5. **Убрать из supported path IDE** опору на **`AGENT_NOTES_CANON_PATH`** для in-proc Core (миграционная заметка в release notes). **`AGENT_NOTES_FILE`** для глобального hot-файла — по-прежнему допустим как в Core 2.0.

6. **Environment readiness:** ячейка «канон agent-notes» → проверка наличия/валидности **TOML** и каталога primary root; текст подсказок без `AGENT_NOTES_CANON_PATH`.

---

## Вне scope (этот ADR)

- Реализация **[status]** HTTP (localhost diagnostic) — зеркало MCP [ADR 013](https://github.com/AI-Guiders/agent-notes-mcp/blob/develop/docs/adr/013-localhost-status-surface-v1.md); отдельный шаг.
- Настройка **внешнего** `agent-notes-mcp.exe` в Cursor — только документация / пример `mcp.json` в KB.
- Автогенерация TOML из UI — достаточно example + ручное редактирование ([0029](0029-configuration-toml-canonical-ui-facade.md)).

---

## План внедрения (чеклист)

| # | Задача |
|---|--------|
| 1 | Опубликовать **AIGuiders.AgentNotes.Core** 2.0.0; bump `PackageReference` в `CascadeIDE.csproj` |
| 2 | Добавить `agent-notes.toml` example под `%LocalAppData%\CascadeIDE\`; загрузка при старте |
| 3 | `AgentNotesRuntime.Initialize` в composition root / `McpAgentNotesService` |
| 4 | Rename **`canon_path` → `knowledge_path`** (commands, executor, generated, ProtocolDocGen regen) |
| 5 | Обновить `EnvironmentReadiness*` и [0104](0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md) (ссылка на TOML вместо env) |
| 6 | Тесты: fixture TOML как в agent-notes-mcp; убрать тесты только на `AGENT_NOTES_CANON_PATH` |

---

## Последствия

- **Плюс:** один контракт с MCP 2.0 и KB ADR 013/014; scope/workspace из `[workspace]` в TOML.
- **Минус:** breaking rename args для внешних агентов, мигрирующих JSON; нужен опубликованный NuGet 2.0.
- **Риск:** два потребителя TOML (IDE файл vs MCP `--config`) — пути должны указывать на **тот же** primary root в типичной установке (документировать в example).

---

## Отклонённые альтернативы

- **Оставить 1.0 Core и только переименовать args** — не снимает расхождение по META JSON / workspace map.
- **Только env `AGENT_NOTES_CANON_PATH` в IDE** — против направления MCP 2.0 и [0028](0028-user-settings-toml-localappdata-and-secrets.md).

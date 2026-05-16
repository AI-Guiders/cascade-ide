# ADR 0118: Agent Notes Core 2.0 — TOML, `knowledge_path`, паритет с agent-notes-mcp

**Статус:** Accepted (implemented 2026-05)  
**Дата:** 2026-05-16

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Прецедент общего Core (git-mcp) |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | TOML настроек, секреты |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML-first конфигурация IDE |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Поверхность `IdeCommands` |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Контракты MCP |

**Вне репо:** [agent-notes-mcp ADR 014](https://github.com/AI-Guiders/agent-notes-mcp/blob/main/docs/adr/014-agent-notes-local-settings-toml-v1.md) (релиз **2.0**, `--config`); NuGet **AIGuiders.AgentNotes.Core** 2.x.

## Контекст

**agent-notes-mcp 2.0**:

- Обязательный **`--config`** → TOML (`[knowledge]`, `[workspace]`, `[status]`).
- Тулы MCP: аргумент **`knowledge_path`** (вместо `canon_path`).
- **AgentNotes.Core** 2.0: `LocalSettingsLoader`, `AgentNotesRuntime`; без META JSON / **`AGENT_NOTES_CANON_PATH`** в supported path.

**Cascade IDE** до внедрения:

- NuGet **`AIGuiders.AgentNotes.Core` 1.0.0**.
- In-proc **`McpAgentNotesService`**; knowledge-команды с **`canon_path`**.
- Отдельно env **`AGENT_NOTES_CANON_PATH`** и overlay KB-Base.

---

## Решение

1. **Зависимость Core ≥ 2.0.0** — в open-монорепо: `ProjectReference` на `../agent-notes-core`; в CI после публикации NuGet — `PackageReference`.

2. **SSOT конфигурации** — в `settings.toml`:

   ```toml
   [agent_notes]
   config_path = "D:/agent-notes-mcp/agent-notes-mcp.toml"
   ```

   Тот же файл, что **`--config`** в `mcp.json` для Cursor. Относительный путь — от `%LocalAppData%\CascadeIDE\`.

   Загрузка: **`AgentNotesRuntimeLoader.EnsureInitialized`** → `LocalSettingsLoader.Load` → **`AgentNotesRuntime.Initialize`**.

3. **`canon_path` → `knowledge_path`** в `IdeCommands.Knowledge.*`, executor, generated docs. В JSON args принимается legacy alias `canon_path` (чтение в `McpCommandJsonArgs.KnowledgePath`).

4. **KB-Base overlay** (`kb_base_overlay_path`) — ортогонально primary root из TOML; без `AGENT_NOTES_CANON_PATH`.

5. **Environment readiness:** строка «agent-notes config (TOML)» — файл существует, TOML загружен, primary root на диске.

---

## Вне scope

- **[status]** HTTP в IDE — только в процессе MCP ([ADR 013](https://github.com/AI-Guiders/agent-notes-mcp/blob/main/docs/adr/013-localhost-status-surface-v1.md)).
- Автогенерация TOML из UI.

---

## Реализация

| Компонент | Путь |
|-----------|------|
| Loader | `Services/AgentNotesRuntimeLoader.cs` |
| Settings | `Models/AgentNotesSettings.cs` — `config_path` |
| Example | `docs/samples/settings.localappdata.example.toml` |

---

## Последствия

- **Плюс:** один TOML для Cursor MCP и CIDE in-proc; scope/workspace из `[workspace]`.
- **Минус:** breaking rename args; нужен опубликованный NuGet 2.0 для сборок без соседнего `agent-notes-core`.

---

## Отклонённые альтернативы

- **Дубликат TOML в `%LocalAppData%\CascadeIDE\agent-notes.toml`** — расхождение с MCP; отклонено в пользу **`config_path`** на один файл.
- **Только env `AGENT_NOTES_CANON_PATH`** — против MCP 2.0.

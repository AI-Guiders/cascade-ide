# ADR 0119: Agent Notes Core 2.1 — multi-root knowledge (`knowledge_root_id`)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | TOML, `knowledge_path`, Core 2.0 |
| agent-notes-mcp [015](https://github.com/AI-Guiders/agent-notes-mcp/blob/main/docs/adr/015-multi-root-read-only-knowledge-routing-v1.md) | Спека multi-root |

**Вне репо:** NuGet **AIGuiders.AgentNotes.Core** 2.1.x; agent-notes-mcp 2.1.x.

---

## Контекст

В **agent-notes-mcp 2.1** и **AgentNotes.Core 2.1** появились read-only knowledge roots и аргумент `knowledge_root_id` (паритет chmod **g** / group-kb). Cascade IDE in-proc оставался на Core 2.0 без этого аргумента в `IdeCommands` knowledge-*.

---

## Решение

1. **Зависимость** `AIGuiders.AgentNotes.Core` ≥ **2.1.1** (локально — `ProjectReference` на `../agent-notes-core`).

2. **Паритет args** для `read_knowledge_file`, `list_knowledge_files`, `write_*`, `append_*`, `upsert_knowledge_section`, `delete_*`:
   - `knowledge_path` (legacy `canon_path` через `McpCommandJsonArgs.KnowledgePath`);
   - `knowledge_root_id` — взаимоисключим с `knowledge_path` (проверка в Core).

3. **Прокидывание:** `McpCommandJsonArgs.KnowledgeRootId` → `McpAgentNotesService` → `NotesStorage.*(..., knowledgeRootId)`.

4. **Environment readiness:** при наличии `[[knowledge.read_only]]` в TOML — строка с числом read-only roots.

5. **Тесты:** `McpAgentNotesServiceMultiRootTests` (read group, write в read-only → ошибка).

---

## Последствия

- **Плюс:** один TOML + те же tuлы, что у внешнего MCP; агент в CIDE может читать group-kb через `knowledge_root_id=group`.
- **Минус:** без `[agent_notes].config_path` в settings `knowledge_root_id` возвращает понятную ошибку (embedded KB-base не подменяет multi-root).

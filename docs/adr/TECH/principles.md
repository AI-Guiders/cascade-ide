# TECH — принципы (карта канона)

**Входной связный текст** — [0077](../0077-tech-principles-hub.md) (канон абзацев в [`../snippets/tech/`](../snippets/tech/README.md)). Здесь — **расширенная карта**: таблицы «идея → ADR» без дублирования полных нормативов. Плоский индекс — [../README.md](../README.md); этот файл — [README.md](README.md).

---

## Слои, срезы, strangler

| Идея | Где зафиксировано |
|------|-------------------|
| Слои презентации, срезы фич, роль `MainWindowViewModel` | [0006](../0006-presentation-layers-and-feature-slices.md) |
| Сигналы, связность, backpressure; маршалинг UI | [0007](../0007-signals-coupling-and-ui-backpressure.md), [0004](../0004-ui-thread-marshaling.md) |
| Strangler и когда можно отступать от политики | [0009](../0009-strangler-migration-and-exceptions.md) |

---

## MCP, git, LSP, тестируемая инфраструктура

| Идея | Где зафиксировано |
|------|-------------------|
| Контракты MCP, абстракции для процессов/git | [0008](../0008-mcp-contracts-and-testable-infrastructure.md) |
| Общий Git Core для IDE и git-mcp | [0019](../0019-shared-git-core-ide-and-git-mcp.md) |
| LSP: пресеты, командная строка, env по правилам | [0040](../0040-lsp-launch-line-settings-toml-presets-and-environment.md) |
| Операционные команды и схемы | [../../MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) |

---

## Отладка и осведомлённость

| Идея | Где зафиксировано |
|------|-------------------|
| Единый слой отладки для человека и агента | [0002](../0002-debug-human-agent-parity.md) |
| Режим Debug отдельно от Power | [0003](../0003-debug-ui-mode-separate-from-power.md) |
| Гипотезы в JSON | [0001](../0001-debug-hypotheses-json-storage.md) |
| Ситуационная осведомлённость (не только нижняя панель) | [0011](../0011-debug-situational-awareness.md) |

---

## Агент, ACP, транспорт MCP, CLI контракта

| Идея | Где зафиксировано |
|------|-------------------|
| Внешний агент по ACP; ортогонально MCP IDE | [0016](../0016-agent-client-protocol-external-agent.md) |
| Чат через Cursor ACP: `mcpServers`, паритет с IDE MCP | [0048](../0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) |
| ACP в GUI: MCP IDE в том же процессе (loopback), не второй `CascadeIDE` | [0082](../0082-acp-ide-mcp-loopback-single-process.md) |
| Фасад агента: провайдеры, оркестрация тулов | [0038](../0038-agent-facade-ai-provider-and-tool-orchestration.md) |
| `settings.toml` — `[ai].mode` и вложенные секции (local / acp / mcp_only / cloud) | [0083](../0083-ai-mode-and-nested-settings-toml.md) |
| Правки агента: источник правды — буфер редактора; чат — намерение; присутствие отдельно | [0084](../0084-agent-edits-editor-source-of-truth-presence-channel.md) |
| Видимость рассуждения (слои ответа/трассы/лога), честные лимиты провайдеров | [0020](../0020-agent-reasoning-visibility-and-provider-limits.md) |
| Навигация workspace; MCP `get_code_navigation_context`, пресеты, subgraph | [0039](../0039-workspace-navigation-affordances.md) |
| Агент ↔ Roslyn MCP: ключи в `settings.toml` | [0058](../0058-agent-roslyn-mcp-coupling-settings-toml.md) |
| Профили Roslyn MCP, Manager, тактика / EFB | [0059](../0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) |
| Восстановление MCP-транспорта; паритет человек/агент | [0043](../0043-mcp-transport-recovery-human-agent-parity.md) |
| CLI контракта и снапшот-тесты | [0052](../0052-agent-contract-cli-and-snapshot-tests.md) |

---

## Сборка «только TECH-ADR» в один HTML/PDF

Из `docs/adr`:

```bash
dotnet script build-adr.csx --book adr-book-tech.md
```

Выход: `build/adr-book-tech.md`, `out/html/adr-book-tech.html` и т.д. Подробности — [../build/README.md](../build/README.md).

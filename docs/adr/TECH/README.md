# TECH — тематический указатель ADR

Здесь **нет отдельной нумерации**: нормативные решения по-прежнему лежат в родительском каталоге [`docs/adr/`](../README.md) как `NNNN-краткий-kebab-title.md`.

Эта папка — **оглавление по теме TECH** (контракты, MCP, git core, отладка, агент, LSP, CLI контракта): быстрые ссылки, чтобы не терять контекст между обсуждением в чате и файлом в репо.

**С чего начать (один связный текст):** [0077 — центр TECH-принципов](../0077-tech-principles-hub.md) (канон формулировок в [`../snippets/tech/`](../snippets/tech/README.md)). **Карта ссылок** по темам (без длинного текста): [principles.md](principles.md). Сборка TECH-ADR в один HTML/PDF: `dotnet script build-adr.csx --book adr-book-tech.md` — [../build/README.md](../build/README.md).

**Канонический индекс** всего набора ADR — в [../README.md](../README.md). Политика и таблица «решение → ADR» — в [../../architecture-policy.md](../../architecture-policy.md). Операционный протокол MCP — [../../MCP-PROTOCOL.md](../../MCP-PROTOCOL.md).

---

## Указатель по TECH

| Тема | ADR |
|------|-----|
| **Центр TECH** — вводные принципы (границы, контракты, агент, отладка); текст в [`snippets/tech/`](../snippets/tech/README.md) | [0077](../0077-tech-principles-hub.md) |
| Слои, срезы фич, роль `MainWindowViewModel` | [0006](../0006-presentation-layers-and-feature-slices.md) |
| Сигналы, слабая связность, backpressure на UI | [0007](../0007-signals-coupling-and-ui-backpressure.md) |
| Маршалинг UI (`IUiScheduler`, strangler) | [0004](../0004-ui-thread-marshaling.md) |
| Контракты MCP, тестируемая инфраструктура | [0008](../0008-mcp-contracts-and-testable-infrastructure.md) |
| Strangler-миграция, исключения для spike | [0009](../0009-strangler-migration-and-exceptions.md) |
| Отладка: паритет человек/агент | [0002](../0002-debug-human-agent-parity.md) |
| **Профили запуска** (несколько стартовых конфигураций, MCP, миграция с `startup-project.json`) | [0090](../0090-launch-profiles-and-debug-startup-configurations.md) (Proposed) |
| Debug UI отдельно от Power; гипотезы JSON; ситуационная осведомлённость | [0003](../0003-debug-ui-mode-separate-from-power.md), [0001](../0001-debug-hypotheses-json-storage.md), [0011](../0011-debug-situational-awareness.md) |
| Внешний агент ACP (stdio, Cursor CLI) | [0016](../0016-agent-client-protocol-external-agent.md) |
| Чат Cursor ACP в IDE: `mcpServers`, паритет тулов | [0048](../0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) |
| ACP: MCP IDE **в том же процессе** (loopback), не второй `CascadeIDE` | [0082](../0082-acp-ide-mcp-loopback-single-process.md) |
| Общий Git Core: IDE и git-mcp | [0019](../0019-shared-git-core-ide-and-git-mcp.md) |
| Фасад агента: провайдеры, чат, ACP, MCP | [0038](../0038-agent-facade-ai-provider-and-tool-orchestration.md) |
| **`[ai]` в settings.toml:** `mode` + вложенные секции (local / acp / mcp_only / cloud) | [0083](../0083-ai-mode-and-nested-settings-toml.md) |
| **Тема UI:** канон кистей в TOML; JSON в MCP как транспорт; strangler `Themes/*.json` | [0086](../0086-ui-theme-toml-canonical-json-mcp-wire.md) |
| Правки агента в редакторе как источник правды; присутствие отдельно; чат — не основной дифф | [0084](../0084-agent-edits-editor-source-of-truth-presence-channel.md) |
| Видимость рассуждения агента, лимиты провайдеров | [0020](../0020-agent-reasoning-visibility-and-provider-limits.md) |
| Навигация по workspace; MCP `get_code_navigation_context` | [0039](../0039-workspace-navigation-affordances.md) |
| LSP: пресеты и командная строка в `settings.toml` | [0040](../0040-lsp-launch-line-settings-toml-presets-and-environment.md) |
| Агент ↔ Roslyn MCP в `settings.toml` | [0058](../0058-agent-roslyn-mcp-coupling-settings-toml.md) |
| Профили Roslyn MCP, Manager, EFB / GlobalMap | [0059](../0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) |
| MCP-транспорт: восстановление, паритет человек/агент | [0043](../0043-mcp-transport-recovery-human-agent-parity.md) |
| CLI контракта агента и снапшот-тесты | [0052](../0052-agent-contract-cli-and-snapshot-tests.md) |
| **Принципы TECH — карта канона** (таблицы «идея → ADR») | [principles.md](principles.md) |

---

## Связанные документы (не ADR)

- [../../MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) — контракты команд агента.
- [../../debug-human-agent-parity-v1.md](../../debug-human-agent-parity-v1.md) — операционный слой паритета отладки (если ведётся отдельно от ADR).

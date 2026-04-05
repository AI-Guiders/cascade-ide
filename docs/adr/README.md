# Architecture Decision Records (ADR) — CascadeIDE

Здесь фиксируются **принятые архитектурные решения**: контекст, выбор, последствия и отклонённые альтернативы. **[architecture-policy.md](../architecture-policy.md)** — краткий навигатор и таблица ссылок сюда; пошаговая миграция и контракты MCP — в [architecture-migration.md](../architecture-migration.md), [MCP-PROTOCOL.md](../MCP-PROTOCOL.md).

**Связь с политикой:** крупные смены направления — отдельным коммитом с обновлением навигатора и новой записью здесь.

## Индекс

| ID | Название | Статус |
|----|----------|--------|
| [0001](0001-debug-hypotheses-json-storage.md) | Хранение гипотез отладки в одном JSON-файле | Accepted |
| [0002](0002-debug-human-agent-parity.md) | Единый слой состояния отладки для человека и агента | Accepted |
| [0003](0003-debug-ui-mode-separate-from-power.md) | Отдельный UI-режим Debug (не кокпит Power) | Accepted (направление), реализация — по плану |
| [0004](0004-ui-thread-marshaling.md) | Маршалинг обновлений UI через `IUiScheduler` | Accepted (strangler) |
| [0005](0005-defer-dynamic-plugins-mef.md) | Отложить динамические плагины (MEF и аналоги) | Accepted |
| [0006](0006-presentation-layers-and-feature-slices.md) | Слои, срезы фич, роль `MainWindowViewModel` | Accepted |
| [0007](0007-signals-coupling-and-ui-backpressure.md) | Сигналы, связность, нагрузка на UI | Accepted (strangler) |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Контракты MCP и тестируемая инфраструктура | Accepted |
| [0009](0009-strangler-migration-and-exceptions.md) | Strangler-миграция и исключения для spike | Accepted |
| [0010](0010-ui-modes-toml-configuration.md) | Данные UI-режимов в TOML | Accepted (загрузчик и файлы — по плану в ADR) |
| [0011](0011-debug-situational-awareness.md) | Ситуационная осведомлённость в отладке (полоска, hover; детали в панели) | Accepted (направление) |
| [0012](0012-floating-workspace-chrome.md) | Плавающий и отцепляемый хром workspace (телеметрия, полоски; не доки в v1) | Accepted (направление) |
| [0013](0013-command-surface-and-discoverability.md) | Поверхность команд и discoverability (палитра, минимальный toolbar) | Accepted (направление) |
| [0014](0014-situational-checklists.md) | Ситуационные чеклисты (модель, триггеры, UI; родитель — 0013) | Accepted (направление) |
| [0015](0015-editor-toml-syntax-highlighting.md) | Подсветка TOML в редакторе (шипнутый TextMate-пакет taplo; не LSP в v1) | Accepted |

## Соглашения

- **Имя файла:** `NNNN-краткий-kebab-title.md`, четыре цифры с ведущими нулями.
- **Статусы:** Accepted | Superseded | Deprecated (в тексте ADR).
- Новый ADR добавляет строку в таблицу выше и при необходимости строку в таблицу в [architecture-policy.md](../architecture-policy.md).

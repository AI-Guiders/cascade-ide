# Architecture Decision Records (ADR) — CascadeIDE

Здесь фиксируются **принятые архитектурные решения**: контекст, выбор, последствия и отклонённые альтернативы. Пошаговая реализация и детали API живут в коде и в профильных документах ([MCP-PROTOCOL.md](../MCP-PROTOCOL.md), [architecture-policy.md](../architecture-policy.md) и т.д.).

**Связь с политикой:** крупные смены направления — отдельным коммитом с обновлением [architecture-policy.md](../architecture-policy.md) и при необходимости новой записью здесь.

## Индекс

| ID | Название | Статус |
|----|----------|--------|
| [0001](0001-debug-hypotheses-json-storage.md) | Хранение гипотез отладки в одном JSON-файле | Accepted |
| [0002](0002-debug-human-agent-parity.md) | Единый слой состояния отладки для человека и агента | Accepted |
| [0003](0003-debug-ui-mode-separate-from-power.md) | Отдельный UI-режим Debug (не кокпит Power) | Accepted (направление), реализация — по плану |
| [0004](0004-ui-thread-marshaling.md) | Маршалинг обновлений UI через `IUiScheduler` | Accepted (strangler) |
| [0005](0005-defer-dynamic-plugins-mef.md) | Отложить динамические плагины (MEF и аналоги) | Accepted |

Слои, срезы фич и роли `MainWindowViewModel` подробно разобраны в [architecture-policy.md](../architecture-policy.md); при необходимости их можно вынести в отдельные ADR с новыми номерами.

## Соглашения

- **Имя файла:** `NNNN-краткий-kebab-title.md`, четыре цифры с ведущими нулями.
- **Статусы:** Accepted | Superseded | Deprecated (в тексте ADR).
- Новый ADR добавляет строку в таблицу выше.

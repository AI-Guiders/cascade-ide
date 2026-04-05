# Архитектурная политика CascadeIDE (навигатор)

**Статус:** действующая.  
**Назначение этого файла:** краткий **живой навигатор** — куда смотреть и какие решения уже зафиксированы. Детальная **логика решений** (контекст, выбор, последствия, отклонённые варианты) — в [ADR](adr/README.md), не дублируем здесь длинными разделами.

**Связь:** [git-and-submodules-v1.md](git-and-submodules-v1.md), [MCP-PROTOCOL.md](MCP-PROTOCOL.md), [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md), [architecture-migration.md](architecture-migration.md).

---

## Цель

Сохранить скорость разработки одного десктопного приложения (Avalonia + MVVM) при явных границах между UI, сценариями и внешним миром; по отладке — единый слой для человека и агента (см. [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md) и [ADR 0002](adr/0002-debug-human-agent-parity.md)).

Политика **прагматичная**: не полный DDD на весь код — см. [ADR 0006](adr/0006-presentation-layers-and-feature-slices.md).

---

## Где что зафиксировано (ADR)

| Тема | ADR |
|------|-----|
| Слои, срезы фич, роль `MainWindowViewModel`, модели списков | [0006](adr/0006-presentation-layers-and-feature-slices.md) |
| Сигналы, слабая связность, очереди/батчинг; ссылка на UI-поток | [0007](adr/0007-signals-coupling-and-ui-backpressure.md) + [0004](adr/0004-ui-thread-marshaling.md) |
| Контракты MCP, тестируемые абстракции для git/процессов | [0008](adr/0008-mcp-contracts-and-testable-infrastructure.md) |
| Strangler-миграция, когда можно отклоняться от политики | [0009](adr/0009-strangler-migration-and-exceptions.md) |
| Динамические плагины (MEF) — не ближайшая цель | [0005](adr/0005-defer-dynamic-plugins-mef.md) |
| Отладка: паритет человек/агент; Debug UI; гипотезы в JSON; осведомлённость без «только нижняя панель» | [0002](adr/0002-debug-human-agent-parity.md), [0003](adr/0003-debug-ui-mode-separate-from-power.md), [0001](adr/0001-debug-hypotheses-json-storage.md), [0011](adr/0011-debug-situational-awareness.md) |
| Конфигурация UI-режимов (TOML), принято; реализация — по ADR | [0010](adr/0010-ui-modes-toml-configuration.md) |
| Плавающий/отцепляемый хром workspace (нижняя зона, телеметрия; не floating доки в v1) | [0012](adr/0012-floating-workspace-chrome.md) |
| Поверхность команд, палитра, минимальный toolbar; не смешивать с размещением хрома | [0013](adr/0013-command-surface-and-discoverability.md) |
| Ситуационные чеклисты (каталог, триггеры, карточка UI) | [0014](adr/0014-situational-checklists.md) |
| Подсветка TOML в редакторе (шипнутый TextMate-пакет; LSP — отдельно) | [0015](adr/0015-editor-toml-syntax-highlighting.md) |
| Внешний агент по ACP (stdio, Cursor CLI), не путать с MCP-сервером IDE | [0016](adr/0016-agent-client-protocol-external-agent.md) |
| Мультиоконность workspace, вторые поверхности агента, зоны экрана; MCP — несколько корней в scope фичи | [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (Proposed) |
| Реестр `IdeCommands`: каноничные XML-доки (`summary` / `param` / `returns` / `example`) для ProtocolDocGen; миграция с мини-языка в summary | [0018](adr/0018-ide-commands-canonical-xml-documentation.md) (Proposed) |
| Git: общая библиотека логики для встроенных `ide_git_*` и отдельного git-mcp; паритет семантики | [0019](adr/0019-shared-git-core-ide-and-git-mcp.md) (Proposed) |

Полный индекс: [docs/adr/README.md](adr/README.md).

---

## Быстрые ссылки (операционные документы)

| Документ | Зачем |
|----------|--------|
| [architecture-migration.md](architecture-migration.md) | Пошаговый перенос, фазы, статус strangler |
| [MCP-PROTOCOL.md](MCP-PROTOCOL.md) | Контракты команд агента |
| [Features/README.md](../Features/README.md) | Каталог срезов `Features/` |

---

## Версионирование этого навигатора

- **v1** — исходная политика со слоями и срезами в одном файле.  
- **v1.1** — целевой каталог `Features/`, ссылка на architecture-migration; git через `IGitCommandRunner`.  
- **v1.2** — план событий и UI-потока; MEF отложен.  
- **v1.3** — политика свёрнута в **навигатор**; расширенная логика вынесена в ADR 0006–0009 (и ранее 0001–0005).  
- **v1.4** — в таблицу ADR добавлен [0011](adr/0011-debug-situational-awareness.md) (отладка: осведомлённость без опоры только на нижнюю панель).  
- **v1.5** — в таблицу ADR добавлен [0012](adr/0012-floating-workspace-chrome.md) (плавающий хром workspace).  
- **v1.6** — в таблицу ADR добавлен [0013](adr/0013-command-surface-and-discoverability.md) (палитра команд, discoverability, минимальный toolbar).  
- **v1.7** — уточнён [0013](adr/0013-command-surface-and-discoverability.md); добавлен [0014](adr/0014-situational-checklists.md) (ситуационные чеклисты отдельно от палитры/toolbar).  
- **v1.8** — добавлен [0015](adr/0015-editor-toml-syntax-highlighting.md) (подсветка TOML в редакторе через TextMate; не LSP в v1).  
- **v1.9** — добавлен [0016](adr/0016-agent-client-protocol-external-agent.md) (внешний агент по Agent Client Protocol, stdio, Cursor CLI; PoC принят).  
- **v1.10** — добавлен [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (мультиоконность, поверхности агента; статус Proposed до обсуждения).  
- **v1.11** — добавлен [0018](adr/0018-ide-commands-canonical-xml-documentation.md) (каноничные XML-доки для `IdeCommands`/ProtocolDocGen; Proposed).  
- **v1.12** — добавлен [0019](adr/0019-shared-git-core-ide-and-git-mcp.md) (общий Git Core для IDE и git-mcp; Proposed).  
- Изменения направления — отдельным коммитом: обновление этого файла и при необходимости новый ADR в [docs/adr/README.md](adr/README.md).

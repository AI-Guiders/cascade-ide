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
| Git: общая библиотека логики для встроенных `ide_git_*` и отдельного git-mcp; паритет семантики | [0019](adr/0019-shared-git-core-ide-and-git-mcp.md) (Accepted) |
| Агент: слои видимости рассуждения (ответ, трасс инструментов, опционально сырой лог); ограничения API провайдеров — явно, без имитации «полного мыслительного процесса» | [0020](adr/0020-agent-reasoning-visibility-and-provider-limits.md) (Proposed) |
| PFD / MFD / лобовое / EICAS / HUD — модель внимания кокпита | [0021](adr/0021-pfd-mfd-cockpit-attention-model.md) (Proposed) |
| Визуальная поверхность UI (AXAML / Blazor): превью и дизайн-тайм на MFD / втором мониторе; не цель Blend | [0022](adr/0022-mfd-visual-design-surface-axaml-blazor.md) (Proposed) |
| Markdown + диаграммы (Mermaid/PlantUML): first-class опыт через LSP и workflow; инъекция LSP в fenced-блоки — отдельная фаза | [0023](adr/0023-markdown-diagrams-language-tooling.md) (Proposed) |
| SDK для IDE: стабильные контракты и capability‑модель для внутреннего расширения; plugin-host остаётся deferred | [0024](adr/0024-ide-sdk-and-stable-contracts.md) (Proposed) |
| SDK и зоны внимания: канон PFD/MFD/… в метаданных capabilities; overlay презентации без подмены семантики | [0025](adr/0025-sdk-attention-zones-and-capabilities.md) (Proposed) |
| Превью Markdown: где монтируется виджет (`forward_split` / окно / MFD), ключ в `workspace.toml`; внутренние отсылки (peek) — см. тот же ADR | [0026](adr/0026-markdown-preview-surfaces-and-placement.md) (Accepted, частично) |
| Продуктовый фокус: малая команда vs готовность к открытию — оси «границы/контракты» и «очередь/discoverability» | [0027](adr/0027-small-team-focus-vs-public-maturity.md) (Accepted) |
| Пользовательские настройки: путь `settings.toml`, TOML/snake_case, секреты в отдельном `ai-keys.toml` | [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md) (Accepted) |
| Конфигурация: канон на диске (TOML); центр настроек deferred; точечный UI — фасад канона | [0029](adr/0029-configuration-toml-canonical-ui-facade.md) (Accepted) |
| Команды: слои `IdeCommands` / палитра / `hotkeys.toml` / мост VM; единый UI-каталог — чертёж, не обязателен сразу | [0030](adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) (Accepted · Implemented) |
| Чат агента: пакеты уточнений (не одна строка), структура ответов; треды — опционально; ортогонально PFD-подтверждениям | [0031](adr/0031-agent-chat-clarification-batches-and-threading.md) (Proposed) |

Полный индекс: [docs/adr/README.md](adr/README.md).

---

## Быстрые ссылки (операционные документы)

| Документ | Зачем |
|----------|--------|
| [architecture-migration.md](architecture-migration.md) | Пошаговый перенос, фазы, статус strangler |
| [MCP-PROTOCOL.md](MCP-PROTOCOL.md) | Контракты команд агента |
| [Features/README.md](../Features/README.md) | Каталог срезов `Features/` |
| [design/onboarding-first-run-v1.md](design/onboarding-first-run-v1.md) | Онбординг и First Run — живой чертёж (не ADR); дополняется по мере идей |
| [design/attention-zone-panel-playbook-v1.md](design/attention-zone-panel-playbook-v1.md) | Зона ↔ панель shell ↔ SDK: следующий шаг после «это PFD» (не ADR) |
| [design/vertical-slice-attention-capabilities-v1.md](design/vertical-slice-attention-capabilities-v1.md) | Вертикальный срез: регистрация UI surface + проверка дампа / теста |

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
- **v1.13** — [0019](adr/0019-shared-git-core-ide-and-git-mcp.md) принят (Accepted); реализация `GitMcp.Core` в meta-repo `open`, паритет argv для IDE и git-mcp.  
- **v1.14** — добавлен [0020](adr/0020-agent-reasoning-visibility-and-provider-limits.md) (видимость рассуждения агента: слои L0–L2, честные ограничения провайдеров LLM; Proposed).  
- **v1.15** — в таблицу ADR добавлены [0021](adr/0021-pfd-mfd-cockpit-attention-model.md) (модель внимания PFD/MFD) и [0022](adr/0022-mfd-visual-design-surface-axaml-blazor.md) (визуальная поверхность AXAML/Blazor на MFD; Proposed).  
- **v1.16** — добавлен [0025](adr/0025-sdk-attention-zones-and-capabilities.md) (SDK: привязка capabilities к зонам внимания; Proposed).  
- **v1.17** — в [0025](adr/0025-sdk-attention-zones-and-capabilities.md) уточнено: нативные Open/Save vs метаданные зоны, политика по умолчанию (нативный диалог; inline — осознанное исключение).  
- **v1.18** — playbook [attention-zone-panel-playbook-v1](design/attention-zone-panel-playbook-v1.md); в [0025](adr/0025-sdk-attention-zones-and-capabilities.md): `HostAttentionPanelId`, `CapabilityAttentionConsistency`.  
- **v1.19** — [vertical-slice-attention-capabilities-v1](design/vertical-slice-attention-capabilities-v1.md); регистрация `ui.chrome.surface.solution_explorer` для сквозной проверки.  
- **v1.20** — добавлен [0026](adr/0026-markdown-preview-surfaces-and-placement.md) (превью Markdown: поверхности и TOML); UX размещения снят с канона в [0023](adr/0023-markdown-diagrams-language-tooling.md) (там — язык и диаграммы).  
- **v1.21** — добавлен [0027](adr/0027-small-team-focus-vs-public-maturity.md) (узкая команда vs зрелость для открытия: две оси; Proposed).  
- **v1.22** — [0027](adr/0027-small-team-focus-vs-public-maturity.md) принят (Accepted); минимум discoverability (дока, примеры, ADR) + ссылка на [onboarding-first-run-v1](design/onboarding-first-run-v1.md); триггеры вывода задач оси B из бэклога.  
- **v1.23** — добавлен [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md) (пользовательский `settings.toml`, `%LocalAppData%\CascadeIDE\`, `ai-keys.json`; отличие от [0010](adr/0010-ui-modes-toml-configuration.md)).  
- **v1.24** — добавлен [0029](adr/0029-configuration-toml-canonical-ui-facade.md) (TOML-first; UI как фасад; TOML-only допустим).  
- **v1.25** — [0029](adr/0029-configuration-toml-canonical-ui-facade.md): уточнено — deferred **целостный** UI настроек ([0027](adr/0027-small-team-focus-vs-public-maturity.md)); «фасад» = правило для точечного UI и канона.  
- **v1.26** — [0029](adr/0029-configuration-toml-canonical-ui-facade.md): мотивация точечного UI vs TOML-only (в т.ч. редкий заход под одну опцию, ACP).  
- **v1.27** — [0029](adr/0029-configuration-toml-canonical-ui-facade.md): перспектива динамического UI от модели/метаданных; точечный UI = вес кода, осознанно.  
- **v1.28** — [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md): канон пользовательских настроек — `settings.toml`; переход с прежнего формата считается завершённым.  
- **v1.29** — [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md): ветка миграции `settings.json` удалена из `SettingsService`; ADR и SETUP обновлены.  
- **v1.30** — [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md): секреты API — **`ai-keys.toml`** (Tomlyn, как `settings.toml`); `ai-keys.json` не используется.  
- **v1.31** — [0030](adr/0030-command-ids-hotkeys-and-ui-registry-layers.md): слои команд и хоткеев; реестр v1 в `IdeCommandRegistry*.cs`; чертёж [ide-command-registry-v1](design/ide-command-registry-v1.md). Статус ADR: **Implemented**.  
- **v1.32** — добавлен [0031](adr/0031-agent-chat-clarification-batches-and-threading.md) (чат: пакеты уточнений, многострочные ответы, треды опционально; Proposed).  
- **v1.33** — [0026](adr/0026-markdown-preview-surfaces-and-placement.md): намерение по **внутренним отсылкам** в превью (hover/peek «Show Definition» для «см. п. N» и якорей; ортогонально [0023](adr/0023-markdown-diagrams-language-tooling.md)).  
- **v1.34** — [README ADR](adr/README.md#adr-anchors-policy): политика **внутренних якорей** (`adrNNNN-pK`) и ссылок вместо голого «см. п. N»; якоря в **0010**, **0011**, **0012**, **0015**, **0017**, **0021** (§17 п. 10 → §18), перекрёстные ссылки в **0022**, **0031**; якоря списка фазы 5 в [architecture-migration.md](architecture-migration.md).  
- Изменения направления — отдельным коммитом: обновление этого файла и при необходимости новый ADR в [docs/adr/README.md](adr/README.md).

# Архитектурная политика CascadeIDE (навигатор)

**Статус:** действующая.  
**Назначение этого файла:** краткий **живой навигатор** — куда смотреть и какие решения уже зафиксированы. Детальная **логика решений** (контекст, выбор, последствия, отклонённые варианты) — в [ADR](adr/README.md), не дублируем здесь длинными разделами.

**Связь:** [git-and-submodules-v1.md](git-and-submodules-v1.md), [MCP-PROTOCOL.md](MCP-PROTOCOL.md), [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md), [architecture-migration.md](architecture-migration.md), черновик границ продукта [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md) (Cursor + MCP + Cascade).

---

## Цель

Сохранить скорость разработки одного десктопного приложения (Avalonia + MVVM) при явных границах между UI, сценариями и внешним миром; по отладке — единый слой для человека и агента (см. [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md) и [ADR 0002](adr/0002-debug-human-agent-parity.md)).

Политика **прагматичная**: не полный DDD на весь код — см. [ADR 0006](adr/0006-presentation-layers-and-feature-slices.md).

### Продуктовый фокус (ближайший горизонт)

**Приоритет итераций:** комфортный переход пользователя **из Cursor** (MCP, наблюдаемость агента и репозитория в одном контуре с CascadeIDE). **Паритет с Visual Studio** по охвату сценариев — **долгий горизонт**, не критерий скорости ближайших выпусков. Подробнее и формулировка north-star — [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md).

### Документация и справка (намерение)

Развёрнутые объяснения для **конечного пользователя** (в т.ч. раскладки дисплеев, ментальная модель зон внимания) — **отдельный продуктовый слой** от ADR: каналы (внешний User Guide, встроенная справка в IDE, иное), объём и приоритеты задаются **на уровне продукта**, а не «внутри» конкретного ADR по окнам или конфигу. ADR остаются **нормативной** сжатой формой для разработки; пример нотации дисплеев — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md).

## Avalonia и слой кабины (граница ответственности)

**Avalonia** здесь — **несущий каркас**: `TopLevel` и окна, жизненный цикл, фокус, маршрутизация ввода на границе приложения, интеграция с ОС (в т.ч. DPI), хостинг **тяжёлых** контролов там, где переписывание не окупается (типично **редактор** кода).

**Семантика кабины** — какие зоны PFD / Forward / MFD, топология окон, эффективная `presentation`, фиксация долей из конфига без «плавающего» пересчёта ради удобства декларативного layout — живёт в **CDS** и **композиторе поверхности** ([ADR 0036](adr/0036-cds-channel-compositor-surface-pipeline.md), чертёж [cds-contract-v0](design/cds-contract-v0.md)). Это **не** источник истины в `Grid` / `StackPanel` как носителе смысла кокпита.

**Кастомная отрисовка** (например Skia) — **над** Avalonia как хостом: прямоугольники слотов и команды отрисовки выводятся из контракта CDS / композитора. Ядро Avalonia **не** форкается без необходимости; расширения продукта — в своём слое. Стабильность геометрии при явных весах в конфиге — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) ([предохранитель](adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-weight-fuse-policy)).

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
| Мультиоконность workspace, вторые поверхности агента, зоны экрана; MCP — несколько корней в scope фичи | [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (Accepted) |
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
| HUD над редактором: что показывать и в каком виде — в `settings.toml`; опциональная грамматика по образцу `presentation` / `[presentation_grammar]` | [0032](adr/0032-hud-banner-configuration-and-grammar.md) (Proposed) |
| Интернационализация: ResX, культура UI; TOML не словарь всего интерфейса; ортогонально конфигу и HUD | [0033](adr/0033-internationalization-resx-avalonia.md) (Proposed) |
| Оператор недоступен (Incapacitation): Emergency Mode; EICAS + класс сигналов КВС; liveness, HUD по контексту внимания, interlock опасных команд; сенсоры — opt-in | [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) (Proposed) |
| MFD: встроенный WebView2, внешние веб-LLM; веб не равно MCP-клиент; явная передача контекста; мост веб↔MCP — отдельная линия | [0035](adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) (Proposed) |
| CDS: канал → контракт кабины → композитор слота → поверхность (Avalonia); не ARINC 661 целиком | [0036](adr/0036-cds-channel-compositor-surface-pipeline.md) (Accepted) |
| PFD: инварианты поверхности (weight, input lock, каналы) и Roslyn; канон `[PfdStrict]` / `PfdStrictControl` | [0037](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md) (Proposed) |
| Фасад агента: провайдеры LLM (`AiProviderManager`), чат, ACP, автономный режим; внешние MCP; направление развития | [0038](adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) (Accepted · Implemented) |
| Навигация по workspace: C#-first / .NET north-star; несколько представлений, граф/semantic map; PFD/MFD; MCP: `get_workspace_navigation_context` (пресеты, `kind_filter`, subgraph) | [0039](adr/0039-workspace-navigation-affordances.md) (Proposed · MCP implemented) |
| LSP C#/Markdown: пресеты и опциональные `executable`/`arguments` в `settings.toml`; явный флаг чтения из окружения — по [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) | [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) (Accepted · Proposed) |
| Protobuf vs JSON: когда уместен бинарный IDL для агента/IDE; границы с MCP и `IdeCommands`; критерии пилота | [0041](adr/0041-protobuf-for-agent-and-ide-messages.md) (Proposed) |
| Pre-flight briefing: Planned Changes и Review Before Apply; семантический слой перед записью на диск; частичное одобрение; отказ без артефактов | [0042](adr/0042-pre-flight-planned-changes-and-review-before-apply.md) (Proposed) |
| MCP-транспорт: паритет «человек может перезапустить MCP в хосте ↔ агент видит сбой и восстановление»; уровни хост / CascadeIDE / наблюдаемость; не смешивать с паритетом отладки | [0043](adr/0043-mcp-transport-recovery-human-agent-parity.md) (Proposed) |
| Чат агента: модель диалога первична, затем UI; Avalonia как фюзеляж, Skia — гипотеза слоя отрисовки; спайк после/параллельно модели | [0044](adr/0044-avalonia-host-skia-agent-chat-surface.md) (Proposed) |
| Чат агента: persistence через append-only event log (`*.events.ndjson`) + `meta.json`; проекции для UI отдельно | [0045](adr/0045-agent-chat-persistence-event-log-and-projections.md) (Proposed) |
| Раскладка кабины: `presentation` как инвариант P/F/M; единый `PresentationLayoutAuthority` для меню/MCP/UI-режимов и реактивного слоя | [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md) (Proposed) |
| Виджет кабины: дескриптор слота композитора (`CockpitWidgetDescriptor`), не Avalonia-контрол; SE vs Semantic Map как разные `widget_id` в PFD | [0047](adr/0047-cockpit-widget-descriptor-and-slot-composition.md) (Proposed) |

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
| [design/north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md) | Границы «Cursor + MCP + Cascade»; приоритет **перехода из Cursor**, VS-паритет — долгую; матрица сделано/осталось (черновик) |
| [design/cds-contract-v0.md](design/cds-contract-v0.md) | CDS в **контрактном** смысле vs `UiLayoutSnapshot`; черновик полей v0 (живой чертёж; [0021 §1.1](adr/0021-pfd-mfd-cockpit-attention-model.md#glossary-cds-contract)) |
| [CascadeIDE.ArchitectureAnalyzers/README.md](../CascadeIDE.ArchitectureAnalyzers/README.md) | Roslyn: **CASCOPE001**/**CASCOPE002** — слои `Cockpit/Channels`, `Cds`, `Composition` без Avalonia / без `using Features.UiChrome` ([0036](adr/0036-cds-channel-compositor-surface-pipeline.md)) |

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
- **v1.10** — добавлен [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (мультиоконность, поверхности агента; тогда Proposed — см. **v1.38**).  
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
- **v1.35** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): мультиоконность v1 **не** смешивается с переработкой **Power** и прочих режимов; вопрос Flight vs Power для второго окна **снят** до отдельной дорожной карты режимов.  
- **v1.36** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): уточнение **зона Mfd** vs **страницы** `SecondaryShellPage`; не «чат как зона».  
- **v1.37** — подраздел **«Документация и справка (намерение)»**: User Guide / справка в IDE — **продуктовый слой**, не обязанность отдельного ADR; [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) остаётся нормативом по нотации и мультиоконности.  
- **v1.38** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): статус **Accepted** (мультиоконность, `presentation` / EBNF, слой `settings.toml` vs репозиторный workspace).  
- **v1.39** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) [п. 8](adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p8-mfd-host-wide): **`MfdHostWindow`** — только **полный** `SecondaryShellView` (все `SecondaryShellPage`); узкий одностраничный хост **не** планируется.  
- **v1.40** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) [п. 5 доп.](adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p5-primary-vs-forward): primary ОС ≠ семантический Forward; пример сенсорного монитора и «основного» дисплея; согласование раскладки ОС и `presentation` — ответственность пользователя.  
- **v1.41** — черновик границ цели «Cursor + MCP + Cascade вместо VS»: [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md).  
- **v1.42** — тот же документ: **продуктовое видение** — ось внимания / кокпит / Dark Cockpit vs «шкаф окон» VS; не клон VS, а иная повседневная работа.  
- **v1.43** — north-star: для экосистемы JetBrains честнее сравнение с **Rider** (не IntelliJ IDEA); ось дифференциации та же.  
- **v1.44** — north-star: явное **позиционирование CascadeIDE как agent-first IDE** (общий контур с человеком; кокпит не противоречит оси).  
- **v1.45** — north-star: слой **KB / память агента** (канон knowledge, MCP `read_knowledge_file` / …, agent-notes); ссылка на [MCP-PROTOCOL.md](MCP-PROTOCOL.md).  
- **v1.46** — добавлен [0032](adr/0032-hud-banner-configuration-and-grammar.md) (HUD: конфиг содержимого и грамматика как у `presentation`; Proposed).  
- **v1.47** — добавлен [0033](adr/0033-internationalization-resx-avalonia.md) (i18n: ResX/Avalonia; Proposed); уточнён перекрёсток с [0032](adr/0032-hud-banner-configuration-and-grammar.md).  
- **v1.48** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): раздел **«Состояние реализации»** (сверка с кодом: топология, плейсмент, bounds); [concept-to-implementation-map-v1](ux/concept-to-implementation-map-v1.md) **§6** — второй `TopLevel` / `MfdHostWindow`.
- **v1.49** — добавлен [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) (Incapacitation оператора, Emergency Mode, опциональное присутствие через webcam MCP; Proposed).
- **v1.50** — [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md): уточнён контракт — EICAS и сигналы КВС, liveness, контекстный HUD, safety interlock; граница «биометрия» = liveness/присутствие для безопасности.
- **v1.51** — [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md): слой A — прокси присутствия (мышь, клавиатура, фокус); оговорены ложные срабатывания и комбинирование сигналов.
- **v1.52** — [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md): слой C — eye tracking отложен, не baseline; доступность (поле зрения ≠ сигнал трекера); запрет обязательного ET.  
- **v1.49** — чертёж [cds-contract-v0](design/cds-contract-v0.md): CDS (контракт кабины) vs `UiLayoutSnapshot`; [0021](adr/0021-pfd-mfd-cockpit-attention-model.md) §1.1 — глоссарий.  
- **v1.53** — добавлен [0035](adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) (MFD: WebView2, внешние веб-LLM; граница с MCP; гибрид через оператора; мост веб↔MCP — вне baseline; Proposed).  
- **v1.54** — добавлен [0036](adr/0036-cds-channel-compositor-surface-pipeline.md) (канал → CDS → композитор → поверхность; Agent-first; Proposed).  
- **v1.55** — [0036](adr/0036-cds-channel-compositor-surface-pipeline.md): статус **Accepted**; в коде слои `Cockpit/Cds`, `Cockpit/Channels`, `Cockpit/Composition`, `Cockpit/Surface` ([`cds-contract-v0`](design/cds-contract-v0.md) §6–7).  
- **v1.56** — Roslyn-анализатор [`CascadeIDE.ArchitectureAnalyzers`](../CascadeIDE.ArchitectureAnalyzers/README.md): **CASCOPE001** / **CASCOPE002** (границы слоёв Cockpit по ADR 0036).  
- **v1.57** — добавлен [0037](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md) (PFD: weight / input lock / каналы; явные маркеры для Roslyn; Proposed).  
- **v1.58** — добавлен [0038](adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) (фасад агента: `AiProviderManager`, чат vs ACP vs автономный цикл, `McpClientService`; черновик направления в том же ADR).  
- **v1.59** — добавлен [0039](adr/0039-workspace-navigation-affordances.md) (навигация workspace: не только дерево файлов; несколько представлений и «связанные»; Proposed).  
- **v1.60** — [0039](adr/0039-workspace-navigation-affordances.md): продуктовая метафора (шкаф vs карта боя), граф релевантного контекста, PFD/MFD, Semantic Map и `presentation`.  
- **v1.61** — [0037](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md): канон имён строгой поверхности — `[PfdStrict]` / `PfdStrictControl` ([§ канон](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-naming)).  
- **v1.62** — [0039](adr/0039-workspace-navigation-affordances.md): north-star по языкам — C# / .NET, не polyglot IDE; [north-star workbench](design/north-star-cursor-mcp-cascade-workbench-v1.md) обновлён.  
- **v1.63** — раздел **«Avalonia и слой кабины (граница ответственности)»**: фюзеляж (окна, ввод, хост редактора) vs CDS/композитор (семантика кабины); кастомная отрисовка над хостом; ссылка на [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (предохранитель весов). Уточнение [0036](adr/0036-cds-channel-compositor-surface-pipeline.md) п. 4; строка в [cds-contract-v0](design/cds-contract-v0.md) §3.  
- **v1.64** — добавлен [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md): командная строка LSP в `settings.toml` (пресеты, опциональные ключи; флаг `launch_from_environment` — Proposed).  
- **v1.65** — [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md): приоритет **перехода из Cursor** vs паритет VS в долгую; в этом файле — подраздел **«Продуктовый фокус»** под [Цель](#цель).  
- **v1.66** — добавлен [0041](adr/0041-protobuf-for-agent-and-ide-messages.md) (protobuf vs JSON для сообщений агента/IDE: границы, критерии, гибрид; точка входа; Proposed).  
- **v1.67** — [0039](adr/0039-workspace-navigation-affordances.md): зафиксирован реализованный MCP-слой — `get_workspace_navigation_context` (пресеты в `settings.toml`, merge, эхо `kind_filter`, subgraph: `kind` / `related_kind`); cookbook [workspace-navigation-mcp-cookbook.md](design/workspace-navigation-mcp-cookbook.md); закрыт п.5 в [закрытых вопросах](adr/0039-workspace-navigation-affordances.md#adr0039-closed-questions).  
- **v1.68** — добавлен [0045](adr/0045-agent-chat-persistence-event-log-and-projections.md): persistence чата как append-only NDJSON + meta/projections; стартовая реализация `ChatSessionStore` в `Features/Chat/`.
- **v1.69** — добавлен [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md): `presentation` трактуется как инвариант кабины P/F/M; единый `PresentationLayoutAuthority` коэрцирует изменения из меню/MCP/UI-режимов/реактивного слоя.
- **v1.70** — добавлен [0047](adr/0047-cockpit-widget-descriptor-and-slot-composition.md): термин **Widget** (кабинный) и тип `CockpitWidgetDescriptor` на границе композитор → поверхность; не смешивать с `Control`.
- Изменения направления — отдельным коммитом: обновление этого файла и при необходимости новый ADR в [docs/adr/README.md](adr/README.md).

# Архитектурная политика CascadeIDE (навигатор)

**Статус:** действующая.  
**Назначение этого файла:** краткий **живой навигатор** — куда смотреть и какие решения уже зафиксированы. Детальная **логика решений** (контекст, выбор, последствия, отклонённые варианты) — в [ADR](adr/README.md), не дублируем здесь длинными разделами.

**Статусы ADR** (Proposed / Accepted / …, второй тег **`Implemented`** для внедрённого кода): [adr/status-lifecycle.md](adr/status-lifecycle.md).

**Связь:** [git-and-submodules-v1.md](git-and-submodules-v1.md), [MCP-PROTOCOL.md](MCP-PROTOCOL.md), [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md), [architecture-migration.md](architecture-migration.md), черновик границ продукта [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md) (Cursor + MCP + Cascade).

---

## Цель

Сохранить скорость разработки одного десктопного приложения (Avalonia + MVVM) при явных границах между UI, сценариями и внешним миром; по отладке — единый слой для человека и агента (см. [debug-human-agent-parity-v1.md](debug-human-agent-parity-v1.md) и [ADR 0002](adr/0002-debug-human-agent-parity.md)).

Политика **прагматичная**: не полный DDD на весь код — см. [ADR 0006](adr/0006-presentation-layers-and-feature-slices.md).

### Продуктовый фокус (ближайший горизонт)

**Приоритет итераций:** комфортный переход пользователя **из Cursor** (MCP, наблюдаемость агента и репозитория в одном контуре с CascadeIDE). **Паритет с Visual Studio** по охвату сценариев — **долгий горизонт**, не критерий скорости ближайших выпусков. Подробнее и формулировка north-star — [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md).

**Keyboard-first:** CascadeIDE **запланирована** как IDE, где типовой контур — **клавиатура и палитра команд**, а не обязательная навигация мышью по плотным панелям; согласуется с [ADR 0013](adr/0013-command-surface-and-discoverability.md) и разделом про keyboard-first в north-star выше.

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
| Контракты MCP, тестируемые абстракции для git/процессов | [0008](adr/0008-mcp-contracts-and-testable-infrastructure.md) (Accepted · Implemented) |
| Strangler-миграция, когда можно отклоняться от политики | [0009](adr/0009-strangler-migration-and-exceptions.md) |
| Динамические плагины (MEF) — не ближайшая цель | [0005](adr/0005-defer-dynamic-plugins-mef.md) |
| Отладка: паритет человек/агент; Debug UI; гипотезы в JSON; осведомлённость без «только нижняя панель»; **профили запуска (несколько стартовых конфигураций)**; **гипотеза PFD-deck при отладке (плотность Mfd)** | [0002](adr/0002-debug-human-agent-parity.md), [0003](adr/0003-debug-ui-mode-separate-from-power.md), [0001](adr/0001-debug-hypotheses-json-storage.md), [0011](adr/0011-debug-situational-awareness.md), [0090](adr/0090-launch-profiles-and-debug-startup-configurations.md) (Proposed), [0091](adr/0091-pfd-debug-situational-deck-hypothesis.md) (Proposed) |
| Конфигурация UI-режимов (TOML) | [0010](adr/0010-ui-modes-toml-configuration.md) (Accepted · Implemented) |
| Плавающий/отцепляемый хром workspace (нижняя зона, телеметрия; не floating доки в v1) | [0012](adr/0012-floating-workspace-chrome.md) |
| Поверхность команд, палитра, минимальный toolbar; не смешивать с размещением хрома | [0013](adr/0013-command-surface-and-discoverability.md) |
| Ситуационные чеклисты (каталог, триггеры, карточка UI) | [0014](adr/0014-situational-checklists.md) |
| Подсветка TOML в редакторе (шипнутый TextMate-пакет; LSP — отдельно) | [0015](adr/0015-editor-toml-syntax-highlighting.md) (Accepted · Implemented) |
| Внешний агент по ACP (stdio, Cursor CLI), не путать с MCP-сервером IDE | [0016](adr/0016-agent-client-protocol-external-agent.md) (Accepted · Implemented) |
| Мультиоконность workspace, вторые поверхности агента, зоны экрана; MCP — несколько корней в scope фичи | [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (Accepted · Implemented) |
| Реестр `IdeCommands`: каноничные XML-доки (`summary` / `param` / `returns` / `example`) для ProtocolDocGen; миграция с мини-языка в summary | [0018](adr/0018-ide-commands-canonical-xml-documentation.md) (Proposed) |
| Git: общая библиотека логики для встроенных `ide_git_*` и отдельного git-mcp; паритет семантики | [0019](adr/0019-shared-git-core-ide-and-git-mcp.md) (Accepted · Implemented) |
| Агент: слои видимости рассуждения (ответ, трасс инструментов, опционально сырой лог); ограничения API провайдеров — явно, без имитации «полного мыслительного процесса» | [0020](adr/0020-agent-reasoning-visibility-and-provider-limits.md) (Proposed) |
| PFD / MFD / лобовое / EICAS / HUD — модель внимания кокпита | [0021](adr/0021-pfd-mfd-cockpit-attention-model.md) (Proposed) |
| PFD **instrument deck** — варианты состава и поверхностей (SA, метрики кода, карта намерений, ADR indicator); таблица-каталог, критерии отбора; не смешивать с [0063](adr/0063-instrument-deck-named-composition-one-anchor.md) как нормативом терминов | [0073](adr/0073-pfd-instrument-deck.md) (Proposed) |
| Визуальная поверхность UI (AXAML / Blazor): превью и дизайн-тайм на MFD / втором мониторе; не цель Blend | [0022](adr/0022-mfd-visual-design-surface-axaml-blazor.md) (Proposed) |
| **Трек** Visual UI (дизайнер разметки): **отдельная крупная** программная линия CIDE; **бэклог и приоритет стеков** (Avalonia → Blazor → опц. Razor); детали UX — **в 0022** | [0092](adr/0092-visual-ui-designer-major-track.md) (Accepted) |
| Markdown + диаграммы (Mermaid/PlantUML): first-class опыт через LSP и workflow; инъекция LSP в fenced-блоки — отдельная фаза | [0023](adr/0023-markdown-diagrams-language-tooling.md) (Proposed) |
| SDK для IDE: стабильные контракты и capability‑модель для внутреннего расширения; plugin-host остаётся deferred | [0024](adr/0024-ide-sdk-and-stable-contracts.md) (Proposed) |
| SDK и зоны внимания: канон PFD/MFD/… в метаданных capabilities; overlay презентации без подмены семантики | [0025](adr/0025-sdk-attention-zones-and-capabilities.md) (Proposed) |
| Markdown Preview: отдельный tool surface; primary placement — MFD, secondary — окно; renderer decoupled от placement; authoring-расширение Markdown ортогонально preview | [0069](adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md) (Accepted) |
| Command Palette: прямой overlay surface в host, маршрутизация в активный `TopLevel`; `ModalOverlay` не продуктовый baseline для палитры | [0070](adr/0070-command-palette-direct-overlay-surface.md) (Accepted) |
| **IDS (Ide Display System):** оверлеи IDE (палитра и далее toast/модалки) — intent → композитор → снимок → поверхность; **не** расширять CDS этим контуром; единый хост ввода и слоты — strangler по [0079](adr/0079-ide-display-system-ids-overlay-pipeline.md) | [0079](adr/0079-ide-display-system-ids-overlay-pipeline.md) (Accepted) |
| **Intercom:** пользовательское имя канала связи в IDE (не только LLM); модель «участники + система»; **командный масштаб** — внешний стек/API по умолчанию, не дублировать в IDE; алиасы discoverability; код `chat*` — strangler ([0080](adr/0080-intercom-naming-and-multi-party-channel-model.md)) | [0080](adr/0080-intercom-naming-and-multi-party-channel-model.md) (Proposed) |
| **Intent Melody (параметры):** диапазон строк редактора в хвосте (`:start:end`), валидация, рефакторинги по диапазону; индикатор ввода; ортогонально якорям из [0080](adr/0080-intercom-naming-and-multi-party-channel-model.md) | [0081](adr/0081-parametric-intent-melodies-editor-line-ranges.md) (Proposed) |
| **ACP + MCP IDE (один процесс):** вместо второго `CascadeIDE --mcp-stdio` — MCP хост на loopback (HTTP/SSE) в GUI; токен в заголовках; внешний `--mcp-stdio` не отменять | [0082](adr/0082-acp-ide-mcp-loopback-single-process.md) (Proposed) |
| **`settings.toml` — `[ai]`:** дискриминант `mode` (local / acp / mcp_only / cloud), вложенные таблицы; старый плоский `provider` не мигрируем автоматически | [0083](adr/0083-ai-mode-and-nested-settings-toml.md) (Accepted · Implemented) |
| **Агент и редактор:** единый источник правды — буфер редактора; чат — намерение/статус; присутствие агента (курсор, «пишет») отдельным каналом; дифф в чате не default; preview/live и безопасность — см. ADR | [0084](adr/0084-agent-edits-editor-source-of-truth-presence-channel.md) (Proposed) |
| **Editor HUD** vs **HUD banner:** inline-слой по месту кода/каретки vs file-level полоса над текстом; глобальные оверлеи IDE — IDS ([0079](adr/0079-ide-display-system-ids-overlay-pipeline.md)), не Editor HUD | [0085](adr/0085-editor-hud-inline-layer-and-hud-banner.md) (Proposed) |
| Философия продукта (VS/Copilot-class риски, «хороший актёр»); принципы AI в IDE | [cascadeide-philosophy-v1](design/cascadeide-philosophy-v1.md), [0071](adr/0071-ai-assistance-sovereignty-locality-invisibility.md) (Proposed) |
| Продуктовый фокус: малая команда vs готовность к открытию — оси «границы/контракты» и «очередь/discoverability» | [0027](adr/0027-small-team-focus-vs-public-maturity.md) (Accepted) |
| Пользовательские настройки: путь `settings.toml`, TOML/snake_case, секреты в отдельном `ai-keys.toml` | [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md) (Accepted · Implemented) |
| **Тема UI:** канон кистей в TOML; JSON в MCP (`ide_get/set_ui_theme`) — транспорт; пресеты `Themes/*.json` — strangler | [0086](adr/0086-ui-theme-toml-canonical-json-mcp-wire.md) (Proposed) |
| **Именование:** омнибус агента `get_ide_state` (вместо `get_workspace_state`); канал **IDE Health** (вместо Workspace Health); ортогонально [0002](adr/0002-debug-human-agent-parity.md) | [0089](adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md) (Accepted) |
| Конфигурация: канон на диске (TOML); центр настроек deferred; точечный UI — фасад канона | [0029](adr/0029-configuration-toml-canonical-ui-facade.md) (Accepted · Implemented) |
| UI настроек: компактная вёрстка, целевой якорь **MFD**; политика при нехватке места в **P+F+M** (scroll, ресайз, fallback-окно — см. ADR) | [0074](adr/0074-settings-ui-mfd-compact-layout-overflow.md) (Proposed; не отменяет канон [0029](adr/0029-configuration-toml-canonical-ui-facade.md)) |
| Тематический указатель **UI** (`docs/adr/UI/`), соглашения по страницам MFD (payload vs проекция, keyboard-first) | [0075](adr/0075-ui-topic-index-and-mfd-page-conventions.md) (Proposed) |
| **UI/UX — центр принципов:** связный вводный текст (кокпит, философия продукта); канон формулировок в [`snippets/ui`](adr/snippets/ui/README.md); полные ADR — по ссылкам | [0076](adr/0076-ui-ux-principles-hub.md) (Proposed) |
| **TECH — центр принципов:** связный вводный текст (границы/контракты, агент/отладка); канон формулировок в [`snippets/tech`](adr/snippets/tech/README.md); указатель [`TECH/README.md`](adr/TECH/README.md) | [0077](adr/0077-tech-principles-hub.md) (Proposed) |
| Команды: слои `IdeCommands` / палитра / `hotkeys.toml` / мост VM; единый UI-каталог — чертёж, не обязателен сразу | [0030](adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) (Accepted · Implemented) |
| Чат агента: пакеты уточнений как structured flow, thread/decision graph как first-class модель; Skia surface — продуктовый путь, ортогонально PFD-подтверждениям | [0031](adr/0031-agent-chat-clarification-batches-and-threading.md) (Proposed), [0057](adr/0057-chat-surface-pipeline-adoption.md) (Accepted) |
| HUD над редактором: что показывать и в каком виде — в `settings.toml`; опциональная грамматика по образцу `presentation` / `[presentation_grammar]` | [0032](adr/0032-hud-banner-configuration-and-grammar.md) (Proposed) |
| Интернационализация: ResX, культура UI; TOML не словарь всего интерфейса; ортогонально конфигу и HUD | [0033](adr/0033-internationalization-resx-avalonia.md) (Proposed) |
| Оператор недоступен (Incapacitation): Emergency Mode; EICAS + класс сигналов КВС; liveness, HUD по контексту внимания, interlock опасных команд; сенсоры — opt-in | [0034](adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) (Proposed) |
| MFD: встроенный WebView2, внешние веб-LLM; веб не равно MCP-клиент; явная передача контекста; мост веб↔MCP — отдельная линия | [0035](adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) (Proposed) |
| CDS: канал → контракт кабины → композитор слота → поверхность (Avalonia); не ARINC 661 целиком | [0036](adr/0036-cds-channel-compositor-surface-pipeline.md) (Accepted · Implemented) |
| PFD: инварианты поверхности (weight, input lock, каналы) и Roslyn; канон `[PfdStrict]` / `PfdStrictControl` | [0037](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md) (Proposed) |
| Фасад агента: провайдеры LLM (`AiProviderManager`), чат, ACP, автономный режим; внешние MCP; направление развития | [0038](adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) (Accepted · Implemented) |
| Навигация по workspace: C#-first / .NET north-star; несколько представлений, граф/semantic map; PFD/MFD; MCP: `get_code_navigation_context` (пресеты, `kind_filter`, subgraph) | [0039](adr/0039-workspace-navigation-affordances.md) (Accepted · Implemented) |
| LSP C#/Markdown: пресеты и опциональные `executable`/`arguments` в `settings.toml`; явный флаг чтения из окружения — по [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) | [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) (Accepted · Implemented) |
| Protobuf vs JSON: когда уместен бинарный IDL для агента/IDE; границы с MCP и `IdeCommands`; критерии пилота | [0041](adr/0041-protobuf-for-agent-and-ide-messages.md) (Proposed) |
| Pre-flight briefing: Planned Changes и Review Before Apply; семантический слой перед записью на диск; частичное одобрение; отказ без артефактов | [0042](adr/0042-pre-flight-planned-changes-and-review-before-apply.md) (Proposed) |
| MCP-транспорт: паритет «человек может перезапустить MCP в хосте ↔ агент видит сбой и восстановление»; уровни хост / CascadeIDE / наблюдаемость; не смешивать с паритетом отладки | [0043](adr/0043-mcp-transport-recovery-human-agent-parity.md) (Proposed) |
| Чат агента: модель диалога первична, затем UI; Avalonia как фюзеляж, Skia — гипотеза слоя отрисовки; спайк после/параллельно модели | [0044](adr/0044-avalonia-host-skia-agent-chat-surface.md) (Proposed) |
| Чат агента: persistence через append-only event log (`*.events.ndjson`) + `meta.json`; проекции для UI отдельно | [0045](adr/0045-agent-chat-persistence-event-log-and-projections.md) (Proposed) |
| Раскладка кабины: `presentation` как инвариант P/F/M; канон в CDS — `CockpitPresentationLayoutPolicy`; coercion для меню/MCP/UI-режимов и реактивного слоя | [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md) (Accepted · Implemented) |
| Инструмент кабины: дескриптор слота композитора (`CockpitInstrumentDescriptor`), не Avalonia-контрол; SE vs карта намерений как разные `instrument_id` в PFD | [0047](adr/0047-cockpit-instrument-descriptor-and-slot-composition.md) (Accepted · Implemented) |
| Чат Cursor ACP в IDE: `mcpServers`, авто IDE MCP; приложения — пробелы тулов, разбор хоста Cursor (`mcp.json`) ↔ `ide_*` | [0048](adr/0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) (Proposed) |
| Rollout Skia по CIDE: Avalonia остается host/fuselage, Skia расширяется в surface-слое волнами (dual-path, fallback до стабилизации) | [0049](adr/0049-skia-surface-rollout-over-avalonia-host.md) (Proposed) |
| Карта «инструмент → зона/слот» в TOML: merge bundle/repo/user, `[instrument_routing]`, alias, `InstrumentPlacementRuntime` | [0050](adr/0050-declarative-instrument-zone-placement-toml.md) (Accepted · Implemented) |
| Intent-based attention routing из TOML (маршрутизация внимания) | [0051](adr/0051-intent-based-attention-routing-toml.md) (Accepted · Implemented) |
| CLI для контракта агента (паритет с MCP) и снапшот-тесты | [0052](adr/0052-agent-contract-cli-and-snapshot-tests.md) (Accepted · Implemented: CLI, CI smoke, golden slice CDS) |
| Карта намерений на PFD: поток управления в методе (условные ветки, схождение), KISS; Roslyn CFG как источник; расширение subgraph / MCP | [0053](adr/0053-semantic-map-control-flow-pfd.md) (Accepted · Implemented) |
| Бенчмарки производительности CIDE: сценарии, метрики, baseline и протокол измерения | [0054](adr/0054-benchmarking-methodology-and-baselines.md) (Proposed) |
| Сопряжение агента и Roslyn MCP в `settings.toml`: лимиты, виды узлов, таймауты, пресеты запросов; ортогонально `[semantic_map]` и пресетам навигации [0039](adr/0039-workspace-navigation-affordances.md) | [0058](adr/0058-agent-roslyn-mcp-coupling-settings-toml.md) (Proposed) |
| Профили Roslyn MCP, Manager: тактика на PFD; EFB / GlobalMap на **MFD** (не PFD); Auto-Focus / Combat / Echelon | [0059](adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) (Proposed) |
| Keyboard-first: аккордный слой (CascadeChord / Ctrl+K), FMS-style S/T, overlay-подсказки, MODE на PFD; палитра Ctrl+Q не заменяется | [0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) (Proposed; расширение [0013](adr/0013-command-surface-and-discoverability.md)) |
| Чат: topic cards, drill-in/back, intent-команды навигации по темам; Melody/Chords/palette паритет ([0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) — уточнение только chat-domain) | [0072](adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) (Proposed) |
| ADR как слой осведомлённости: карта путь → ADR в `workspace.toml`, индикатор на PFD, краткий intent; агент — advisory при отклонении от привязанного ADR | [0061](adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md) (Proposed; реализация отложена) |
| **GitMap** (отдельно от CNC): git submodules / границы репо; не смешивать с `get_code_navigation_context`; переиспользование Skia pipeline; см. [git-and-submodules-v1](git-and-submodules-v1.md) | [0062](adr/0062-git-submodules-semantic-map-subgraph.md) (Proposed) |
| Именованная композиция в якоре (**instrument deck**); ось формы **`ContentRepresentation`**; таксономия примитивов (в т.ч. Presence/Activity) и **Dark Cockpit**; не смешивать с v1 `[instrument_routing]` | [0063](adr/0063-instrument-deck-named-composition-one-anchor.md) (Accepted) |
| **Виды индикаторов** deck: единое графическое воплощение; **библиотека отрисовки** (`PrimitivesKit`); **семантическая палитра**; вид индикатора ≠ токены метрик целой сцены; отдельный runtime-слой не вводится | [0064](adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) (Accepted) |
| **Категории инструментов** и **типы графов** (`graph_kind`): ось домена + структура подграфа; «Карта намерений» = **карта намерений кода** (не «любой граф связей»); опциональные поля дескриптора / JSON — по дорожной карте | [0065](adr/0065-instrument-categories-domain-taxonomy.md) (Accepted) |
| **Cockpit UI** (deck, приборы, `PrimitivesKit`, палитра кабины) **отдельно** от **presentation IDE** (хром, модальные оверлеи, тема shell); правило по умолчанию — в ADR; не смешивать слои в ревью; **CASCOPE011/012** — запрет перекрёстных `using` между `Features/UiChrome` и `Cockpit/PrimitivesKit` | [0066](adr/0066-cockpit-ui-vs-ide-presentation-layer.md) (Accepted) |
| **Graph-backed surfaces** — общий контракт для графовых экранов (карта намерений, GitMap, будущие); измерения: данные, interaction, навигация, layout, selection, sync с workspace | [0067](adr/0067-graph-backed-surfaces-contract.md) (Accepted) |
| **Полезная нагрузка строки** / **проекция представления** / **слот**: канал vs то, как строка рисуется (лампа, глиф, таблица); ортогонально ContentRepresentation и deck ([0063](adr/0063-instrument-deck-named-composition-one-anchor.md)) | [0068](adr/0068-deck-row-payload-and-presentation-projection.md) (Accepted) |

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
| [CascadeIDE.ArchitectureAnalyzers/README.md](../CascadeIDE.ArchitectureAnalyzers/README.md) | Roslyn: **CASCOPE001**/**CASCOPE002** — слои `Cockpit/Channels`, `Cds`, `Composition` без Avalonia / без `using Features.UiChrome` ([0036](adr/0036-cds-channel-compositor-surface-pipeline.md)); **CASCOPE003** — intent P/M у `MainWindowViewModel` без «тихих» присваиваний вне белого списка ([0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md)); **CASCOPE013–016** — граница **IDS** (`IdeDisplay/`) vs кабина / Avalonia / `UiChrome` и обратный запрет `Cockpit` → `IdeDisplay` ([0079](adr/0079-ide-display-system-ids-overlay-pipeline.md)) |

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
- **v1.20a** — добавлен [0069](adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md): preview переосмыслен как отдельный MFD-first tool surface; `0026` superseded в части inline/`forward_split`; authoring-расширение Markdown зафиксировано как ортогональная линия.  
- **v1.20b** — добавлен [0070](adr/0070-command-palette-direct-overlay-surface.md): command palette закреплена как прямой overlay surface в активном `TopLevel`; `ModalOverlay` больше не считается каноническим baseline для palette render-host.  
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
- **v1.36** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): уточнение **зона Mfd** vs **страницы** `MfdShellPage`; не «чат как зона».  
- **v1.37** — подраздел **«Документация и справка (намерение)»**: User Guide / справка в IDE — **продуктовый слой**, не обязанность отдельного ADR; [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) остаётся нормативом по нотации и мультиоконности.  
- **v1.38** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md): статус **Accepted** (мультиоконность, `presentation` / EBNF, слой `settings.toml` vs репозиторный workspace).  
- **v1.39** — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) [п. 8](adr/0017-multi-window-workspace-and-agent-surfaces.md#adr0017-p8-mfd-host-wide): **`MfdHostWindow`** — только **полный** `MfdShellView` (все `MfdShellPage`); узкий одностраничный хост **не** планируется.  
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
- **v1.50** — чат: [0031](adr/0031-agent-chat-clarification-batches-and-threading.md) и [0057](adr/0057-chat-surface-pipeline-adoption.md) синхронизированы под pipeline snapshot, structured clarification flow и единый Skia product path без обязательного Avalonia fallback.
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
- **v1.60** — [0039](adr/0039-workspace-navigation-affordances.md): продуктовая метафора (шкаф vs карта боя), граф релевантного контекста, PFD/MFD, карта намерений и `presentation`.  
- **v1.61** — [0037](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md): канон имён строгой поверхности — `[PfdStrict]` / `PfdStrictControl` ([§ канон](adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md#adr0037-naming)).  
- **v1.62** — [0039](adr/0039-workspace-navigation-affordances.md): north-star по языкам — C# / .NET, не polyglot IDE; [north-star workbench](design/north-star-cursor-mcp-cascade-workbench-v1.md) обновлён.  
- **v1.63** — раздел **«Avalonia и слой кабины (граница ответственности)»**: фюзеляж (окна, ввод, хост редактора) vs CDS/композитор (семантика кабины); кастомная отрисовка над хостом; ссылка на [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) (предохранитель весов). Уточнение [0036](adr/0036-cds-channel-compositor-surface-pipeline.md) п. 4; строка в [cds-contract-v0](design/cds-contract-v0.md) §3.  
- **v1.64** — добавлен [0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md): командная строка LSP в `settings.toml` (пресеты, опциональные ключи; флаг `launch_from_environment` — Proposed).  
- **v1.65** — [north-star-cursor-mcp-cascade-workbench-v1.md](design/north-star-cursor-mcp-cascade-workbench-v1.md): приоритет **перехода из Cursor** vs паритет VS в долгую; в этом файле — подраздел **«Продуктовый фокус»** под [Цель](#цель).  
- **v1.66** — добавлен [0041](adr/0041-protobuf-for-agent-and-ide-messages.md) (protobuf vs JSON для сообщений агента/IDE: границы, критерии, гибрид; точка входа; Proposed).  
- **v1.67** — [0039](adr/0039-workspace-navigation-affordances.md): зафиксирован реализованный MCP-слой — `get_code_navigation_context` (пресеты в `settings.toml` секция `[code_navigation]`, merge, эхо `kind_filter`, subgraph: `kind` / `related_kind`); cookbook [workspace-navigation-mcp-cookbook.md](design/workspace-navigation-mcp-cookbook.md); закрыт п.5 в [закрытых вопросах](adr/0039-workspace-navigation-affordances.md#adr0039-closed-questions).  
- **v1.68** — добавлен [0045](adr/0045-agent-chat-persistence-event-log-and-projections.md): persistence чата как append-only NDJSON + meta/projections; стартовая реализация `ChatSessionStore` в `Features/Chat/`.
- **v1.69** — добавлен [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md): `presentation` трактуется как инвариант кабины P/F/M; канон правил в CDS (`CockpitPresentationLayoutPolicy`); coercion изменений из меню/MCP/UI-режимов/реактивного слоя.
- **v1.70** — добавлен [0047](adr/0047-cockpit-instrument-descriptor-and-slot-composition.md): дескриптор слота на границе композитор → поверхность; не смешивать с `Control`.
- **v1.71** — [0047](adr/0047-cockpit-instrument-descriptor-and-slot-composition.md): канонический термин **Instrument** (кабинный) и тип `CockpitInstrumentDescriptor` вместо черновика *Widget* / `CockpitWidgetDescriptor`; переименован файл ADR.
- **v1.72** — [0047](adr/0047-cockpit-instrument-descriptor-and-slot-composition.md): статус **Accepted** (ось термина и дескриптора закреплены; реестр/wire — по дорожной карте).
- **v1.73** — слой **`Cockpit/Composition/HostSurface`**: `MainWindowHostSurfaceFrame` + композитор (shell + `CockpitInstrumentDescriptor`); VM собирает кадр одним вызовом — граница перед Skia в слотах, Avalonia как хост ([cds-contract-v0](design/cds-contract-v0.md) §3, §7).
- **v1.74** — `Cockpit/Cds/CockpitSurfaceState` (schema `0.3`) получил `instruments` как проекцию HostSurface-кадра для MCP/наблюдаемости; добавлен `Cockpit/Surface/MainWindowInstrumentMountRegistry` (`instrument_id → mount`, хост-слой Avalonia/Skia, без UI карты намерений на этом шаге).
- **v1.75** — [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md): заголовок, §«Решение» и индексы приведены к канону CDS (`CockpitPresentationLayoutPolicy`); уточнено, что статический слой в `Services/Presentation` снят, на границе VM — только intent `Apply*`.
- **v1.76** — Roslyn **CASCOPE003** ([`CascadeIDE.ArchitectureAnalyzers`](../CascadeIDE.ArchitectureAnalyzers/README.md)): запрет прямых присваиваний intent P/M вне белого списка файлов; см. [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md).
- **v1.77** — добавлен [0049](adr/0049-skia-surface-rollout-over-avalonia-host.md): поэтапный rollout Skia-поверхностей по CIDE при сохранении Avalonia как host; волновая миграция, dual-path и fallback до стабилизации.
- **v1.78** — в индекс ADR: [0050](adr/0050-declarative-instrument-zone-placement-toml.md) (карта инструментов в TOML), [0051](adr/0051-intent-based-attention-routing-toml.md) (intent routing), [0052](adr/0052-agent-contract-cli-and-snapshot-tests.md) (CLI контракта агента и снапшот-тесты).
- **v1.79** — [0052](adr/0052-agent-contract-cli-and-snapshot-tests.md): статус **Accepted** (направление CLI + снапшот-тесты; открытых вопросов нет).
- **v1.80** — [0052](adr/0052-agent-contract-cli-and-snapshot-tests.md): первая поставка — `--agent-contract get_ui_modes_diagnostics`, `AgentContractRunner`, тесты; MCP-PROTOCOL §CLI контракта.
- **v1.81** — [adr/status-lifecycle.md](adr/status-lifecycle.md): договорённость по статусам ADR (**Accepted · Implemented** для внедрённого кода); каталог `.cursor/` в репозитории в `.gitignore` — при желании продублируй суть в локальных правилах Cursor. Строка [0051](adr/0051-intent-based-attention-routing-toml.md) в таблице ниже — **Accepted · Implemented**.
- **v1.82** — таблица ADR выше: пометки **Accepted · Implemented** выровнены с заголовками ADR и [adr/README.md](adr/README.md) для уже внедрённых решений (в т.ч. [0008](adr/0008-mcp-contracts-and-testable-infrastructure.md), [0010](adr/0010-ui-modes-toml-configuration.md), [0015](adr/0015-editor-toml-syntax-highlighting.md)–[0017](adr/0017-multi-window-workspace-and-agent-surfaces.md), [0019](adr/0019-shared-git-core-ide-and-git-mcp.md), [0028](adr/0028-user-settings-toml-localappdata-and-secrets.md)–[0029](adr/0029-configuration-toml-canonical-ui-facade.md), [0036](adr/0036-cds-channel-compositor-surface-pipeline.md), [0039](adr/0039-workspace-navigation-affordances.md)–[0040](adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md), [0046](adr/0046-presentation-layout-authority-and-cockpit-invariants.md)–[0047](adr/0047-cockpit-instrument-descriptor-and-slot-composition.md)).
- **v1.83** — [0053](adr/0053-semantic-map-control-flow-pfd.md): карта намерений и поток управления на PFD (условные ветки, KISS, Roslyn CFG, subgraph/MCP); статус **Accepted · Implemented**.
- **v1.84** — добавлен [0054](adr/0054-benchmarking-methodology-and-baselines.md): протокол бенчмарков CIDE (сценарии, метрики, baseline, правила сравнения); статус **Proposed**.
- **v1.85** — добавлен [0058](adr/0058-agent-roslyn-mcp-coupling-settings-toml.md): сопряжение агент ↔ Roslyn MCP в `settings.toml` (лимиты, виды узлов, таймауты, пресеты); таблица потребителей; v0 vs отложенное; статус **Proposed**.
- **v1.86** — [0058](adr/0058-agent-roslyn-mcp-coupling-settings-toml.md): §6 — именованные профили, Manager, Auto-Focus / Combat / Echelon (тогда Glide Slope); связь с [0010](adr/0010-ui-modes-toml-configuration.md), [0051](adr/0051-intent-based-attention-routing-toml.md), [0055](adr/0055-skia-instrument-composition-pipeline.md).
- **v1.87** — [0058](adr/0058-agent-roslyn-mcp-coupling-settings-toml.md): §7 — третий монитор как EFB / стратегический `Profile.GlobalMap` vs тактический PFD; [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md), [0021](adr/0021-pfd-mfd-cockpit-attention-model.md).
- **v1.88** — разделение: [0058](adr/0058-agent-roslyn-mcp-coupling-settings-toml.md) — только ключи/оси TOML для сопряжения MCP; [0059](adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) — профили, Manager, режимы, EFB (перенос из бывших §6–§7 у 0058).
- **v1.89** — [0059](adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md): режим спокойного ввода переименован **Glide Slope → Echelon** (меньше путаницы с ILS glide path).
- **v1.90** — [0059](adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md): **EFB = MFD**, не PFD; гистерезис только для тактического контура (§2); EFB статичен / по намерению.
- **v1.91** — добавлен [0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md): аккордный слой и FMS-style (**S/T**), overlay, индикация MODE; расширение [0013](adr/0013-command-surface-and-discoverability.md); **Ctrl+K** vs палитра **Ctrl+Q**.
- **v1.92** — добавлен [0061](adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md): контекстная карта ADR ↔ пути в `workspace.toml`, индикатор на PFD, intent/tooltip, advisory агента; статус **Proposed** (реализация отложена).
- **v1.93** — добавлен [0063](adr/0063-instrument-deck-named-composition-one-anchor.md): **instrument deck** — именованная композиция инструментов в одном якоре; отличие от WH Page и от `[instrument_routing]` v1; статус **Proposed**.
- **v1.94** — [0063](adr/0063-instrument-deck-named-composition-one-anchor.md): ось **форма представления** — канон **`ContentRepresentation`** (Strip/Page); `DedicatedPage` у WH — режим Page на этой оси; ось **композиция** — deck / порядок сегментов WH; чертёж [workspace-health-implementation-map-v1](design/workspace-health-implementation-map-v1.md) §1.
- **v1.95** — [0063](adr/0063-instrument-deck-named-composition-one-anchor.md): направление **типы индикаторов** в deck — **Lamp / Bar / Sign** (компактные страницы; не контракт кода v1).
- **v1.96** — [0063](adr/0063-instrument-deck-named-composition-one-anchor.md): расширенная таксономия примитивов; **Presence/Activity** и согласование с **Dark Cockpit** ([0021](adr/0021-pfd-mfd-cockpit-attention-model.md) §6).
- **v1.97** — [0063](adr/0063-instrument-deck-named-composition-one-anchor.md): статус **Accepted** (терминология **instrument deck** / **`ContentRepresentation`**; ключи топологии дисплеев — [0017](adr/0017-multi-window-workspace-and-agent-surfaces.md) § `display.screens` / `topology`).
- **v1.98** — добавлен [0064](adr/0064-deck-primitives-visual-language-render-layer-and-palette.md): продуктовые примитивы deck — единое графическое представление; слой графики (`Cockpit/PrimitivesKit`); семантическая палитра и Dark Cockpit; продуктовый примитив vs токены сцены; статус **Accepted**.
- **v1.99** — [0064](adr/0064-deck-primitives-visual-language-render-layer-and-palette.md): канон **«вид индикатора»** / **форма сигнала**; лексикон продукт ↔ код (`DeckPrimitiveKind`); **библиотека отрисовки** вместо «ещё одного слоя»; отдельный runtime-tier **не** вводится.
- **v2.00** — добавлен [0065](adr/0065-instrument-categories-domain-taxonomy.md): **категории инструментов** как ось домена (CodeNavigation / WorkspaceNavigation / топология repo); ортогонально слоту и `instrument_id`; «Карта намерений» — граф, не прибор; опциональное `instrument_category` в дескрипторе — по мере необходимости; статус **Accepted**.
- **v2.01** — [0065](adr/0065-instrument-categories-domain-taxonomy.md): «Карта намерений» в узком смысле — **карта намерений кода**; введена ось **`graph_kind`** (тип графа в wire/MCP); таблица минимальных значений.
- **v2.02** — добавлен [0066](adr/0066-cockpit-ui-vs-ide-presentation-layer.md): две опоры — **Cockpit UI** vs **presentation IDE** (`UiChrome`, оверлеи, тема); инвариант не смешивать семантику приборов и оболочки; статус **Accepted**.
- **v2.03** — [0066](adr/0066-cockpit-ui-vs-ide-presentation-layer.md): в сборке **CASCOPE011** / **CASCOPE012** (`CascadeIDE.ArchitectureAnalyzers`) — граница `using` между `Features/UiChrome` и `Cockpit/PrimitivesKit`.
- **v2.04** — добавлен [0067](adr/0067-graph-backed-surfaces-contract.md): **graph-backed surface** как архитектурный класс; таблица измерений контракта (данные, interaction, навигация, layout, выделение, sync); статус **Accepted**.
- **v2.05** — добавлен [0068](adr/0068-deck-row-payload-and-presentation-projection.md): три слоя — **полезная нагрузка строки**, **идентичность слота**, **проекция представления**; связь с [0063](adr/0063-instrument-deck-named-composition-one-anchor.md); однородный DTO в v1 допустим; статус **Accepted**.
- **v2.06** — добавлены [cascadeide-philosophy-v1](design/cascadeide-philosophy-v1.md) (справочник философского слоя: DX, «хороший актёр», класс риска облачного inline-ассистента) и [0071](adr/0071-ai-assistance-sovereignty-locality-invisibility.md) (принципы суверенитета/локальности/невидимости для AI в IDE; анти-паттерн); статус ADR **Proposed**.
- **v2.07** — добавлен [0072](adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md): topic cards, drill-in/back, adaptive default view; обязательный v1 intent-слой для навигации по темам чата (палитра / Melody / Chords); связь с [0031](adr/0031-agent-chat-clarification-batches-and-threading.md), [0057](adr/0057-chat-surface-pipeline-adoption.md); **уточняет** [0060](adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) только в chat-domain; статус **Proposed**.
- **v2.08** — добавлен [0073](adr/0073-pfd-instrument-deck.md): PFD instrument deck — каталог вариантов (SA, code metrics, карта намерений, ADR indicator…), критерии размещения; **Proposed**, дополняет [0021](adr/0021-pfd-mfd-cockpit-attention-model.md) и [0063](adr/0063-instrument-deck-named-composition-one-anchor.md).
- **v2.09** — добавлен [0074](adr/0074-settings-ui-mfd-compact-layout-overflow.md): настройки — компактнее, якорь на MFD; таблица стратегий при нехватке места в P+F+M; **Proposed**, канон [0029](adr/0029-configuration-toml-canonical-ui-facade.md) не меняется.
- **v2.10** — добавлен [0075](adr/0075-ui-topic-index-and-mfd-page-conventions.md): папка [adr/UI](adr/UI/README.md) как тематический указатель по UI; нормативные ADR остаются в `docs/adr/`; **Proposed**.
- **v2.11** — добавлен [0076](adr/0076-ui-ux-principles-hub.md): **центр UI/UX-принципов** — связный текст из [`snippets/ui`](adr/snippets/ui/README.md) (внимание/кокпит, философия продукта vs «вторая VS» и класс риска облачного inline); **Proposed**; дополняет [0075](adr/0075-ui-topic-index-and-mfd-page-conventions.md) и [cascadeide-philosophy-v1](design/cascadeide-philosophy-v1.md).
- **v2.12** — добавлен [0077](adr/0077-tech-principles-hub.md): **центр TECH-принципов** — связный текст из [`snippets/tech`](adr/snippets/tech/README.md) (границы/контракты/MCP/git/LSP; агент/отладка/наблюдаемость); тематический указатель [adr/TECH/README.md](adr/TECH/README.md), сборка `adr-book-tech.md`; **Proposed**.
- **v2.13** — добавлен [0079](adr/0079-ide-display-system-ids-overlay-pipeline.md): **IDS** — пайплайн IDE-оверлеев (палитра и далее), ортогонально **CDS**; единый input capture и слоты — strangler; статус **Accepted**; таблица «Где что зафиксировано» обновлена.
- **v2.14** — [0079](adr/0079-ide-display-system-ids-overlay-pipeline.md): Roslyn **CASCOPE013–016** в `CascadeIDE.ArchitectureAnalyzers` — `IdeDisplay/` без Cockpit/Avalonia/UiChrome; `Cockpit/` без `IdeDisplay`; README анализаторов и строка навигатора обновлены.
- **v2.15** — добавлен [0080](adr/0080-intercom-naming-and-multi-party-channel-model.md): **Intercom** как продуктовое имя канала связи (агент, команда, система); открытые вопросы UI/i18n; **Proposed**; строка в таблице «Где что зафиксировано».
- **v2.16** — [0080](adr/0080-intercom-naming-and-multi-party-channel-model.md): §5 готовый командный контур (интеграция/API) vs собственная реализация; ссылка на [0035](adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) для риска WebView; открытый вопрос п.5; индекс README и строка навигатора уточнены.
- **v2.17** — [0080](adr/0080-intercom-naming-and-multi-party-channel-model.md): канон письма **Intercom** (одна **m**); файл ADR переименован с `0080-intercomm-…` на `0080-intercom-…`; **Intercomm** как опечатка в §4.
- **v2.18** — [0080 § идеи развития](adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities): async голос в тред, выразительный TTS (в т.ч. OpenTTS), дуплекс/«радио» как отдельный класс требований; без коммита на v1.
- **v2.19** — [0080 § идеи развития](adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities): якоря на код (deep link, диапазон, выделение) и альтернативное представление якоря; связь с [0039](adr/0039-workspace-navigation-affordances.md), [0067](adr/0067-graph-backed-surfaces-contract.md); без коммита на v1.
- **v2.20** — добавлен [0081](adr/0081-parametric-intent-melodies-editor-line-ranges.md): параметрические Intent Melody с суффиксом `:startLine:endLine` для операций над текстом редактора; валидация; рефакторинги по диапазону; UX (индикатор, chord vs палитра); **Proposed**; индекс [README](adr/README.md), строка навигатора, [UI/README](adr/UI/README.md).
- **v2.21** — добавлен [0082](adr/0082-acp-ide-mcp-loopback-single-process.md): ACP внутри IDE — направление на MCP IDE в **том же процессе** (loopback HTTP/SSE), без второго `CascadeIDE --mcp-stdio`; безопасность; сохранение сценария внешнего `--mcp-stdio`; **Proposed**; индекс [README](adr/README.md), [TECH/README](adr/TECH/README.md), строка навигатора.
- **v2.22** — добавлен [0084](adr/0084-agent-edits-editor-source-of-truth-presence-channel.md): правки агента — **единый текст в редакторе**; чат — намерение и статус; слой **присутствия** (курсор, «пишет») отдельно; дифф в чате не основной путь; preview/live и риски — в ADR; **Proposed**; индекс [README](adr/README.md), [UI/README](adr/UI/README.md), [TECH/README](adr/TECH/README.md), `principles.md`, `ui-adr-manifest` / `tech-adr-manifest`, строка навигатора.
- **v2.23** — [0084 § контекст и таблица рисков](adr/0084-agent-edits-editor-source-of-truth-presence-channel.md): персона **review-only / lead** (в основном читает и направляет) снижает *частоту* конфликтов с личным вводом; канон смягчения при одновременном редактировании **не** ослабляется.
- **v2.24** — добавлен [0085](adr/0085-editor-hud-inline-layer-and-hud-banner.md): **Editor HUD** (inline у каретки/в тексте) vs **HUD banner** (полоса над редактором); отличие от IDS; **Proposed**; индекс [README](adr/README.md), [UI/README](adr/UI/README.md), `UI/principles.md`, `ui-adr-manifest`, строка навигатора; отсылка в [0021 §9](adr/0021-pfd-mfd-cockpit-attention-model.md).
- **v2.25** — добавлен [0086](adr/0086-ui-theme-toml-canonical-json-mcp-wire.md): тема UI — **канон в TOML** (`settings.toml`), **JSON** для MCP `ide_get/set_ui_theme` как транспорт, strangler `Themes/*.json`; **Proposed**; индекс [README](adr/README.md), строка навигатора, `tech-adr-manifest`.
- **v2.26** — добавлен [0092](adr/0092-visual-ui-designer-major-track.md): трек **Visual UI** (дизайнер разметки) как **отдельная крупная** программная линия; [0022](adr/0022-mfd-visual-design-surface-axaml-blazor.md) остаётся нормативом по продукту/UX; приоритет стеков Avalonia → Blazor → (опц.) Razor; **Accepted**; индекс [README](adr/README.md), строка навигатора.
- Изменения направления — отдельным коммитом: обновление этого файла и при необходимости новый ADR в [docs/adr/README.md](adr/README.md).

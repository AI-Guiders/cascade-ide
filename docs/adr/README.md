# Architecture Decision Records (ADR) — CascadeIDE

Здесь фиксируются **принятые архитектурные решения**: контекст, выбор, последствия и отклонённые альтернативы. **[architecture-policy.md](../architecture-policy.md)** — краткий навигатор и таблица ссылок сюда; пошаговая миграция и контракты MCP — в [architecture-migration.md](../architecture-migration.md), [MCP-PROTOCOL.md](../MCP-PROTOCOL.md).

**Связь с политикой:** крупные смены направления — отдельным коммитом с обновлением навигатора и новой записью здесь.

**Статусы и жизненный цикл** (без списка ADR): [status-lifecycle.md](status-lifecycle.md).

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
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Контракты MCP и тестируемая инфраструктура | Accepted · Implemented |
| [0009](0009-strangler-migration-and-exceptions.md) | Strangler-миграция и исключения для spike | Accepted |
| [0010](0010-ui-modes-toml-configuration.md) | Данные UI-режимов в TOML | Accepted · Implemented |
| [0011](0011-debug-situational-awareness.md) | Ситуационная осведомлённость в отладке (полоска, hover; детали в панели) | Accepted (направление) |
| [0012](0012-floating-workspace-chrome.md) | Плавающий и отцепляемый хром workspace (телеметрия, полоски; не доки в v1) | Accepted (направление) |
| [0013](0013-command-surface-and-discoverability.md) | Поверхность команд и discoverability (палитра, минимальный toolbar) | Accepted (направление) |
| [0014](0014-situational-checklists.md) | Ситуационные чеклисты (модель, триггеры, UI; родитель — 0013) | Accepted (направление) |
| [0015](0015-editor-toml-syntax-highlighting.md) | Подсветка TOML в редакторе (шипнутый TextMate-пакет taplo; не LSP в v1) | Accepted · Implemented |
| [0016](0016-agent-client-protocol-external-agent.md) | Внешний агент по ACP (stdio, Cursor CLI); вендор SDK; UTF-8; ортогонально MCP | Accepted · Implemented |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Несколько окон приложения, зоны экрана, поверхности агента; MCP multi-root в scope | Accepted · Implemented |
| [0018](0018-ide-commands-canonical-xml-documentation.md) | Каноничные XML-доки для `IdeCommands` и ProtocolDocGen (вместо мини-языка только в summary) | Proposed |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Общий Git Core для Cascade IDE и git-mcp (паритет логики, прецедент agent-notes-core) | Accepted · Implemented |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | Видимость рассуждения агента: слои (ответ, трасс, опциональный лог), честные ограничения провайдеров LLM | Proposed |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / MFD — модель внимания кокпита Cascade IDE | Proposed |
| [0022](0022-mfd-visual-design-surface-axaml-blazor.md) | Визуальная поверхность разработки UI (AXAML / Blazor), ориентир WinForms, размещение на MFD | Proposed |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown + диаграммы (Mermaid/PlantUML) — first-class опыт через LSP и workflow | Proposed |
| [0024](0024-ide-sdk-and-stable-contracts.md) | SDK для CascadeIDE — стабильные контракты для внутреннего расширения и будущих плагинов | Proposed |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | SDK: привязка capabilities к зонам внимания (PFD/MFD/Forward/EICAS/HUD) | Proposed |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | Превью Markdown: поверхности и размещение (`workspace.toml`); внутренние отсылки (peek по «п. N»/якорям); не смешивать с языковым ADR 0023 | Superseded |
| [0027](0027-small-team-focus-vs-public-maturity.md) | Узкая команда (человек + ассистент) vs зрелость «для открытия»: две оси (границы vs очередь) | Accepted |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Пользовательские настройки: `settings.toml`, `%LocalAppData%\CascadeIDE\`, секреты в `ai-keys.toml` | Accepted · Implemented |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | Конфигурация: TOML-first; целостный UI настроек deferred; точечный UI — фасад канона | Accepted · Implemented |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Слои `command_id`, хоткеев и UI: IdeCommands, палитра, TOML, мост VM; чертёж единого каталога | Accepted · Implemented |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Чат агента: пакеты уточнений, ответы сложнее да/нет, треды; не смешивать с PFD-подтверждениями | Proposed |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD над редактором: настраиваемое содержимое, грамматика как у `presentation`; EBNF в ADR, парсер — по сложности DSL | Proposed |
| [0033](0033-internationalization-resx-avalonia.md) | i18n: ResX / культура .NET, Avalonia; строки UI не в TOML как основной слой; плюрализация — ключи или библиотека | Proposed |
| [0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) | Incapacitation, Emergency Mode; EICAS + сигналы КВС; liveness, контекстный HUD, safety interlock; webcam/analysis MCP opt-in | Proposed |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Встроенный браузер в MFD (WebView2), внешние веб-LLM; граница доверия с MCP; гибрид через оператора; мост веб↔MCP — вне baseline | Proposed |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Канал → CDS → композитор поверхности → поверхность; CDS как маршрутизация в модели внимания (Agent-first) | Accepted · Implemented |
| [0037](0037-pfd-surface-invariants-and-roslyn-enforcement.md) | PFD: инварианты поверхности; Roslyn; канон `[PfdStrict]` / `PfdStrictControl` | Proposed |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | Фасад агента: `AiProviderManager`, чат vs ACP vs автономный JSON-цикл; внешние MCP; идеи оркестрации и tool-calling | Accepted · Implemented |
| [0039](0039-workspace-navigation-affordances.md) | Навигация по workspace; C# / .NET north-star (не polyglot v1); граф/semantic map; PFD/MFD; MCP: пресеты, `kind_filter`, subgraph | Accepted · Implemented |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP C#/Markdown: командная строка в `settings.toml`, пресеты, опциональные ключи; env — открыто в ADR | Accepted · Implemented |
| [0041](0041-protobuf-for-agent-and-ide-messages.md) | Protobuf vs JSON для сообщений агента/IDE: границы, критерии, гибрид; точка входа (Proposed) | Proposed |
| [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | Pre-flight briefing: Planned Changes (намерение + SA) и Review Before Apply (превью, семантический слой, отказ без мусора); машина состояний; ортогонально «построчному доверию» | Proposed |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | Паритет восстановления MCP-транспорта (человек ↔ агент), граница хоста (Cursor) vs IDE; ортогонально ADR 0002 | Proposed |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Avalonia как хост (фюзеляж), кастомная отрисовка чата (Skia — гипотеза); **модель первична**, спайк следом; см. ADR 0031 | Proposed |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Persistence истории чата: append-only NDJSON события + метаданные и проекции; UI/рендер не источник правды | Proposed |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | CDS: `CockpitPresentationLayoutPolicy` и инварианты P/F/M; `presentation` как источник истины для меню/MCP/режимов/реактивного слоя | Accepted · Implemented |
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Инструмент кабины (`Instrument`): дескриптор слота (`CockpitInstrumentDescriptor`), не `Control`; SE vs карта намерений как примеры | Accepted · Implemented |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Чат через Cursor ACP в IDE: `mcpServers`, авто IDE MCP; приложения — пробелы тулов, разбор `mcp.json` ↔ CIDE | Proposed |
| [0049](0049-skia-surface-rollout-over-avalonia-host.md) | Поэтапный rollout Skia-поверхностей по CIDE при Avalonia-host; миграция волнами и dual-path | Proposed |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | Декларативная карта «инструмент → зона/слот» в TOML | Accepted · Implemented |
| [0051](0051-intent-based-attention-routing-toml.md) | Intent-based attention routing (TOML) | Accepted · Implemented |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | CLI для контракта агента (паритет с MCP) и снапшот-тесты | Accepted · Implemented |
| [0053](0053-semantic-map-control-flow-pfd.md) | Карта намерений и поток управления на PFD (control flow, KISS, subgraph) | Accepted · Implemented |
| [0054](0054-benchmarking-methodology-and-baselines.md) | Бенчмарки производительности и baseline-метрики для CIDE | Proposed |
| [0055](0055-skia-instrument-composition-pipeline.md) | Общий pipeline композиции Skia-инструментов (Intent -> Declutter -> Layout -> Render) | Accepted |
| [0056](0056-semantic-map-pipeline-adoption.md) | Карта намерений: внедрение общего Skia pipeline (композитор, controlFlow layout, cursor semantics) | Accepted · Implemented |
| [0057](0057-chat-surface-pipeline-adoption.md) | Chat surface: adoption общего Skia pipeline (threads, confirmations, dual-path rollout) | Accepted |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Сопряжение агента и Roslyn MCP в `settings.toml` (лимиты, виды узлов, таймауты, пресеты) | Proposed |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Профили Roslyn MCP, Manager, тактика (PFD) / EFB на MFD, Auto-Focus / Combat / Echelon | Proposed |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Аккордный слой (Ctrl+K), FMS-style, S/T, overlay; расширение ADR 0013 | Proposed |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | Контекстная карта ADR ↔ пути в `workspace.toml`, индикатор на PFD, intent/tooltip, advisory для агента (GPWS для доков) | Proposed (реализация отложена) |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | **GitMap:** submodules и git-границы **отдельно** от WSNC/карты намерений; общий Skia pipeline; собственный контракт/MCP; [git-and-submodules-v1](../git-and-submodules-v1.md) | Proposed |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **Instrument deck** + **`ContentRepresentation`**; таксономия примитивов (в т.ч. Readout, Trend, Gauge, Presence); **Presence/Activity vs Dark Cockpit**; `DedicatedPage` — режим Page для WH, не deck | Accepted |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | **Виды индикаторов** deck: единое графическое воплощение; **библиотека отрисовки** + **семантическая палитра**; без лишнего архитектурного слоя; `DeckPrimitiveKind` = каталог видов | Accepted |
| [0065](0065-instrument-categories-domain-taxonomy.md) | **Категории инструментов** и **типы графов** (`graph_kind`): домен ортогонально слоту/`instrument_id`; «Карта намерений» = карта **намерений кода**; см. таблицу `graph_kind` | Accepted |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | **Cockpit UI** vs **presentation IDE** (хром, оверлеи, тема): две опоры; не смешивать приборы/deck с оболочкой; `PrimitivesKit` vs `UiChrome` | Accepted |
| [0067](0067-graph-backed-surfaces-contract.md) | **Graph-backed surfaces:** общий контракт для семейства графовых экранов (данные, взаимодействие, навигация, layout, выделение, sync); карта намерений, GitMap, будущие графы | Accepted |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | **Полезная нагрузка строки канала** vs **проекция представления** vs **слот**: таблица/полоса ≠ тип ячейки; v1 — один DTO; гетерогенность — дискриминатор и шаблоны | Accepted |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | **Markdown Preview:** MFD tool как primary surface, native Markdig renderer first, WebView как optional adaptor; authoring-расширение ортогонально preview | Accepted |
| [0070](0070-command-palette-direct-overlay-surface.md) | **Command Palette:** прямой overlay surface в host, маршрутизация в активный `TopLevel`; `ModalOverlay` не канон для palette baseline | Accepted |
| [0071](0071-ai-assistance-sovereignty-locality-invisibility.md) | **AI / ассистент в IDE:** суверенитет, локальность, невидимость; анти-паттерн «облачный inline по умолчанию без контроля»; нарратив — [cascadeide-philosophy-v1](../design/cascadeide-philosophy-v1.md) | Proposed |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | **Чат:** topic cards, drill-in/back, adaptive default; intent-based Melody/Chords v1 для навигации по темам; уточняет [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) в chat-domain | Proposed |
| [0073](0073-pfd-instrument-deck.md) | **PFD instrument deck:** каталог вариантов состава (SA, code metrics, карта намерений, ADR indicator…); критерии «PFD vs по запросу»; живой черновик до выбора пресета | Proposed |
| [0074](0074-settings-ui-mfd-compact-layout-overflow.md) | **Настройки:** компактнее, якорь на **MFD**; нехватка места в **P+F+M** — таблица стратегий (scroll, min width, fallback-окно, …) | Proposed |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | **UI:** тематический указатель [`UI/README.md`](UI/README.md); соглашения по страницам MFD (payload vs проекция, keyboard-first); не заменяет плоский индекс | Proposed |
| [0076](0076-ui-ux-principles-hub.md) | **UI/UX:** центр принципов — связный текст из [`snippets/ui/`](snippets/ui/README.md) (внимание/кокпит, философия продукта); не заменяет исходные ADR | Proposed |
| [0077](0077-tech-principles-hub.md) | **TECH:** центр принципов — связный текст из [`snippets/tech/`](snippets/tech/README.md) (границы/контракты, агент/отладка/наблюдаемость); не заменяет исходные ADR | Proposed |
| [0078](0078-git-preflight-and-noise-control-for-cide.md) | **Git preflight:** шум-контроль (EOL/BOM/whitespace), safe fixes, подсказки логических коммитов и post-push отчёт | Proposed |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | **IDS (Ide Display System):** пайплайн IDE-оверлеев (intent → композитор → снимок → поверхность), ортогонально CDS; единый input host и слоты — по плану; Roslyn **CASCOPE013–016** | Accepted |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | **Intercom:** продуктовое имя канала связи вместо узкого «чат»; агент + команда + системные реплики; **внешний** командный контур vs своя «гора»; discoverability/i18n; strangler для кода | Proposed |
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | **Параметрические Intent Melody:** суффикс `:startLine:endLine` для операций над текстом редактора; валидация; рефакторинги по диапазону; UX (индикатор, chord vs палитра) | Proposed |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | **ACP + IDE MCP:** одна копия процесса — loopback HTTP/SSE вместо второго `CascadeIDE --mcp-stdio`; безопасность localhost; stdio для внешнего хоста сохранить | Proposed |
| [0083](0083-ai-mode-and-nested-settings-toml.md) | **`[ai]` в settings.toml:** `mode` = local \| acp \| mcp_only \| cloud; вложенные секции; без обратной совместимости со старым `provider` | Accepted · Implemented |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | **Правки агента:** единственный текст в редакторе; чат — намерение/статус; слой присутствия (курсор, «пишет»); дифф в чате не основной; без обязательного CRDT | Proposed |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | **Editor HUD:** inline-слой в редакторе (каретка, текст, gutter) vs **HUD banner** (полоса над текстом); IDS оверлеи отдельно; конфиг баннера — [0032](0032-hud-banner-configuration-and-grammar.md) | Proposed |
| [0086](0086-ui-theme-toml-canonical-json-mcp-wire.md) | **Тема UI:** канон кистей в `settings.toml`; JSON для `ide_get/set_ui_theme` как транспорт; strangler `Themes/*.json` | Proposed |
| [0087](0087-microsoft-agent-framework-builtin-agent-orchestration.md) | **Microsoft Agent Framework (MAF):** ориентир на оркестрацию встроенного агентного контура; след. шаг — PoC | Accepted · **след. шаг: PoC** |
| [0088](0088-host-slot-region-deck-cell-taxonomy.md) | **Host slot / регион / ячейка deck** — таксономия уровней; не смешивать | Proposed |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | **`get_ide_state`** вместо `get_workspace_state`; канал **IDE Health** вместо Workspace Health; ортогонально ADR 0002 | Accepted |
| [0090](0090-launch-profiles-and-debug-startup-configurations.md) | Профили запуска / несколько стартовых конфигураций отладки (как launch profiles в VS), хранение, MCP, миграция с `startup-project.json` | Accepted · Implemented |
| [0091](0091-pfd-debug-situational-deck-hypothesis.md) | Гипотеза: PFD **instrument deck** в режиме отладки — одной страницы Mfd (DebugStack) может не хватить; PFD = краткая сводка, Mfd = детали | Proposed |
| [0092](0092-visual-ui-designer-major-track.md) | Трек **Visual UI** (дизайнер разметки): отдельная крупная программная линия; норматив по UX — [0022](0022-mfd-visual-design-surface-axaml-blazor.md); приоритет Avalonia → Blazor → (опц.) Razor | Accepted (направление) |
| [0093](0093-mfd-embedded-browser-for-launch-url.md) | Расширение [0090](0090-launch-profiles-and-debug-startup-configurations.md): **опциональный** встроенный просмотр URL Kestrel на MFD рядом с отладкой; внешний браузер остаётся default; WebView2 / кроссплатформа — в roadmap | Proposed |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | **Шина доставки в UI** (аналогия **AFDX**): `Channel<T>`, батчинг, backpressure; ортогонально CDS-«каналу»; strangler с журнала сборки | Proposed |
| [0095](0095-workspace-solution-ide-health-stratification.md) | Три уровня Health: **Workspace** (папки, Git) · **Solution** (сборка, тесты) · **IDE** (LSP, MCP, окружение); таксономия для каналов/CDS/MCP; strangler от текущего IDE Health | Proposed |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | **Intercom:** карточка темы = заголовок + **сводка** (картотека); **spine** продуктовой линии ортогональна main thread (CIDE — пример); дополняет [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Proposed |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | **Вычислительные блоки кабины (CCU)**, аналог LRU *Unit*: свёртка сырья → DTO/снимок канала; ортогонально [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) (транспорт) и CDS; IDE Health в коде — эталон цепочки | Accepted · Implemented |
| [0098](0098-semantic-first-document-as-projection.md) | **Semantic-first:** первична семантическая карта; код/доки/git — проекции; согласование с [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) по сессии правок; strangler | Proposed |
| [0099](0099-ide-databus-typed-events-and-projections.md) | **IDE DataBus:** типизированные события в процессе IDE; развязка источников и проекций, без подмены 0094 (transport) и 0097 (CCU) | Accepted · Implemented |
| [0100](0100-project-constitution.md) | **Конституция проекта:** долговременные принципы, архитектурные инварианты, governance и порядок изменений основания проекта | Proposed |
| [0101](0101-licensing-and-commercialization-strategy.md) | Лицензирование и коммерциализация: матрица лицензий, правила зависимостей, guardrails для copyleft и план внедрения | Proposed |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | **Data Acquisition Layer (DAL):** явная граница добычи внешних данных и контракт DAL ↔ CCU ↔ UI | Proposed |
| [0103](0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) | **Субстрат Editor HUD:** `SemanticProjectionPipeline` / `EditorHudEngine` / `IEditorSurfaceAdapter`; DAL / CCU / DataBus; отдельный hi-freq bounded-контур; baseline AvaloniaEdit; сравнение хостов в `design/`, roadmap UI в `ux/` | Accepted (strangler) |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid codebase index: переносимое ядро + MCP для C#/Razor/AXAML (Roslyn истина для C#); гибрид FTS+vec | Accepted · Implemented |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | Встраивание hybrid index в CascadeIDE (DAL/CCU/DataBus/freshness) и граница Semantic Map | Proposed |
| [0107](0107-blank-solution-creation-via-dotnet-new-sln.md) | Пустое решение: `dotnet new sln`, меню/MCP, `BlankSolutionCreator` + `IDotnetCommandRunner` | Accepted · Implemented |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Веб-портал для внешних веб-ИИ: WebView, Host Object → `IdeCommands`/MCP; allowlist, согласие; PoC (Atlas / Search AI) | Accepted · Implemented |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Intent Melody: целевой каталог `[[melody_root]]` (`shape` + `tail_signature`); миграция с `[aliases]`+`[[parametric]]`; args в коде; плагины — обработка у плагина | Accepted · Implemented |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Рефакторинги Roslyn по диапазону: мост Intent Melody / IDE ↔ Roslyn MCP; без дублей Features в ядре; `rmx`/`rix` не в бандле до моста | Proposed |
| [0111](0111-editor-linenumber-linerange-value-objects.md) | Редактор: `LineNumber` / `LineRange` (1-based, Start ≤ End); `ParsedLineRange`; граница к JSON — `int` в args команд | Accepted · Implemented |
| [0112](0112-command-palette-query-modes-strategy.md) | Палитра (Ctrl+Q): режимы строки, стратегии и контракт бэкенда workspace-поиска (`t:`/`m:`/`x:`) с переключением в settings | Accepted · Implemented |
| [0113](0113-hci-semantic-map-orientation-layer.md) | HCI × Semantic Map: ориентация; оси **`graph_kind`** / **provenance** / **`relation_kind`**; быстрый текстовый referenced-by → Roslyn; `SemanticMapInputSnapshot` / CCU | Proposed |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | Тип отношения на ребре (`relation_kind`): «наследует», «ссылается на», partial peer, текстовое совпадение; ортогонально `graph_kind` и provenance; связь с `hit_kind` | Proposed |
| [0115](0115-cds-graph-backed-shared-layer.md) | CDS: общий слой реализации graph-backed **приборов** в кабине (не IDS); `IGraphDataSource` (v0); стык с [0036](0036-cds-channel-compositor-surface-pipeline.md) и [0067](0067-graph-backed-surfaces-contract.md) | Accepted |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Intercom: дерево сессии (ветвление, rewind, bookmark) и **steer** / **follow-up** при работе агента; [0045](0045-agent-chat-persistence-event-log-and-projections.md), [0080](0080-intercom-naming-and-multi-party-channel-model.md), [0096](0096-intercom-topic-card-summary-and-product-spine.md) | Proposed |
| [0117](0117-remote-operator-surface-multidevice.md) | Remote operator surface: **PWA**-пульт с телефона/другого ПК, Operator Gateway; не mobile IDE; [0017](0017-multi-window-workspace-and-agent-surfaces.md), [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Proposed |
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | Agent Notes Core **2.0**: TOML in-proc, `knowledge_path` в `IdeCommands`; паритет с agent-notes-mcp; bump NuGet | Proposed |

## Сборка в один документ (HTML, TXT, PDF)

Склейка нумерованных ADR и те же директивы **INCLUDE** / **INCLUDE_MANIFEST** / **INCLUDE_GLOB**, что в `resume/` (без DOCX). Запуск из этого каталога: `dotnet script build-adr.csx`. Зависимости и выходные пути — в [build/README.md](build/README.md).

## Соглашения

- **Имя файла:** `NNNN-краткий-kebab-title.md`, четыре цифры с ведущими нулями.
- **Статусы:** в шапке ADR и в колонке «Статус» — см. [status-lifecycle.md](status-lifecycle.md). Кратко: первый тег (**Proposed** / **Accepted** / **Superseded** / **Deprecated**); для **внедрённого в код** решения — **`Accepted · Implemented`** (второй тег через **` · `**). Без подпапок по статусу — один `docs/adr/`.
- **Тематические подпапки** (не по статусу): опционально указатель по теме — [0075](0075-ui-topic-index-and-mfd-page-conventions.md) и [`UI/README.md`](UI/README.md); **TECH** — [`TECH/README.md`](TECH/README.md) и [0077](0077-tech-principles-hub.md).
- Новый ADR добавляет строку в таблицу выше и при необходимости строку в таблицу в [architecture-policy.md](../architecture-policy.md).

<a id="adr-anchors-policy"></a>

### Внутренние якоря и отсылки (чтобы «см. п. N» работали как ссылки)

Канон текста — [snippets/adr-anchors-policy.md](snippets/adr-anchors-policy.md) (в сборке `build-adr.csx` можно подключать через `{{ INCLUDE: snippets/adr-anchors-policy.md }}` из `adr-book.md`).

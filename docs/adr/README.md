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
| [0047](0047-cockpit-instrument-descriptor-and-slot-composition.md) | Инструмент кабины (`Instrument`): дескриптор слота (`CockpitInstrumentDescriptor`), не `Control`; SE vs Semantic Map как примеры | Accepted · Implemented |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Чат через Cursor ACP в IDE: `mcpServers`, авто IDE MCP; приложения — пробелы тулов, разбор `mcp.json` ↔ CIDE | Proposed |
| [0049](0049-skia-surface-rollout-over-avalonia-host.md) | Поэтапный rollout Skia-поверхностей по CIDE при Avalonia-host; миграция волнами и dual-path | Proposed |
| [0050](0050-declarative-instrument-zone-placement-toml.md) | Декларативная карта «инструмент → зона/слот» в TOML | Accepted · Implemented |
| [0051](0051-intent-based-attention-routing-toml.md) | Intent-based attention routing (TOML) | Accepted · Implemented |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | CLI для контракта агента (паритет с MCP) и снапшот-тесты | Accepted · Implemented |
| [0053](0053-semantic-map-control-flow-pfd.md) | Semantic Map и поток управления на PFD (control flow, KISS, subgraph) | Accepted · Implemented |
| [0054](0054-benchmarking-methodology-and-baselines.md) | Бенчмарки производительности и baseline-метрики для CIDE | Proposed |
| [0055](0055-skia-instrument-composition-pipeline.md) | Общий pipeline композиции Skia-инструментов (Intent -> Declutter -> Layout -> Render) | Accepted |
| [0056](0056-semantic-map-pipeline-adoption.md) | Semantic Map: внедрение общего Skia pipeline (композитор, controlFlow layout, cursor semantics) | Accepted · Implemented |
| [0057](0057-chat-surface-pipeline-adoption.md) | Chat surface: adoption общего Skia pipeline (threads, confirmations, dual-path rollout) | Accepted |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | Сопряжение агента и Roslyn MCP в `settings.toml` (лимиты, виды узлов, таймауты, пресеты) | Proposed |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Профили Roslyn MCP, Manager, тактика (PFD) / EFB на MFD, Auto-Focus / Combat / Echelon | Proposed |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Аккордный слой (Ctrl+K), FMS-style, S/T, overlay; расширение ADR 0013 | Proposed |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | Контекстная карта ADR ↔ пути в `workspace.toml`, индикатор на PFD, intent/tooltip, advisory для агента (GPWS для доков) | Proposed (реализация отложена) |
| [0062](0062-git-submodules-semantic-map-subgraph.md) | **GitMap:** submodules и git-границы **отдельно** от WSNC/Semantic Map; общий Skia pipeline; собственный контракт/MCP; [git-and-submodules-v1](../git-and-submodules-v1.md) | Proposed |
| [0063](0063-instrument-deck-named-composition-one-anchor.md) | **Instrument deck** + **`ContentRepresentation`**; таксономия примитивов (в т.ч. Readout, Trend, Gauge, Presence); **Presence/Activity vs Dark Cockpit**; `DedicatedPage` — режим Page для WH, не deck | Accepted |
| [0064](0064-deck-primitives-visual-language-render-layer-and-palette.md) | **Виды индикаторов** deck: единое графическое воплощение; **библиотека отрисовки** + **семантическая палитра**; без лишнего архитектурного слоя; `DeckPrimitiveKind` = каталог видов | Accepted |
| [0065](0065-instrument-categories-domain-taxonomy.md) | **Категории инструментов** и **типы графов** (`graph_kind`): домен ортогонально слоту/`instrument_id`; Semantic Map = карта **намерений кода**; см. таблицу `graph_kind` | Accepted |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | **Cockpit UI** vs **presentation IDE** (хром, оверлеи, тема): две опоры; не смешивать приборы/deck с оболочкой; `PrimitivesKit` vs `UiChrome` | Accepted |
| [0067](0067-graph-backed-surfaces-contract.md) | **Graph-backed surfaces:** общий контракт для семейства графовых экранов (данные, взаимодействие, навигация, layout, выделение, sync); Semantic Map, GitMap, будущие графы | Accepted |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | **Полезная нагрузка строки канала** vs **проекция представления** vs **слот**: таблица/полоса ≠ тип ячейки; v1 — один DTO; гетерогенность — дискриминатор и шаблоны | Accepted |
| [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | **Markdown Preview:** MFD tool как primary surface, native Markdig renderer first, WebView как optional adaptor; authoring-расширение ортогонально preview | Accepted |
| [0070](0070-command-palette-direct-overlay-surface.md) | **Command Palette:** прямой overlay surface в host, маршрутизация в активный `TopLevel`; `ModalOverlay` не канон для palette baseline | Accepted |
| [0071](0071-ai-assistance-sovereignty-locality-invisibility.md) | **AI / ассистент в IDE:** суверенитет, локальность, невидимость; анти-паттерн «облачный inline по умолчанию без контроля»; нарратив — [cascadeide-philosophy-v1](../design/cascadeide-philosophy-v1.md) | Proposed |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | **Чат:** topic cards, drill-in/back, adaptive default; intent-based Melody/Chords v1 для навигации по темам; уточняет [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) в chat-domain | Proposed |
| [0073](0073-pfd-instrument-deck.md) | **PFD instrument deck:** каталог вариантов состава (SA, code metrics, Semantic Map, ADR indicator…); критерии «PFD vs по запросу»; живой черновик до выбора пресета | Proposed |
| [0074](0074-settings-ui-mfd-compact-layout-overflow.md) | **Настройки:** компактнее, якорь на **MFD**; нехватка места в **P+F+M** — таблица стратегий (scroll, min width, fallback-окно, …) | Proposed |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | **UI:** тематический указатель [`UI/README.md`](UI/README.md); соглашения по страницам MFD (payload vs проекция, keyboard-first); не заменяет плоский индекс | Proposed |
| [0076](0076-ui-ux-principles-hub.md) | **UI/UX:** центр принципов — связный текст из [`snippets/ui/`](snippets/ui/README.md) (внимание/кокпит, философия продукта); не заменяет исходные ADR | Proposed |
| [0077](0077-tech-principles-hub.md) | **TECH:** центр принципов — связный текст из [`snippets/tech/`](snippets/tech/README.md) (границы/контракты, агент/отладка/наблюдаемость); не заменяет исходные ADR | Proposed |
| [0078](0078-git-preflight-and-noise-control-for-cide.md) | **Git preflight:** шум-контроль (EOL/BOM/whitespace), safe fixes, подсказки логических коммитов и post-push отчёт | Proposed |

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

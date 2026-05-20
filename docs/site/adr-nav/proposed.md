---
hide:
  - toc
---

# Proposed

Черновик на обсуждение — решение ещё не принято.

[← Навигатор ADR](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0020](../../adr/0020-agent-reasoning-visibility-and-provider-limits.md) | Видимость рассуждения агента и ограничения провайдеров LLM | Proposed |
| [0022](../../adr/0022-mfd-visual-design-surface-axaml-blazor.md) | Визуальная поверхность разработки UI (AXAML / Blazor) — ориентир WinForms, размещение на MFD | Proposed |
| [0023](../../adr/0023-markdown-diagrams-language-tooling.md) | Markdown + диаграммы (Mermaid/PlantUML) — first-class опыт через LSP и workflow | Proposed |
| [0024](../../adr/0024-ide-sdk-and-stable-contracts.md) | SDK для CascadeIDE — стабильные контракты для внутреннего расширения и будущих плагинов | Proposed |
| [0025](../../adr/0025-sdk-attention-zones-and-capabilities.md) | SDK и зоны внимания (PFD / Forward / MFD / EICAS / HUD) | Proposed |
| [0031](../../adr/0031-agent-chat-clarification-batches-and-threading.md) | Чат агента — пакеты уточнений, ответы сложнее «да/нет», треды (направление) | Proposed (черновик направления до переработки UI чата; детали протокола и экранов — по итерациям) |
| [0032](../../adr/0032-hud-banner-configuration-and-grammar.md) | HUD над редактором — настраиваемое содержимое и грамматика (как у `presentation`) | Proposed (намерение зафиксировано; реализация — по плану). |
| [0033](../../adr/0033-internationalization-resx-avalonia.md) | Интернационализация (i18n) — ресурсы .NET, Avalonia, ортогонально TOML | Proposed (направление зафиксировано; объём языков и миграция строк — по плану). |
| [0034](../../adr/0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) | Недееспособность оператора (Incapacitation), Emergency Mode и опциональное сенсорное присутствие | Proposed (намерение и границы зафиксированы; реализация — по отдельной дорожной карте). |
| [0035](../../adr/0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Встроенный браузер в MFD, внешние веб-LLM и граница с MCP | Proposed (намерение и инварианты доверия; детали WebView и UX — по дорожной карте). |
| [0037](../../adr/0037-pfd-surface-invariants-and-roslyn-enforcement.md) | PFD — инварианты поверхности и проверка Roslyn (weight, input lock, каналы) | Proposed |
| [0041](../../adr/0041-protobuf-for-agent-and-ide-messages.md) | Protocol Buffers — рассмотрение для сообщений агента и IDE (точка входа) | Proposed (фиксация направления обсуждения и критериев; **не** решение о немедленной миграции с JSON) |
| [0042](../../adr/0042-pre-flight-planned-changes-and-review-before-apply.md) | Pre-flight briefing — Planned Changes и Review Before Apply | Proposed |
| [0043](../../adr/0043-mcp-transport-recovery-human-agent-parity.md) | Паритет восстановления MCP-транспорта (человек ↔ агент) и границы хоста | Proposed |
| [0054](../../adr/0054-benchmarking-methodology-and-baselines.md) | Бенчмарки производительности и baseline-метрики | Proposed |
| [0058](../../adr/0058-agent-roslyn-mcp-coupling-settings-toml.md) | Сопряжение агента и Roslyn MCP в `settings.toml` (лимиты, виды узлов, таймауты, пресеты) | Proposed |
| [0059](../../adr/0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | Профили Roslyn MCP, Manager, тактика/стратегия и EFB (MFD) | Proposed |
| [0061](../../adr/0061-context-aware-adr-map-pfd-knowledge-indicator.md) | Контекстная привязка ADR к путям кода (GPWS для документации) и индикатор на PFD | Proposed (реализация отложена) |
| [0062](../../adr/0062-git-submodules-semantic-map-subgraph.md) | GitMap — карта git-границ (submodules) отдельно от workspace navigation context | Proposed — черновик для обсуждения; реализация не зафиксирована. |
| [0071](../../adr/0071-ai-assistance-sovereignty-locality-invisibility.md) | Принципы интеграции AI/ассистента в IDE — суверенитет, локальность, невидимость | Proposed |
| [0073](../../adr/0073-pfd-instrument-deck.md) | PFD instrument deck — каталог вариантов состава и поверхностей (SA) | Proposed |
| [0074](../../adr/0074-settings-ui-mfd-compact-layout-overflow.md) | UI настроек — компактнее, якорь на MFD; нехватка места в раскладке P+F+M | Proposed |
| [0075](../../adr/0075-ui-topic-index-and-mfd-page-conventions.md) | Тематический указатель UI (`docs/adr/UI/`) и соглашения по страницам MFD | Proposed |
| [0077](../../adr/0077-tech-principles-hub.md) | TECH — центр принципов (связный текст из канона) | Proposed |
| [0082](../../adr/0082-acp-ide-mcp-loopback-single-process.md) | ACP и MCP IDE — одна копия процесса (loopback HTTP/SSE вместо второго `CascadeIDE --mcp-stdio`) | Proposed |
| [0084](../../adr/0084-agent-edits-editor-source-of-truth-presence-channel.md) | Правки агента в редакторе как единственный текстовый источник правды; чат — намерение и статус; слой присутствия (GDocs-like, без обязательного CRDT) | Proposed |
| [0085](../../adr/0085-editor-hud-inline-layer-and-hud-banner.md) | Editor HUD — inline-слой в редакторе и отличие от HUD banner | Proposed |
| [0086](../../adr/0086-ui-theme-toml-canonical-json-mcp-wire.md) | Тема UI — канон в TOML, JSON как транспорт MCP (strangler от `Themes/*.json`) | Proposed |
| [0088](../../adr/0088-host-slot-region-deck-cell-taxonomy.md) | Host slot, регион внимания и ячейка deck — таксономия (не смешивать) | Proposed |
| [0091](../../adr/0091-pfd-debug-situational-deck-hypothesis.md) | Гипотеза — PFD instrument deck в режиме отладки (MFD DebugStack не исчерпывает) | Proposed |
| [0098](../../adr/0098-semantic-first-document-as-projection.md) | Семантика первична; документ и репозиторий — проекции (Semantic-First) | Proposed |
| [0104](../../adr/0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md) | Reasoning Substrate and Cognitive Decomposition Loop for MAF | Proposed |
| [0110](../../adr/0110-roslyn-refactor-intent-melody-bridge.md) | Рефакторинги Roslyn по диапазону — мост Intent Melody / IDE и Roslyn MCP | Proposed |
| [0113](../../adr/0113-hci-semantic-map-orientation-layer.md) | HCI и Semantic Map — слой ориентации (не граф) | Proposed |
| [0114](../../adr/0114-graph-edge-relation-kind-taxonomy.md) | Тип отношения на рёбрах графа (`relation_kind`) — семантика связи | Proposed |
| [0116](../../adr/0116-intercom-session-tree-and-agent-message-steering.md) | Intercom — дерево сессии (ветвление) и steer / follow-up при работе агента | Proposed |
| [0117](../../adr/0117-remote-operator-surface-multidevice.md) | Remote operator surface — мультидевайсность оператора (пульт, не mobile IDE) | Proposed |
| [0122](../../adr/0122-collaborative-iop-environment-and-shared-situational-display.md) | Командная среда IOP — рабочие места и общий ситуационный экран | Proposed |
| [0127](../../adr/0127-intercom-spine-and-topic-tabs-chrome-navigation.md) | Intercom — spine и вкладки тем в chrome (навигация без overview) | Proposed · **согласованное направление** (2026-05-18) |
| [0132](../../adr/0132-intercom-federated-transport-and-multi-client-boundary.md) | Intercom — федерация, общий transport и граница multi-client (CIDE / Web / MCC) | Proposed |
| [0133](../../adr/0133-commander-cockpit-shared-attention-model-and-instrument-deck.md) | Commander cockpit — общая модель внимания и instrument deck по роли | Proposed |


---

_Сгенерировано `tools/gen_adr_pages.py`. Не редактировать вручную._

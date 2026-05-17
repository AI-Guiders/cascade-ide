---
hide:
  - toc
---

# Accepted · Implemented

Принято и основная поставка в коде выполнена.

[← Навигатор ADR](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0008](../../adr/0008-mcp-contracts-and-testable-infrastructure.md) | Стабильные контракты MCP и тестируемая инфраструктура | Accepted · Implemented |
| [0010](../../adr/0010-ui-modes-toml-configuration.md) | Данные UI-режимов (Focus / Balanced / …) в TOML | Accepted · Implemented |
| [0015](../../adr/0015-editor-toml-syntax-highlighting.md) | Подсветка TOML в текстовом редакторе | Accepted · Implemented |
| [0016](../../adr/0016-agent-client-protocol-external-agent.md) | Внешний агент по Agent Client Protocol (stdio, Cursor CLI) | Accepted · Implemented |
| [0017](../../adr/0017-multi-window-workspace-and-agent-surfaces.md) | Несколько окон приложения (мультиоконность), зоны экрана и поверхности агента | Accepted · Implemented |
| [0019](../../adr/0019-shared-git-core-ide-and-git-mcp.md) | Общий Git Core для Cascade IDE и git-mcp | Accepted · Implemented |
| [0028](../../adr/0028-user-settings-toml-localappdata-and-secrets.md) | Пользовательские настройки — `settings.toml`, каталог `%LocalAppData%\CascadeIDE\`, секреты отдельно | Accepted · Implemented |
| [0029](../../adr/0029-configuration-toml-canonical-ui-facade.md) | Конфигурация — **TOML-first** (канон на диске); **целостный** UI настроек — **deferred**; точечный UI — **фасад канона**, не вторая правда | Accepted · Implemented (TOML-first на диске; целостный UI настроек — deferred; точечный UI — фасад) |
| [0030](../../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) | Слои идентификаторов команд, хоткеев и UI (без одной таблицы «всё в одном» пока) | Accepted · Implemented (реестр команд v1 в коде) |
| [0036](../../adr/0036-cds-channel-compositor-surface-pipeline.md) | Канал → CDS → композитор поверхности → поверхность (Agent-first отображение) | Accepted · Implemented |
| [0038](../../adr/0038-agent-facade-ai-provider-and-tool-orchestration.md) | Фасад агента — провайдеры LLM, чат и оркестрация инструментов | Accepted · Implemented (текущий код); раздел «Направление» — черновик идей, не обязательства |
| [0039](../../adr/0039-workspace-navigation-affordances.md) | Навигация по workspace — несколько представлений и «текущий файл + связанные» | Accepted · Implemented |
| [0040](../../adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP (C# / Markdown) — командная строка в `settings.toml`: пресеты, опциональные ключи, переопределение через окружение | Accepted · Implemented (как [§Решение](#решение) ниже) |
| [0044](../../adr/0044-avalonia-host-skia-agent-chat-surface.md) | Разделение ролей — Avalonia как хост («фюзеляж»), кастомная отрисовка для чата агента (Skia как гипотеза) | Accepted · Implemented |
| [0045](../../adr/0045-agent-chat-persistence-event-log-and-projections.md) | Persistence истории чата — append-only события + проекции | Accepted · Implemented |
| [0046](../../adr/0046-presentation-layout-authority-and-cockpit-invariants.md) | Cockpit CDS — policy раскладки (`CockpitPresentationLayoutPolicy`) и инварианты P/F/M | Accepted · Implemented |
| [0047](../../adr/0047-cockpit-instrument-descriptor-and-slot-composition.md) | Инструмент кабины (`Instrument`) — дескриптор композиции слота, не `Control` | Accepted · Implemented |
| [0050](../../adr/0050-declarative-instrument-zone-placement-toml.md) | Декларативная карта «инструмент → зона/слот» в TOML | Accepted · Implemented |
| [0051](../../adr/0051-intent-based-attention-routing-toml.md) | Intent-based attention routing (TOML) | Accepted · Implemented |
| [0052](../../adr/0052-agent-contract-cli-and-snapshot-tests.md) | CLI для контракта агента (паритет с MCP) и снапшот-тесты | Accepted · Implemented |
| [0053](../../adr/0053-semantic-map-control-flow-pfd.md) | Карта намерений и поток управления на PFD (control flow) | Accepted · Implemented |
| [0056](../../adr/0056-semantic-map-pipeline-adoption.md) | Semantic Map adoption of Skia composition pipeline | Accepted · Implemented |
| [0057](../../adr/0057-chat-surface-pipeline-adoption.md) | Chat surface adoption of Skia composition pipeline | Accepted · Implemented |
| [0069](../../adr/0069-markdown-preview-tool-surface-and-renderer-decoupling.md) | Markdown Preview — инструмент MFD, renderer-first decoupling и отказ от inline preview в документе | Accepted · Implemented |
| [0070](../../adr/0070-command-palette-direct-overlay-surface.md) | Command Palette как прямой overlay surface, маршрутизируемый в активный TopLevel | Accepted · Implemented |
| [0072](../../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Chat topic cards, drill-in/back и intent-based Melody/Chords для навигации по темам | Accepted · Implemented |
| [0078](../../adr/0078-git-preflight-and-noise-control-for-cide.md) | Git preflight и шум-контроль изменений в CIDE | Accepted · Implemented |
| [0081](../../adr/0081-parametric-intent-melodies-editor-line-ranges.md) | Параметрические Intent Melody — диапазоны строк редактора (`:start:end`) | Accepted · Implemented |
| [0083](../../adr/0083-ai-mode-and-nested-settings-toml.md) | `settings.toml` — дискриминант `ai.mode` и вложенные секции (local / acp / mcp_only / cloud) | Accepted · Implemented |
| [0090](../../adr/0090-launch-profiles-and-debug-startup-configurations.md) | Профили запуска и несколько стартовых конфигураций отладки (как launch profiles в VS) | Accepted · Implemented |
| [0093](../../adr/0093-mfd-embedded-browser-for-launch-url.md) | Встроенный просмотр URL запуска на MFD (расширение к профилям и launchBrowser) | Accepted · Implemented |
| [0096](../../adr/0096-intercom-topic-card-summary-and-product-spine.md) | Intercom — сводка на карточке темы (картотека) и сквозная линия продукта (spine) | Accepted · Implemented |
| [0097](../../adr/0097-cockpit-compute-units-transport-to-channel-dto.md) | Вычислительные блоки кабины (CCU; аналог LRU *Unit*) — слой между транспортом, смыслом и каналом | Accepted · Implemented |
| [0099](../../adr/0099-ide-databus-typed-events-and-projections.md) | IDE DataBus — типизированные события и проекции состояния | Accepted · Implemented |
| [0105](../../adr/0105-hybrid-codebase-index-for-csharp-web.md) | Hybrid Codebase Index (ядро + MCP) for C# stacks with Roslyn Truth | Accepted · Implemented |
| [0107](../../adr/0107-blank-solution-creation-via-dotnet-new-sln.md) | Создание пустого решения через `dotnet new sln` (самодостаточность workspace) | Accepted · Implemented |
| [0108](../../adr/0108-web-ai-portal-host-object-tools-bridge.md) | Встроенный веб-портал для внешних веб-ИИ и мост инструментов через Host Object (WebView → IDE) | Accepted · Implemented |
| [0109](../../adr/0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Единый декларативный каталог параметрических Intent Melody (TOML + кодовое связывание args) | Accepted · Implemented |
| [0112](../../adr/0112-command-palette-query-modes-strategy.md) | Режимы строки палитры (`f:` / `t:` / `m:` / `x:` / `c:`) — модель режимов, стратегии и **бэкенды** workspace-поиска | Accepted · Implemented |
| [0117](../../adr/0117-ide-skia-kit.md) | SkiaKit — переиспользуемые Skia-примитивы IDE | Accepted · Implemented |
| [0118](../../adr/0118-agent-notes-core-2-toml-and-knowledge-path.md) | Agent Notes Core 2.0 — TOML, `knowledge_path`, паритет с agent-notes-mcp | Accepted · Implemented |


---

_Сгенерировано `tools/gen_adr_pages.py`. Не редактировать вручную._

---
hide:
  - toc
---

# Accepted

Принято как норма; внедрение в коде не завершено или намеренно растянуто.

[← Навигатор ADR](index.md)

| ID | Title | Status (raw) |
|----|-------|----------------|
| [0001](../../adr/0001-debug-hypotheses-json-storage.md) | Хранение гипотез отладки в одном JSON-файле | Accepted |
| [0002](../../adr/0002-debug-human-agent-parity.md) | Единый слой состояния отладки для человека и агента | Accepted |
| [0003](../../adr/0003-debug-ui-mode-separate-from-power.md) | Отдельный UI-режим Debug (не кокпит Power) | Accepted (продуктовое направление); **реализация** — по плану релизов |
| [0004](../../adr/0004-ui-thread-marshaling.md) | Маршалинг обновлений UI через IUiScheduler | Accepted (план внедрения — strangler) |
| [0005](../../adr/0005-defer-dynamic-plugins-mef.md) | Не целевой шаг — динамические плагины (MEF и аналоги) | Accepted |
| [0006](../../adr/0006-presentation-layers-and-feature-slices.md) | Слои, вертикальные срезы и роль MainWindowViewModel | Accepted |
| [0007](../../adr/0007-signals-coupling-and-ui-backpressure.md) | Сигналы, слабая связность и снятие нагрузки с UI | Accepted (внедрение — strangler) |
| [0009](../../adr/0009-strangler-migration-and-exceptions.md) | Strangler-миграция и когда допускаются отклонения от политики | Accepted |
| [0011](../../adr/0011-debug-situational-awareness.md) | Ситуационная осведомлённость в отладке (приоритет над «полной» нижней панелью) | Accepted (направление; конкретные экраны и хоткеи — по итерациям реализации) |
| [0012](../../adr/0012-floating-workspace-chrome.md) | Плавающий и отцепляемый хром workspace (к нижней зоне и ситуационной осведомлённости) | Accepted (направление; объём v1 и конкретные контролы — по итерациям) |
| [0013](../../adr/0013-command-surface-and-discoverability.md) | Поверхность команд и discoverability (палитра, минимальный toolbar) | Accepted (направление; состав команд и итерации UI — отдельно) |
| [0014](../../adr/0014-situational-checklists.md) | Ситуационные чеклисты (модель, триггеры, UI) | Accepted |
| [0018](../../adr/0018-ide-commands-canonical-xml-documentation.md) | Каноничные XML-доки для `IdeCommands` и ProtocolDocGen | Accepted (частично: ProtocolDocGen + generated summaries; полный XML на IdeCommands — по миграции) |
| [0021](../../adr/0021-pfd-mfd-cockpit-attention-model.md) | PFD / MFD — модель внимания кокпита Cascade IDE | Accepted |
| [0022](../../adr/0022-workspace-health-lexicon.md) | Лексикон и канон имён — IDE Health (эволюция названий; файл ADR сохранён как 0022) | Accepted |
| [0023](../../adr/0023-environment-readiness-glance.md) | Канал «готовность окружения» (glance) — отдельно от IDE Health | Accepted (границы решения и отбор сигналов; конкретный UI и типы в коде — по мере реализации) |
| [0027](../../adr/0027-small-team-focus-vs-public-maturity.md) | Узкая команда (человек + ассистент) и зрелость «для открытия» — две оси, не одна очередь | Accepted |
| [0048](../../adr/0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Чат через Cursor ACP в IDE — цель паритета с хостом Cursor, поверхность инструментов и MCP | Accepted (частично: Cursor ACP в IDE, auto-inject MCP; полный parity — по ADR) |
| [0049](../../adr/0049-skia-surface-rollout-over-avalonia-host.md) | Поэтапный rollout Skia-поверхностей при Avalonia-host (CIDE-wide) | Accepted (частично: chat surface, SkiaKit; остальные поверхности — по волнам) |
| [0055](../../adr/0055-skia-instrument-composition-pipeline.md) | Skia instrument composition pipeline (Intent -> Declutter -> Layout -> Render) | Accepted |
| [0060](../../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) | Аккордный слой (FMS-style), S/T и overlay — расширение keyboard-first (ADR 0013) | Accepted (частично: Intent Melody каталог + chord/palette v1; полный FMS T/S overlay — по ADR) |
| [0063](../../adr/0063-instrument-deck-named-composition-one-anchor.md) | Instrument deck — именованная композиция инструментов в одном якоре внимания | Accepted |
| [0064](../../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) | Виды индикаторов deck — визуальный язык, отрисовка и семантическая палитра | Accepted |
| [0065](../../adr/0065-instrument-categories-domain-taxonomy.md) | Категории инструментов и типы графов (ортогонально слоту и `instrument_id`) | Accepted |
| [0066](../../adr/0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI и слой presentation IDE — раздельные опоры | Accepted |
| [0067](../../adr/0067-graph-backed-surfaces-contract.md) | Graph-backed surfaces — общий контракт для семейства графовых экранов | Accepted |
| [0068](../../adr/0068-deck-row-payload-and-presentation-projection.md) | Полезная нагрузка строки канала и проекция на поверхность (layout vs cell content) | Accepted |
| [0076](../../adr/0076-ui-ux-principles-hub.md) | UI/UX — центр принципов (связный текст из канона) | Accepted |
| [0079](../../adr/0079-ide-display-system-ids-overlay-pipeline.md) | IDS (Ide Display System) — пайплайн оверлеев IDE, ортогонально CDS | Accepted |
| [0080](../../adr/0080-intercom-naming-and-multi-party-channel-model.md) | Intercom — имя и модель канала связи (не только «чат с агентом») | Accepted (strangler: Intercom в UI и docs v1; multi-party и внешний контур — по дорожной карте) |
| [0087](../../adr/0087-microsoft-agent-framework-builtin-agent-orchestration.md) | Microsoft Agent Framework (MAF) — ориентир на слой оркестрации **встроенного** агентного контура | Accepted · **следующий шаг: PoC** |
| [0089](../../adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | Именование омнибуса агента (`get_ide_state`) и канал **IDE Health** (вместо Workspace Health) | Accepted |
| [0092](../../adr/0092-visual-ui-designer-major-track.md) | Трек **Visual UI** (дизайнер разметки) — отдельная крупная программная линия CIDE | Accepted (направление) |
| [0094](../../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Шина доставки событий в UI (аналогия AFDX) и `System.Threading.Channel<T>` | Accepted |
| [0095](../../adr/0095-workspace-solution-ide-health-stratification.md) | Три уровня Health — Workspace, Solution, IDE (таксономия каналов) | Accepted (частично: WorkspaceHealth MFD + каналы; полная трёхуровневая таксономия — по ADR) |
| [0100](../../adr/0100-project-constitution.md) | Конституция проекта | Accepted |
| [0101](../../adr/0101-licensing-and-commercialization-strategy.md) | Лицензирование и стратегия коммерциализации | Accepted |
| [0102](../../adr/0102-data-acquisition-layer-boundary-and-contract.md) | Data Acquisition Layer — граница внешних интерфейсов и адаптеров | Accepted |
| [0103](../../adr/0103-editor-hud-substrate-semantic-projection-and-surface-adapter.md) | субстрат Editor HUD — семантическая проекция, адаптер поверхности, границы DAL / CCU / DataBus | Accepted (strangler) |
| [0115](../../adr/0115-cds-graph-backed-shared-layer.md) | CDS — общий слой graph-backed приборов (реализация в кабине, не IDS) | Accepted |
| [0121](../../adr/0121-intent-oriented-programming-paradigm.md) | Парадигма Intent-Oriented Programming (IOP) — концептуальный фундамент Cascade IDE | Accepted |


---

_Сгенерировано `tools/gen_adr_pages.py`. Не редактировать вручную._

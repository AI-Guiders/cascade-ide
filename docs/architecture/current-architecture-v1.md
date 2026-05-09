# Текущая архитектура CascadeIDE (v1)

Этот документ — **единая точка входа** про то, **как устроено сейчас** (а не “почему так”).  
Контекст решений и альтернативы — в **ADR**: [`docs/adr/README.md`](../adr/README.md).  
Политика и “куда смотреть” — [`docs/architecture-policy.md`](../architecture-policy.md).

---

## 1. Модель системы (на одном экране)

CascadeIDE — desktop IDE (Avalonia + MVVM), где **семантика “кабины”** (PFD / Forward / MFD) задаёт структуру внимания, а MCP делает IDE управляемой агентом.

- **PFD**: первичная зона внимания, короткая ситуационная сводка и “командирские” индикаторы.
- **Forward**: рабочая зона (редактор/доки), основной поток действий.
- **MFD**: вторичный контур — **длинные потоки** (терминал/сборка/Git/…): **страницы** стека.

Эталон layout главного окна и имена регионов для MCP:
- [`docs/ux/cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md)

---

## 2. Слои и границы ответственности

Норматив по слоям и “что где живёт”:
- **ADR 0006**: слои, срезы фич, роль `MainWindowViewModel` — [`docs/adr/0006-presentation-layers-and-feature-slices.md`](../adr/0006-presentation-layers-and-feature-slices.md)
- **ADR 0102**: DAL (граница внешних адаптеров) — [`docs/adr/0102-data-acquisition-layer-boundary-and-contract.md`](../adr/0102-data-acquisition-layer-boundary-and-contract.md)
- **ADR 0097**: CCU (свёртка сырья → DTO/снимок) — [`docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md`](../adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
- **ADR 0099**: IDE DataBus (типизированные события) — [`docs/adr/0099-ide-databus-typed-events-and-projections.md`](../adr/0099-ide-databus-typed-events-and-projections.md)
- **ADR 0036**: CDS → композитор → поверхность (кабина как домен смысла) — [`docs/adr/0036-cds-channel-compositor-surface-pipeline.md`](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **ADR 0079**: IDS (IDE overlays) как отдельный домен от CDS — [`docs/adr/0079-ide-display-system-ids-overlay-pipeline.md`](../adr/0079-ide-display-system-ids-overlay-pipeline.md)

Практическая ментальная модель (сверху вниз):

- **UI (Views)**: `Views/*.axaml` и связанные `*.axaml.cs`. Держим простым: layout, биндинги, именование регионов.
- **VM (ViewModels)**: композиция состояния, команд и связей между зонами внимания.
- **Application / orchestration**: use-case координация внутри фичи (как правило `Features/<Feature>/Application/*`).
- **DAL**: вход/выход во внешний мир (процессы, git, LSP, MCP-клиенты, файловая система).
- **Transport / bus / batching**: доставка событий/строк в UI (bounded, backpressure).
- **CCU**: свёртка событий/сырья в DTO, пригодный для UI и наблюдаемости.

---

## 3. Архитектура UI “Flight” (факт, не концепт)

Главное окно — **три колонки** PFD | Forward | MFD. Длинные потоки (Terminal/Build/Git/…) живут как **страницы MFD**, а не как нижняя панель на всю ширину.

Подробно:
- [`docs/ux/cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md)
- Карта “концепт → код” (что историческое, что актуальное): [`docs/ux/concept-to-implementation-map-v1.md`](../ux/concept-to-implementation-map-v1.md)

Ключевые элементы MFD (по текущим именам):
- `MfdShellView` + `MfdShellPageStack`
- регион “хост стека” (для снимков/темы/контрактов): `MfdContourStackHost`

---

## 4. MCP: IDE как сервер инструментов

Контракт и протокол:
- [`docs/MCP-PROTOCOL.md`](../MCP-PROTOCOL.md)
- **ADR 0008** (контракты и тестируемая инфраструктура): [`docs/adr/0008-mcp-contracts-and-testable-infrastructure.md`](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **ADR 0052** (CLI контракта и снапшот-тесты): [`docs/adr/0052-agent-contract-cli-and-snapshot-tests.md`](../adr/0052-agent-contract-cli-and-snapshot-tests.md)

Что важно помнить:
- MCP ориентирован на **наблюдаемость** (снимки, диагностики) и **управление** (команды), а не на “тайные” API.
- Ключи/имена регионов UI, которые видит агент, должны быть стабильны и задокументированы (см. layout doc выше).

---

## 5. Hybrid Index и навигация по коду (вкратце)

- **Hybrid index (FTS + vec)** как локальная БД контекста:  
  [`docs/adr/0105-hybrid-codebase-index-for-csharp-web.md`](../adr/0105-hybrid-codebase-index-for-csharp-web.md) (Accepted · Implemented)  
  [`docs/adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md`](../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) (Proposed)
- Навигационный MCP (`get_code_navigation_context`) и presets:  
  [`docs/adr/0039-workspace-navigation-affordances.md`](../adr/0039-workspace-navigation-affordances.md)

---

## 6. “Где смотреть в коде” (якоря)

Это не полный список, а “первая десятка” для ориентации.

- **UI layout / регионы**: `Views/MainWindow.axaml`, `Views/MfdShellView.axaml`
- **VM главного окна**: `ViewModels/MainWindowViewModel.*.cs` (partials)
- **Hybrid Index orchestration**: `Features/HybridIndex/Application/*`
- **CCU / каналы кабины**: `Cockpit/ComputingUnits/*`, `Cockpit/Channels/*`, `Cockpit/Cds/*`, `Cockpit/Composition/*`, `Cockpit/Surface/*`
- **MCP tool catalog / protocol docs**: `Services/*` (см. ADR 0008 и `MCP-PROTOCOL.md`)

Если цель — “поймать границу слоёв”, полезны Roslyn анализаторы:
- [`CascadeIDE.ArchitectureAnalyzers/README.md`](../../CascadeIDE.ArchitectureAnalyzers/README.md)

---

## 7. Что считать историческим (и не путать с текущим)

В репозитории есть документы и концепты, описывающие старые макеты (например “нижняя панель” на всю ширину).  
Смотри явные пометки “старой топологии” в:
- [`docs/ux/cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md)
- [`docs/architecture-migration.md`](../architecture-migration.md)

---

## 8. Как обновлять этот документ

Обновляй, когда меняется хотя бы одно из:
- топология зон внимания (PFD/Forward/MFD), ключевые регионы и их имена;
- границы слоёв (DAL/CCU/DataBus/IDS/CDS) или главный “путь данных”;
- контракт MCP (новые инструменты, смена ключей/форматов).

Версия: **v1** (актуальный “срез”).  

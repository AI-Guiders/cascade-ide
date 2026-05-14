# ADR map: как читать архитектуру CascadeIDE (v1)

Этот документ отвечает на вопрос: **“какие ADR читать, чтобы понять систему”** — по темам и по роли.  
Он не заменяет индекс ADR: [`docs/adr/README.md`](../adr/README.md).  
Текущее состояние архитектуры “как есть” — [`current-architecture-v1.md`](current-architecture-v1.md).

---

## 1. С чего начать (быстрый маршрут)

- **Слои и границы**: [`0006`](../adr/0006-presentation-layers-and-feature-slices.md)
- **Поток данных и “кабина” (CDS)**: [`0036`](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **Overlay-домен IDE (IDS), отдельно от CDS**: [`0079`](../adr/0079-ide-display-system-ids-overlay-pipeline.md)
- **MCP контракты и тестируемая инфраструктура**: [`0008`](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **Навигация по коду / MCP навигации**: [`0039`](../adr/0039-workspace-navigation-affordances.md)

Если ты читаешь это ради “как сейчас устроен UI Flight”, начни не с ADR, а с layout doc:
- [`docs/ux/cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md)

---

## 2. UI и модель внимания (PFD / Forward / MFD)

- **Модель внимания и терминология PFD/MFD**: [`0021`](../adr/0021-pfd-mfd-cockpit-attention-model.md) *(Proposed, но задаёт язык)*  
- **Инварианты раскладки и authority `presentation`**: [`0046`](../adr/0046-presentation-layout-authority-and-cockpit-invariants.md) *(Accepted · Implemented)*
- **Мультиоконность и поверхности**: [`0017`](../adr/0017-multi-window-workspace-and-agent-surfaces.md) *(Accepted · Implemented)*

---

## 3. MVVM, срезы фич и “что где живёт”

- **Срезы и слои**: [`0006`](../adr/0006-presentation-layers-and-feature-slices.md)
- **Strangler-миграция и исключения**: [`0009`](../adr/0009-strangler-migration-and-exceptions.md)

---

## 4. Transport / backpressure / доставка в UI

- **Сигналы, связность, backpressure**: [`0007`](../adr/0007-signals-coupling-and-ui-backpressure.md)
- **Маршалинг на UI**: [`0004`](../adr/0004-ui-thread-marshaling.md)
- **Шина доставки (AFDX-аналоги)**: [`0094`](../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md)

---

## 5. DAL / CCU / DataBus (pipeline “сырьё → DTO → UI”)

- **DAL boundary**: [`0102`](../adr/0102-data-acquisition-layer-boundary-and-contract.md)
- **CCU как слой свёртки**: [`0097`](../adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
- **CDS (канал → композитор → поверхность)**: [`0036`](../adr/0036-cds-channel-compositor-surface-pipeline.md)
- **Graph-backed приборы — общий слой внутри CDS**: [`0115`](../adr/0115-cds-graph-backed-shared-layer.md) *(Proposed)*
- **IDE DataBus**: [`0099`](../adr/0099-ide-databus-typed-events-and-projections.md)
- **Health stratification**: [`0095`](../adr/0095-workspace-solution-ide-health-stratification.md) *(если интересует “что такое IDE/Solution/Workspace health”)*  

---

## 6. MCP, агент и тестируемость контрактов

- **MCP contracts + testable infrastructure**: [`0008`](../adr/0008-mcp-contracts-and-testable-infrastructure.md)
- **Agent contract CLI + snapshot tests**: [`0052`](../adr/0052-agent-contract-cli-and-snapshot-tests.md)
- **Visibility of reasoning / provider limits**: [`0020`](../adr/0020-agent-reasoning-visibility-and-provider-limits.md)

---

## 7. Навигация по коду и индексация

- **Workspace navigation affordances**: [`0039`](../adr/0039-workspace-navigation-affordances.md)
- **Hybrid codebase index core**: [`0105`](../adr/0105-hybrid-codebase-index-for-csharp-web.md) *(Accepted · Implemented)*
- **Integration Hybrid index ↔ CascadeIDE**: [`0106`](../adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)
- **HCI и Semantic Map (ориентация, три оси)**: [`0113`](../adr/0113-hci-semantic-map-orientation-layer.md)
- **Тип отношения на ребре (`relation_kind`)**: [`0114`](../adr/0114-graph-edge-relation-kind-taxonomy.md)
- **Типы графов (`graph_kind`) и категории инструментов**: [`0065`](../adr/0065-instrument-categories-domain-taxonomy.md)
- **Graph-backed surfaces (контракт семейства графов)**: [`0067`](../adr/0067-graph-backed-surfaces-contract.md)
- **Semantic map control flow (PFD)**: [`0053`](../adr/0053-semantic-map-control-flow-pfd.md) *(Accepted · Implemented)*

---

## 8. Команды и keyboard-first

- **Command surface & discoverability**: [`0013`](../adr/0013-command-surface-and-discoverability.md)
- **Command palette direct overlay**: [`0070`](../adr/0070-command-palette-direct-overlay-surface.md)
- **Chord stack (Ctrl+K) / FMS-style**: [`0060`](../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md)

---

## 9. Где держать “описание текущей архитектуры”

ADR фиксируют решения и мотивацию. Для “как сейчас” держим:
- `docs/architecture/current-architecture-v1.md` (эталонная точка входа)
- `docs/ux/cascade-ide-ui-layout-v1.md` (эталон layout/имена регионов)
- `docs/MCP-PROTOCOL.md` (эталон контракта MCP)

Версия: **v1**.

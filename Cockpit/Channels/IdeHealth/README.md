# Канал IDE Health — слой **CCU** (cockpit compute unit)

Папка на диске: `Cockpit/Channels/IdeHealth/`. Контракт канала и сегментов остаётся в **`CascadeIDE.Cockpit.Channels.WorkspaceHealth`** (историческое имя), а CCU вынесены в **`CascadeIDE.Cockpit.ComputingUnits.IdeHealth`** — **не** смешивать это с продуктовым «Workspace Health» (см. [ADR 0089](../../../docs/adr/0089-ide-omnibus-naming-and-ide-health-channel-rename.md)).

## Связь с [ADR 0097](../../../docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)

**CCU** — вычислительный блок между **транспортом** (например [ADR 0094](../../../docs/adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md)) и **DTO/снимком канала** кабины. Для **IDE Health** в коде уже выделена эталонная цепочка (чертёж [`workspace-health-implementation-map-v1.md`](../../../docs/design/workspace-health-implementation-map-v1.md)):

| Роль (0097) | Тип(ы) | Примечание |
|-------------|--------|------------|
| Нормализованные **входы** (граница снимка) | `IdeHealthInputSnapshot` (`ICockpitComputeUnitPayload`), `IdeHealthSegmentInput` | Один снимок на четыре источника: build, tests, debug, git + `Stratum` (`workspace`/`solution`/`ide`, ADR 0095), `Scope` (`solution`/`project`) и `ProjectPath?` для project-level faceting. |
| **Свёртка в строки** (чистая логика, тесты без UI) | `IdeHealthFormattingUnit` (`ICockpitComputeUnit`, `Default`) | Не VM, не DAP-объекты в глубине — только скаляры. |
| **Сбор снимка из окружения** | `IdeHealthSnapshotUnit` : `IIdeHealthChannel` | Делегаты и DAP подставляются с `MainWindowViewModel`; канал **не** тянет `UiChrome` внутрь. |
| **Композиция поверхности канала** (порядок сегментов) | `IdeHealthSurfaceCompositor` в `Cockpit/Composition/IdeHealth/` | Build → Tests → Debug → Git; не Avalonia. |

**Не CCU:** шина [0094](../../../docs/adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md), CDS-маршрутизация [0036](../../../docs/adr/0036-cds-channel-compositor-surface-pipeline.md), сами `Views/*Strip*`.

**Дальше по 0095:** при эволюции контрактов — поле **`stratum`** (`workspace` / `solution` / `ide`) и/или **отдельные** юниты (рабочие имена WSCU/SSCU/ISCU в [0095](../../../docs/adr/0095-workspace-solution-ide-health-stratification.md#adr0095-stratum-ccu-examples)) — **без** обязательного переименования `IdeHealth*` в `*ComputeUnit` в одном PR.

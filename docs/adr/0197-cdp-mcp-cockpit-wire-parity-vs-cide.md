# ADR 0197: cdp-mcp cockpit wire parity vs CIDE

**Статус:** Accepted  
**Дата:** 2026-07-27  
**Tags:** #cdp #cide #architecture #wire #dal #databus #ccu #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Channel → CDS → Compositor → Surface |
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | Transport / ingestion |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | DAL boundary |
| [0196](0196-architecture-staging-board-arch-desk.md) | Arch board / as_built |
| [0198](0198-toolchain-ensure-vs-lsp.md) | Toolchain hangs on DAL |
| [0199](0199-dual-agent-process-profile-isolation.md) | Process isolation (отдельно) |
| [0200](0200-cdpcope-architecture-analyzers-desk-wire.md) | CDPCOPE gun (CASCOPE spirit, no Avalonia) |

## Резюме

- North star: **parity по швам** CIDE в agent habitat (cdp-mcp), не UI-порт Avalonia.
- Статусы роли: `real` (контракт+код как в CIDE) · `peel` (IdeCockpit.* с именем роли) · `missing` (слот на board без locus).
- **Не** ждать полной runtime-parity DataBus/DAL перед следующими органами (toolchain).
- Toolchain organ вешается на шов **DAL** (добыча внешнего runtime/bin); LSP остаётся intelligence.

## Контекст

IdeCockpit вырос peel'ами. `op=as_built` уже сканирует профили `cide` и `cdp_desk`. Нужна явная gap-таблица и видимые **missing** роли (особенно DAL) на board — иначе новые soft organs снова садятся «рядом» без дома.

## Решение

### Таблица швов (cdp-mcp vs CIDE)

| Role | CIDE эталон | cdp-mcp статус | Примечание |
|------|-------------|----------------|------------|
| transport | 0094 ingest | **real** `Cockpit/Transport/DeskIngestionBus` | Bounded Channel\<T\>; peel publishes |
| ccu | 0097 ComputingUnits | **real-ish** multi-unit + Build peel | Attention/DeskDetail/WorldScene/Focus/DeskLoci/DeskNext/SniperLocus/GoVerbs/OrganJsonPulse; BuildAsync thin (probes+world-go+tiles); still orchestrator |
| dal | 0102 DAL | **real-ish** `Cockpit/DataAcquisition` | Toolchain PATH probe |
| databus | 0099 IDataBus | **real** + host | DeskDataBusHost publishes DeskSurfaceBuilt |
| channel | IChannel | **real** `DeferredSoftOrganChannel` | Peel delegates Peek |
| cds | ICdsRouter | **real** `AttentionCdsRouter` | Peel NormalizeAttentionRouting |
| ids | IdeDisplay | **real** `FeatureSearchUnit` | Peel SearchFeatures; Cockpit/Ids |
| compositor | ISurfaceCompositor | **real** seats+tiles compositors | Peel projects; compose under Cockpit/ |
| surface | Surface mounts | **real-ish** gate/match/presenter/alias/meta/world+editor + SoftOrgans | Pane loop thin; Dispatch peel; Handle still peel |
| instrument | Instrument deck | **real** `DeskInstrumentMountRegistry` | Seats sync → deck; JSON pulse on surface |
| gun | CASCOPE* | **CDPCOPE*** | ADR 0200 |

### as_built

- Profile `cdp_desk`: peels + seed DAL/DataBus when `Cockpit/DataAcquisition` + `Cockpit/DataBus` present; else **gap** (`transport`, `instrument` remain GAP).
- Profile `cide`: seed существующих locus; DAL — seed `Cockpit/DataAcquisition` when present, иначе gap.
- Рёбра intended seams рисуются и для gap→peel (архитектурная видимость).
- Gun: [0200](0200-cdpcope-architecture-analyzers-desk-wire.md).

### Toolchain placement

- Primary seam: **`dal`** (acquire external toolchain bins).
- Optional `pairs_lsp` → `lsp_ensure` (другая ось, [0190](0190-agent-ide-settings-organ.md) / [0198](0198-toolchain-ensure-vs-lsp.md)).
- Build/run router через recipe — фаза после ensure; не требует полного DataBus.

## Не делать

- Портировать Avalonia UI / полный InMemoryDataBus в одном PR «ради parity».
- Считать peel = real CIDE pack.
- Класть toolchain в CDS/Compositor.

## Последствия

- Arch board показывает gap без эссе в чате.
- Следующий thin slice: [0198](0198-toolchain-ensure-vs-lsp.md) soft organ на DAL-adjacent.
- Dual-agent: [0199](0199-dual-agent-process-profile-isolation.md), не смешивать с wire remap.

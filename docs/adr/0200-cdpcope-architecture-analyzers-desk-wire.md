# ADR 0200: CDPCOPE — architecture analyzers for cdp-mcp desk wire

**Статус:** Accepted  
**Дата:** 2026-07-28  
**Tags:** #cdp #cide #architecture #analyzers #cdpcope #cascope #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0197](0197-cdp-mcp-cockpit-wire-parity-vs-cide.md) | Wire parity north star |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | DAL boundary |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Cabin wire |
| [0099](0099-ide-databus-typed-events-and-projections.md) | DataBus |
| CASCOPE (CascadeIDE.ArchitectureAnalyzers) | CIDE gun (Avalonia cabin) |

## Резюме

- **Полное выравнивание провода** CIDE ↔ cdp-mcp **без порта Avalonia**.
- Пушка: пакет `CdpMcp.ArchitectureAnalyzers`, ID префикс **CDPCOPE*** — тот же дух, что **CASCOPE***, правила под desk/TUI peels.
- Surface в CDP = seats/JSON desk, не `Views`/`Avalonia`.

## Решение

| ID | Уровень | Смысл |
|----|---------|--------|
| CDPCOPE001 | Error | Channel/Cds/Compositor peels (+ Cockpit folders) — нет `using Avalonia*` |
| CDPCOPE016 | Error | Ids / IdeDisplay — нет Avalonia |
| CDPCOPE020 | Warning | Channel/Cds/Compositor/Build (+ ComputingUnits) — нет прямого `File`/`Process`/`HttpClient`; I/O → `Cockpit/DataAcquisition` |

Locus в дереве cdp-mcp:

- `Cockpit/DataAcquisition/` — DAL (toolchain PATH probe)
- `Cockpit/DataBus/` — thin `IDataBus` / `InMemoryDataBus`

## Не делать

- Копировать Avalonia-specific CASCOPE003/017/018 «как есть».
- Считать peel = полный CIDE pack только из‑за анализаторов.

## Последствия

- `dotnet build` на CdpMcp гоняет CDPCOPE*.
- as_built desk промоутит DAL/DataBus когда файлы есть; transport/instrument остаются честными GAP.

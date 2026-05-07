# ADR 0102: Data Acquisition Layer — граница внешних интерфейсов и адаптеров

**Status:** Accepted  
**Date:** 2026-04-26

**Related:** [0006](0006-presentation-layers-and-feature-slices.md), [0008](0008-mcp-contracts-and-testable-infrastructure.md), [0009](0009-strangler-migration-and-exceptions.md), [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md), [0095](0095-workspace-solution-ide-health-stratification.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0099](0099-ide-databus-typed-events-and-projections.md)

---

<a id="adr0102-context"></a>

## 1. Контекст

В кодовой базе есть вычислительные юниты (CCU), orchestration в `MainWindowViewModel` и набор сервисов/адаптеров, которые читают внешние источники (файлы, процессы, конфиги, wire payload).  
Без отдельного каноничного термина и границы этот внешний слой «расползается» по `Services`, `ViewModels`, а иногда попадает в `ComputingUnits`.

Это создаёт дрейф:

- CCU начинают смешиваться с IO;
- сложно автоматически проверять границы анализаторами;
- растёт связность и стоимость рефакторинга.

---

<a id="adr0102-decision"></a>

## 2. Решение

Ввести и закрепить единый термин: **Data Acquisition Layer (DAL)**.

`DAL` — слой внешних интерфейсов для вычислительного контура: входящий сбор/чтение и исходящая передача данных наружу.

<a id="adr0102-dal-in-scope"></a>

### В DAL входит

- `fs` операции (`File`, `Directory`, проверка существования, пути);
- запуск внешних процессов и сбор их вывода;
- parse/serialize внешних форматов (`json`, `toml`, wire payload);
- интеграционные резолверы и адаптеры источников.
- исходящая передача наружу (запись, отправка, внешние вызовы), где это требуется фичей.

<a id="adr0102-dal-out-of-scope"></a>

### В DAL не входит

- вычисление смысловых snapshot/DTO канала;
- UI-рендер и UI-композиция;
- маршрутизация слотов/поверхностей.

---

<a id="adr0102-ccu-ui-boundary"></a>

## 3. Граница с CCU и UI

- **DAL**: добывает и нормализует внешние данные.
- **CCU** (`Cockpit/ComputingUnits/*`): считает смысловые snapshot/DTO из уже подготовленного входа.
- **CDS/UI**: отображают готовые снимки.

Инвариант:

1. CCU не выполняет прямых внешних вызовов (ни inbound, ни outbound).
2. DAL не подменяет собой CCU-композицию канала.
3. `MainWindowViewModel` не становится местом массовой добычи данных из внешнего мира.

---

<a id="adr0102-code-layout"></a>

## 4. Размещение кода (strangler)

Базовое размещение DAL:

- `Features/<Feature>/DataAcquisition/*`

Соседний слой для orchestration/use-case логики:

- `Features/<Feature>/Application/*`

Разделение ответственности:

- `DataAcquisition` — адаптеры внешнего мира (I/O, process, wire, API).
- `Application` — подготовка use-case payload/правил сценария без прямых внешних вызовов.
  Для классов оркестрации рекомендуется явный нейминг `*Orchestrator`.
- `CCU` — вычисление смысловых snapshot/DTO канала.

На переходном этапе допустимы узкие адаптеры в `Services/*`, если:

- есть явный интерфейс;
- слой не тянет UI;
- есть план переноса в feature-инфраструктуру.

---

<a id="adr0102-first-slice-example"></a>

## 5. Пример первого среза

Launch-контур:

- `LaunchProfilesStore` и `LaunchSettingsJsonImport` относятся к DAL;
- `LaunchReadinessUnit`, `LaunchPreResolvePipelineUnit`, `LaunchProfileProjectResolveUnit` остаются в CCU;
- `MainWindowViewModel` выполняет orchestration.

---

<a id="adr0102-guardrails"></a>

## 6. Проверка границ

Для DAL/CCU вводятся архитектурные guardrails (CASCOPE*):

- запрет IO/process/wire parse в `Cockpit/ComputingUnits/*`;
- запрет зависимостей CCU на `ViewModels/Views/Ui*`;
- поэтапный rollout: warning -> baseline cleanup -> error.

---

<a id="adr0102-consequences"></a>

## 7. Последствия

- Граница «добыча данных vs вычисление смысла» становится явной.
- Рефакторинг `Services` в feature-срезы становится детерминированным.
- Анализаторы получают однозначную цель для правил.

---

<a id="adr0102-non-goals"></a>

## 8. Не цели

- Big-bang перенос всех `Services/*` в один коммит.
- Принудительный rename всех существующих namespace за один этап.
- Изменение контракта DataBus этим ADR.

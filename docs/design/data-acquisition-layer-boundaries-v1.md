# Data Acquisition Layer Boundaries (v1)

Статус: draft (рабочий канон до фиксации отдельным ADR).

## Назначение

`Data Acquisition Layer` (DAL) — слой внешних интерфейсов и адаптеров для вычислительных блоков кабины (CCU): входящий сбор/чтение и исходящая передача наружу.

DAL отвечает за:

- файловые операции (`File`, `Directory`, пути, проверка существования);
- запуск внешних процессов и чтение их результата;
- парсинг/сериализацию внешних форматов (`json`, `toml`, wire payload);
- интеграционные резолверы и адаптеры источников.
- исходящая передача наружу (запись, отправка, внешние вызовы) при необходимости фичи.

## Границы с CCU

`Cockpit/ComputingUnits/*`:

- принимает уже подготовленные данные/снапшоты;
- вычисляет смысловые DTO/снимки канала;
- не выполняет внешние вызовы напрямую.

Запрещено в CCU:

- `File.*`, `Directory.*`, `FileInfo`, `DirectoryInfo`;
- `Process` и запуск команд;
- прямой HTTP/клиенты внешних API;
- UI API.

## Размещение кода

- DAL: `Features/<Feature>/DataAcquisition/*`.
- Application: `Features/<Feature>/Application/*`.
- CCU: `Cockpit/ComputingUnits/*`.
- UI orchestration: `ViewModels/*`.

Разделение слоёв:

- `DataAcquisition` — внешние адаптеры (I/O, process, wire, API).
- `Application` — use-case подготовка/правила сценария без прямых внешних вызовов.
  Для orchestration-классов — нейминг `*Orchestrator`.
- `CCU` — вычислительный контур (смысловые DTO/снимки).

## Пример (Launch)

- DAL: `Features/Launch/DataAcquisition/LaunchProfilesStore.cs`, `LaunchSettingsJsonImport.cs`.
- CCU: `Cockpit/ComputingUnits/Launch/LaunchPreResolvePipelineUnit.cs`, `LaunchReadinessUnit.cs`, `LaunchProfileProjectResolveUnit.cs`.

## Эволюция

Далее этот документ должен быть закреплён отдельным ADR про DAL и ссылками из ADR 0097/0095.

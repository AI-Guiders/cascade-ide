# ADR 0099: IDE DataBus — типизированные события и проекции состояния

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-25

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0094](0094-ingestion-bus-afdx-analogy-and-threading-channels.md) | шина доставки и backpressure |
| [0097](0097-cockpit-compute-units-transport-to-channel-dto.md) | CCU: свёртка в DTO канала |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | канал → CDS → композиция поверхности |
| [0095](0095-workspace-solution-ide-health-stratification.md) | уровни Workspace/Solution/IDE |
| [0004](0004-ui-thread-marshaling.md) | UI marshaling |
| [0007](0007-signals-coupling-and-ui-backpressure.md) | сигналы и связность |
---

<a id="adr0099-context"></a>

## Контекст

В коде уже есть рабочие элементы шины и свёртки:

- транспорт и батчинг вывода (`ADR 0094`);
- вычислительные блоки CCU и каналовая композиция (`ADR 0097`, `ADR 0036`).

Но пока нет единого **прикладного** контракта «событие домена IDE → подписчики → проекция состояния».  
Из-за этого часть сигналов продолжает идти через прямые делегаты и ручную склейку в `ViewModel`.

---

<a id="adr0099-decision"></a>

## Решение

Ввести в архитектуру слой **IDE DataBus** как in-process typed event bus.

<a id="adr0099-p1"></a>

1. **Контракт:** `IDataBus` с минимальным API:
   - `Publish<TEvent>(TEvent evt)`
   - `Subscribe<TEvent>(Action<TEvent> handler)` (с disposable-отпиской)

<a id="adr0099-p2"></a>

2. **События типизированные** (не `object`/string):
   - `BuildStateChanged`
   - `TestsStateChanged`
   - `DebugStateChanged`
   - `GitStateChanged`
   - (по мере надобности) `ScopeDecisionChanged` и т.д.

<a id="adr0099-p3"></a>

3. **DataBus не подменяет 0094:**  
   `Channel<T>`/ingestion остаётся транспортом потока; DataBus — слой распространения **нормализованных событий домена**.

<a id="adr0099-p4"></a>

4. **DataBus не подменяет 0097/0036:**  
   CCU и compositor по-прежнему отвечают за свёртку/проекцию. DataBus доставляет входные события к местам, где строятся снимки/DTO.

<a id="adr0099-p5"></a>

5. **Базовая реализация v1:** синхронный in-memory bus в одном процессе IDE, без внешнего брокера/IPC.

<a id="adr0099-p6"></a>

6. **Git в IDE Health:** один продуктовый путь — после обновления git-строк в `UiChromeViewModel` вызывается `AfterGitWorkspaceHealthSummaryApplied` (в конце `RefreshGitSummaryAsync` на UI-потоке), что в `MainWindowViewModel` привязано к `PublishGitToIdeDataBusAndRebuildIdeHealth` (публикация `GitStateChanged` + `RebuildIdeHealth`). Сид начального состояния: `SeedIdeHealthDataBus()` в конструкторе (startup + первый `GitStateChanged`), без `PropertyChanged` на отдельные поля git.

<a id="adr0099-p7"></a>

7. **Снимок канала и UI:** `IdeHealthSnapshotUnit.Build` вызывается только из `MainWindowViewModel.IdeHealth` (`RebuildIdeHealth`); результат кэшируется в `_lastIdeHealthInputSnapshot`, геттеры строк в `MainWindowViewModel.Presentation` читают кэш. Roslyn **CASCOPE019** фиксирует эту границу.

<a id="adr0099-p8"></a>

8. **Жизненный цикл:** `IdeHealthSnapshotUnit` реализует `IDisposable` (отписка от шины); при закрытии главного окна — `ReleaseWorkspaceHealthChannel()`.

<a id="adr0099-p9"></a>

9. **Порядок для IDE Health (внедрено):** прикладной `InMemoryDataBus` главного окна — **синхронная** диспетчеризация (`asynchronousDispatch: false`), чтобы подписчики `IdeHealthSnapshotUnit` отработали до возврата из `Publish`, а `RebuildIdeHealth()` читал согласованный снимок. Сборка из UI: сначала `BuildStateChanged` (старт/финиш), затем `IsBuilding` — чтобы `NotifyPropertyChangedFor`→`RebuildIdeHealth` не обходил обновление `_buildSnapshot`. Публикации с фона MCP — через `UiScheduler.InvokeAsync` в `PublishToIdeDataBusAndRebuild` (тот же UI-поток, что и свёртка).

---

<a id="adr0099-exchange-principles"></a>

## Принципы обмена

1. **Неблокирующий транспорт между слоями:**  
   ни IDS, ни CDS, ни CCU не должны зависеть от синхронного ответа друг друга в runtime-цепочке.  
   Публикация выполняется как «fire-and-forward» в соответствующий канал/шину, обработка — по готовности потребителя.

2. **Строгая типизация сообщений:**  
   никаких `object`/`dynamic` в каналах домена.  
   Используются типизированные события и явные контракты сообщений (record/иерархия типов; discriminated-union-стиль через pattern matching C#).

3. **Backpressure и политика потерь по классу данных:**
   - для критичных сигналов (ошибки, жизненный статус IDE, safety/health) — режим без потерь (unbounded, bounded+wait или отдельный приоритетный контур);
   - для тяжёлых/высокочастотных сигналов (например графовые срезы для Skia) — `BoundedChannel` с политикой вроде `DropOldest`/«latest wins», чтобы не копить устаревшие кадры.

4. **Изоляция доменов:**  
   CCU получает вход из своего typed input-потока (сенсоры/источники) и публикует отдельный typed output-поток (индикация/проекции).  
   Ошибки в контуре отрисовки/потребления не должны валить анализ/вычисление.

---

<a id="adr0099-boundaries"></a>

## Границы

- **Можно:** использовать DataBus для развязки источников и проекций (UI/MCP/cockpit snapshot).
- **Нельзя:** смешивать в одном типе транспортные механики (`Channel<T>`, backpressure) и бизнес-события.
- **Нельзя:** переносить рендер/UI-логику в обработчики событий шины.

---

<a id="adr0099-strangler-plan"></a>

## Strangler-план

1. Пилотный vertical slice: `BuildStateChanged` от источника сборки до IdeHealth snapshot.
2. Затем `TestsStateChanged` и `DebugStateChanged`.
3. После стабилизации — расширить на Git/прочие доменные сигналы.
4. Закрепить границы в `CascadeIDE.ArchitectureAnalyzers`: **CASCOPE019** — запрет прямого `_workspaceHealth.Build` вне `MainWindowViewModel.IdeHealth` (и прежние правила pipeline для устаревших API, см. README анализаторов).

---

<a id="adr0099-consequences"></a>

## Последствия

- Меньше связности в `MainWindowViewModel`.
- Проще тестировать куски по событиям (publish → проверка проекции).
- Проще добавлять новые каналы/снимки без каскадной правки существующих сервисов.
- Появляется риск «event spaghetti» при слабой дисциплине именования/границ — гасится typed-событиями и ADR-гайдлайнами.

---

<a id="adr0099-non-goals"></a>

## Не цели

- Внешний message broker, распределённая шина или межпроцессный transport.
- Унификация всех потоков в один универсальный envelope на первом шаге.
- Массовая миграция всех существующих сигналов в один коммит.

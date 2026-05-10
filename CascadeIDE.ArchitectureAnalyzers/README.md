# CascadeIDE.ArchitectureAnalyzers

Roslyn-анализаторы для границ [ADR 0036](../docs/adr/0036-cds-channel-compositor-surface-pipeline.md) (CDS / кабина) и [ADR 0079](../docs/adr/0079-ide-display-system-ids-overlay-pipeline.md) (IDS / `IdeDisplay/`). Норматив по **вычислительным юнитам (CCU)** между транспортом и каналом — [ADR 0097](../docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md); для IDE Health закреплено **CASCOPE019** ([ADR 0099](../docs/adr/0099-ide-databus-typed-events-and-projections.md)); прочие диагностики под CCU — по мере выделения устойчивых анти-паттернов (см. §3–4 в 0097). Подключён к `CascadeIDE.csproj` как `Analyzer` (`ReferenceOutputAssembly=false`), **кроме** загрузки через **RoslynMcp**: при `RoslynMcpWorkspace=true` ссылка на этот проект отключается, чтобы процесс MCP не блокировал `bin\…\CascadeIDE.ArchitectureAnalyzers.dll` во время `dotnet build` (общий механизм — раздел «RoslynMcpWorkspace» в [roslyn-mcp README](../../roslyn-mcp/README.md)).

**Поведение:** обычный `dotnet build` / CI без `RoslynMcpWorkspace` по-прежнему получают CASCOPE*; в MCP по главному приложению эти диагностики из локального проекта анализатора не подмешиваются — для проверки правил ориентируйся на **`dotnet build`** или на этот проект отдельно. Если в тулы передаётся полный **`CascadeIDE.sln`**, проект анализатора всё ещё может подгружаться; для MCP достаточно контекста приложения — предпочтительнее путь к **`CascadeIDE.csproj`**.

| ID | Уровень | Смысл |
|----|---------|--------|
| **CASCOPE001** | Error | В `Cockpit/Channels`, `Cockpit/Cds`, `Cockpit/Composition` запрещены ссылки на типы Avalonia UI (включая `using Avalonia…` и типы в сигнатурах). `Cockpit/Surface` и `Views` не затрагиваются. |
| **CASCOPE002** | Error | В тех же трёх папках запрещён `using CascadeIDE.Features.UiChrome` (семантика зон для MCP — на границе Surface). |
| **CASCOPE003** | Error | Прямые присваивания `IsPfdRegionExpanded` / `IsMfdRegionExpanded` (и полям `_is*`) у `MainWindowViewModel` только в белом списке файлов (`PresentationLayoutAuthority`, relay-команды, ctor / `ShellConstruction`, `ShellState`, `UiGitWorkspace`); иначе — дрейф от ADR 0046 (используй `Apply*` / relay). |
| **CASCOPE011** | Error | В `Features/UiChrome/` запрещён `using CascadeIDE.Cockpit.PrimitivesKit` (ADR 0066: хром IDE отдельно от отрисовки deck/кабины). |
| **CASCOPE012** | Error | В `Cockpit/PrimitivesKit/` запрещён `using CascadeIDE.Features.UiChrome` (ADR 0066: примитивы кабины не тянут зоны/хром). |
| **CASCOPE013** | Error | В `IdeDisplay/` запрещён `using CascadeIDE.Cockpit…` и типы из `CascadeIDE.Cockpit` в сигнатурах членов (ADR 0079: IDS ортогонален CDS/кабине). |
| **CASCOPE014** | Error | В `IdeDisplay/` запрещены Avalonia UI (как у CASCOPE001: `using Avalonia…` и типы в членах). |
| **CASCOPE015** | Error | В `IdeDisplay/` запрещён `using CascadeIDE.Features.UiChrome` и типы из этого пространства в членах (семантика оверлея отдельно от хрома shell). |
| **CASCOPE016** | Error | В `Cockpit/` запрещён `using CascadeIDE.IdeDisplay…` (кабина не зависит от IDS). |
| **CASCOPE017** | Error | Лимит строк: `Views/MfdShellView.axaml` (каркас EICAS + `MfdContourStackHost` + `MfdShellPageStack`) — см. `MaxLineCountMfdShellView` в анализаторе; `Views/MfdShellPageStack.axaml` (набор страниц Mfd) — `MaxLineCountMfdShellPageStack`. Детальная вёрстка — в *MfdPageView. |
| **CASCOPE018** | Error | В обоих файлах (см. **CASCOPE017**) запрещены тяжёлые inline-паттерны (`ListBox`, `TextBox`, `ItemsControl`, `GridSplitter`, `DataTemplate`, многостолбцовая `ColumnDefinitions=…`) — вынос в *MfdPageView. |
| **CASCOPE019** | Error | Во всех `MainWindowViewModel*.cs`, кроме `MainWindowViewModel.IdeHealth.cs`, запрещён вызов `_workspaceHealth.Build(...)` (единая точка свёртки в `RebuildIdeHealth`, строки в UI — из кэша; см. [ADR 0099](../docs/adr/0099-ide-databus-typed-events-and-projections.md)). |
| **CASCOPE020** | Warning | В `Cockpit/ComputingUnits/*` запрещён прямой доступ к внешним источникам (`File`, `Directory`, `Process`, `HttpClient`, `JsonDocument/Serializer` и др.): добыча данных — в DAL ([ADR 0102](../docs/adr/0102-data-acquisition-layer-boundary-and-contract.md)). |
| **CASCOPE021** | Warning | В `Cockpit/ComputingUnits/*` запрещены UI-зависимости через `using` (`CascadeIDE.ViewModels`, `CascadeIDE.Views`, `CascadeIDE.Features.Ui*`, `Avalonia*`). |

### Черновые направления (правила пока не вводим)

- **HCI: JSON MCP против события DataBus.** Ответы `SerializeStatus(IndexStatus)` в `Features/HybridIndex/McpParity/` содержат полный снимок ядра; типизированное `HybridIndexStateChanged` после CCU узже (см. ADR 0106 и XML на `HybridIndexStateChangedUnit` / `CodebaseIndexIdeJsonResponses.SerializeStatus`). Возможное жёсткое CASCOPE-правило на подмножество полей — только после того, как стабилизируем контракт шины для UI и MCP parity.

`MfdShellView.axaml` и `MfdShellPageStack.axaml` — в `<AdditionalFiles>` в `CascadeIDE.csproj` (**CASCOPE017**/**018**). Без записи проверка не сработает.

Расширение правил: новые диагностики в этом проекте, версии `Microsoft.CodeAnalysis.CSharp` держать совместимыми с SDK.

Rollout для новых правил границ DAL/CCU: сначала `Warning` (baseline и очистка), затем перевод в `Error` после стабилизации.

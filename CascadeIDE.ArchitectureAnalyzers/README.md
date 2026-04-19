# CascadeIDE.ArchitectureAnalyzers

Roslyn-анализаторы для границ [ADR 0036](../docs/adr/0036-cds-channel-compositor-surface-pipeline.md). Подключён к `CascadeIDE.csproj` как `Analyzer` (`ReferenceOutputAssembly=false`), **кроме** загрузки через **RoslynMcp**: при `RoslynMcpWorkspace=true` ссылка на этот проект отключается, чтобы процесс MCP не блокировал `bin\…\CascadeIDE.ArchitectureAnalyzers.dll` во время `dotnet build` (общий механизм — раздел «RoslynMcpWorkspace» в [roslyn-mcp README](../../roslyn-mcp/README.md)).

**Поведение:** обычный `dotnet build` / CI без `RoslynMcpWorkspace` по-прежнему получают CASCOPE*; в MCP по главному приложению эти диагностики из локального проекта анализатора не подмешиваются — для проверки правил ориентируйся на **`dotnet build`** или на этот проект отдельно. Если в тулы передаётся полный **`CascadeIDE.sln`**, проект анализатора всё ещё может подгружаться; для MCP достаточно контекста приложения — предпочтительнее путь к **`CascadeIDE.csproj`**.

| ID | Уровень | Смысл |
|----|---------|--------|
| **CASCOPE001** | Error | В `Cockpit/Channels`, `Cockpit/Cds`, `Cockpit/Composition` запрещены ссылки на типы Avalonia UI (включая `using Avalonia…` и типы в сигнатурах). `Cockpit/Surface` и `Views` не затрагиваются. |
| **CASCOPE002** | Error | В тех же трёх папках запрещён `using CascadeIDE.Features.UiChrome` (семантика зон для MCP — на границе Surface). |
| **CASCOPE003** | Error | Прямые присваивания `IsPfdRegionExpanded` / `IsMfdRegionExpanded` (и полям `_is*`) у `MainWindowViewModel` только в белом списке файлов (`PresentationLayoutAuthority`, relay-команды, ctor, `ShellState`, `UiGitWorkspace`); иначе — дрейф от ADR 0046 (используй `Apply*` / relay). |
| **CASCOPE011** | Error | В `Features/UiChrome/` запрещён `using CascadeIDE.Cockpit.PrimitivesKit` (ADR 0066: хром IDE отдельно от отрисовки deck/кабины). |
| **CASCOPE012** | Error | В `Cockpit/PrimitivesKit/` запрещён `using CascadeIDE.Features.UiChrome` (ADR 0066: примитивы кабины не тянут зоны/хром). |

Расширение правил: новые диагностики в этом проекте, версии `Microsoft.CodeAnalysis.CSharp` держать совместимыми с SDK.

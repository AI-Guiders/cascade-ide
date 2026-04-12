# CascadeIDE.ArchitectureAnalyzers

Roslyn-анализаторы для границ [ADR 0036](../docs/adr/0036-cds-channel-compositor-surface-pipeline.md). Подключён к `CascadeIDE.csproj` как `Analyzer` (`ReferenceOutputAssembly=false`).

| ID | Уровень | Смысл |
|----|---------|--------|
| **CASCOPE001** | Error | В `Cockpit/Channels`, `Cockpit/Cds`, `Cockpit/Composition` запрещены ссылки на типы Avalonia UI (включая `using Avalonia…` и типы в сигнатурах). `Cockpit/Surface` и `Views` не затрагиваются. |
| **CASCOPE002** | Error | В тех же трёх папках запрещён `using CascadeIDE.Features.UiChrome` (семантика зон для MCP — на границе Surface). |

Расширение правил: новые диагностики в этом проекте, версии `Microsoft.CodeAnalysis.CSharp` держать совместимыми с SDK.

# ADR 0056: Semantic Map adoption of Skia composition pipeline

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-17

**Связь:** [0053](0053-semantic-map-control-flow-pfd.md) (controlFlow intent), [0055](0055-skia-instrument-composition-pipeline.md) (общий pipeline для Skia-инструментов).

---

## Контекст

После внедрения `controlFlow` для Semantic Map выяснилось:

1. Layout и политика плотности были частично размазаны между VM/контролом/движком.
2. На компактном PFD viewport узлы слипались, чтение маршрута ухудшалось.
3. Поведение MCP без явных `line/column` требовало предсказуемого fallback к текущему курсору.

---

## Решение

<a id="adr0056-p1"></a>
### 1) Перевести Semantic Map на отдельный композитор инструмента

Введён `CodeNavigationMapCompositor` как первый доменный адаптер общего подхода из ADR 0055:

- выбор layout по `semantic_map.level`;
- вычисление рекомендуемой высоты viewport для читаемости;
- возврат scene + параметров отображения.

<a id="adr0056-p2"></a>
### 2) Явно разделить file/controlFlow композицию

- `file` использует компактную схему (исторический star layout);
- `controlFlow` использует вертикальный flight-plan layout с большим шагом между уровнями.

<a id="adr0056-p3"></a>
### 3) Сохранить семантику метода "под курсором"

Для `controlFlow`:

- без валидной позиции не делать fallback на "первый метод файла";
- если `line/column` не переданы через MCP, подставлять текущую позицию каретки;
- при отсутствии позиции и каретки возвращать пустой подграф, а не ложный контент.

---

## Последствия

### Плюсы

- Semantic Map стала первым валидированным потребителем общего pipeline.
- Граница "instrument internals vs host surface" стала прозрачной в коде.
- Улучшена читаемость controlFlow в узком viewport.

### Минусы

- Появился дополнительный слой композиции и новые тесты.
- Declutter-policy пока минимальный и требует дальнейшей эволюции.

---

## Реализация (срез)

- `Services/Navigation/CodeNavigationMapCompositor.cs`
- `Services/Navigation/ICodeNavigationMapCompositor.cs`
- `ViewModels/MainWindowViewModel.WorkspaceNavigationMap.cs`
- `Views/WorkspaceNavigationMapView.axaml`
- `Services/Navigation/CodeNavigationMapControlFlowGraphLayoutEngine.cs`
- `Services/CodeNavigation/CodeNavigationControlFlowSubgraphBuilder.cs`

---

## Тесты

- `CodeNavigationMapCompositorTests`
- `CodeNavigationMapControlFlowGraphLayoutEngineTests`
- `CodeNavigationControlFlowSubgraphBuilderTests`
- `CodeNavigationControlFlowMcpCursorFallbackTests`


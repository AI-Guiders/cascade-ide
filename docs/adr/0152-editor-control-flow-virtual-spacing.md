# ADR 0152: Virtual Spacing для глифов control-flow в редакторе

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-27

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0053](0053-semantic-map-control-flow-pfd.md) | PFD / карта намерений, глифы CF |
| [0151](0151-control-flow-subgraph-intent-vs-detailed-grain.md) | Subgraph под курсором |
| [0085](0085-editor-inline-hud.md) | Inline HUD редактора |

## Проблема

Глифы control-flow в gutter редактора рисовались `IBackgroundRenderer` по `rect.Left` строки кода — **поверх** первых символов (`try`, `process`, цифры легенды). Это ломает читаемость и не совпадает с ожиданием «полосы намерения» слева от текста.

## Решение

**Virtual Spacing:** при активной карте CF для текущего `.cs` (тот же якорь, что subgraph) в начале **каждой** визуальной строки вставляется элемент с `DocumentLength = 0` и фиксированной визуальной шириной (`ControlFlowVirtualSpacingElementGenerator`, по аналогии с inlay). Текст и каретка сдвигаются вправо; глифы рисуются в центре зарезервированной полосы (`EditorControlFlowVirtualSpacing.LaneWidthPixels`).

Условие включения совпадает с отображением gutter-глифов: `controlFlow` + непустая сцена subgraph + путь файла = якорь CF.

Константы полосы: `Services/EditorControlFlowVirtualSpacing.cs`.

## Последствия

- При смене уровня карты / якоря / сцены — `TextView.Redraw()` (уже было для CF).
- Ширина полосы фиксирована (не из TOML в v1); при необходимости — настройка позже.
- Не затрагивает wire subgraph / MCP — только редактор.

## Отклонённые альтернативы

- **Только сдвинуть отрисовку в margin 50px:** номера строк и CF конкурируют; текст не сдвигается, каретка расходится с глифом на отступах.
- **Пробелы в документе:** портят копирование и offset-based API.

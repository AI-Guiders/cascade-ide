# ADR 0151: Зерно subgraph control-flow — `intent` против `detailed`

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-27

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0053](0053-semantic-map-control-flow-pfd.md) | Продуктовая цель карты: **намерение** метода, без «микро-CFG» на PFD |
| [0039](0039-workspace-navigation-affordances.md) | MCP `get_code_navigation_context`, режим subgraph |
| [0056](0056-semantic-map-pipeline-adoption.md) | Общий Skia-пайплайн и представление узлов |

## Проблема

Один и тот же JSON-контракт subgraph для `controlFlow` поддерживал **пошаговую** декомпозицию (условные хвосты, отдельные шаги внутри `for` и т.д.). Это соответствует «настоящему» локальному CFG, но противоречит продуктовому фильтру из [0053](0053-semantic-map-control-flow-pfd.md): на карте должно быть видно **смысл** метода (waypoints), а не копию мелкой структуры исходника.

При этом режим пошаговой разбивки полезен для отладки и для сценариев «покажи всё».

## Решение

Ввести параметр конфигурации **`[code_navigation_map].control_flow_grain`**:

| Значение | Смысл |
|----------|--------|
| **`intent`** (по умолчанию) | Subgraph через `CodeNavigationMethodIntentSubgraphBuilder`: операторное зерно; циклы `for` / `while` / `foreach` / `do` — один узел `loop_step` с заголовком; меньше шума (см. 0053). |
| **`detailed`** | Прежнее поведение: `CodeNavigationControlFlowSubgraphBuilder` (пошаговый CFG по выражениям). |

Нормализация: неизвестные и пустые строки → **`intent`**. Ключ в TOML — snake_case, свойство в модели — `ControlFlowGrain` / `NormalizedControlFlowGrain`.

### Где применяется

- Обновление карты в кокпите: `WorkspaceNavigationMapContextJsonBuilder` выбирает билдер по нормализованному зерну.
- MCP `get_code_navigation_context` при `level: controlFlow` использует **те же** настройки (`McpSettings.CodeNavigationMap`), без отдельного аргумента wire (при необходимости аргумент можно добавить позже как override).

### Область subgraph (курсор)

- Subgraph строится **только** для области под кареткой: тело **метода** (включая expression-bodied), **local function** в top-level, или все **top-level statements** файла (C# 9+), если курсор в `GlobalStatementSyntax`.
- Refresh CF читает **живую** каретку и текст из активного `TextEditor` (не только throttled `_editorCaretOffset`; guard stabilized-input — `PathsReferToSameFile`).
- Нет fallback на «первый метод» файла, `Program.cs` или якорный узел при загрузке решения без курсора: wire `error: no_control_flow_scope`, UI не рисует сцену subgraph.
- Refresh CF: позиция только из **текущего** открытого `.cs`, когда путь навигации совпадает с редактором (`ResolveControlFlowCursorForRefresh`).

Капы subgraph control-flow (UI и MCP по умолчанию): **48** узлов / **96** рёбер (`CodeNavigationContextBuilder.DefaultControlFlowSubgraphMax*`); при обрезке — `truncated_nodes` / `truncated_edges` в JSON. Старый лимит **12** узлов остаётся для режима `related` / file-subgraph.

Контракт JSON ответа (режим subgraph, поля узлов/рёбер) **не меняется** — меняется только состав узлов и рёбер.

## Последствия

- Пользователи с кастомным `settings.toml` без ключа получают **`intent`** (как дефолт в коде и в `defaults-settings.toml`).
- Документация MCP: см. [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) («семантическая навигация»).

## Отклонённые альтернативы

- **Только один режим (только intent):** упрощает продукт, но лишает возможности воспроизвести старый граф без форка.
- **Отдельный MCP preset вместо TOML:** дублирование источников правды; нормы [0039](0039-workspace-navigation-affordances.md) уже тянут пресеты из `[code_navigation]`, карта же — секция `[code_navigation_map]`.

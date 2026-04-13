# Cookbook: семантическая навигация через MCP (`get_workspace_navigation_context`)

Контекст: [ADR 0039](../adr/0039-workspace-navigation-affordances.md).

## Зачем

Команда `ide_execute_command` с `command_id` = `get_workspace_navigation_context` возвращает JSON со связанными файлами (`mode`: `related`) или компактным подграфом (`mode`: `subgraph`). Агенту нужны **стабильные имена видов связей** и **эхо фильтра**, чтобы не гадать, что реально применилось после пресета и `include_kinds` / `exclude_kinds`.

## Виды связей (канон)

`partial_peer`, `project_peer`, `xaml_codebehind_pair`, `test_counterpart`, `same_namespace`, `same_directory`.

## Пресеты в `settings.toml`

Секция **`[workspace_navigation_context]`**, поле **`presets`** — JSON-объект: ключ — имя пресета, значение — `include_kinds` и/или `exclude_kinds` (массивы строк).

Путь файла: `%LocalAppData%\CascadeIDE\settings.toml` (см. ADR 0028). В коде есть встроенные defaults (`WorkspaceNavigationContextSettings.DefaultPresetsJson`): `peers_only`, `no_namespace_noise`, `tests_and_peers`, `structure_only`.

Пример:

```toml
[workspace_navigation_context]
presets = """
{
  "my_focus": { "include_kinds": ["partial_peer", "project_peer"], "exclude_kinds": ["same_namespace"] }
}
"""
```

## Аргументы MCP

| Аргумент | Назначение |
|----------|------------|
| `mode` | `related` или `subgraph` |
| `file_path` | Якорный файл; если не задан — текущий открытый в редакторе |
| `preset` | Имя пресета из `presets` (merge с явными списками см. ниже) |
| `include_kinds` | Явный белый список; если передан непустой — **перекрывает** include из пресета |
| `exclude_kinds` | Объединяется с exclude пресета (дедуп по канону), если оба заданы |

Неизвестное имя пресета или битый JSON пресетов → JSON с `error`: `bad_preset`.

## Эхо фильтра в ответе

В режимах `related` и `subgraph` в JSON есть объект **`kind_filter`**:

- `preset` — какой пресет запрашивали (или `null`)
- `include_kinds_effective` / `exclude_kinds_effective` — **канонические** списки после merge и нормализации (пустой exclude — `[]`)

Так агент видит эффективный фильтр без повторного парсинга настроек.

## Режим `subgraph`

- У **узла** поле **`kind`** — семантический вид связи с якорем (не общее слово `related`).
- У **ребра** поле **`related_kind`** — вид связи для этой пары.

Используй это, чтобы отличать, например, `project_peer` от `same_namespace`.

## Минимальные примеры вызова

Только пресет:

```json
{ "command_id": "get_workspace_navigation_context", "args": { "mode": "related", "preset": "no_namespace_noise" } }
```

Якорь + явный exclude поверх дефолтного пресета (exclude в запросе объединяется с пресетом, если пресет задаёт exclude):

```json
{
  "mode": "related",
  "file_path": "D:/repo/src/Foo.cs",
  "preset": "tests_and_peers",
  "exclude_kinds": ["same_directory"]
}
```

Подграф с капами:

```json
{ "mode": "subgraph", "max_nodes": 16, "max_edges": 32 }
```

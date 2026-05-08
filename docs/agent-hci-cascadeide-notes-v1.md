# HCI в CascadeIDE: операционные заметки для агента и разработчика

**Связь:** [ADR 0106](adr/0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md), [ADR 0105](adr/0105-hybrid-codebase-index-for-csharp-web.md).

## Где задаётся конфигурация

- **Диск:** пользовательский `settings.toml` (см. `SettingsService.GetSettingsPath()`), секция **`[hybrid_index]`**. Образец полей — `docs/samples/settings.toml`.
- **UI:** окно «Настройки» главного окна CascadeIDE — блок **Hybrid Codebase Index (HCI)**; те же значения пишутся в модель и при изменении переезжают в in-proc оркестратор без перезапуска IDE.

## Смысл ключей (кратко)

| TOML / модель | Назначение |
|---------------|------------|
| `enabled` | Мастер-переключатель: если выкл — фонового watcher нет; кнопка reindex на MFD всё равно выполняет полную переиндексацию in-proc (без постоянного watch). |
| `index_dir` | Относительный каталог под **корнем workspace** для SQLite и артефактов; должен совпадать с тем, что ожидает внешний MCP `hybrid-codebase-index` при совместном использовании. |
| `debounce_ms` | Задержка после событий `FileSystemWatcher` перед очередным reindex (ограничение 0…60000 в UI). |
| `watch_files` | Поднять watcher; выкл — только статус/hand poke. |
| `auto_reindex_on_solution_open` | При смене/открытии решения выполнить `Poke`, чтобы подтянуть свежесть без ожидания событий ФС. |
| `scope_mode` | `workspace` или `workspace+solution`: один ключ базы на workspace vs отдельные под-папки на решение под `index_dir`. |
| `pause_when_mcp_stdio_host` | При режиме чата **`mcp_only`** не держать фонового watcher (снижение конкуренции с stdio MCP). |

## Как проверять состояние

- **В IDE:** вторичный контур MFD → страница INDEX/HCI (`show_hybrid_index_page`), лампа OK/Caution, счётчик документов, `FRESHNS`, пути scope/БД в подсказках.
- **Снаружи:** MCP `hybrid-codebase-index` к тому же workspace и тем же параметрам области — тот же смысл `databasePath`.

## Те же операции через IDE MCP (паритет с внешним HCI MCP)

Из чата/host с подключённым CascadeIDE MCP агент вызывает **те же идентификаторы**, что у тулов `hybrid-codebase-index`: через `ide_execute_command` или прокси-тулы `ide_codebase_index_*`:

- `codebase_index_status` — JSON статуса;
- `codebase_index_search` — гибридный поиск (аргументы как у внешнего MCP);
- `codebase_index_explain` — explain по `hit_id`;
- `codebase_index_reindex` — инкремент по умолчанию, `full_rebuild: true` — полная перестройка; после успешного прогона публикуется `HybridIndexStateChanged` для MFD.

Если не переданы `workspace_path` / `solution_path`, область берётся из текущего открытого решения с тем же `ResolveHybridIndexScope`, что оркестратор HCI (`scope_mode` в настройках).

## Анти-паттерны

- Удалять папку `publish` MCP или junction на неё без остановки процесса, который держит `exe`/`dll`.
- Абсолютный `index_dir` в UI/TOML: нормализация принудительно возвращает относительный путь или дефолт `.hybrid-codebase-index`.

## Кодовые якоря

- Оркестратор: `Features/HybridIndex/Application/HybridIndexOrchestrator.cs`
- Связка с настройками и решением: `MainWindowViewModel.cs` (`ApplyHybridCodebaseIndexOrchestrationForCurrentSolution`), `MainWindowViewModel.SettingsReactive.cs`, `MainWindowViewModel.HybridIndexSettings.cs`
- Проекция UI MFD: `MainWindowViewModel.HybridIndex.cs`, `Views/HybridIndexMfdPageView.axaml`

# ADR 0135: Intercom attach — кэш Roslyn и symbol sidecar (HCI colocation)

**Статус:** Accepted · In progress  
**Дата:** 2026-05-20  

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | HCI — FTS/vec по тексту; **не** symbol truth для C# |
| [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md) | In-proc HCI, watcher, reindex, `index_dir` |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, member re-resolve @ recipient |
| [0134](0134-intercom-message-prepare-pipeline-v1.md) | Prepare pipeline; MCP fast path без Roslyn @ send |

## Резюме

1. **L1 — in-memory file cache** на scope `(workspace, solution)`: parse/semantic model по `.cs` с инвалидацией по `LastWriteTimeUtc`.
2. **L2 — symbol line sidecar** (SQLite рядом с HCI): `(relativePath, lookupKey) → lineStart/lineEnd`, заполняется после HCI reindex и при успешном Roslyn resolve; lookup **до** полного parse при совпадении `file_mtime`.
3. **Roslyn остаётся истиной** при промахе, устаревшем mtime или `syntaxScope` — как [0128](0128-intercom-attachment-anchors-and-code-references.md) §9.1.
4. **Не использовать** HCI FTS как единственный источник строк member (хрупко для перегрузок).

## Контекст

Attach/reveal по `[F:… M:…]` вызывает `ReadAllText` + `ParseText` + мини-compilation на каждый клик/отправку. MCP fast path ([0134](0134-intercom-message-prepare-pipeline-v1.md)) откладывает member @ send, но **клик** и composer всё ещё платят полный Roslyn на холодном файле.

HCI уже индексирует `.cs` как **текст** (чанки + `LineStart`/`LineEnd` hit). Это не карта «`Measure` → объявление метода». Пользовательское предложение — «индекс, методы редко меняются» — реализуется **отдельной symbol-таблицей**, синхронизированной с freshness HCI (reindex / mtime), а не подменой FTS.

## Решение

### L1: `IntercomAttachmentRoslynWorkspaceCache`

| Аспект | Значение |
|--------|----------|
| Ключ scope | `IntercomAttachResolveScopeKey.From(workspaceRoot, solutionPath)` |
| Ключ файла | absolute path + `LastWriteTimeUtc.Ticks` |
| Значение | `FileEntry` (text, tree, model) — тот же тип, что в per-send session |
| Инвалидация | несовпадение mtime; `InvalidateFile`; смена scope — новый словарь |

`AttachmentAnchorRoslynResolver.TryGetOrCreateEntry` сначала session (per-send), затем workspace cache, затем disk read.

### L2: `IntercomSymbolLineIndex` (sidecar SQLite)

| Аспект | Значение |
|--------|----------|
| Путь | `{workspaceRoot}/{index_dir}/intercom-symbol-lines.sqlite` (тот же `index_dir`, что [0106](0106-hybrid-codebase-index-cascadeide-integration-and-semantic-map.md)) |
| Таблица | `symbol_line(scope_key, relative_path, lookup_kind, lookup_key, line_start, line_end, file_mtime_ticks)` |
| `lookup_kind` | `docid` (DocumentationCommentId), `simple` (только если имя уникально в файле) |
| Заполнение | `IntercomSymbolLineIndexBuilder` после `HybridIndexOrchestrator` reindex; upsert после успешного Roslyn resolve |
| Lookup | перед member walk; при `syntaxScope` — только L1 (кэш parse) |

### Порядок resolve (member, не scope)

```
symbol sidecar (mtime ok)?
  → yes → lines
workspace file cache → parse
Roslyn walk (существующая логика)
  → success → upsert sidecar
```

### MCP / composer

| Путь | Поведение |
|------|-----------|
| MCP @ send | без изменений ([0134](0134-intercom-message-prepare-pipeline-v1.md) fast path) |
| MCP / лента @ click | L2 → L1 → Roslyn |
| Composer @ send | L2 → L1 → Roslyn (быстрее после первого reindex) |

### Связь с HCI

**v0.1.2+ `HybridCodebaseIndex.Core`:** extension point `ICodebaseIndexReindexObserver` — при reindex HCI вызывает `OnFileIndexed` с текстом файла; Cascade IDE: `IntercomSymbolLineHciReindexObserver` пишет symbol sidecar **без второго прохода по диску**.

**Fallback:** `IntercomSymbolLineIndexCoordinator.ScheduleRebuildAfterHybridIndex` — если reindex был без observer (внешний MCP); in-proc IDE observer включён, отдельный rebuild не вызывается.

## Не-цели

- Замена Roslyn MCP / полного MSBuild workspace compilation.
- Symbol cache для `syntaxScope` (for/if index) — только parse cache.
- FTS-only resolve member без Roslyn verify.

## Последствия

- Диск: один SQLite на workspace index dir; рост ~O(символов в .cs).
- Первый reindex после открытия solution — фоновая сборка sidecar; повторные attach/reveal — O(1) lookup.
- Тесты: scope key, sidecar hit/miss по mtime, workspace cache reuse.

## Критерии приёмки

1. Второй подряд reveal того же `[M:Foo]` в том же файле без save — заметно быстрее первого (L1/L2).
2. После save файла — re-resolve не возвращает устаревшие строки (mtime).
3. После HCI reindex sidecar содержит строки для тестового метода в известном `.cs`.
4. `syntaxScope` по-прежнему работает через Roslyn walk.

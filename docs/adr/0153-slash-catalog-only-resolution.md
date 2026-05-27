# ADR 0153: Slash — только каталог (удаление parser shape и legacy loader)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-28

## Резюме

Идентичность слэш-команды (**какой путь** `/map type file`, `/intercom topic create …`) задаётся **только** `IntentMelody/intent-catalog.toml` и build-time trie `SlashRouteCatalogPathsGenerated`. Параллельный разбор `head` / `action` / `sub_action` (`ChatSlashParsePipeline`, `IntercomSlashPathBuilder`, `TryResolve(parse)`) **удалён**. Autocomplete, preview, Enter и runner используют один `SlashLineResolver` (ADR 0150).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash в composer, local execution, `command_id` |
| [0136](0136-intercom-feed-gutter-and-slash-namespace.md) | Канон `/intercom …`; отказ от top-level `/topic` |
| [0150](0150-slash-line-canonical-resolution.md) | `SlashLineResolver`, `arg_tail`, единый резолв строки — **дополняется** этим ADR (источник пути) |
| [0124](0124-slash-parametric-editor-line-commands.md) | Параметрические хвосты (`wire_class`, сегменты) — **без** отдельной стадии парсера пути |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | Динамический completion после канонического пути |

## Проблема

После [0150](0150-slash-line-canonical-resolution.md) в коде оставались **два контура**:

| Контур | Как определял путь |
|--------|-------------------|
| Каталог + `SlashLineResolver` | Longest-prefix по `intent-catalog` |
| Legacy parser + `TryResolve(parse)` | `Head` / `Action` / `SubAction` + `IntercomSlashPathBuilder` |

Они расходились. Пример регрессии: ввод `/map type file` резолвился как путь `/map type`, а `file` уходил в хвост args — команда не переключала уровень карты.

Дополнительный шум: legacy TOML (`[[slash_route]]`, `[[melody_root]]`), `BuildFromLegacyTables`, ~12 стадий `SlashParse/*`, дублирующие тесты на shape парсера.

## Решение

### 1. Единственный источник пути

```text
intent-catalog.toml
    → IntentCatalogLoader (только [[command]] / [[command.form.slash]])
    → IntentSlashCatalog + SlashRouteCatalogIndex (runtime)
    → ProtocolDocGen → SlashRouteCatalogPathsGenerated.g.cs (build)
    → SlashLineResolver.TryResolveSlashLine
```

Новая команда: **строка `path` в TOML** (+ при необходимости `arg_tail`, handlers). Не новая стадия в C#-парсере.

### 2. API резолва (норматив)

| Метод | Назначение |
|-------|------------|
| `SlashLineResolver.TryResolveSlashLine` | Канонический путь + сырой `ArgTail` + `ArgTailKind` + флаги UI |
| `ChatSlashCommandCatalog.TryResolveInput` | Descriptor для execute/preview: **путь из резолва**, хвост нормализован; **не** требует непустой хвост при `required` (ошибку даёт runner / intercom handler) |
| `ChatSlashCommandCatalog.TryResolveCanonical` | Lookup с политикой `arg_tail` (`required` → хвост обязателен) — runnable, часть autocomplete |
| `ChatSlashCommandParser` | Только `IsSlashLine` и `ShouldAutoExecuteAfterAutocompleteCommit` — **без** `TryParse` |

### 3. Удалено из продукта

| Артефакт | Причина |
|----------|---------|
| `Features/Chat/SlashParse/*` | Дублировал каталог спец-кейсами |
| `IntercomSlashPathBuilder` | Собирал путь из parser shape |
| `ChatSlashCommandCatalog.TryResolve(parse)` | Второй вход в каталог |
| `ChatSlashCommandParseResult`, `ChatSlashCommandShape` | Модель больше не нужна |
| `IntentCatalogLoader.BuildFromLegacyTables`, `LoadLegacySlashRoutes` | Каталог только command-first |
| `requires_arg_tail` в loader/codegen | Заменено явным `arg_tail` в TOML |

### 4. Алиасы пути — только в каталоге

Синонимы вроде `/intercom anchor peek` ↔ `/anchor peek` — **отдельные** `[[command.form.slash]]` с тем же `intercom_handler`, не glue в парсере.

Спец-формы без пробела (`/anchor peekabcd1234`) **не** поддерживаются: канон `/anchor peek <id>`.

### 5. Старые top-level слэши (ADR 0136)

`/topic list`, `/overview`, `/attach selection` и т.п. **не** в каталоге → `TryResolveInput` = false, preview/runner: «нет такой команды». Отдельные `RejectReason` с текстом миграции **не** восстанавливаем (были частью `SlashParseLegacyRejectStage`).

## Последствия

- **Плюс:** один контур, предсказуемый longest-prefix, проще тесты (`TryResolveInput` + `SlashLineResolver`).
- **Плюс:** codegen trie синхронизирован с bundled catalog на каждой сборке.
- **Минус:** оверлей TOML без `[[command]]` больше не загружается — нужен command-first файл.
- **Минус:** пользователи старых коротких путей должны перейти на `/intercom …` или получить явный alias в TOML.

## Non-goals

- `System.CommandLine` / полноценный CLI-парсер для всей строки composer.
- Автогенерация `arg_tail` из JSON-schema `IdeCommands` без записи в TOML.
- Восстановление parser shape «на всякий случай» для parametric — хвост режет `SlashLineResolver`; валидация сегментов остаётся в `ParametricSegmentListParser` / binders.

## Альтернативы (отклонены)

| Вариант | Почему нет |
|---------|------------|
| Оставить `TryResolve(parse)` как fallback | Снова два источника правды и расхождения |
| Только починить `/map type file` в одной стадии парсера | Следующая команда снова ломается |
| Combinator-библиотека для всех slash | Сложность без выигрыша при полном каталоге путей |

## Дополнение к ADR 0150

Секции 0150 про «парсер **не меняем** в v1» и «`IntercomSlashPathBuilder` остаётся для `TryResolve(parse)`» **отменены** этим ADR. Актуальны: `SlashLineResolver`, `arg_tail`, правила UI по `ArgTailKind`.

## Статус реализации

| Элемент | Артефакт |
|---------|----------|
| Codegen trie | `tools/CascadeIDE.ProtocolDocGen/IntentCatalogSlashPathCollector.cs` → `Services/Generated/SlashRouteCatalogPathsGenerated.g.cs` |
| Резолв | `SlashLineResolver.cs`, `ChatSlashCommandCatalog.cs` |
| Runner / preview / send | `ChatSlashCommandRunner.cs`, `SlashCommandPreviewRules.cs`, `ChatPanelViewModel.IntercomSend.cs` |
| Каталог | `IntentMelody/intent-catalog.toml` |
| Тесты | `ChatSlashCatalogTestSupport`, `SlashLineResolverTests`, обновлённые `ChatSlash*` / `IntercomAnchor*` |

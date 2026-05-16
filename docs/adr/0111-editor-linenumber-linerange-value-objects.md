# ADR 0111: `LineNumber` и `LineRange` как доменные типы редактора

В тексте ADR для краткости: **LN** = `LineNumber`, **LR** = `LineRange`.

| Поле | Значение |
|------|----------|
| Статус | Accepted · Implemented (v1 — строки melody → JSON; v2 — MCP/колонки/Roslyn/портал, см. ниже) |
| Дата | 2026-05-11 |
| Контекст | Параметрические intent melody (ADR 0081, каталог ADR 0109) передают в команды JSON с полями `start_line` / `end_line` как `int`. Метанотация `:ln` / `:linenumber` в `tail_signature` описывает слот каталога, но не даёт типобезопасности и единых инвариантов в C#. |

## Решение

- Ввести **value objects** в пространстве имён `CascadeIDE.Models.Editor`:
  - **LN (`LineNumber`)** — 1-based номер строки; инвариант `Value >= LineNumber.MinimumOneBasedInclusive` (1); создание через `TryCreate(int, out LineNumber)`; операторы сравнения (в т.ч. для CA1036 / `IComparable`).
  - **LR (`LineRange`)** — пара `Start` / `End` типа LN с инвариантом `Start <= End` (инклюзивный диапазон в терминах редактора и IDE-команд); создание через `TryCreate(LineNumber, LineNumber, out LineRange)`.
- **`ParametricIntentMelody.ParsedLineRange`** хранит диапазон как **`LineRange Lines`**, а не два «голых» `int`.
- Парсер хвоста melody (`TryExtractLineRangeFromRemainder` в `ParametricIntentMelody`):
  - **Одно целое** без второго слота — **одна строка** (`<line>` по смыслу совпадает с `<line>:<line>` как LR).
  - **Два целых** — границы одного инклюзивного диапазона; порядок ввода не важен: после разбора применяется **`min..max`** (например `7:3` и `3:7` → один LR).
  - У **`LineRange.TryCreate`** для **явного кода** контракт по-прежнему «второй аргумент не раньше первого»; «перевёрнутый» ввод обрабатывается **до** вызова `TryCreate`, за счёт нормализации в парсере.
- На границе к JSON (**`ParametricLineRangeArgsBuilder`**) в анонимные DTO по-прежнему уходят **`int`** через `.Value` у LN — **wire-формат** `IdeCommands.Select` / `IdeCommands.ApplyEdit` не меняется.
- **`IntentMelodyTailSemantics.MinEditorLineNumber`** согласован с **`LineNumber.MinimumOneBasedInclusive`** (одна константа-источник для «минимальной строки 1-based»).

## Реализация v1 (источники в коде)

| Артефакт | Назначение |
|----------|------------|
| `Models/Editor/LineNumber.cs` | LN, константа минимума, `TryCreate`, равенство и сравнение |
| `Models/Editor/LineRange.cs` | LR, `TryCreate` при `End >= Start` |
| `Services/ParametricIntentMelody.cs` | `ParsedLineRange`, `TryParseLineRangeTail`, `TryExtractLineRangeFromRemainder`, делегирование в `ParametricLineRangeArgsBuilder` |
| `Services/ParametricLineRangeArgsBuilder.cs` | Сборка JSON-args из `ParsedLineRange.Lines` + проверка вылета за длину файла |
| `Services/IntentMelodyTailSemantics.cs` | `MinEditorLineNumber` → ссылка на минимум LN |
| `CascadeIDE.Tests/EditorLineNumberRangeTests.cs` | Юнит-тесты LN/LR |
| `CascadeIDE.Tests/ParametricIntentMelodyTests.cs` | Парсинг, одна строка, `min..max`, сборка args |
| `IntentMelody/intent-melody-aliases.toml` и копия в `publish-gh-release/IntentMelody/` | Подсказки палитры для `els` / `eld` (диапазон и одна строка) |

Каталог по-прежнему описывает **два** числовых слота в `tail_signature` (`<start:ln>:<end:ln>`); сокращение «одно число» — **соглашение парсера приложения**, не отдельная строка каталога.

## Реализация v2 (дорожная карта после v1 — закрыта в коде)

| Артефакт | Назначение |
|----------|------------|
| `Models/Editor/ColumnNumber.cs` | CN — 1-based колонка (`TryCreate`, сравнение) |
| `Models/Editor/EditorDocumentPath.cs` | Обёртка пути документа: `CanonicalFilePath.TryNormalize`, сравнение без регистра |
| `Models/Editor/EditorMcpSpans.cs` | `EditorTextSpan.TryParse`, `EditorContentLineRangeMcpArgs.TryParse`, `EditorGoToPositionMcpArgs.TryParse` |
| `Services/RoslynLinePositionMapper.cs` | `Microsoft.CodeAnalysis.Text.LinePosition` (0-based) → `(LineNumber, ColumnNumber)` для UI/MCP |
| `Services/ContextMinimizer.cs`, `Services/WorkspaceDiagnosticsCoordinator.cs` | Используют маппер вместо дублирования `+1` |
| `ViewModels/IdeMcpCommandExecutor.Handlers.Editor/*.cs` | `Select` / `ApplyEdit` / `GoToPosition` / `GetEditorContentRange` через общий разбор VO |
| `Services/ParametricLineRangeArgsBuilder.cs` | Границы колонок через `ColumnNumber` + канонический `EditorDocumentPath` |
| `Features/WebAiPortal/Application/WebAiPortalChatMixInFormatter.cs` | Снимок диапазона редактора: `LineRange` вместо «голых» int строк |
| `CascadeIDE.Tests/EditorMcpSpansTests.cs`, расширение `EditorLineNumberRangeTests` | Отказы и границы парсера MCP |

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | Семантика параметрики по строкам; 0111 — VO LR/LN в домене IDE |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Каталог, `tail_signature`; `:ln` — метаданные каталога |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn по диапазону; будущий мост к аргументам MCP |

## Отклонённые альтернативы

- **Только алиасы `:ln` в TOML** — не заменяют проверки и самодокументацию API в C#.
- **`record` с int без инвариантов** — дублирование проверок в каждом потребителе.
- **Полный переход команд на typed DTO вместо JSON** — вне scope v1; граница остаётся на сериализации.
- **Спец-токены «до конца файла»** (`7:end`, `7:*` и т.п.) — не входят в v1: отдельная мини-грамматика, взаимодействие с длиной файла на parse vs build, риск коллизий; оставлено на отдельное решение при явной потребности.

## Последствия (текущие)

- Новые сценарии **только по строкам** в ветке parametric melody → предпочтительно через LR/LN.
- Проверка «строки есть в файле» остаётся в **`ParametricLineRangeArgsBuilder`**, не в порядке двух чисел во вводе и не в LN/LR как таковых.

---

## Дорожная карта (после v1) — выполнено (v2)

Цель итераций v2 — **тот же паттерн**: инварианты в типах до границы JSON/MCP, без смены wire-контрактов `IdeCommands`.

### Приоритет 1 — граница MCP → редактор — **сделано**

- **`EditorTextSpan`** + **`ColumnNumber`**, **`TryParse`** из `IReadOnlyDictionary<string, JsonElement>` для `Select` / `ApplyEdit`; **`EditorGoToPositionMcpArgs`** для `go_to_position`; **`EditorContentLineRangeMcpArgs`** для `get_editor_content_range` (при отсутствии ключей — 1..1 как раньше; инвертированный явный диапазон — отказ с сообщением).

### Приоритет 2 — веб-портал — **сделано**

- **`WebAiPortalChatMixInFormatter`**: `EditorRangeSnap` хранит **`LineRange`**; разбор JSON нормализует границы через `min/max` при «перевёрнутых» int в wire-ответе.

### Приоритет 3 — parametric args — **сделано**

- **`ParametricLineRangeArgsBuilder`**: колонки через **`ColumnNumber.TryCreate`**, путь через **`EditorDocumentPath`**.

### Приоритет 4 — Roslyn — **сделано**

- **`RoslynLinePositionMapper`**: `LinePosition` (0-based) → `(LineNumber, ColumnNumber)`; **`ContextMinimizer`**, **`WorkspaceDiagnosticsCoordinator`**.

### Путь к файлу — **сделано**

- **`EditorDocumentPath`** поверх **`CanonicalFilePath.TryNormalize`**.

### Критерий готовности

- Два call site с одним набором полей JSON (`Select` + `ApplyEdit`) → **`EditorTextSpan`**; тесты в **`EditorMcpSpansTests`**.

# ADR 0111: `LineNumber` и `LineRange` как доменные типы редактора

В тексте ADR для краткости: **LN** = `LineNumber`, **LR** = `LineRange`.

| Поле | Значение |
|------|----------|
| Статус | Accepted · Implemented (v1 — строки melody → JSON команд) |
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

## Связь с другими ADR

- **0081** — семантика параметрики по строкам; ADR 0111 фиксирует представление LR/LN в доменной модели IDE поверх 0081.
- **0109** — каталог и `tail_signature`; `:ln` остаётся метаданными каталога, VO — слой приложения.
- **0110** — Roslyn по диапазону; когда появится мост, LR может переиспользоваться или маппиться в аргументы Roslyn MCP (отдельный шаг).

## Отклонённые альтернативы

- **Только алиасы `:ln` в TOML** — не заменяют проверки и самодокументацию API в C#.
- **`record` с int без инвариантов** — дублирование проверок в каждом потребителе.
- **Полный переход команд на typed DTO вместо JSON** — вне scope v1; граница остаётся на сериализации.
- **Спец-токены «до конца файла»** (`7:end`, `7:*` и т.п.) — не входят в v1: отдельная мини-грамматика, взаимодействие с длиной файла на parse vs build, риск коллизий; оставлено на отдельное решение при явной потребности.

## Последствия (текущие)

- Новые сценарии **только по строкам** в ветке parametric melody → предпочтительно через LR/LN.
- Проверка «строки есть в файле» остаётся в **`ParametricLineRangeArgsBuilder`**, не в порядке двух чисел во вводе и не в LN/LR как таковых.

---

## Дорожная карта (после v1)

Цель следующих итераций — **тот же паттерн**: инварианты в типах до границы JSON/MCP, без смены wire-контрактов `IdeCommands` без отдельного ADR.

### Приоритет 1 — граница MCP → редактор

Сейчас в обработчиках `IdeMcpCommandExecutor` (`Handlers/Editor/*.cs`) из JSON многократно читаются **`file_path` + четыре int** (`start_line`, `start_column`, `end_line`, `end_column`) и передаются в `IIdeMcpActions` «сырьём».

- Ввести разбор в один тип (рабочее имя: **`EditorTextSpan`** или **`EditorLineColumnSpan`**: путь + LR по строкам + границы по колонкам), с **`TryParse`** из словаря/`JsonElement`.
- Колонки — кандидат на отдельный VO **`ColumnNumber`** (1-based, ≥1), по аналогии с LN.
- Выгода: одна валидация, меньше дублирования между `Select`, `ApplyEdit`, опциональными end в `go_to_position`.

### Приоритет 2 — веб-портал и компактные ответы

- **`WebAiPortalChatMixInFormatter`** (`EditorRangeSnap`): заменить «голые» `StartLine`/`EndLine` на LR (и при необходимости общий тип пути), чтобы согласовать с MCP и melody.

### Приоритет 3 — колонки в сборке parametric args

- **`ParametricLineRangeArgsBuilder`**: сейчас колонки зашиты как «начало строки / конец строки + 1» от текста; вынесение в CN/малый span-тип упростит сопоставление с полным `EditorTextSpan` из п.1.

### Приоритет 4 — Roslyn / диагностики

- В `ContextMinimizer`, координаторе диагностик и подобных местах: вместо повторяющихся `LinePosition.Line + 1` / `Character + 1` — **локальные расширения** или один маппер `RoslynLinePosition → (LN, CN)` с комментарием о 0-based vs 1-based.

### Путь к файлу

- Для нормализации путей в решении уже используется **`CanonicalFilePath`**; отдельный «документ IDE» может быть тонкой обёрткой над канонической строкой, если п.1–2 начнут пересекать дерево решения и редактор.

### Критерий готовности очередного шага

- Есть **два независимых call site** с одинаковым набором полей JSON → вынос в общий тип с тестами на отказ и на граничные значения.

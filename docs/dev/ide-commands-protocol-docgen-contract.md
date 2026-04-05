# Контракт XML-доков для `IdeCommands` и ProtocolDocGen

Инструмент `tools/CascadeIDE.ProtocolDocGen` **не** разбирает полный набор стандартных тегов C# XML documentation (`<returns>`, `<param>`, `<remarks>`, …).

## Что реально читается

- **Только текст внутри `<summary>…</summary>`** у каждого `public const string` в частичном классе `IdeCommands` (после склейки `IdeCommands.cs` + `IdeCommands.*.cs`).
- Строки `///` вне блока summary для константы **игнорируются** парсером (в т.ч. отдельная строка `/// <returns>json</returns>` **не попадёт** в контракт и не заменит `returns:` в summary).

## Мини-язык внутри summary

Для линта и генерации (`IdeCommandsContract`, `IdeCommandsArgs`, таблица в `MCP-PROTOCOL.md`) в **одной** строке summary используются договорённые фрагменты:

| Фрагмент | Назначение |
|----------|------------|
| `args: …` | Схема аргументов для `IdeCommandsArgs` (см. грамматику в `Program.cs`, `IdeCommandArgsParser`). |
| `returns: json` \| `text` \| `none` | Вид возвращаемого значения для контракта и подсказок. |
| `example: {…}` | Пример JSON args (обязателен, если в summary есть `args:`). |

Пробелы нормализуются; вложенные XML-теги в summary вычищаются, кроме подстановки `` ` `` для `<c>…</c>`.

## Почему не `<returns>`

Отдельный тег `/// <returns>` был бы правильнее для IDE и компилятора, но потребовал бы **другой парсер** (многострочные теги, порядок относительно `const`, слияние с summary). Текущая схема — один поток текста на команду, предсказуемый линт и простая генерация.

Если понадобится: можно **расширить** `IdeCommandsParser`, чтобы он подмешивал текст из `<returns>` в нормализованный summary (или заполнял поле возврата отдельно), не ломая существующий мини-язык.

## См. также

- Реализация: `tools/CascadeIDE.ProtocolDocGen/Program.cs` (`IdeCommandsParser`, `IdeCommandsLint`, эмиттеры).
- Чертёж реестра: [ide-command-registry-v1.md](../design/ide-command-registry-v1.md).
- Целевое состояние (каноничные XML-теги, миграция, в т.ч. **вариант B:** стандартные теги + свои для машинного контракта): [ADR 0018](../adr/0018-ide-commands-canonical-xml-documentation.md).

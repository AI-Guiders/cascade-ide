# ADR 0015: Подсветка TOML в текстовом редакторе

**Статус:** Accepted (реализация: `EditorLanguageSupport`, `TextMateTomlGrammar`, каталог `TextMateGrammars/toml`, тесты)  
**Дата:** 2026-04-02  
**Связь:** [0010-ui-modes-toml-configuration.md](0010-ui-modes-toml-configuration.md) (продуктовые данные в TOML), `Services/EditorLanguageSupport.cs`, `Services/TextMateTomlGrammar.cs`, `Views/MainWindow.Editor.axaml.cs` (после `new RegistryOptions` — загрузка грамматики), Tomlyn — парсинг конфигов, не подсветка.

## Контекст

В продукте TOML уже **первый класс**: режимы интерфейса (`UiModes/`), пользовательские настройки (`settings.toml`), загрузка через Tomlyn ([0010](0010-ui-modes-toml-configuration.md)). Пользователи и разработчики открывают эти файлы во **встроенном редакторе** Cascade IDE.

Подсветка синтаксиса идёт через **AvaloniaEdit.TextMate** и **`RegistryOptions.GetLanguageByExtension`**, а перечень расширений — в **`EditorLanguageSupport`**. Встроенный бандл **TextMateSharp.Grammars** **не содержит** отдельной грамматики для `*.toml` (`GetLanguageByExtension(".toml")` → null без дозагрузки).

Отдельно: **Tomlyn** остаётся для **семантики** (десериализация, валидация). Он **не** заменяет TextMate-подсветку.

## Решение

1. **Область ADR — только подсветка (TextMate)** для `*.toml`. **Не входит:** LSP, схемы, форматирование — отдельные ADR/фичи.

2. **Грамматика по спецификации TOML в формате TextMate:** шипуется VS Code-совместимый пакет под **`TextMateGrammars/toml/`** (`package.json` + `syntaxes/toml.tmLanguage.json`). Текущий файл — **MIT**, происхождение **taplo** (см. `_source` в JSON и `TextMateGrammars/toml/README.md`). Это не «написать грамматику с нуля в репо», а **поддерживаемый артефакт** в стандартном формате; при необходимости обновление — замена файла из upstream (taplo / дистрибутив вроде flox-vscode), с сохранением лицензии.

3. **Загрузка в рантайме:** `RegistryOptions.LoadFromLocalFile` (есть в **TextMateSharp.Grammars 2.x**; в проекте явные ссылки `TextMateSharp` / `TextMateSharp.Grammars` **2.0.3**, чтобы переопределить транзитивную 1.0.56 от AvaloniaEdit.TextMate) сразу после `new RegistryOptions(ThemeName.DarkPlus)` — **`TextMateTomlGrammar.TryLoadInto`**. Каталог копируется в вывод сборки (`CascadeIDE.csproj` → `Content`), тесты подключают тот же каталог (`CascadeIDE.Tests`).

4. **`EditorLanguageSupport`:** **`.toml`** в `Supported` (имя **TOML**), в **`ExtensionToGrammarExtension`** — **`.toml` → `.toml`** (после загрузки пакета `GetLanguageByExtension(".toml")` не null).

5. **MCP / настройки:** по-прежнему из `Supported` без дублирующих списков.

6. **Инвариант:** тест **`ExtensionToGrammarExtension_AllResolveInTextMateRegistry`** использует тот же порядок инициализации, что приложение (`TryLoadInto` перед проверкой).

## Последствия

- Подсветка соответствует **TOML как языку** (таблицы, строки, числа, даты — по правилам грамматики), а не приближению через INI.
- Небольшой **шипнутый бандл** рядом с exe; обновление грамматики — правка файлов + прогон тестов.
- Ожидания **«как taplo LSP в VS Code»** по-прежнему не заявляются: только TextMate.

## Перспектива: «умный» TOML и внешний редактор

На **первое время** достаточно подсветки в встроенном редакторе и валидации при загрузке через **Tomlyn** ([0010](0010-ui-modes-toml-configuration.md)). Сложную правку `UiModes/`, `settings.toml` и т.п. можно **осознанно** вести во **внешнем редакторе** (VS Code, Cursor, …) с привычным TOML-тулингом — это не недостаток релиза, а **сознательное сужение scope**, пока нет боли «всё время правим конфиги только из Cascade».

Когда появится продуктовая потребность в **диагностиках, схеме ключей, форматировании, автодополнении** прямо в IDE — оформлять **отдельным ADR** (LSP или in-process на Tomlyn и т.д.), без расширения 0015 задним числом.

## Отклонённые альтернативы

- **Tomlyn как движок подсветки** — отклонено (не tokenization для TextMate).
- **Маппинг на INI** — использовался как временный обход до шипнутой грамматики; для финального состояния заменён на п. 2–3.
- **LSP TOML в этом ADR** — отклонено по объёму.

## Обновление грамматики

См. `TextMateGrammars/toml/README.md`: заменить `toml.tmLanguage.json` из выбранного upstream (MIT), при необходимости подправить `package.json`, убедиться что тесты резолва проходят.

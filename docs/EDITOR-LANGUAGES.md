# Языки редактора: список и поддержка

Список проверен вызовом `GetLanguageByExtension` + `GetScopeByLanguageId` (TextMateSharp.Grammars 1.0.56). Исходники: [danipen/TextMateSharp](https://github.com/danipen/TextMateSharp) — имена грамматик в `src/TextMateSharp.Grammars/GrammarNames.cs`, расширения по языкам в `package.json` каждого пакета в `Resources/Grammars/`. В редакторе используются только расширения из этого списка.

## Что есть в бандле (проверено)

**Поддерживаются** (28 расширений):  
`.bat`, `.cjs`, `.cs`, `.cshtml`, `.css`, `.go`, `.htm`, `.html`, `.js`, `.json`, `.less`, `.markdown`, `.md`, `.mjs`, `.ps1`, `.psd1`, `.psm1`, `.py`, `.razor`, `.rs`, `.scss`, `.sh`, `.sql`, `.ts`, `.tsx`, `.xml`, `.yaml`, `.yml`.

**Не поддерживаются** (GetLanguageByExtension даёт исключение):  
`.txt`, `.toml`, `.mts`, `.cts` (для `.mts`/`.cts` в IDE маппим на грамматику `.ts` — это расширения TypeScript 5+ для явного типа модуля: ESM и CommonJS).

## Поддержка в Cascade IDE

Единый источник правды — `Services/EditorLanguageSupport`: списки `Supported` и `ExtensionToGrammarExtension`. Подсветка применяется только для расширений из словаря; при неизвестном расширении грамматика не меняется (остаётся от предыдущего файла).

| Язык        | Расширения в IDE |
|-------------|-------------------|
| C#          | `.cs` |
| Markdown    | `.md`, `.markdown` |
| XML / XAML  | `.xml`, `.axaml`, `.xaml`, `.csproj`, `.config`, `.props`, `.targets` |
| JSON        | `.json` |
| SQL         | `.sql` |
| HTML        | `.html`, `.htm` |
| CSS / SCSS / Less | `.css`, `.scss`, `.less` |
| JavaScript  | `.js`, `.mjs`, `.cjs` |
| TypeScript  | `.ts`, `.tsx`, `.mts`, `.cts` (последние два → грамматика `.ts`) |
| YAML        | `.yml`, `.yaml` |
| PowerShell  | `.ps1`, `.psm1`, `.psd1` |
| Razor       | `.razor`, `.cshtml` |
| Batch       | `.bat` |
| Bash        | `.sh` |
| Python      | `.py` |
| Go          | `.go` |
| Rust        | `.rs` |

## Что не добавляли

- **.txt** — в бандле нет грамматики «plain text»; для неизвестных расширений подсветка не переключается.
- **.toml** — в бандле нет.
- **Dockerfile / .editorconfig / .ini / .gitignore** — не проверялись; при появлении в бандле можно добавить в `EditorLanguageSupport`.

## Как добавить синтаксис другого языка

### Язык уже есть в бандле TextMateSharp.Grammars

Добавить расширения в `Services/EditorLanguageSupport.cs`: в список `Supported` и в словарь `ExtensionToGrammarExtension`. Ключ — расширение файла (например `.csx`), значение — то расширение, которое принимает `GetLanguageByExtension` (в репо смотреть в `Resources/Grammars/<имя>/package.json` → `contributes.languages[].extensions`).

### Языка нет в бандле (TOML, Dockerfile, свой язык)

В `RegistryOptions` (исходники TextMateSharp) есть методы **`LoadFromLocalDir`** и **`LoadFromLocalFile`**: они подгружают грамматику из локальной папки/файла в тот же реестр, после чего `GetLanguageByExtension` начнёт находить новые расширения.

**Формат пакета грамматики** (как в VS Code / TextMate):

- Папка с именем грамматики (латиница, например `toml`).
- Внутри — **`package.json`** с полем `contributes`:
  - `contributes.languages[]`: `id`, **`extensions`** (массив строк, например `[".toml"]`), опционально `aliases`, `configuration` (путь к language-configuration.json).
  - `contributes.grammars[]`: `language` (тот же id), **`scopeName`** (например `source.toml`), **`path`** — путь к `.tmLanguage.json` или `.tmLanguage` (TextMate grammar).
- В той же папке — файл грамматики по пути из `path` (например `syntaxes/toml.tmLanguage.json`).

Грамматики в формате TextMate (`.tmLanguage.json`) можно брать из расширений VS Code или с [Open VSX](https://open-vsx.org/) / GitHub (например [vscode-toml](https://github.com/toml-lang/toml/blob/main/code/toml.tmLanguage.json)).

**Шаги в Cascade IDE:**

1. Выбрать или собрать пакет грамматики (папка с `package.json` + `syntaxes/*.tmLanguage.json`).
2. Положить папку в известное место (например рядом с exe: `Grammars/toml/`, или в настройках пользователя).
3. При инициализации редактора: после создания `RegistryOptions(ThemeName.LightPlus)` и **до** `InstallTextMate(registryOptions)` вызвать `registryOptions.LoadFromLocalDir(pathToGrammarsFolder)`. В `LoadFromLocalDir` каждая подпапка считается пакетом (по имени папки ищется `package.json` внутри).
4. Добавить новые расширения в `EditorLanguageSupport` (`Supported` и `ExtensionToGrammarExtension`), чтобы по ним выбиралась грамматика.

Сейчас в коде вызов `LoadFromLocalDir` не делается — при добавлении первого «внешнего» языка нужно в `MainWindow.axaml.cs` в `SetupEditorAndTextMate()` после `_registryOptions = new RegistryOptions(...)` добавить проверку: если есть папка с грамматиками (например `Path.Combine(AppContext.BaseDirectory, "Grammars")`), вызвать `_registryOptions.LoadFromLocalDir(thatPath)`.

## Где менять

- **Список языков и маппинг:** `Services/EditorLanguageSupport.cs` (`Supported`, `ExtensionToGrammarExtension`).
- **Применение грамматики:** `Views/MainWindow.axaml.cs`, метод `ApplyGrammarByFilePath`.
- **Подгрузка внешних грамматик:** там же, в `SetupEditorAndTextMate()`, после создания `_registryOptions` — вызов `LoadFromLocalDir` (когда появится папка с пакетами).
- **Проверка бандла:** при необходимости — отдельная консольная программа с `RegistryOptions(ThemeName.LightPlus)` и перебором расширений через `GetLanguageByExtension`.
- **Сверка с исходниками:** расширения из нашего списка совпадают с объявленными в package.json репо (csharp, razor, javascript, typescriptbasics, html, powershell, shellscript, sql и т.д.). В репо есть дополнительные расширения (напр. .csx, .cake для C#; .pssc, .psrc для PowerShell; .dsql для SQL), при желании их можно добавить в EditorLanguageSupport.

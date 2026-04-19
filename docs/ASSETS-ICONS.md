# Иконки дерева решения (Assets/Icons)

В дереве решения используются SVG-иконки из каталога **Assets/Icons**:

- **solution**, **project**, **folder**, **file** — узлы дерева (решение, проект, папка, общий файл).
- По расширению файла: **cs**, **ts**, **json**, **md**, **xml**, **html**, **css**, **js**, **yaml**, **yml**, **py**, **go**, **rs**, **scss**, **less**, **ps1**, **sh**, **bat**, **sql** и др. — имя файла иконки совпадает с расширением (например `cs.svg`, `ts.svg`).

Если для расширения нет файла (например `rs.svg`), показывается общая иконка **file.svg**.

## Текущие иконки по расширениям

В **Assets/Icons** уже добавлены SVG из набора **vivid** пакета file-icon-vectors для расширений: **cs**, **ts**, **json**, **md**, **xml**, **html**, **css**, **js**, **yaml**, **py**, **go**, **sql**, **ps1**, **sh**, **bat**, **scss**, **less**. Источник: [dmhendricks/file-icon-vectors](https://github.com/dmhendricks/file-icon-vectors) (лицензия MIT / атрибуция по README).

## Рекомендуемый пак: file-icon-vectors

**[dmhendricks/file-icon-vectors](https://github.com/dmhendricks/file-icon-vectors)** — свободный набор SVG по расширениям (MIT, с атрибуцией для части иконок). Около 1170 иконок, имена файлов = расширение: `cs.svg`, `ts.svg`, `json.svg` и т.д.

### Как подключить

1. Клонировать или скачать репозиторий, перейти в **dist/icons/vivid** (или **classic**, **square-o**).
2. Скопировать нужные `.svg` в **Assets/Icons** проекта CascadeIDE.

Расширения, которые поддерживает редактор (подсветка синтаксиса) и для которых в file-icon-vectors есть иконки в наборе vivid:

| Расширение | Файл иконки | Язык (редактор) |
|------------|-------------|-----------------|
| cs | cs.svg | C# |
| ts, mts, cts | ts.svg | TypeScript |
| js, cjs, mjs | js.svg | JavaScript |
| json | json.svg | JSON |
| md, markdown | md.svg | Markdown |
| html, htm | html.svg | HTML |
| css | css.svg | CSS |
| scss | scss.svg | SCSS |
| less | less.svg | Less |
| xml, config, props, targets, axaml, xaml, csproj | xml.svg | XML / XAML |
| yaml, yml | yaml.svg или yml.svg | YAML |
| py | py.svg | Python |
| go | go.svg | Go |
| rs | (в vivid может не быть — использовать file.svg) | Rust |
| sql | sql.svg | SQL |
| ps1, psd1, psm1 | ps1.svg | PowerShell |
| sh | sh.svg | Bash |
| bat | bat.svg | Batch |

### Другие наборы

- **[file-icons/icons](https://github.com/file-icons/icons)** (ISC) — иконки по имени языка/технологии (C#.svg, TypeScript.svg); для маппинга расширение→имя файла нужна своя таблица.
- **vscode-icons** — расширение VS Code с SVG; можно брать отдельные иконки (лицензия MIT).

Текущий конвертер ожидает имя файла = расширение (`cs.svg`, `ts.svg`), поэтому без доработок удобнее всего **file-icon-vectors**.

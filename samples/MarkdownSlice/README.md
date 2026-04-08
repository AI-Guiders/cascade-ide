# Markdown slice (include + диаграммы)

Минимальный набор файлов для проверки среза **0023** / кода:

| Файл | Назначение |
|------|------------|
| `sample.md` | Документ с `{{ INCLUDE: … }}` внутри fenced-блоков `mermaid` и `plantuml` |
| `hello.mmd` | Вынесенная Mermaid-диаграмма |
| `hello.puml` | Вынесенный PlantUML |

## Автоматически

Тест `MarkdownSliceSampleFiles_IncludesExpandFromCopiedSamples` в `CascadeIDE.Tests` читает копию этих файлов из выходного каталога тестов и проверяет `MarkdownIncludeExpansion`.

## В IDE

1. Открой `sample.md` в CascadeIDE (из исходников репо или из собранного дерева).
2. Превью: при **включённом** Kroki в настройках fenced-блоки `mermaid` / `plantuml` / `puml` уходят на сервер рендера — нужна сеть (или свой инстанс в URL).
3. **Export expanded** (если команда доступна): сначала логично прогнать include — это делает пайплайн экспорта поверх `MarkdownIncludeExpansion`.

## Ограничения среза (честно)

- Отдельные `*.puml` / `*.mmd` как документы редактора — без отдельного ADR **0023**-LSP пока «как файл в дереве», не обязательно с PlantUML LSP.
- Kroki в тестах CI не дергаем: сеть и флаки; проверка рендера — ручная или opt-in.

# Примеры Markdown (include и диаграммы)

Готовые файлы, чтобы **вручную** проверить в CascadeIDE превью и include из [ADR 0023](../../docs/adr/0023-markdown-diagrams-language-tooling.md); размещение превью — [0026](../../docs/adr/0026-markdown-preview-surfaces-and-placement.md).

| Файл | Зачем |
|------|--------|
| `sample.md` | Один документ: `{{ INCLUDE: … }}` внутри блоков `mermaid` и `plantuml` |
| `hello.mmd` | Диаграмма Mermaid в отдельном файле |
| `hello.puml` | Диаграмма PlantUML в отдельном файле |

## Автопроверка

В `CascadeIDE.Tests` класс **`MarkdownExamplesVerificationTests`** копирует эту папку в выход тестов и проверяет `MarkdownIncludeExpansion` (без сети).

## В IDE

1. Открой `sample.md`.
2. Превью с **Kroki**: в настройках включи рендер диаграмм — нужна сеть или свой URL сервера.
3. **Export expanded** (если есть в продукте): пайплайн опирается на тот же include.

## Чего здесь нет

- Отдельный LSP для `*.puml` / `*.mmd` — по дорожной карте **0023**, не про эти примеры.
- Вызов Kroki в CI — намеренно не автоматизируем (сеть, флаки); рендер проверяй глазами при необходимости.

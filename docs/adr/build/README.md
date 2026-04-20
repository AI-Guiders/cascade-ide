# Сборка ADR (как `resume/`)

Скрипт [`../build-adr.csx`](../build-adr.csx) склеивает нумерованные ADR (`NNNN-*.md`) и гоняет **Pandoc** → HTML, TXT, PDF (без **DOCX**).

## Зависимости

- [dotnet-script](https://github.com/dotnet-script/dotnet-script): `dotnet tool install -g dotnet-script`
- [Pandoc](https://pandoc.org/): `winget install JohnMacFarlane.Pandoc`
- PDF без LaTeX: Microsoft Edge (headless print-to-pdf), как в `resume/build-resume.csx`

## Запуск

Из каталога `docs/adr` (корень ADR):

```bash
dotnet script build-adr.csx
```

Иначе:

```bash
dotnet script build-adr.csx --root "D:\path\to\cascade-ide\docs\adr"
```

**Тематическая сборка UI-ADR** (отдельный корень и имена артефактов):

```bash
dotnet script build-adr.csx --book adr-book-ui.md
```

Корневой файл [`../adr-book-ui.md`](../adr-book-ui.md) тянет [`../UI/ui-adr-manifest.txt`](../UI/ui-adr-manifest.txt); карта смыслов — [`../UI/principles.md`](../UI/principles.md).

**Тематическая сборка TECH-ADR:**

```bash
dotnet script build-adr.csx --book adr-book-tech.md
```

Корневой файл [`../adr-book-tech.md`](../adr-book-tech.md) тянет [`../TECH/tech-adr-manifest.txt`](../TECH/tech-adr-manifest.txt); карта смыслов — [`../TECH/principles.md`](../TECH/principles.md).

## Режимы

1. **По умолчанию** — все файлы `NNNN-*.md` в `docs/adr` по имени, между ними разделитель `---`; сверху YAML-шапка со датой сборки.
2. **Свой порядок и преамбула** — положи `adr-book.md` в `docs/adr` (YAML + директивы `{{ INCLUDE: ... }}` / `INCLUDE_MANIFEST` / `INCLUDE_GLOB`), как `resume.md` в репо резюме.
3. **`--book <файл.md>`** — другой корень (например `adr-book-ui.md`); выход: `build/<stem>.md`, `out/html/<stem>.html` и т.д.

Общие фрагменты — каталог [`snippets/`](../snippets/README.md) (пример пути в INCLUDE: `snippets/foo.md`).

**Fenced-блоки** (` ``` ` … ` ``` `): директивы INCLUDE **внутри** таких блоков не обрабатываются (чтобы литературные примеры в ADR, напр. [0023](../0023-markdown-diagrams-language-tooling.md), не требовали фиктивных файлов).

## Выход

| Путь | Содержимое |
|------|------------|
| `build/<stem>.md` | Развёрнутый Markdown после INCLUDE (`<stem>` — имя без `.md` у `--book` или `adr-book`) |
| `out/html/<stem>.html` | + `adr.css` |
| `out/txt/<stem>.txt` | plain |
| `out/pdf/<stem>.pdf` | Edge или xelatex |

Каталоги `build/` и `out/` в `.gitignore`.

# ADR 0156: Страница Correspondence (MFD) и Reverse Anchors через CodeAnchor

**Статус:** Accepted  
**Дата:** 2026-05-28

## Резюме

- **Correspondence Surface (CRS)** — отдельная страница оболочки Mfd (`MfdShellPage.Correspondence`), по образцу **ERS** ([0023](0023-environment-readiness-glance.md)): один экран вторичного контура только для слоёв correspondence **L0–L4** ([0155](0155-documentation-code-correspondence-and-architectural-drift.md)), без смешения со списком related-файлов.
- **PFD** остаётся компактным: бейдж слоёв + переход на CRS; детали — на MFD.
- **Reverse Anchors** — направление **док → код**: какие ADR/KB **явно привязаны** к текущему файлу/диапазону через **CodeAnchor** (канон из [0128](0128-intercom-attachment-anchors-and-code-references.md), не дублируя второй словарь координат).
- **L1 path map** ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)) — *forward* (путь → ADR); reverse — *дополнение*, не замена.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | Слои L0–L4, каталог kinds, feature registry |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | L1: `[workspace.adr.map]` |
| [0023](0023-environment-readiness-glance.md) | Образец: dedicated MFD page + refresh + MCP `show_*_page` |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD = внимание; MFD = вторичный контур |
| [0039](0039-workspace-navigation-affordances.md) | Якорь карты; PFD граф/CF; MFD `RelatedFiles` |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | **CodeAnchor** / `AttachmentAnchor` — file + line range + member |
| [0137](0137-intercom-message-code-correspondence.md) | L4 discourse ↔ code |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML для экземпляров L1; не онтология kinds |
| [0157](0157-cide-magic-link-protocol.md) | Magic Link `cide://` из браузера → IDE |

---

## Контекст

После MVP correspondence ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md), [0155](0155-documentation-code-correspondence-and-architectural-drift.md)) на PFD/MFD появились:

- строка **Feature** (L1′),
- строка **ADR** (L1),
- бейдж **Correspondence: L0 · L1′ · …**,
- страница **RelatedFiles** со смешанным контентом (related-код + те же строки correspondence).

Это работает как **индикатор**, но:

1. **Нет «кабины» для correspondence** — как ERS для окружения: один экран, куда сводятся лампы/слои, refresh, действия.
2. **Forward-only:** map говорит «для этого файла читай ADR X», но не «ADR Y *ссылается на этот файл* в §Implementation».
3. **Дублирование UX:** correspondence размазан между PFD, `RelatedFiles` и Markdown Preview.

Идея **Reverse Anchors**: при открытии `Foo.cs` показать не только ADR по префиксу пути, но и документы, в тексте которых есть **устойчивый якорь** на `Foo.cs` (или на символ в нём).

---

## Решение

### 1. Correspondence Surface (CRS) — страница Mfd

#### 1.1. Размещение

| Элемент | Решение |
|---------|---------|
| Enum | `MfdShellPage.Correspondence` (новое значение после `Editor`) — [F:Models/MfdShellPage.cs] |
| View | `CorrespondenceMfdPageView` — `DataContext = NavigationMap` (тот же `WorkspaceNavigationMapViewModel`, что карта PFD) — [F:Views/CorrespondenceMfdPageView.axaml] |
| Allowance | всегда `true` (как `HybridIndex`, `RelatedFiles`) |
| Порядок в `PageOrder` | после `RelatedFiles`, перед `HybridIndex` |

Паттерн как у **ERS** ([0023](0023-environment-readiness-glance.md)):

- `show_correspondence_page` / `close_correspondence_page` (MCP + intent-catalog),
- при входе на страницу — `ScheduleWorkspaceNavigationMapRefresh()` (актуальный якорь),
- `EnsureMfdShellSurfaceForLayout()` при навигации.

#### 1.2. Содержимое CRS (v1)

Одна прокручиваемая колонка (не TabControl):

```text
┌─ Correspondence · workspace ─────────────────┐
│ Якорь: Features/Chat/ChatPanelViewModel.cs     │
│ Correspondence: L0 · L1′ · L1 · L2           │  ← tooltip = расшифровка слоёв
├──────────────────────────────────────────────┤
│ L1′ Feature: intercom.chat (+ docs)     [open] │
│ L1  ADR: 0119 (+3)                      [open] │
│ Docs coverage / templates hint                 │
├──────────────────────────────────────────────┤
│ Reverse anchors (preview)                        │
│  · ADR 0155 — «…Features/Chat…» (line ~42)     │
│  · ADR 0061 — link to path in map table         │
├──────────────────────────────────────────────┤
│ Linked ADRs (forward, полный список)           │
│  · docs/adr/0119-…                             │
│  · docs/adr/0150-…                             │
└──────────────────────────────────────────────┘
```

**Не входит в CRS v1:** related-файлы Roslyn (остаются на `RelatedFiles`); control-flow граф (PFD).

#### 1.3. PFD после CRS

| Было | Станет |
|------|--------|
| Feature / ADR / layers / docs на PFD и в RelatedFiles | PFD: **краткий** бейдж слоёв + клик → CRS; опционально одна строка «ADR: … (+n)» |
| RelatedFiles: дубли correspondence | RelatedFiles: **только** related list + якорь |

Клик по бейджу слоёв или `ADR:` на PFD → `MfdShellPage.Correspondence` (не сразу Markdown Preview).

#### 1.4. Данные и refresh

- Источник истины для UI — `WorkspaceNavigationMapViewModel` (уже есть refresh pipeline).
- Новые поля VM (направление реализации):
  - `WorkspaceCorrespondenceDocPaths` — полный forward-список ADR;
  - `WorkspaceReverseAnchorItems` — коллекция для reverse-секции;
  - `ShowCorrespondencePageCommand` — navigate CRS (`show_correspondence_page`).
- Слои L0–L4: логика как в `CorrespondenceLayersProjection` + каталог `wire/correspondence/correspondence-kinds.v1.json`.

---

### 2. Reverse Anchors и CodeAnchor

#### 2.1. Термины

| Термин | Направление | Смысл |
|--------|-------------|--------|
| **Forward map** (L1) | код → док | Префикс пути → ADR ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)) |
| **Reverse anchor** | док → код | Документ *упоминает* участок кода устойчивым якорем |
| **CodeAnchor** | — | Минимальный контракт привязки: `repoRelativeFile`, опционально `lineStart`/`lineEnd`, опционально `memberKey` / `syntaxScope` |

**CodeAnchor** для correspondence **согласуется** с полями `AttachmentAnchor` ([0128](0128-intercom-attachment-anchors-and-code-references.md)) там, где есть `file` + диапазон; Intercom-специфика (`ordinal`, `excerpt` в prose) остаётся в L4.

#### 2.2. Источники reverse (приоритет)

| Источник | `provenance` | v1 |
|----------|--------------|-----|
| **Explicit registry** | `workspace.correspondence.anchors` в `.cascade/workspace.toml` | опционально, позже |
| **Markdown scan** | `doc_body` | **да** — эвристики по телу ADR/KB |
| **Forward map inverse** | `adr_map` | нет — map не хранит обратных ссылок на строки |
| **Intercom event log** | `discourse` (L4) | отдельно [0137](0137-intercom-message-code-correspondence.md) |

#### 2.3. Эвристики markdown-scan (v1)

По каждому forward-linked ADR (и при необходимости KB из `feature.docs`):

1. Inline `` `Features/Chat/Foo.cs` `` и `` `src/...` `` (repo-relative, нормализация `/`).
2. Markdown-ссылки `[label](path/to/file.cs)` — не `http`.
3. Упоминания `file.cs:10-20`, `lines 10–20`, `L10-L20` рядом с путём в той же строке/абзаце.
4. Ссылки на ADR не считаются code anchor.

**Сопоставление с якорем карты:** файл якоря = `navigationPath`; диапазон — optional filter (если в доке указаны строки вне файла — anchor всё равно показывается с пометкой «range»).

**Не v1:** полный Roslyn resolve `memberKey` в ADR; structural picker.

#### 2.4. Модель записи (IDE / JSON)

```json
{
  "docPath": "docs/adr/0155-documentation-code-correspondence-and-architectural-drift.md",
  "docTitle": "ADR 0155",
  "kind": "documents",
  "codeAnchor": {
    "file": "Features/WorkspaceNavigation/Application/WorkspaceCorrespondenceResolver.cs",
    "lineStart": 19,
    "lineEnd": null,
    "memberKey": null
  },
  "excerpt": "…WorkspaceCorrespondenceResolver.Resolve…",
  "provenance": "doc_body"
}
```

Канонический `kind` — из [0155 §6](0155-documentation-code-correspondence-and-architectural-drift.md) (`documents`).

#### 2.5. Explicit registry (v2, TOML)

Опциональная секция в `.cascade/workspace.toml` для ручных reverse без парсинга:

```toml
[[workspace.correspondence.code_anchors]]
doc = "docs/adr/0155-documentation-code-correspondence-and-architectural-drift.md"
file = "Features/WorkspaceNavigation/Presentation/WorkspaceNavigationMapViewModel.cs"
line_start = 470
line_end = 490
kind = "documents"
```

Merge с markdown-scan: explicit **перекрывает** эвристику для того же `(doc, file)`.

<a id="adr0156-anchor-authoring"></a>

#### 2.6. Как устроены Anchors: та же модель, три поверхности записи

**Ответ на вопрос «как в чате `[F,M,L,S]` или иначе»:** семантика **та же**, что в [0128 §5.1](0128-intercom-attachment-anchors-and-code-references.md) (`F` | `M` | `L` | `S` → поля `AttachmentAnchor`). В correspondence это называется **CodeAnchor** — тот же shape без Intercom-полей (`messageOrdinal`, chip id в ленте).

| Ось | В чате | В ADR / correspondence |
|-----|--------|-------------------------|
| File | `F:` или active file | **`F:` обязателен** (док не привязан к «текущему файлу») |
| Member | `M:` | `M:` — re-resolve у читателя ADR |
| Lines | `L:` fallback | `L:` — снимок в тексте; при клике из CRS — hint, не контракт навсегда |
| Scope | `S:` | `S:` — «второй for в Run», как в чате |

**Не дублируем** отдельный «ADR-алфавит»: bracket-parser ([0131](0131-editor-slash-select-code-by-bracket-reference.md) / `BracketCodeReferenceParser`) **переиспользуется** для тела ADR и sidecar.

##### Три способа разместить якоря (рекомендуемый стек)

```text
Точность / устойчивость ──────────────────────────────────────►
  (1) inline bracket в ADR     (2) sidecar у ADR     (3) workspace TOML
  лучший для людей             bulk / codegen        repo-wide overlay
```

| # | Где | Формат | Когда использовать |
|---|-----|--------|-------------------|
| **A** | **В тексте ADR** (inline) | L1: `[F:Features/Chat/Foo.cs M:RunAsync]` · L2 в front-matter: `code_refs = ["F:…; M:…"]` | Норматив: «реализация — вот этот метод»; читается в GitHub без IDE |
| **B** | **Файл-спутник** рядом с ADR | `docs/adr/0156-….code-anchors.toml` (или `.json`) — массив `CodeAnchor` + `doc` implicit | Много ссылок, генерация из Roslyn; не засорять narrative |
| **C** | **Workspace** | `[[workspace.correspondence.code_anchors]]` в `.cascade/workspace.toml` | Связь без правки ADR (аудит, миграция map), feature-level glue |

**Merge при индексации (reverse):** `explicit(C)` > `explicit(B)` > `parsed(A)` > markdown-эвристики (backticks, `:line`) из §2.3.

##### A. Inline bracket в ADR (предпочтительно для авторов)

Пример в теле ADR:

```markdown
Реализация refresh — [F:Features/WorkspaceNavigation/Presentation/WorkspaceNavigationMapViewModel.Refresh.cs M:RunWorkspaceNavigationMapRefreshAsync].
```

Правила как в 0128:

- Парсить **только вне** fenced code blocks (иначе примеры кода ломают индекс).
- `@file` не используем — только `[…]`.

##### Сосуществование с Markdown (`[]` / `()`)

**Markdown-ссылка** в CommonMark — только пара **`[текст](url)`**: после `]` сразу (допускается пробел) идёт `(`…`)`.

| В ADR | Markdown-рендер (GitHub / Markdig) | Code anchor parse |
|-------|-------------------------------------|-------------------|
| `[M:Run]` | Обычный текст в квадратных скобках, **не** ссылка | да — префикс `M:` |
| `[F:Features/Chat/Foo.cs M:Bar]` | то же | да |
| `[ADR 0061](docs/adr/0061-….md)` | **кликабельная** MD-ссылка | **нет** — нет осей `F:`/`M:`/`L:`/`S:` внутри `[]` |
| `[M:Run](docs/adr/foo.md)` | MD-ссылка с подписью `M:Run` | **не использовать** — URL в `()` перехватывает markdown |

Итого: **не ломается**, потому что code-bracket **не** использует `](…)` сразу после `]`. ADR-перекрёстные ссылки `[title](path)` остаются как сейчас; code anchor — отдельный вид `[F:… M:…]`.

**Дисциплина авторов:** не писать `[M:Foo](relative.md)` — либо markdown-ссылка на док, либо code anchor `[F:… M:Foo]` без скобок-URL.

**Парсер:** `BracketCodeReferenceParser` срабатывает только если внутри `[…]` есть ось `F:` / `M:` / `L:` / `S:` (как в 0128); сегменты внутри inline-code и fenced code block (ограждение из трёх grave-accent на отдельной строке) не сканируются. В narrative **не вставлять** непарные grave-accent-тройки — иначе ломается `MarkdownProseSegments`.

**Опционально** для параноидной совместимости с чужими рендерерами: обернуть в `` `code` `` — тогда в prose не парсить (как fenced); **не рекомендуем** как канон, только для примеров в списках.

- Для агента в front-matter (опционально, v2):

```yaml
---
code_refs:
  - "F:Features/Chat/ChatPanelViewModel.cs; M:TryCommitCockpitCommandLineAsync"
---
```

YAML front-matter — **не** канон wire; канон после parse — `CodeAnchor` JSON / C# record.

##### B. Sidecar (файл-спутник)

Соглашение об имени (предложение):

```text
docs/adr/0156-correspondence-mfd-surface-and-reverse-code-anchors.md
docs/adr/0156-correspondence-mfd-surface-and-reverse-code-anchors.code-anchors.toml
```

```toml
# schema: correspondence.code-anchors.v1
[[anchor]]
bracket = "F:Features/WorkspaceNavigation/Application/WorkspaceCorrespondenceResolver.cs; M:Resolve"
kind = "documents"
note = "§2.1 entry point"

[[anchor]]
file = "Features/WorkspaceNavigation/Presentation/WorkspaceNavigationMapViewModel.cs"
line_start = 470
line_end = 490
kind = "documents"
```

Поле `bracket` — если задано, parse → те же поля, что у `file`/`line_*`. Sidecar **не обязателен**, если всё уже в prose.

**Плюс:** CI может валидировать anchors без парсинга всего Markdown. **Минус:** второй файл на ADR — держать в sync дисциплиной или codegen.

##### C. Workspace TOML (уже §2.5)

Для связей «этот ADR ↔ этот файл» без указания member/строк — достаточно `doc` + `file`. Для **точных** участков — дублировать bracket в `bracket = "F:…; M:…; S:…"` или `line_start`/`line_end`.

##### Что не делаем

| Идея | Почему |
|------|--------|
| Отдельные префиксы `D:` doc-only | `F/M/L/S` уже покрывают; doc — контейнер reverse-записи |
| Только эвристики без bracket | Остаются как **bootstrap v1**, не как долгосрочный канон |
| Дублировать полный `AttachmentAnchor` wire в ADR | Event log — L4; ADR — L1 `documents` |

##### Реализация (направление)

1. **v1:** scan §2.3 + explicit TOML (C).
2. **v1.1:** `BracketCodeReferenceParser` по телу ADR (A) + optional sidecar (B).
3. **CRS UI:** показывать `displayLabel` как в чате (`Run › for (2)`), клик → `intercom.reveal_attachment`-совместимый reveal (open + range), без открытия Intercom.

---

### 3. MCP и команды

Реализация CRS и reverse (v1): навигация — [F:Features/WorkspaceNavigation/Presentation/WorkspaceNavigationMapViewModel.Correspondence.cs M:ShowCorrespondencePage]; reverse scan — [F:Features/WorkspaceNavigation/Application/DocReverseAnchorResolver.cs M:Resolve]; JSON для агента — [F:Features/WorkspaceNavigation/Application/WorkspaceCorrespondenceContextBuilder.cs M:BuildJson]; bracket в prose — [F:Services/Intercom/BracketCodeReferenceParser.cs M:EnumerateInProse].

| Команда | Поведение |
|---------|-----------|
| `show_correspondence_page` | `TryNavigateToMfdShellPage(Correspondence)` + expand Mfd |
| `close_correspondence_page` | первая другая разрешённая страница |
| `open_workspace_adr_correspondence` | v1: открыть **первый** ADR в preview; v1.1: фокус CRS + highlight строки в списке |
| `get_correspondence_context` (новая, v1.1) | JSON: layers, feature, forward docs[], reverse anchors[] для `file?` |

Не дублировать `get_code_navigation_context` — другой слой (L2).

---

### 4. Политика (IOP)

| Уровень | Reverse anchors |
|---------|-----------------|
| **Inform** | список на CRS, tooltip excerpt |
| **Advisory** | агент: «ADR 0155 ссылается на этот метод» |
| **Verify** | не v1 — не блокировать merge из-за «ADR не упоминает новый файл» |

Эвристики markdown **могут ложноположительно** матчить путь в примере кода внутри ADR — показывать `provenance` и не трактовать как доказательство.

---

## Альтернативы (отклонены)

| Альтернатива | Почему нет |
|--------------|------------|
| Оставить correspondence только на RelatedFiles | Смешение L1/L2; нет «кабины» как ERS |
| Отдельный `CorrespondenceViewModel` | Дублирует refresh якоря карты; достаточно расширить `NavigationMap` |
| Второй граф «док ↔ код» в TOML | Дорого, drift; reverse = scan + optional explicit |
| Новый wire-тип `CodeAnchor` без связи с 0128 | Два контракта координат |
| Reverse только через L1 map | Map не знает строки и member в ADR |

---

## Дорожная карта

| Этап | Работа | Критерий |
|------|--------|----------|
| **C1** | ADR 0156 Accepted | Этот документ |
| **C2** | `MfdShellPage.Correspondence` + `CorrespondenceMfdPageView` + MCP show/close | Навигация как ERS |
| **C3** | CRS UI: layers, feature, forward ADR list | PFD slim; RelatedFiles без дубля correspondence |
| **C4** | `DocReverseAnchorResolver` + markdown-scan v1 | Секция reverse на CRS; тесты на sample ADR |
| **C5** | `get_correspondence_context` | **Done** — JSON layers/forward/reverse |
| **C6** | TOML `[[workspace.correspondence.code_anchors]]` | **Done** — merge explicit > scan |

**C2–C3** не блокируются **C4** — reverse-секция может быть пустой с подписью «scan pending».

---

## Открытые вопросы

1. Имя страницы в UI: **Correspondence** vs **Doc correspondence** vs аббревиатура **CRS** в chrome only.
2. Индексировать reverse при refresh (дорого) vs lazy при открытии CRS.
3. Включать ли KB (`knowledge/`) в scan наравне с `docs/adr/`.
4. Связь reverse anchor click → open doc + scroll to line (Markdown Preview) vs side-by-side.
5. Codegen: общий C# record `CodeAnchor` из schema, shared с Intercom wire subset.
6. Sidecar: обязательный `.code-anchors.toml` для Accepted ADR vs только inline bracket.
7. Front-matter `code_refs` — поддерживать в IDE preview или только body parse.

---

## Последствия

- **Плюс:** correspondence читается как **подсистема** с одной точкой входа (CRS), симметрично ERS.
- **Плюс:** reverse закрывает вопрос «какие ADR реально говорят про этот файл», не только «какой ADR назначен папке».
- **Плюс:** переиспользование **CodeAnchor** / 0128 снижает drift координат.
- **Минус:** markdown-scan требует тестов и оговорок про false positives.
- **Минус:** ещё одна страница Mfd — нужен discoverability (PFD badge, palette, MCP).

---

## Статус реализации

| Компонент | Статус |
|-----------|--------|
| L0–L4 badge + каталог kinds | Implemented (частично, до CRS) |
| `[workspace.adr.map]` + feature registry | Implemented |
| `MfdShellPage.Correspondence` + CRS UI | **Implemented** (C2–C3) |
| Reverse anchors scan + explicit TOML | **Implemented** (C4, C6) |
| `get_correspondence_context` | **Implemented** (C5) |

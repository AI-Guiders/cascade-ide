# ADR 0105: Hybrid Codebase Index for C# (Web + Avalonia AXAML + общий контур) with Roslyn Truth

**Статус:** Proposed  
**Дата:** 2026-05-06  

**Связь:** [0039](0039-workspace-navigation-affordances.md), [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md), [0052](0052-agent-contract-cli-and-snapshot-tests.md), [0069](0069-markdown-preview-tool-surface-and-renderer-decoupling.md), [0079](0079-ide-display-system-ids-overlay-pipeline.md) (IDS vs CDS; AXAML индекс — не IDS), [0095](0095-workspace-solution-ide-health-stratification.md), [0097](0097-cockpit-compute-units-transport-to-channel-dto.md), [0099](0099-ide-databus-typed-events-and-projections.md), [0100](0100-project-constitution.md), [0101](0101-licensing-and-commercialization-strategy.md), [0102](0102-data-acquisition-layer-boundary-and-contract.md).

---

<a id="adr0105-context"></a>

## Контекст

CascadeIDE — MCP-first IDE: агенту нужно быстро ориентироваться в кодовой базе и собирать контекст в малом окне модели (или при ограниченном бюджете шагов/вызовов).

Для **любых** .NET/C# решений у нас уже есть “источник истины” для точных семантических операций:

- Roslyn (через roslyn-mcp и IDE wiring) для: diagnostics, go-to-definition, find-usages, rename, symbol-level navigation.

Но Roslyn не решает полностью задачу:

- быстрый “обзор по смыслу” и “первую карту” решения без чтения десятков файлов;
- полнотекст и ориентация по **Markdown**, конфигам, `.csproj` / `.sln` / `.slnx`, YAML/TOML, **веб-слою** (**Razor/Blazor `.razor`**, HTML/CSS), разметке **Avalonia (`.axaml`)** и другим артефактам **без** семантической модели Roslyn для этих форматов;
- для **обычного** C#‑проекта (включая сам **CascadeIDE**) тот же гибридный слой даёт быстрый keyword/опц. semantic по **репозиторию целиком** — в том числе по **тексту `.cs`** ([слой B](#adr0105-layer-b): только FTS, не символы), пока переименование/impact остаются на Roslyn;
- устойчивость между сессиями: “карта” должна жить рядом с проектом/профилем IDE и не требовать каждый раз переобучения агента.

Есть внешние решения (например SocratiCode) с hybrid search + graph + impact, но они добавляют инфраструктурную нагрузку (Docker/Qdrant/Ollama), а также риск лицензий (AGPL) для интеграции в продукт.

Дополнительно: CascadeIDE кроссплатформенный (Avalonia). Мы не хотим делать критичный слой навигации завязанным на Windows-only/драйверы/Docker, но на Linux можем разрешать более “тяжёлые” backend-опции.

---

<a id="adr0105-decision-summary"></a>

## Решение в одном предложении

Ввести **двухслойную модель навигации**: **Roslyn — источник истины для C# семантики**, а рядом — **лёгкий гибридный индекс** по **контуру решения**: веб‑артефакты (`.razor`, MD, HTML/CSS), **Avalonia `.axaml`** (и при необходимости эвристика пары с code-behind `.cs`), конфигурация и сопровождение (**в т.ч. опционально полнотекст по `.cs` как тексту**, без подмены symbol-level операций); keyword + опциональная семантика; минимальная ops‑цена и кроссплатформенность.

---

<a id="adr0105-goals"></a>

## Цели

1. **Снизить число шагов агента**: 1–2 вызова → достаточно релевантного контекста для решения.
2. Дать “первую карту” без “прочитать 20 файлов”: топ-файлы/узлы/потоки, входные точки — для **Blazor/Web**, для **Avalonia (AXAML + привязки/имена контролов)** и для **обычного C#**, в том числе при **разработке самого CascadeIDE** на том же стеке инструментов.
3. Сохранить **семантическую корректность**: refactor-impact по C# не эвристический, а Roslyn-based.
4. Работать **без обязательного Docker** (особенно на Windows), с предсказуемой локальной установкой/обновлением.
5. Быть кроссплатформенным (Windows/Linux/macOS), с optional backend-ускорителями на Linux.

---

<a id="adr0105-non-goals"></a>

## Не-цели (на первом этапе)

- Полный “polyglot dependency graph” по 18+ языкам.
- Замена Roslyn MCP: Roslyn остаётся истиным слоем для C#.
- Обязательная векторная БД/контейнеры для базовых сценариев.
- “Один граф, который всегда прав”: граф/impact вне C# допускает эвристику и требует верификации.

---

<a id="adr0105-architecture"></a>

## Архитектура (в терминах слоёв)

<a id="adr0105-layer-a"></a>

### Слой A: Roslyn Truth (C#)

Использовать Roslyn для:

- diagnostics / code actions;
- find usages / rename;
- symbol navigation;
- (по возможности) call graph / entrypoints в пределах C# проекта.

Этот слой — **точный**, но “дорогой” по workflow: агенту всё равно нужно знать, *что искать*.

<a id="adr0105-layer-b"></a>

### Слой B: Hybrid Index (артефакты вокруг C#, веб-слой, Avalonia AXAML, опционально текст `.cs`)

Индекс для файлов и фрагментов **вне** Roslyn-символики или **как текст** (не как граф типов):

- `.razor`, `.razor.cs` (включая связь partial / file pairing);
- `.md` / `.mdx`;
- `.html`, `.css`, `.scss` (включая `@import`, классы/селекторы);
- базовые конфиги (`appsettings*.json`, `.editorconfig`, `*.props`, `*.targets`, `*.csproj`, `*.slnx`, pipeline YAML, `*.yml`, `*.toml` и т.п.);
- **`.axaml`** (и типичный code-behind `*.axaml.cs`, если есть): разметка и атрибуты — **как текст для FTS** и лёгких эвристик (`x:Name`, `{Binding …}`, `Classes=`, пути `avares:`); **не** подмена XAML-Avalonia-парсера, **не** семантика CDS/IDS (см. [0079 — CDS vs IDS](0079-ide-display-system-ids-overlay-pipeline.md#adr0079-cds-vs-ids));
- **`*.cs` (опция индекса):** только **полнотекст/keyword** (идентификаторы и строки встречаются как совпадения в тексте файла); **rename/find-usages/impact** по-прежнему только Roslyn. В ответах инструмента явно помечать hits по `.cs` как **text-ranked**, чтобы не смешать с символьной истиной.

Индекс предоставляет:

- **keyword / BM25**: строки конфигов, CSS, маршруты Razor, фрагменты `.cs`/`.axaml`/доков;
- **опционально semantic**: поиск “по смыслу” (через embeddings), но без обязательного Docker.

Данные индекса:

- хранятся локально (профиль IDE или рядом с проектом);
- обновляются инкрементально (watcher + hash);
- имеют явную версионность формата (чтобы migration не ломала UX).

<a id="adr0105-storage"></a>

#### Storage / backend (baseline)

Рекомендуемая конфигурация по умолчанию (без Docker, кроссплатформенно):

- **Keyword/BM25**: SQLite **FTS5** (локальная БД на диске) как быстрый полнотекстовый индекс.
- **Semantic vectors (optional)**: SQLite + **`sqlite-vec`** как локальный vector store (включается только при включённой семантике).

Движок здесь — **классический SQLite** (например `Microsoft.Data.Sqlite` или другой провайдер к той же библиотеке SQLite), **не** [WitDatabase](https://github.com/dmitrat/WitDatabase) (`*.witdb`): Wit остаётся для данных приложения CascadeIDE; файл индекса — отдельный SQLite на диске.

Важно: hybrid = **FTS (keyword)** + **vec (semantic)** как два независимых подиндекса, объединяемых на уровне сервиса (ранжирование/фьюжн), а не “магия одной БД”.

<a id="adr0105-layer-c"></a>

### Слой C: Composition (agent workflow)

Дефолтный workflow агента:

1. Hybrid search (быстро, дешево) → получить топ-N фрагментов и карту.
2. Roslyn navigation для точной проверки/рефакторинга в C#.
3. Точечное чтение файлов/фрагментов из IDE только после поиска.

<a id="adr0105-deployment"></a>

### Развёртывание: библиотека + отдельный MCP

Индекс оформлять как **общую библиотеку** (ядро: индексация, SQLite, форматы запроса/ответа) и **отдельный MCP-сервер** (тонкий слой stdio + регистрация tools), чтобы:

- использовать поиск **вне контура CascadeIDE** (другие IDE/агенты с MCP, CLI, автоматизация);
- изолировать тяжёлый процесс (watcher, файлы SQLite, опционально embeddings): перезапуск и обновления не смешиваются с Avalonia/UI.

CascadeIDE на раннем этапе может подключать **то же ядро in-proc** или поднимать **тот же бинарник MCP** как дочерний процесс — **идентификаторы и контракт tools** сохраняются общими для обоих сценариев.

<a id="adr0105-cide-alignment"></a>

### Согласование в контуре CascadeIDE

Встроенная связка (in-proc или дочерний процесс, поднятый IDE) должна вписываться в принятую архитектуру кабины, а не жить «отдельной вселенной» в `MainWindowViewModel`:

- **DAL** ([0102](0102-data-acquisition-layer-boundary-and-contract.md)): обход workspace, чтение файлов под индекс, при необходимости сеть/процессы для embeddings и прочий внешний I/O — в духе `Features/<slice>/DataAcquisition/`, без забрасывания сырого I/O в VM.
- **Оркестраторы `Application`**: сценарии `reindex` / `search`, конфигурация из `settings.toml`, связка watcher ↔ ядро индекса ↔ жизненный цикл SQLite-файлов.
- **CCU** ([0097](0097-cockpit-compute-units-transport-to-channel-dto.md)): свёртка результата поиска (FTS + vec, версия индекса, метаданные hit) в **стабильные DTO** для MCP и при необходимости в снимки каналов (топ-N, explain) — без превращения CCU в второй «движок индекса».
- События смены файлов/прогресса индекса и подписки UI — по смыслу через **IDE DataBus** ([0099](0099-ide-databus-typed-events-and-projections.md)), а не ad-hoc событиями из глубины storages.

Структурно — тот же принцип feature-slices ([0006](0006-presentation-layers-and-feature-slices.md)), что и в [architecture-migration.md](../architecture-migration.md): ядро может быть общим пакетом, а границы DAL/CCU в CIDE остаются явными.

---

<a id="adr0105-config-ux"></a>

## Конфигурация и UX-инварианты

- **Off-by-default по инфраструктуре**: если semantic embeddings требуют внешнего провайдера, это должно быть opt-in.
- **Кроссплатформенность**: одинаковые tool ids/контракты в MCP, разница только в backend-провайдере.
- **Работа в малом окне**: ответы инструментов должны быть “компактными по умолчанию” (top-N, с указанием пути/диапазона/score), с отдельной командой для расширения.

---

<a id="adr0105-impl-watchouts"></a>

## На что смотреть при внедрении

Операционные моменты, без которых dogfood и продакшен быстро разочаруют:

<a id="adr0105-impl-watchouts-volume"></a>

1. **Объём и шум.** FTS по всем `*.cs` раздувает индекс и может **засорять топ-N** сырьевыми строковыми попаданиями. Нужны явные **умолчания и фильтры** в `settings.toml` (или эквивалент): игноры/`gitignore`-согласование, маски путей, **ранжирование** (например приоритет документов/конфигов перед «сырым» `.cs`, или наоборот — режим «сначала код»), возможность временно исключить `*.cs` из FTS без отключения остального индекса.

<a id="adr0105-impl-watchouts-freshness"></a>

2. **Свежесть (freshness).** При частых сохранениях обновление чанков по `.cs`/`.axaml` должно быть **дёшевым инкрементом** (хеш файла, пересборка затронутых документов в FTS, без полного reindex на каждый keypress). Иначе **dogfood на самом CascadeIDE** раздражает и отбивает от инструмента.

<a id="adr0105-impl-watchouts-hit-kind"></a>

3. **Контракт MCP с первого прототипа.** В структуре ответа поиска — **стабильное поле типа попадания** (например `hit_kind`: `text_fts` / `text_vector` / `symbol_followup_roslyn` или эквивалент), чтобы агент и человек не гадали по свободному тексту. Менять семантику поля позже дороже, чем заложить его в v0.

---

<a id="adr0105-alternatives"></a>

## Альтернативы и почему нет (сейчас)

<a id="adr0105-alt-roslyn-grep"></a>

### A) “Только Roslyn + grep”

Плюсы: минимальная инфраструктура, высокая точность для C#.  
Минусы: слишком много шагов и чтения файлов для агентных сценариев; плохо покрывает docs/config/web и **глобальный** “где упомянуто” по репозиторию, если не считать тяжёлый только-Roslyn обход.

<a id="adr0105-alt-socraticode"></a>

### B) Встроить SocratiCode целиком

Плюсы: готовый hybrid+graph+impact слой, быстрое “orientation” по большому репо.  
Минусы:
- ops: Docker/Qdrant/Ollama в baseline;
- корректность графа вне C# зависит от эвристик;
- **лицензия AGPL** — нежелательна для встраивания в продукт (см. [0101](0101-licensing-and-commercialization-strategy.md)).

<a id="adr0105-alt-lsp-all"></a>

### C) LSP для всего (полный polyglot)

Плюсы: потенциальная семантическая точность по языкам.  
Минусы: слишком большая операционная и интеграционная цена; не решает проблему “малое окно/мало вызовов” без отдельного индекса/ранжирования.

---

<a id="adr0105-consequences"></a>

## Последствия

<a id="adr0105-consequences-positive"></a>

### Положительные

- Агент получает быстрый “первый проход” по решению **и** может **dogfood’ить** тот же индекс при разработке **самого CascadeIDE** и других C#‑репо без ограничения сценарием «только Blazor».
- Roslyn остаётся “истиной” для опасных операций (rename/impact/diagnostics).
- Docker становится optional: Windows-friendly baseline, Linux может получать расширенные режимы.

<a id="adr0105-consequences-risks"></a>

### Негативные / риски

- Появляется новый слой данных (индекс) → нужны версии, миграции, наблюдаемость.
- Есть риск ложных связей в `.razor`/CSS/HTML эвристиках → нужен “confidence” и явная маркировка, что это подсказка.
- Индекс по `.cs`/`.axaml` как текст может **случайно выглядеть** как «семантический find» → см. [§ на что смотреть при внедрении](#adr0105-impl-watchouts) ([`hit_kind`](#adr0105-impl-watchouts-hit-kind), [ранжирование](#adr0105-impl-watchouts-volume)).
- Нужно удерживать инструменты компактными, иначе hybrid-индекс может “спамить” контекстом и ухудшить UX.

---

<a id="adr0105-rollout-plan"></a>

## План внедрения (эскиз)

1. Определить MCP-контракты инструментов индекса (search/topN, status, reindex, explain-result) **с полем типа попадания** (`hit_kind` и т.п., см. [§ на что смотреть](#adr0105-impl-watchouts-hit-kind)); **ядро — библиотека**, MCP — отдельный хост ([§ развёртывание](#adr0105-deployment)).
2. Базовый keyword index (быстрый, кроссплатформенный) + watcher; покрытие конфигов и MD; включение **`*.cs` в FTS** через настройку (разумный default для **dogfood**: репозиторий самого `cascade-ide`).
3. Razor-specific heuristics: `@page`, компонентные теги, параметры/инжекты, pair `.razor` ↔ `.razor.cs`. **AXAML:** базовые эвристики (имена контролов, `Binding`, `Classes`, ключи ресурсов), pair **`*.axaml` ↔ `*.axaml.cs`** где применимо; граница с CDS/IDS — [слой B](#adr0105-layer-b), [0079 — CDS vs IDS](0079-ide-display-system-ids-overlay-pipeline.md#adr0079-cds-vs-ids).
4. Опциональные embeddings: выбрать провайдер без Docker в baseline (local Ollama, cloud), включение через `settings.toml`.
5. Интеграция workflow: “search before read” и маршрутизация в Roslyn tools для точности.


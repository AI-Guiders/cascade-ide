# ADR 0154: Slash-каталог — domain · object · intent (семантика и алиасы пути)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-28

## Резюме

После [0153](0153-slash-catalog-only-resolution.md) каноническим остаётся строка `path` в `[[command.form.slash]]`. Для **единообразия именования и autocomplete** вводим явную тройку **`domain` · `object` · `intent`** (в UI: домен → объект → действие). Строка в composer — **поверхность ввода**; короткие пути (`/build run`) — **алиасы** полной семантики, а не «пустой object». Термины **workspace / solution / project** в slash **не смешиваем** с группой UI `group = "Workspace"`.

**v1 (продукт):** один пользовательский путь `/build run` (и семейство test/debug с тем же elision); executor — **implicit scope = solution** (открытое `.sln` / контекст после `load_solution`). Отдельный slash `/project build run` и второй `command_id` для сборки проекта — **не в v1**; модель `project` как domain остаётся в ADR для будущего и для breadcrumb после миграции TOML.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash в composer, `command_id`, autocomplete |
| [0150](0150-slash-line-canonical-resolution.md) | `SlashLineResolver`, `arg_tail`, Enter/preview |
| [0153](0153-slash-catalog-only-resolution.md) | Резолв только по каталогу + codegen trie |
| [0125](0125-slash-workspace-file-commands-and-dynamic-completion.md) | `/file`, `/solution`, динамический completion |
| [0039](0039-workspace-navigation-affordances.md) | «Workspace» как контекст IDE / навигация — **не** slash-domain |
| [0136](0136-intercom-feed-gutter-and-slash-namespace.md) | domain slash `intercom` |

## Проблема

1. **Popup уже думает иерархией** «домен → объект → действие → аргумент» (`ChatSlashAutocomplete.GetHierarchyContext`), но в TOML задан только `path` — глубина выводится из числа токенов, без семантики.
2. **Короткие пути вводят в заблуждение:** `/build run` autocomplete помечает `build` как «домен», хотя по смыслу это **объект** при **сжатом** domain (см. ниже).
3. **Смешение слов workspace / solution / project:** в каталоге `group = "Workspace"` для `/solution open` и `/build run`; в продукте **workspace** — широкий контекст открытой работы ([0039](0039-workspace-navigation-affordances.md)), **solution** — `.sln`/`.slnx`, **project** — `.csproj` внутри solution. Slash-сегмент `solution` в пути **не равен** «всё про workspace».

## Глоссарий (slash vs IDE)

| Термин в IDE | Что это | Пример slash | Slash `domain`? |
|--------------|---------|--------------|-----------------|
| **Workspace** (контекст) | Открытая папка и/или загруженное solution; область поиска, MCP multi-root | `/search`, `/folder open` | Только если первый сегмент пути буквально про контекст: `workspace` (`/workspace show`) |
| **Solution** | Файл решения `.sln` / `.slnx`, `dotnet build` на solution | `/solution load`, `/solution new` | `solution` |
| **Project** | Проект `.csproj` в solution | `/solution new console` (шаблон + имя в хвосте) | Не отдельный domain: создание project — **intent** под `solution` + `new` + хвост |
| **File** | Файл на диске в контексте | `/file open` | `file` |
| **Intercom** | Канал чата / темы / сообщения | `/intercom topic list` | `intercom` |
| **Build / test / debug** | Сборка/тест/отладка **цели** в IDE — см. § «Scope: solution и project» | `/build run`, `/test run` | Сжатый path; domain в строке **не** `build` |

**Правило:** `group` в TOML (`Сборка`, `Workspace`, `Intercom`) — **группировка подсказок в UI**, ортогональна полям `domain` / `object` / `intent`.

## Решение

### 1. Семантическая тройка (канон)

| Поле | Смысл | Примеры |
|------|--------|---------|
| **domain** | Продуктовая область / **цель** команды (что затрагиваем) | `intercom`, `solution`, `project`, `file`, `editor`, `map`, `git` |
| **object** | Подсистема или вид операции **внутри** domain | `topic`, `build`, `test`, `debug`, `line`, `type`, `server` |
| **intent** | Действие (глагол, режим) | `list`, `run`, `open`, `select`, `show`, `launch` |

- **Не путать** `intent` с `command_id`: `intent` — человекочитаемый сегмент пути; `command_id` — канон исполнения ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)).
- **Хвост после полного пути** — не четвёртый уровень иерархии, а `arg_tail` ([0150](0150-slash-line-canonical-resolution.md)): аргументы, параметрические диапазоны, пути файлов.

### 2. Канонический путь и алиасы (elision)

**Канонический путь** — строка, собранная из тройки (и опционально статических `args` в TOML):

```text
canonical_path = "/" + domain + " " + object + " " + intent   // все сегменты непустые
```

**Алиас** — отдельная запись `[[command.form.slash]]` с тем же `command_id` (и теми же handlers), у которой `path` короче за счёт **опущенного domain** (или, реже, другого согласованного сжатия):

| Канон (семантика) | Алиас (частый ввод) | Примечание |
|-------------------|---------------------|------------|
| `solution` · `build` · `run` | `/build run` | см. § scope — алиас, когда цель берётся из контекста IDE |
| `project` · `build` · `run` | *(пока нет в каталоге)* | явная сборка **проекта** (`.csproj`), не всего solution |
| `solution` · `test` · `run` | `/test run` | то же сжатие scope |
| `solution` · `debug` · `launch` | `/debug launch` | семейство `/debug …` |
| `intercom` · `topic` · `list` | `/intercom topic list` | полная форма = алиас (domain не сжимается) |
| `map` · `type` · `set` + `[args] level=file` | `/map type file` | `file` — **не** intent, а дискриминатор в `args` (см. §4) |

Loader (v2): при наличии `domain`/`object`/`intent` проверяет согласованность с `path`; при отсутствии полей — только `path` (как сейчас, [0153](0153-slash-catalog-only-resolution.md)).

### 2a. Scope: solution и project (сборка, тесты, отладка)

В .NET и в IDE **собрать можно и solution, и project** — это разные цели (`dotnet build Foo.sln` vs `dotnet build Foo.csproj`). Slash-модель должна это **различать**, а не сводить всё к одному domain `solution`.

| Уровень | Смысл | Пример канона |
|---------|--------|----------------|
| **domain** | **Что** собираем/тестируем/отлаживаем | `solution` или `project` |
| **object** | **Какая** операция | `build`, `test`, `debug` |
| **intent** | **Как** выполняем | `run`, `launch`, `attach`, … |

**`/build run` в composer** — не «domain=build». Видимые токены `build` + `run` — это **object + intent**. Domain (`solution` **или** `project`) в короткой форме **не печатается**: подставляется из контекста IDE (открытое solution, startup project, выделенный `.csproj` — политика в `command_id` / executor, не в парсере пути).

| Ситуация | Канонический path (целевой) | Сегодня в каталоге ([0153](0153-slash-catalog-only-resolution.md)) |
|----------|----------------------------|----------------------------------------------------------------------|
| Сборка всего solution | `/solution build run` | только `/build run` → `command_id = build` (фактически solution; см. XML-doc) |
| Сборка одного project | `/project build run` | **нет** отдельного slash — зазор продукта |
| Частый ввод без scope | `/build run` | **alias**; scope implicit |

**v1 CIDE (принято):**

| Решение | Содержание |
|---------|------------|
| Пользовательский path | Только `/build run` (и аналоги `/test run`, `/debug …`); полная форма `/solution build run` в UI **не обязательна** |
| Scope в executor | **Solution** по умолчанию (`command_id = build`); не startup project, не выделенный `.csproj`, пока нет отдельной политики |
| `/project build run` | **Не добавляем** в каталог и executor без явного use case (большие sln, агент «собери только X», несогласованность с debug) |
| Семантика в ADR | Канон `solution` · `build` · `run` + алиас `/build run`; domain `project` — **зарезервирован**, не «навсегда только solution» |

Опционально позже (без обязательного второго slash): scope из контекста explorer; или `[command.form.slash.args] scope = project` / хвост с путём к `.csproj` — при появлении второго поведения в executor.

### 3. Предлагаемая форма TOML (v2, не обязательна сразу)

```toml
[[command.form.slash]]
domain = "solution"
object = "build"
intent = "run"
path = "/build run"              # alias; scope implicit (solution или project)
path_role = "alias"

[[command.form.slash]]
domain = "solution"
object = "build"
intent = "run"
path = "/solution build run"
path_role = "canonical"
help = "Собрать открытое solution (structured JSON)."
group = "Сборка"
arg_tail = "none"
```

Несколько `[[command.form.slash]]` на одну команду:

```toml
[[command.form.slash]]
domain = "solution"
object = "build"
intent = "run"
path = "/solution build run"
path_role = "canonical"

[[command.form.slash]]
domain = "solution"
object = "build"
intent = "run"
path = "/build run"
path_role = "alias"
```

**Минимальный шаг без ломки v1:** только документировать семантику в комментариях к `path` и в ADR; поля добавить, когда будет миграция каталога.

### 4. Особые случаи

| Случай | Правило |
|--------|---------|
| **Плоские команды** | `/help` — `domain = "help"` (или `intercom`), без object/intent; один сегмент после `/` |
| **Два сегмента в path** | Либо alias (§2), либо `domain` + `intent` без object (`git` + `status` — уточнить при добавлении) |
| **Три сегмента + параметр** | `/editor line select` — domain=`editor`, object=`line`, intent=`select`; хвост — parametric ([0124](0124-slash-parametric-editor-line-commands.md)) |
| **Статический дискриминатор** | `/map type file` и `/map type controlflow`: один смысл «задать уровень карты»; различие в `[command.form.slash.args] level=…`, не в том, что `file` — intent. Семантика: domain=`map`, object=`type`, intent=`set` (или `level`); path остаётся как сейчас для trie |
| **Шаблон в path** | `/solution new console` — domain=`solution`, object=`new`, intent=`console` **или** intent=`new`, хвост=имя project — **зафиксировать одну схему** при миграции (предпочтение: intent=`new`, object=`project`, template в хвосте/args) |

### 5. Autocomplete и breadcrumb

- **Breadcrumb** в popup показывает **каноническую** тройку (`solution › build › run` или `project › build › run`), даже если в строке `/build run`; при implicit scope — пометка «цель: solution» / «цель: startup project» из IDE.
- **Подстановка** по-прежнему может вставлять **алиас** (короткий path), если `path_role = alias` и `auto_run_on_commit`.
- **Сегментные подсказки** строятся по **domain**, затем **object**, затем **intent** из индекса каталога, а не по «первому токену = domain».

## Последствия

- Единый язык для авторов каталога, тестов и ADR; меньше путаницы workspace/solution/project.
- Допускается несколько `path` на одну семантику без второго парсера.
- Миграция **постепенная**: [0153](0153-slash-catalog-only-resolution.md) остаётся; поля `domain`/`object`/`intent` — следующий шаг loader + codegen.

## Non-goals

- Автоматический вывод domain из `command_id` без явной записи в TOML.
- Обязательная полная форма `/solution build run` в UI (алиасы остаются).
- Слияние `group` и `domain` в одно поле.
- Переименование IDE-терминов workspace/solution в коде Roslyn/MCP — только slash-каталог.
- **v1:** отдельный slash `/project build run`, второй `command_id` только для сборки `.csproj`, обязательная дублирующая запись canonical path в каталоге.

## Альтернативы (отклонены)

| Вариант | Почему нет |
|---------|------------|
| Только `path`, без тройки | UI и авторы каталога продолжают расходиться в трактовке сегментов |
| `object = ""` для `/build run` | Маскирует elision; ломает иерархию подсказок |
| domain = `workspace` для всех build/test/debug | Смешивает контекст IDE с операцией над **solution**; в каталоге уже есть отдельный сегмент `workspace` (`/workspace show`) |
| Один domain `solution` для всех build/test/debug **без** варианта `project` | Отрицает реальный `dotnet build` на csproj; оставляем оба канона + алиас |

## Принятые решения

| # | Вопрос | Решение |
|---|--------|---------|
| A | Дефолт `/build run` | **Solution**; один `command_id = build`; отдельный project-slash в v1 **нет** (см. §2a) |
| B | `canonical_path` в TOML | **Не вводим** отдельное поле; достаточно `domain`/`object`/`intent` + `path` + `path_role` (`canonical` \| `alias`) |
| C | Массовая миграция каталога | **Done** (2026-05-28): все `[[command.form.slash]]` в bundled `intent-catalog.toml`; `intent_catalog_schema_version = 2` |

## Открытые вопросы

1. **Канон для `/solution new console`:** предпочтение при миграции — `domain=solution`, `object=project`, `intent=new`, шаблон `console` в хвосте/args (см. §4); финально — при разметке семейства `solution`.
2. **Семейство `format` / `git` / `agent`:** таблица domain·object·intent при пошаговой миграции TOML (не блокирует Accepted ADR).
3. **Project-scope без второго slash:** при появлении фичи — тот же `build` + `scope` в executor vs контекст explorer; отдельный `command_id` только если поведение принципиально другое.

## Связь с реализацией

| Сейчас ([0153](0153-slash-catalog-only-resolution.md)) | После 0154 |
|--------------------------------------------------------|------------|
| Только `path` | `domain`, `object`, `intent` + проверка `path` в TOML |
| Trie по полному `path` (резолв) | + `SlashSemanticCatalogIndex` для popup |
| Breadcrumb по глубине токенов | Breadcrumb по семантике (elision `/build ` → «действие») |

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-28 | Proposed: семантика domain/object/intent, elision, глоссарий workspace≠solution≠project |
| 2026-05-28 | §2a: build/test — domain `solution` **или** `project`; `/build run` = object+intent, scope implicit |
| 2026-05-28 | Accepted v1: один `/build run`, implicit solution; project-slash deferred; § «Принятые решения»; статус Deferred для TOML/loader |
| 2026-05-28 | Implemented: `domain`/`object`/`intent`/`path_role` в TOML; `SlashRouteSemantics`; loader validation; popup breadcrumb для elision (`/build ` → «действие») |
| 2026-05-28 | `SlashSemanticCatalogIndex`: popup по domain → object → intent (elision starters на корне) |

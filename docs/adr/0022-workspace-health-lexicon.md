# ADR 0022: Лексикон и канон имён — IDE Health (эволюция названий; файл ADR сохранён как 0022)

**Статус:** Accepted  
**Дата:** 2026-04-11  
**Обновлено:** 2026-04-25 — TOML-ключи контура **IDE Health** в режимах: канон **`ide_health_*`** (см. [0010](0010-ui-modes-toml-configuration.md)). Подробности — [§ История](#adr0022-history).  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | модель внимания, канал vs слот презентации |
| [0010](0010-ui-modes-toml-configuration.md) | ключи TOML и capabilities |
| [0012](0012-floating-workspace-chrome.md) | размещение полосы и нижней зоны |
| [0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md) | **переименование** продукта и типов: *Workspace Health* → **IDE Health**, `WorkspaceHealth*` → `IdeHealth*` |

### Вне ADR

| Документ | Роль |
|----------|------|
| [`workspace-health-implementation-map-v1.md`](../design/workspace-health-implementation-map-v1.md) | код и поток данных |

## Контекст

1. Слово **telemetry** в английском UI и в инженерной речи перегружено: продуктовая аналитика, «телеметрия агента», приборные показания в метафоре кокпита и т.д.
2. Канал **состояния задачи в workspace** (сборка, тесты, сессия отладки, git) нужен был с **устойчивым именем** в коде, конфиге и ADR без коллизий.
3. Переименование **без обратной совместимости** для ключей TOML и типов проведено в репозитории; этот ADR фиксирует **решение и рамки**, а не пошаговую миграцию. **0089** зафиксировал смену **продуктового** имени на **IDE Health** и префикс типов **`IdeHealth*`**; wire-ключи режимов для этого контура — **`ide_health_*`** в TOML ([0010](0010-ui-modes-toml-configuration.md), `UiModes/*.toml`), ранее `workspace_health_*`.

---

## Решение (канон)

| Слой | Канон | Русские формулировки в UI/доках | Где зафиксировано |
|------|--------|----------------------------------|-------------------|
| Продукт / ADR | **IDE Health** (ранее *Workspace Health*) | Рядом уместно **состояние IDE** / «сводка сборки и среды» (избегать путаницы с каталогом *workspace* на диске) | **0089**, этот ADR, **0021** §1.1–1.2 |
| Код | Префикс типов **`IdeHealth*`** (`IdeHealthSurfaceCompositor`, `IdeHealthStripView`, …); часть имён VM/привязок может ещё содержать `WorkspaceHealth*` до зачистки | — | Репозиторий, чертёж implementation map |
| Конфиг режимов | Ключи **`ide_health_*`** (`ide_health_strip`, `ide_health_surface`, `ide_health_on_terminal_tab`, `ide_health_main_column_span`) — wire-формат в TOML | — | **0010**, `UiModes/*.toml` |

**Семантика:** один **канал смысла** (что происходит с задачей: build/tests/debug/git), несколько **слоёв представления** (полоса под редактором, страница MFD, дубль на вкладке терминала — по пресету). Композитор смысла — `IdeHealthSurfaceCompositor`; зона экрана и хром — по **0021** и пресетам.

---

## Не путать с IDE Health

| Имя / ключ | Смысл | Почему не переименовывали в тот же заход |
|------------|--------|------------------------------------------|
| **`autonomous_agent_telemetry`** (TOML), **`AutonomousAgentTelemetry`** (capabilities) | Кокпит Power: явный доступ к **выводу** (терминал), подсказки при скрытом терминале | Другой продуктовый контур; слово *telemetry* здесь про «приборку/вывод сессии», не про канал build/tests/debug/git. См. **0010** и UX-таблицы. |
| Строки вроде **«Telemetry: on»** в UI | Привязка к терминалу в Power | Локализация и переименование свойств VM — отдельная задача. |
| Стабильные **id якорей** в markdown (напр. `#anchor-pfd-mfd-content-vs-telemetry-page`) | Постоянные ссылки из других доков | Менять ломает внешние ссылки; смысл якоря описан в **0021**. |

---

## Эволюция (кратко)

| Было (устарело) | Стало (канон) |
|-----------------|---------------|
| Черновики: «телеметрия работы», «операционная телеметрия» | **IDE Health** / состояние IDE |
| `WorkspaceTelemetry*` | `IdeHealth*` (типы); ключи TOML `ide_health_*` |
| Ключи `telemetry_*` в TOML режимов | `ide_health_*` |
| Чертёж `workspace-telemetry-compositor-implementation-v1.md` | [`workspace-health-implementation-map-v1.md`](../design/workspace-health-implementation-map-v1.md) |
| Продуктовое имя *Workspace Health* | **IDE Health** ([0089](0089-ide-omnibus-naming-and-ide-health-channel-rename.md)) |

История формулировок в шапках **0021** и в UX-доках может ссылаться на старые имена в **прошедшем времени** — это норма.

---

## Последствия

- Новые фичи для этого канала — имена типов **`IdeHealth*`**, ключи TOML в **`[capabilities]`** — префикс **`ide_health_*`** ([0010](0010-ui-modes-toml-configuration.md)).
- Обзорные UX-доки: [`cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md), [`concept-to-implementation-map-v1.md`](../ux/concept-to-implementation-map-v1.md), [`ui-modes-overview-v1.md`](../ux/ui-modes-overview-v1.md) — выровнены по термину **IDE Health**; при расхождении **приоритет у 0089, этого ADR и 0021**.

---

## Открытые пункты (не блокируют канон)

- Отдельное переименование **`autonomous_agent_*`** / строк UI, если продукт выберет единый английский глоссарий без «telemetry» для кокпита Power.
- Локализация RU/EN для всех видимых строк — вне скоупа этого ADR.
- Полная зачистка остаточных имён `WorkspaceHealth*` в VM/AXAML при отсутствии необходимости сохранять wire-совместимость для внешних интеграций.

---

## История изменений

<a id="adr0022-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-04-25 | TOML-ключи контура **IDE Health** в режимах: канон **`ide_health_*`** (см. [0010](0010-ui-modes-toml-configuration.md)). |

# ADR 0155: Соответствие документации и кода (correspondence) и архитектурный дрейф

**Статус:** Proposed  
**Дата:** 2026-05-28

## Резюме

- **Сквозной каркас:** как в Cascade отслеживать согласованность **намерений в документации** (ADR, KB, Intercom) с **кодом** и семантической картой — шире, чем одна связь «relate».
- **Не один механизм:** несколько **слоёв** (осведомлённость, навигация, план правок, дискуссия) с общей дисциплиной **verify / advisory**, без подмены code review.
- **0061** — первый практический шаг (карта путь → ADR); полная автоматическая верификация «текст ADR = реализация» — отдельная дорожная карта.
- **Канон видов связи** — отдельный версионированный каталог (предпочтительно **JSON + JSON Schema**); **экземпляры** привязок (путь → ADR) — в `workspace.toml`; per-layer wire сохраняет нативные имена + alias на канон.
- **Feature** (продуктово-архитектурная единица) — хаб: **code scope** (файлы, слои, неймспейсы) + **несколько доков**; L1 path map остаётся fallback ([§7](#adr0155-feature-unit)).

## Связанные ADR

| ADR | Роль в correspondence |
|-----|------------------------|
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | **L1:** декларативная карта `путь → ADR`, PFD, advisory агента (GPWS для доков) |
| [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) | **CRS:** страница Mfd для L0–L4; **reverse anchors** (док → код через CodeAnchor) |
| [0098](0098-semantic-first-document-as-projection.md) | **L0:** семантическая карта первична; код/доки/git — проекции; риск drift между проекциями |
| [0039](0039-workspace-navigation-affordances.md) | **L2:** код ↔ код («связанные» файлы, subgraph, MCP `get_code_navigation_context`) |
| [0053](0053-semantic-map-control-flow-pfd.md) | **L2:** намерения **в коде** (control flow, grain intent/detailed) |
| [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | **L3:** план правок vs семантическая карта до записи на диск |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Event log — носитель явных связей Intercom |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | Якоря «кусок кода» в сообщении |
| [0137](0137-intercom-message-code-correspondence.md) | **L4:** gutter ↔ код, `message_relate` / `messages_for_code` |
| [0121](0121-intent-oriented-programming-paradigm.md) | IOP: коммуникация и явные намерения как противоядие хаосу |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Куда подавать сигналы (PFD, advisory, не banner blindness) |
| [0073](0073-pfd-instrument-deck.md) | ADR indicator как вариант прибора на PFD |
| [0114](0114-graph-edge-relation-kind-taxonomy.md) | **`relation_kind`** на рёбрах графа (L2); ортогонально `graph_kind` и provenance |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML-first для **конфигов**; каталоги и wire — отдельно |
| [0006](0006-presentation-layers-and-feature-slices.md) | Вертикальный **feature slice** в коде (`Features/<Имя>/`) — не реестр correspondence |
| [0065](0065-instrument-categories-domain-taxonomy.md) | `graph_kind`, уровни абстракции графа; фича может агрегировать узлы L2 |

### Вне ADR

| Документ | Роль |
|----------|------|
| [feature-archetype-v1.md](../design/feature-archetype-v1.md) | Чеклист «как добавить возможность»; кандидат в `code_scope` фичи |

---

## Контекст

Идеи «следить, не разъехались ли код и намерения» уже разбросаны по ADR: карта ADR к путям ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)), semantic-first ([0098](0098-semantic-first-document-as-projection.md)), навигация и карта намерений кода ([0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md)), pre-flight ([0042](0042-pre-flight-planned-changes-and-review-before-apply.md)), Intercom ↔ код ([0137](0137-intercom-message-code-correspondence.md)).

Без **общего имени и слоёв** легко:

- путать «привязку ADR к папке» с «доказательством, что реализация верна»;
- дублировать второй источник истины (TOML-карта vs граф Roslyn vs event log);
- требовать от одного механизма (`relate` в чате) то, что он не обещает.

Этот ADR — **навигатор по теме**, не замена дочерних ADR.

---

## Проблема

| Симптом | Что ломается |
|---------|----------------|
| ADR есть, в файле не читают | Норма не участвует в рабочем контексте |
| Код уехал, ADR не обновили | **Архитектурный дрейф** — тихий |
| Агент правит «как понял», без стыка с ADR/KB | Повторяет ошибку «слепого доверия» |
| В чате обсуждали метод X, в коде правят Y | Потеря **correspondence** между дискурсом и diff |
| Semantic map и файлы расходятся | Drift проекций ([0098](0098-semantic-first-document-as-projection.md)) |

Нужна модель **шире, чем `relates`**: типизированные связи, слои, политика **accept / warn / block** (по зрелости).

---

## Решение: модель correspondence

### 1. Термины

| Термин | Смысл |
|--------|--------|
| **Документ-носитель намерения** | ADR, playbook/KB, TOML-политика, зафиксированное сообщение Intercom, ADR front-matter |
| **Артефакт реализации** | Исходники, сгенерированный код, тесты, конфиг |
| **Correspondence** | Заявленная или выведенная связь «этот документ **относится** к этому участку системы» + ожидаемый **характер** связи |
| **Drift** | Наблюдаемое расхождение: правка нарушает заявленное намерение, документ устарел, или проекции разошлись |
| **Feature** (correspondence) | Именованная продуктово-архитектурная единица: заявленный **охват кода** + **набор доков**; см. [§7](#adr0155-feature-unit) |

### 2. Виды связи (шире `relates`)

Ориентир для людей и advisory; **машинный канон** — [§6](#adr0155-relation-kinds-canon). Детализация рёбер L2 — [0114](0114-graph-edge-relation-kind-taxonomy.md) (`relation_kind`).

| `kind` | Направление | Пример |
|--------|-------------|--------|
| `documents` | док → код/зона | ADR-0061 описывает `src/.../mcp` |
| `implements` | код → док | Класс реализует контракт из ADR-0008 |
| `constrains` | док → будущие правки | «Публичный API только через X» |
| `discusses` | Intercom → код | Сообщения 3–5 про метод `Foo` ([0137](0137-intercom-message-code-correspondence.md)) |
| `supersedes` | док → док | ADR-0153 отменяет parser shape |
| `related` | код ↔ код | `project_peer`, `test_counterpart` ([0039](0039-workspace-navigation-affordances.md)) |
| `projects` | смысл → файл | Semantic map / CCU → файловая проекция ([0098](0098-semantic-first-document-as-projection.md)) |

**`relates` в Intercom** — частный случай `discusses` + явный event в log, не весь correspondence.

### 3. Слои (кто что делает)

```text
L0  Semantic canon (0098)     — «где правда о смысле»; проекции синхронизируем осознанно
L1′ Feature registry (§7)    — «эта возможность = code scope + docs[]»; приоритет над голым L1
L1  ADR path map (0061)      — fallback: префикс пути → ADR, если файл вне фичи
L2  Code intent graph        — навигация, CF map, related files (0039, 0053, 0065)
L3  Change intent (0042)     — план и preview до apply; семантический diff
L4  Discourse ↔ code (0137)  — обсуждение в Intercom привязано к якорю/диапазону
```

Слои **ортогональны**: один файл может попасть под L1′ (фича), L1 (path map) и L2 (subgraph). **L1′ не заменяет L2** — не дублировать Roslyn-граф в реестре фич.

### 4. Политика реакции (IOP-совместимая)

| Уровень | Кто | Поведение |
|---------|-----|-----------|
| **Inform** | UI (PFD, HUD) | Индикатор, tooltip intent, ссылка на ADR |
| **Advisory** | Агент / trace | «Курс расходится с ADR-NNN — подтверди» ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)) |
| **Review gate** | Человек | Pre-flight approve / reject ([0042](0042-pre-flight-planned-changes-and-review-before-apply.md)) |
| **Verify** (будущее) | CI / анализ | Статические проверки, снапшоты контракта — **не v1** |

**Не-цель v1:** автоматический `build failed` из-за «нарушения ADR» без человека.

### 5. Источники истины (без дублирования)

| Вопрос | Канон | Не дублировать |
|--------|-------|----------------|
| Какая **фича** и какие доки у неё? | Реестр `features.v1.json` ([§7](#adr0155-feature-unit)) | Дублировать scope в ADR-таблицах без CI |
| Какие ADR для пути (fallback)? | `[workspace.adr.map]` в repo TOML ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)) | Второй индекс в коде |
| Какие файлы связаны по коду? | Roslyn + solution ([0039](0039-workspace-navigation-affordances.md)) | Ручной граф в TOML |
| О чём говорили в чате? | Event log + explicit relate ([0137](0137-intercom-message-code-correspondence.md)) | Парсинг prose без якоря |
| Где «я» в методе? | Semantic / CF subgraph ([0053](0053-semantic-map-control-flow-pfd.md)) | Дублировать CFG в Markdown |
| Какие **виды** связи существуют? | Каталог `correspondence-kinds` (JSON + schema), см. [§6](#adr0155-relation-kinds-canon) | Второй enum в TOML / ADR-таблицах без синхронизации с wire |
| Какой смысл у ребра в subgraph? | `relation_kind` ([0114](0114-graph-edge-relation-kind-taxonomy.md)) | Копировать Roslyn-граф в конфиг |

<a id="adr0155-relation-kinds-canon"></a>

### 6. Канон видов отношений (формат и гибрид)

**Решение (направление):** разделить **словарь** (редко меняется, контракт) и **экземпляры** (часто, per-repo).

| Артефакт | Содержимое | Формат |
|----------|------------|--------|
| **Реестр видов** | `id`, семейство (UML-core / cascade-ext), слои L0–L4, `uml`-метка, alias per-layer, краткое описание | **JSON** + **JSON Schema** (CI, codegen) |
| **Экземпляры L1** | `путь → [ADR]` | **TOML** `[workspace.adr.map]` ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md), [0029](0029-configuration-toml-canonical-ui-facade.md)) |
| **Факты L2** | рёбра subgraph | Roslyn / MCP — **не** в конфиге |
| **Факты L4** | явные связи в event log | Wire extensions (см. ниже) |

TOML ([0029](0029-configuration-toml-canonical-ui-facade.md)) остаётся каноном **настроек и привязок**; для **онтологии связей** TOML не обязателен: вложенность, условные поля (`target_kind` → обязательные поля) и синхронизация с wire удобнее в JSON Schema (уже есть в Intercom wire).

#### 6.1. UML-core + cascade-ext

- **UML-core** (для L2, символьный код): `Generalization`, `Realization`, `Dependency`, … — маппинг на `relation_kind` в [0114](0114-graph-edge-relation-kind-taxonomy.md) (`inherits`, `implements`, `references_*`, …).
- **cascade-ext** (L1, L3, L4, governance): `documents`, `discusses`, `constrains`, `supersedes`, `projects` — не притворяются чистым UML; в каталоге поле `family = "cascade-ext"`.

Три оси из [0114](0114-graph-edge-relation-kind-taxonomy.md) **не схлопываются** в один `kind`:

| Ось | Вопрос |
|-----|--------|
| `graph_kind` | В каком доменном графе? ([0065](0065-instrument-categories-domain-taxonomy.md)) |
| `edge_provenance` | Кто посчитал? (Roslyn, HCI, event log) |
| `relation_kind` / канонический `id` | Какой смысл связи? |

Correspondence в смысле 0155 — **надстройка** (роли, зоны, дискурс); для рёбер L2 канонический `id` **согласуется** с `relation_kind`, а не дублирует второй словарь без alias-таблицы.

#### 6.2. Per-layer wire и alias

Слои сохраняют **нативные** имена в API и event log; каталог задаёт **`aliases`**:

| Слой | Нативно (пример) | Канон (пример) |
|------|------------------|----------------|
| L1 | неявно (только path → ADR) | `documents` |
| L2 | `kind_filter`, `related_kind`, `relation_kind` | `related`, `implements`, … |
| L4 | `message_relate`, wire `relation` | `discusses`, `documents`, … |

Агентский фасад (будущий MCP) отдаёт: `canonical_id`, `layer`, `native_kind`, `provenance`.

#### 6.3. Уже существующий wire (не дублировать enum)

Черновики в репозитории — **отдельные** enum до сведения с общим каталогом:

| Файл | Поле | Значения (v1) |
|------|------|----------------|
| [`relates-to-v1.schema.json`](../../wire/intercom-wire/schemas/v1/extensions/relates-to-v1.schema.json) | `relation` | `discusses`, `implements`, `references`, `documents`, `blocks`, `duplicates` |
| [`code-doc-link-v1.schema.json`](../../wire/intercom-wire/schemas/v1/extensions/code-doc-link-v1.schema.json) | `link_kind` | `specifies`, `explains`, `tests`, `deprecates`, `see_also` |

**R2+:** один источник истины — каталог `correspondence-kinds.v1.json` + schema; wire enum — **генерация или `$ref`**, тест «wire ⊆ каталог».

#### 6.4. Альтернатива: TOML-каталог (как `intent-catalog.toml`)

Допустимо для единообразия с [`intent-catalog.toml`](../../IntentMelody/intent-catalog.toml): `[[correspondence_kind]]`, `correspondence_kinds_schema_version`, codegen в C# — **если** есть тест синхронизации с JSON Schema / wire. Без теста — риск двух расходящихся enum.

**Не-цель:** полный граф связей в любом конфиге; Markdown-таблицы в ADR — для людей, не machine canon.

#### 6.5. Предлагаемое размещение (черновик)

```text
wire/correspondence/
  correspondence-kinds.v1.json
  correspondence-kinds.v1.schema.json
```

Оверлей repo/workspace — только **экземпляры** L1 в `workspace.toml`, не переопределение глобального словаря без форка IDE.

<a id="adr0155-feature-unit"></a>

### 7. Feature как единица correspondence

**Направление (Proposed):** поднять уровень абстракции выше префикса пути — **именованная фича** как хаб между кодом и документацией.

#### 7.1. Отличие от feature slice в коде

| Понятие | Где | Назначение |
|---------|-----|------------|
| **Feature slice** ([0006](0006-presentation-layers-and-feature-slices.md)) | `Features/<Имя>/`, неймспейс | Как **организовать** код (MVVM, DAL/CCU) |
| **Feature archetype** ([feature-archetype-v1.md](../design/feature-archetype-v1.md)) | чертёж | DoD при добавлении возможности |
| **Feature (correspondence)** | реестр §7 | **Что** эта возможность охватывает и **какие** доки на неё ссылаются |

Совпадение имени `Features/Chat/` и `feature_id = "intercom.chat"` **желательно**, но не обязательно: реестр может быть **мельче** (slash) или **крупнее** (весь Intercom), чем одна папка.

#### 7.2. Модель записи

```json
{
  "id": "intercom.slash",
  "title": "Slash / unified command line",
  "code_scope": {
    "paths": ["Features/Chat/**", "IntentMelody/intent-catalog.toml"],
    "namespaces": ["CascadeIDE.Features.Chat"],
    "projects": ["CascadeIDE"],
    "layers": ["Features", "IntentMelody"]
  },
  "docs": [
    { "path": "docs/adr/0150-slash-line-canonical-resolution.md" },
    { "path": "docs/adr/0153-slash-catalog-only-resolution.md" },
    { "path": "docs/adr/0154-slash-catalog-domain-object-intent.md" }
  ],
  "relations": [
    { "kind": "depends_on", "target_feature_id": "intent-melody.catalog" }
  ]
}
```

| Поле | Смысл |
|------|--------|
| `id` | Стабильный ключ (`domain.subdomain`, kebab/dot) |
| `code_scope` | Декларативный охват: glob путей, префиксы неймспейсов, проекты solution, горизонтальные слои ([0006](0006-presentation-layers-and-feature-slices.md)) |
| `docs[]` | 0..N: ADR, KB, spec, фрагмент каталога; опционально `anchor` |
| `relations[]` | Между фичами: `depends_on`, `supersedes` — канонический `kind` из [§6](#adr0155-relation-kinds-canon) |

**Инвариант:** `code_scope` — **заявление** для GPWS/advisory/CI; фактические связи символов — по-прежнему L2 (Roslyn).

#### 7.3. Разрешение «в какой фиче я сейчас»

1. Текущий файл (и опционально символ) проверяется против `code_scope` всех фич (специфичность: longest path, явный `primary` при пересечении — см. открытые вопросы).
2. Если совпадений нет — **L1 fallback** ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md)).
3. PFD / advisory: «фича **intercom.slash**» + краткий intent из `docs[]`, не только один ADR по папке.

#### 7.4. Drift на уровне фичи

| Сигнал | Пример |
|--------|--------|
| **Scope drift** | Новый файл в `Features/Chat/`, не попадающий ни в одну фичу |
| **Doc drift** | ADR в `docs[]`, помечен Superseded, а фича не обновлена |
| **Slice drift** | Папка `Features/Foo/` есть, записи `feature_id` нет (обратное) |
| **Overlap** | Файл попал в две фичи без правила `primary` |

Реакция — та же политика §4 (Inform / Advisory), не автоблок сборки в v1.

#### 7.5. Связь с Semantic Map и L2

- Уровень абстракции «фича» на карте намерений ([0039](0039-workspace-navigation-affordances.md)) может **агрегировать** узлы subgraph под `feature_id`.
- `graph_kind` и `relation_kind` ([0065](0065-instrument-categories-domain-taxonomy.md), [0114](0114-graph-edge-relation-kind-taxonomy.md)) остаются осями **рёбер**; фича — **контейнер** correspondence, не замена `graph_kind`.

#### 7.6. Формат и размещение

Рядом с каталогом видов связи ([§6](#adr0155-relation-kinds-canon)):

```text
wire/correspondence/
  correspondence-kinds.v1.json
  features.v1.json
  features.v1.schema.json
```

`workspace.toml` — **оверлей**: дополнительные `docs` для фичи в этом репо, отключение фичи, не форк глобального `features.v1.json` без согласования.

Отдельный ADR (**0156** и далее) — при нормативе MCP `get_feature_context`, CASCOPE, codegen; до тех пор §7 задаёт **направление** в рамках 0155.

---

## Дорожная карта (по слоям)

| Этап | ADR / работа | Критерий готовности |
|------|----------------|---------------------|
| **R0** | Этот ADR **Accepted** | Единая терминология в индексе и policy |
| **R1** | [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) Implemented | TOML map + PFD indicator + тесты матчинга путей |
| **R1b** | Каталог §6 + сведение wire enum | `correspondence-kinds.v1.json`; тест wire ⊆ каталог; alias L4 → канон |
| **R1c** | Реестр фич §7 (черновик) | `features.v1.json` для 2–3 пилотных фич (напр. slash); резолвер file → feature; L1 fallback |
| **R2** | [0137](0137-intercom-message-code-correspondence.md) + [0128](0128-intercom-attachment-anchors-and-code-references.md) | `messages_for_code` / relate стабильны в event log |
| **R3** | [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | Planned Changes ссылается на ADR map + CNC subgraph |
| **R4** | Verify (новый ADR или расширение 0061) | Выбранный минимум статических проверок (API surface, запрещённые зависимости) |

**R4** явно **не блокирует** R1: осведомлённость ценна без полной верификации.

---

## Открытые вопросы

1. ~~Единый реестр `kind` в TOML vs per-layer~~ → **закрыто направлением:** канон JSON + schema; экземпляры L1 — TOML; per-layer wire + alias ([§6](#adr0155-relation-kinds-canon)). Остаётся: **только JSON** vs **TOML-каталог + тест синхронизации**; точный путь `wire/correspondence/`.
2. Нужен ли **front-matter** в ADR (`intent_summary`, `affected_paths`) для автогенерации map.
3. Как **KB/agent-notes** (вне `docs/adr/`) входят в L1 — отдельная секция map или ссылки из ADR.
4. MCP для агента: один tool `get_correspondence_context` vs существующие `get_code_navigation_context` + будущий ADR map.
5. Маппинг `code-doc-link-v1.link_kind` ↔ канонический `id` (отдельная ось doc↔code или подмножество `documents` / `specifies`).
6. **Feature:** гранулярность (`intercom` vs `intercom.slash`); правило **пересечения** `code_scope` (`primary`, теги, `shared`); полуавто из `Features/` vs только ручной реестр.
7. ~~Нужен ли отдельный ADR **0156**~~ → **[0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md)** (CRS + Reverse Anchors / CodeAnchor).

---

## Альтернативы (отклонены)

| Альтернатива | Почему нет |
|--------------|------------|
| Один глобальный граф «всё со всем» | Шум, дорогая поддержка, дублирует Roslyn |
| Только чат `relate` | Не покрывает ADR и архитектурный дрейф |
| Блокировать merge при любом расхождении с ADR | Ложные срабатывания; против IOP (verify, не trust) |
| Вручную вести correspondence только в Wiki | Не в рабочем контексте IDE |
| Онтология связей целиком в `workspace.toml` | Смешивает словарь и факты; плохо для schema/CI; против [0029](0029-configuration-toml-canonical-ui-facade.md) для каталогов |
| Два независимых enum (TOML + wire) без codegen/теста | Drift `relates-to-v1` vs каталог |
| Только L1 path map без фич | Фича размазана по ADR/папкам; GPWS по префиксу шумит на `Services/` |

---

## Последствия

- **Плюс:** одна точка входа для агентов, ревьюеров и дизайна приборов ([0073](0073-pfd-instrument-deck.md)).
- **Плюс:** 0061 и 0137 читаются как **части одной системы**, а не конкурирующие идеи.
- **Плюс:** фича даёт **один** контекст «зачем эти файлы и ADR» для агента и PFD ([§7](#adr0155-feature-unit)).
- **Минус:** нужна дисциплина обновления `[workspace.adr.map]` и реестра фич при переездах каталогов.
- **Риск:** переоценка автоматики до готовности R4 — см. политику **Advisory** по умолчанию.

---

## Статус реализации

**Каркас (этот ADR):** Proposed.  
**Дочерние:** см. таблицы статусов в [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md), [0137](0137-intercom-message-code-correspondence.md), [0098](0098-semantic-first-document-as-projection.md).

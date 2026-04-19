# ADR 0058: Сопряжение агента и Roslyn MCP в `settings.toml` (лимиты, виды узлов, таймауты, пресеты)

**Статус:** Proposed  
**Дата:** 2026-04-18  

**Связь:** [0008](0008-mcp-contracts-and-testable-infrastructure.md) (контракты MCP), [0028](0028-user-settings-toml-localappdata-and-secrets.md) (`settings.toml`), [0039](0039-workspace-navigation-affordances.md) (навигация, пресеты MCP; Semantic Map в UI), [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) (паттерн TOML), [0053](0053-semantic-map-control-flow-pfd.md) (Semantic Map на PFD), [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) (профили, Manager, режимы Auto-Focus / Combat / Echelon, EFB на третьем мониторе — **отдельный ADR**).

---

## Контекст

Агентский слой и **Roslyn** связаны через **MCP-инструменты**. Без явных правил «сколько и чего отдавать» агент перегружает контекст или недополучает структуру. Менять это нужно **в конфиге** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), без обязательной правки C#.

**Интуиция (не норма ADR):** агентский слой — контур запросов, Roslyn MCP — шлюз семантики; настройки ниже задают **объём, фильтр, тайминг**.

**Уже есть (не отменяется):**

- `[semantic_map]` — вид/глубина UI — [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md).
- Пресеты **`get_code_navigation_context`** — [0039 § Agent/MCP](0039-workspace-navigation-affordances.md#adr0039-mcp-workspace-navigation).

Этот ADR — **слой параметров сопряжения агент ↔ Roslyn MCP** в TOML. Поведение **профилей**, **Manager**, тактика/стратегия, третий монитор — **[0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)**.

---

## Решение (принципы)

<a id="adr0058-p1"></a>

### 1. Одна декларативная схема — несколько потребителей

| Категория | Пример смысла | Кто обязан учитывать |
|-----------|----------------|----------------------|
| Лимиты объёма ответа | `max_nodes_per_query`, лимиты глубины обхода | **Roslyn MCP** (или адаптер агрегации графа) |
| Фильтр видов узлов / символов | `included_kinds` / исключения | **Roslyn MCP** |
| Таймауты / «грязная» семантика | ожидание компиляции vs stale graph | **Roslyn MCP** ± **IDE** |
| Пресеты режимов запроса | условные `ExploreMode` / `RefactoringMode` | **Roslyn MCP**; агент может переопределить в вызове |

**Правило приоритета:** аргумент вызова > секция TOML > дефолт сервера — зафиксировать в реализации и [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) при появлении полей.

<a id="adr0058-p2"></a>

### 2. Четыре группы параметров (контрактные оси)

1. **Throttling & scoping** — лимиты объёма и глубины за запрос (`max_nodes_per_query`, `max_recursion_depth` или эквиваленты в спецификации тула).

2. **Маппинг видимости (capabilities)** — какие классы узлов/символов в выдаче.

3. **Таймауты и согласованность** — ждать компиляцию; stale graph; лимиты времени на тяжёлые запросы.

4. **Инструментальные пресеты** — именованные наборы (controlFlow / architecture / обзор vs рефакторинг). Связь с [0039](0039-workspace-navigation-affordances.md): ссылка по имени или ортогональные пресеты — без неявного слияния.

<a id="adr0058-p3"></a>

### 3. Размещение в TOML

Целевая секция, например **`[agent.roslyn_mcp]`**, отдельно от `[semantic_map]` и от **`[[code_navigation.presets]]`** ([0039](0039-workspace-navigation-affordances.md)) без явного маппинга. Точные ключи — после прототипа; здесь зафиксированы **оси**.

<a id="adr0058-p4"></a>

### 4. Версионирование

Опциональные ключи и явные дефолты; смена обязательной формы — bump схемы ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

<a id="adr0058-p5"></a>

### 5. Минимальный v0 vs отложенное

**v0:** лимиты + один-два таймаута + один пресет «обзор vs глубже» без полного `included_kinds`.

**Отложено:** исчерпывающий справочник kinds, сложная recursion, мегаконфиг; **автоматика профилей и режимов** — [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md).

---

## Последствия

- Явный контракт PR: ключ относится к MCP-серверу или IDE.
- Тесты на дефолты и merge приоритетов.

---

## Отклонённые альтернативы

- Только env — хуже воспроизводимости.
- Только системный промпт — нет детерминизма на уровне тула.

---

## Открытые вопросы

- Синхронизация с конфигом **standalone** `roslyn-mcp` вне IDE — одна схема или две.
- Какие тулы в scope первой реализации (navigation / semantic map / оба).

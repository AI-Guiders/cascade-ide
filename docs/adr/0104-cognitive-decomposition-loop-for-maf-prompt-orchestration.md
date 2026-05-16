# ADR 0104: Reasoning Substrate and Cognitive Decomposition Loop for MAF

**Статус:** Proposed  
**Дата:** 2026-05-05

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | Канал → CDS → композитор поверхности → поверхность (Agent-first отображение) |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | CLI для контракта агента (паритет с MCP) и снапшот-тесты |
| [0053](0053-semantic-map-control-flow-pfd.md) | Карта намерений и поток управления на PFD (control flow) |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE DataBus — типизированные события и проекции состояния |
| [0100](0100-project-constitution.md) | Конституция проекта |
## Резюме

- **Reasoning Substrate** для MAF — отдельный слой мышления, не только if-then роутинг.
- Цикл: top-down модель → bottom-up grounding → integration → execution.
- Rule/scorer — нижний компилируемый слой ограничений и fallback.
- Совместимость с KB-Base (integrity, scope, completion discipline).


---

<a id="adr0104-context"></a>

---
## Контекст

Текущий роутинг инструкций решает задачу выбора поведения, но не задает источник мышления. Цепочки `if-then` полезны как исполняемая форма, но это запись уже принятого решения, а не процесс, в котором возникает понимание задачи.

Для длинных и архитектурно сложных задач агенту нужен отдельный когнитивный слой: пространство конкурирующих гипотез, декомпозиция системы, проверка гипотез фактами и управляемая редукция неопределенности.

---

<a id="adr0104-decision-summary"></a>

## Решение в одном предложении

Ввести **Reasoning Substrate** как основной механизм мышления агента, а rule/scorer оставить как нижний компилируемый слой ограничений и fallback-поведения.

---

<a id="adr0104-goals"></a>

## Цели

1. Разделить "мышление" и "маршрутизацию инструкций" как разные уровни.
2. Зафиксировать устойчивый цикл: Top-Down model building + Bottom-Up grounding + Integration + Execution.
3. Снизить контекстный шум: активный фокус только на текущей подсистеме и ее контрактах, остальное хранить как сжатые инварианты.
4. Сохранить совместимость с базовым контрактом KB (**KB-Base**, ранее обозначался как L0: integrity, epistemic distrust, completion discipline, scope clarity).

<a id="adr0104-non-goals"></a>

## Не-цели (на первом этапе)

- Полная автоматическая проверка архитектуры на соответствие ADR.
- Замена всего роутера на semantic scorer.
- Обязательный архитектурный режим для мелких локальных правок.

---

<a id="adr0104-architecture"></a>

## Архитектурная модель

<a id="adr0104-layer-reasoning"></a>

### Уровень A: Reasoning Substrate (источник мышления)

Рабочее пространство, где агент:

- держит несколько интерпретаций задачи;
- выбирает не "самый знакомый шаблон", а шаг с максимальным уменьшением неопределенности;
- различает факт, гипотезу, инвариант и решение;
- ведет локальный фокус по подсистемам и периодически проверяет глобальную согласованность.

<a id="adr0104-layer-policy"></a>

### Уровень B: Compiled Policy Layer (исполняемый контроль)

Rule/scorer слой:

- обеспечивает безопасность, бюджет, deterministic fallback;
- формализует уже принятые эвристики;
- не подменяет Reasoning Substrate как источник решения.

---

<a id="adr0104-cognitive-cycle"></a>

## Когнитивный цикл (обязательная логика)

1. **Top-Down pass (Decompose):**
   - выделить подсистемы/контуры;
   - для каждой подсистемы зафиксировать цель, данные, контракты, риски;
   - выбрать `Focus Subsystem` по ожидаемому снижению неопределенности.

2. **Bottom-Up pass (Ground):**
   - собрать факты по `Focus Subsystem` из кода, tool-output, истории;
   - явно отделить факты от гипотез;
   - обновить карту неопределенности.

3. **Integration pass:**
   - проверить межподсистемные связи и глобальные риски:
   - consistency, privacy/security, performance, coupling, operational constraints.

4. **Execution pass:**
   - выполнить один проверяемый шаг в `Focus Subsystem`;
   - обновить сжатые инварианты и следующий шаг цикла.

---

<a id="adr0104-kb-base-compat"></a>

## KB-Base совместимость (KB baseline)

Когнитивный цикл работает **поверх** KB-Base и не отменяет его:

- integrity/безопасность (необсуждаемые границы),
- epistemic-default-distrust (проверка фактов),
- response-one-step-before-finish (дожим перед финалом),
- scope-disambiguation-all-everywhere (строгая область действия).

Если reasoning и policy расходятся, приоритет у контракта KB-Base.

---

<a id="adr0104-prompt-representation"></a>

## Prompt-представление

- `meta_decomposition_contract` (always-on light): короткий контракт reasoning cycle.
- `architecture_mode_contract` (on-demand): расширенный режим для задач проектирования и декомпозиции.

Примечание: в переходный период эти контракты могут технически доставляться через существующий pack-механизм, но в модели ADR это не "ещё два pack-а", а именованные reasoning-контракты.

<a id="adr0104-architecture-mode-response-shape"></a>

### Обязательная структура ответа в architecture mode

- `System Goal`
- `Subsystem Decomposition`
- `Focus Subsystem (current step)`
- `Interfaces/Dependencies`
- `Next Step`

---

<a id="adr0104-distribution-contract"></a>

## Distribution Contract (KB delivery with CIDE)

Для воспроизводимого поведения reasoning substrate CIDE должен поставляться с минимальным KB-слоем.

**Форматный контракт:** knowledge и prompt-артефакты поставляются как `md`; конфигурация загрузки и приоритизация — `toml-first`.

<a id="adr0104-distribution-required"></a>

### Required artifact (всегда в поставке)

- **KB-Base bundle**:
  - integrity/безопасность;
  - epistemic baseline;
  - completion discipline;
  - scope clarity.

Без KB-Base инсталляция считается деградированной по reasoning-контракту.

<a id="adr0104-distribution-optional"></a>

### Optional artifacts (по требованию)

- Расширенные playbook/knowledge-пакеты (доменные и проектные).
- Локальные или пользовательские knowledge-слои.
- TOML-манифесты подключения/приоритетов (например, секции в `workspace.toml` или отдельный `kb-bundle.toml`).

<a id="adr0104-distribution-offline"></a>

### Offline / degraded behavior

- При отсутствии optional пакетов агент продолжает работу на KB-Base + локальном контексте кода.
- При отсутствии KB-Base normal режим не допускается: только explicit degraded mode с явной диагностикой в trace.

---

<a id="adr0104-kb-base-extended-policy"></a>

## KB-Base vs KB-Extended Policy

<a id="adr0104-kb-base-inclusion-criteria"></a>

### Критерий включения в KB-Base

Артефакт включается в KB-Base, если одновременно выполняются условия:

1. Нужен в большинстве (порядка 60-80%) рабочих сценариев CIDE.
2. Без него поведение агента становится невоспроизводимым или небезопасным.
3. Его нельзя без потери смысла заменить коротким инвариантом в prompt.

Если хотя бы одно условие не выполняется — артефакт относится к KB-Extended.

<a id="adr0104-kb-layering-table"></a>

### Первичное разнесение

| Слой | Назначение | Типичные артефакты |
| --- | --- | --- |
| KB-Base (required) | Базовый контракт поведения и маршрутизации | integrity core/spec, базовый router index, operating principles, ключевые core-playbook'и |
| KB-Extended (optional) | Доменные расширения, глубокие evidence и междоменные матрицы | domain playbook'и, большие evidence-корпуса, узкоспециализированные runbook'и |

<a id="adr0104-kb-operational-rule"></a>

### Operational rule

- В нормальном режиме CIDE всегда загружает KB-Base.
- KB-Extended подмешивается on-demand по задаче/intent.
- KB-Base должен оставаться компактным: при росте пересматривается состав, а не раздувается бесконечно.

<a id="adr0104-kb-include-governance"></a>

### KB-Base include governance checklist

Для каждого изменения `knowledge/kb-base-cide.include`:

1. **Критерий полезности:** файл нужен в большинстве повседневных CIDE-сценариев.
2. **Критерий обязательности:** отсутствие файла ломает воспроизводимость или безопасность поведения.
3. **Критерий недублирования:** смысл нельзя уместить в более короткий инвариант.
4. **Бюджет:** изменение не должно неконтролируемо увеличивать размер KB-Base bundle.
5. **Связность:** в include не добавляются "висячие" ссылки без базового контекста.
6. **Проверка сборки:** после изменения обязателен прогон `pwsh ./scripts/build-kb-base-cide.ps1` в **репозитории канона заметок** (корень задаётся через `AGENT_NOTES_CANON_PATH`, иначе у тебя в хуках дефолт `D:\Experiments\agent-notes` — см. `scripts/git-hooks/pre-commit.ps1` в корне PersonalCursorFolder; не путать с каталогом `open/agent-notes` внутри `financial-open`). Скрипт собирает `dist/kb-base-cide.zip` и копирует в CIDE: переменная **`CASCADE_IDE_ROOT`**, параметр **`-CascadeIdeRoot`**, или соседний `../cascade-ide` от корня канона. **`-SkipPublishToCascadeIde`** — только zip. Резерв копирования: `cascade-ide/tools/publish-kb-base-embed.ps1`.
7. **Проверка режима:** при возможности проверить normal и degraded сценарии загрузки.

---

<a id="adr0104-trace"></a>

## Trace и наблюдаемость

Trace должен показывать не только выбор контрактов/режимов, но и состояние мышления:

- `focus_subsystem`
- `candidate_hypotheses`
- `selected_hypothesis_reason`
- `global_invariants`
- `integration_risks`
- `confidence`
- `policy_fallback_applied` (yes/no)

---

<a id="adr0104-hypothesis-schema"></a>

## Hypothesis Record Schema

Для структурной работы с конкурирующими гипотезами вводится минимальная запись гипотезы:

- `id` — стабильный идентификатор в рамках цикла.
- `statement` — формулировка гипотезы.
- `assumptions` — явные допущения.
- `evidence_for` — подтверждающие сигналы.
- `evidence_against` — опровергающие сигналы.
- `falsifiers` — какие наблюдения/результаты опровергнут гипотезу.
- `confidence` — числовая уверенность (0..1).
- `next_probe` — следующий проверочный шаг.

Правило выбора шага: приоритет у гипотезы, чей `next_probe` даёт наибольшее ожидаемое снижение неопределенности.

<a id="adr0104-trace-json-template"></a>

### Trace JSON template (черновик runtime-представления)

Этот JSON — не формат поставки KB, а внутренний runtime-формат наблюдаемости (trace/DTO).

```json
{
  "focus_subsystem": "messaging",
  "global_invariants": [
    "event-ordering is monotonic per conversation",
    "privacy boundaries between private and group channels"
  ],
  "candidate_hypotheses": [
    {
      "id": "H1",
      "statement": "Duplicate messages caused by non-idempotent consumer retry.",
      "assumptions": [
        "retry policy can replay same event",
        "dedup key is not persisted"
      ],
      "evidence_for": [
        "duplicates appear after retry spikes",
        "same payload hash observed twice"
      ],
      "evidence_against": [
        "no duplicates in low-load path"
      ],
      "falsifiers": [
        "dedup key persisted and checked before write",
        "replay test shows single insert"
      ],
      "confidence": 0.62,
      "next_probe": "Run replay test with forced retry and inspect dedup storage writes."
    }
  ],
  "selected_hypothesis_id": "H1",
  "selected_hypothesis_reason": "Highest expected uncertainty reduction with one bounded experiment.",
  "integration_risks": [
    "cross-service event ordering drift"
  ],
  "policy_fallback_applied": false
}
```

---

<a id="adr0104-storage-model"></a>

## Storage Model (runtime vs knowledge)

Чтобы не смешивать оперативное мышление и долгоживущий канон, вводится разделение:

- **Knowledge (`knowledge/*.md`)**: долговременные правила, playbook'и, evidence и договоренности.
- **Reasoning Memory Block (runtime)**: краткое рабочее состояние текущей задачи в CIDE.

Reasoning Memory Block не является автоматически частью канона KB и не требует записи в `knowledge/` на каждый шаг.

<a id="adr0104-runtime-memory-block"></a>

### Runtime memory block (минимальный состав)

- `session_id`
- `task_id`
- `focus_subsystem`
- `global_invariants[]`
- `candidate_hypotheses[]`
- `selected_hypothesis_id`
- `next_probe`
- `updated_at_utc`

<a id="adr0104-storage-modes"></a>

### Режимы хранения

1. **Phase 1 (MVP):** in-memory + привязка к текущей chat/session state.
2. **Phase 2:** опциональная сериализация в workspace-local state (например, `.cascade-ide/reasoning-state.json`) для восстановления после рестарта.
3. **KB write-back:** в `knowledge/` пишется только устойчивое знание по явному критерию, а не промежуточная оперативная память.

---

<a id="adr0104-consequences"></a>

## Последствия

**Плюсы**

- Меньше имитации мышления через keyword routing.
- Стабильнее решения в длинных и межподсистемных задачах.
- Лучше объяснимость "почему выбран следующий шаг".

**Минусы**

- Выше сложность реализации и валидации.
- Понадобится дисциплина работы с гипотезами и trace.
- Возможен оверхед на мелких задачах (нужен lightweight режим).

---

<a id="adr0104-rollout-plan"></a>

## План внедрения

1. Добавить в `AiPrompts/maf-ide-agent.prompts.md` контракты `meta_decomposition_contract` и `architecture_mode_contract`.
2. Роутер: включать architecture mode по intent "архитектура/декомпозиция/system design", но с сохранением fallback.
3. Расширить trace полями reasoning substrate ([§ Trace и наблюдаемость](#adr0104-trace)).
4. Добавить тесты на:
   - активацию architecture mode,
   - корректный fallback в KB-Base-only,
   - стабильность focus-subsystem при многопроходном цикле.
5. Вторым этапом добавить semantic scorer как сигнал для policy layer, но не как замену reasoning cycle.

---

<a id="adr0104-open-questions"></a>

## Открытые вопросы

- Где хранить сжатые инварианты между шагами: trace-only или отдельный memory block?
- Как формально оценивать "снижение неопределенности" при выборе focus subsystem?
- Нужен ли лимит подсистем в первичной декомпозиции (например, max 5) для контроля контекстного бюджета?
- Где провести границу между "легким режимом" и "architecture mode", чтобы не перегружать простые запросы?

---

<a id="adr0104-implementation-status"></a>

## Статус реализации

**Не начато.** После согласования ADR:

1. Обновить prompt-контракты (`meta_decomposition_contract`, `architecture_mode_contract`).
2. Актуализировать роутер и trace под reasoning substrate.
3. Добавить тесты на новый цикл мышления и fallback-политику.

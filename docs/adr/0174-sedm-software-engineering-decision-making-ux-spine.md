# ADR 0174: SEDM — Software Engineering Decision Making (операционная модель и UX spine)

**Статус:** Proposed  
**Дата:** 2026-07-12

## Резюме

**Semantic Map** и **Correspondence** в CIDE задуманы как ориентация док↔код, но в UX сегодня ближе к **инженерной демке** (subgraph, слои L0–L4, MFD-страница), чем к ответу оператора: «что мы делаем, зачем я в этом файле, чем проверить».

**SEDM (Software Engineering Decision Making)** — именованная **операционная модель** принятия **инженерных** решений в agentic-контуре. От SEDM **выводится UX** (плотность, Surface vs Instrument), а не наоборот.

| Слой | Роль |
|------|------|
| **IOP** [0121](0121-intent-oriented-programming-paradigm.md) | Парадигма: дисциплина коммуникации, намерение, верификация |
| **SEDM** (этот ADR) | Цикл Perceive → Process → Perform → Evaluate; information needs; артефакты T0–T4 |
| **UX / VDS** [cide-vds-v1.md](../design/cide-vds-v1.md) | Производная: какая поверхность в какой фазе |

Эмпирическая база (Tao I-1…I-15, Al Safwan, CRDM, FAA ADM) — KB `kb-cide-sdm-decision-making-research-v1` (agent-notes); research-имя **SDM** сохраняется, продуктовый spine — **SEDM**.

**Принято направление (концепт):**

1. **Фазы SEDM** задают, *какие вопросы* показывать оператору и агенту, не смешивая фазы на одном экране.
2. **Лестница артефактов** T0–T4: реплика → [intent card](0173-intercom-intent-card-session-decision-capture.md) → **context card** → KB → ADR.
3. **Conversation-first** [0172](0172-conversation-first-habitat.md): Perceive + Perform на **Surface** (Intercom, scope strip); SM/CRS — **Process engines** и drill-down, не default.
4. **Context card** (T2) — Perceive-снимок для файла/attach; заменяет «L0–L4 в лицо» как первый экран.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Endsley SA; PFD/MFD; Instrument vs Surface |
| [0039](0039-workspace-navigation-affordances.md) | Semantic Map, `get_code_navigation_context` — Process engine |
| [0053](0053-semantic-map-control-flow-pfd.md) | Control-flow map — engineer reveal |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | L1 path map → Applies one-liner (Perceive) |
| [0105](0105-hybrid-codebase-index-for-csharp-web.md) | HCI orientation — Perceive, не граф |
| [0113](0113-hci-semantic-map-orientation-layer.md) | Граница HCI ≠ SM graph |
| [0121](0121-intent-oriented-programming-paradigm.md) | IOP — парадигма; SEDM — операционная модель под IOP |
| [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) | Perform / Evaluate: verify ladder |
| [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | Correspondence L0–L4 — модель, не operator copy |
| [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) | CRS — drill-down из context card |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Materialization; опционально SEDM phase в system |
| [0170](0170-intercom-feed-readability-mlp.md) | Surface readability — Perceive |
| [0172](0172-conversation-first-habitat.md) | Habitat; scope strip — носитель context card |
| [0173](0173-intercom-intent-card-session-decision-capture.md) | T1 intent card — Process→Perform |

### Вне ADR

| Документ | Роль |
|----------|------|
| [cide-glossary-v1.md](../design/cide-glossary-v1.md) | **Глоссарий** — канон терминов (steer, SEDM, T0–T4, …) |
| [iop-manifest-v1.md](../iop-manifest-v1.md) | IOP публично; SEDM — внутренний spine CIDE (v1) |
| [cide-vds-v1.md](../design/cide-vds-v1.md) | Surface / Chrome / Instrument — привязка к фазам |
| KB `kb-cide-sedm-operational-model-v1` | Операционная модель (зеркало ADR) |
| KB `kb-cide-sdm-decision-making-research-v1` | Исследование information needs |

---

## Контекст

### IOP без операционной модели

IOP [0121](0121-intent-oriented-programming-paradigm.md) задаёт **дисциплину коммуникации** (намерение, дельта, KB). Не фиксирует:

- в **какой момент** оператору нужны alternatives vs blast radius vs tests;
- **где** в UI это живёт при `primary_work_surface = intercom` [0120](0120-primary-work-surface-intercom-or-editor.md);
- как SM, CRS, HCI, intent card **складываются** в один поток, а не конкурируют.

### Симптом: SM / Correspondence как демка

| Наблюдение | Следствие |
|------------|-----------|
| CRS показывает `L1′ · L1 · L2` | Протокол для агента, не история для человека |
| SM на PFD по умолчанию | Instrument в центре при conversation-first |
| Correspondence размазан (PFD, RelatedFiles) | [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) частично чинит; operator path слаб |
| 0061 GPWS | Proposed, отложен — нет «рельефа» one-liner |
| Graphify / рынок | Подтверждают «path/explain», не обязуют копировать стек |

**Гипотеза:** UX выстраивать от **SEDM-фазы** и **information need**, а SM/CRS/HCI — **источники данных** для Process/Perceive, не самоцель.

### SEDM vs SDM (имя)

| | SDM (research) | **SEDM** (продукт) |
|---|----------------|---------------------|
| Охват | software decisions broadly | **engineering** в контуре разработки |
| Носитель | KB research v2 | этот ADR + UX backlog |
| FAA ADM 3P | да | Perceive / Process / Perform + Evaluate |

---

## Решение

<a id="adr0174-phases"></a>

### 1. Фазы SEDM (непрерывный цикл)

```text
PERCEIVE   → обстановка: workline, файл, что применимо (PAVE)
PROCESS    → анализ: alternatives, risk, consistency (CARE)
PERFORM    → действие: steer, код, verify (TEAM)
EVALUATE   → satisficing → снова PERCEIVE
```

| Фаза | Вопрос оператора | Endsley [0021 §11](0021-pfd-mfd-cockpit-attention-model.md) |
|------|------------------|-----------------------------------------------------------|
| **Perceive** | «Что делаем? Где я?» | perception |
| **Process** | «Почему так? Что сломается?» | comprehension |
| **Perform** | «Делаем и проверяем» | projection → action |
| **Evaluate** | «Достаточно хорошо?» | feedback loop |

**Правило UX:** на активной Surface **доминирует одна фаза**; соседняя — одна строка hint, не полный dump.

<a id="adr0174-needs"></a>

### 2. Information needs по фазам (сжатая матрица)

Источник: Tao 2012, Pascarella 2018, Al Safwan 2019 — полная таблица в KB research.

| Need | Perceive | Process | Perform |
|------|:--------:|:-------:|:-------:|
| Outcome / goal | ●●● | | |
| Trigger / pain | ●●● | | |
| Session / file context | ●●● | | |
| Alternatives (N1) | | ●●● | |
| Blast radius (I-9) | | ●●● | |
| Consistency (I-10) | | ●●● | |
| Necessity (N5) | | ●● | |
| Correctness (I-3) | | ● | ●●● |
| Validation / tests | | ● | ●●● |
| Decomposition (N7) | ●● | ● | |

<a id="adr0174-artifacts"></a>

### 3. Лестница артефактов (T0–T4)

| Tier | Артефакт | Горизонт | SEDM |
|------|----------|----------|------|
| **T0** | Сообщение Intercom | turn | Perceive (сырой) |
| **T1** | [Intent card](0173-intercom-intent-card-session-decision-capture.md) | workline | Process → Perform |
| **T2** | **Context card** | файл / attach | **Perceive** |
| **T3** | KB scratch | handoff | Evaluate |
| **T4** | ADR | invariant | кластер T1–T3 |

**Intent card** — сессионное решение (почему B отвергли). **Context card** — «зачем этот файл сейчас» (не ADR, не intent card).

#### 3.1 Context card v1 (schema, целевая)

Проекция в **scope strip** [0172](0172-conversation-first-habitat.md) и/или collapsed system block; источники: active workline, attach anchor, `[workspace.adr.map]` [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md), feature registry [0155](0155-documentation-code-correspondence-and-architectural-drift.md).

```json
{
  "schema_version": 1,
  "anchor": { "path": "Features/…/Foo.cs", "symbol": "optional" },
  "workline": { "id": "wl-…", "label": "habitat G2", "intent_tag": "cascade-ide/habitat" },
  "applies": [
    { "kind": "adr", "ref": "0172", "one_liner": "Session graph habitat", "provenance": "path_map" }
  ],
  "path_hint": "0172 habitat → 0120 intercom → этот файл",
  "risk_advisory": "optional I-9 hint",
  "drill_down": ["crs", "sm_subgraph", "hci_search"]
}
```

**UI (operator, не протокол):**

```text
Here: Foo.cs · workline «habitat G2»
Applies: ADR 0172 — session graph; ADR 0120 — intercom surface
Path: habitat → intercom → Features/…/Foo.cs
        [detail] [SM] [record intent]
```

Слои L0–L4 остаются в **модели** correspondence; в T2 — **human sentences** + `provenance` на строку (аналог EXTRACTED/INFERRED в SEDM vocabulary).

<a id="adr0174-surfaces"></a>

### 4. Поверхности × фазы × VDS

| Поверхность | VDS слой | SEDM (default) | Плотность |
|-------------|----------|----------------|-----------|
| Intercom Forward | Surface | Perceive, Perform | prose, worklines |
| Scope strip + context card | Surface | **Perceive** | one-liner + expand |
| Intent card (timeline) | Surface | Process→Perform | structured card |
| Composer | Surface | Perform | steer, verify CTA |
| Editor reveal | Surface on demand | Perform | diff |
| HCI / INDEX | Chrome | Perceive (orientation) | top-N → Roslyn |
| Semantic Map PFD | Instrument | Process (**engineer**) | subgraph — **reveal** |
| CRS MFD | Chrome | Process (detail) | drill-down from T2 |
| Health / AEE | Instrument | Perform, Evaluate | ladder, deviation only |

**Conversation-first:** moat на **scope strip + context card + intent card** [0172 G2–G4](0172-conversation-first-habitat.md#adr0172-phases), не на hero polish PFD graph.

<a id="adr0174-sm-crs"></a>

### 5. Перепривязка SM и Correspondence

| Было (фактически) | Станет (SEDM) |
|-------------------|---------------|
| SM = визитная карточка кабины | SM = **Process engine**; operator default = context card `path_hint` |
| CRS = первая остановка «док↔код» | CRS = **drill-down** по `[detail]` из T2 |
| `get_code_navigation_context` | Process backend; агент — после Perceive context |
| L0–L4 badges в UI | Модель + tooltip; operator видит **Applies** |
| 0061 индикатор | **Applies one-liner** в scope strip (минимальный GPWS) |

Идеи внешних KG-инструментов (path, explain, provenance tags) — **в vocabulary SEDM**, без обязательной зависимости от сторонних пакетов.

<a id="adr0174-harness"></a>

### 6. Harness и агент (направление)

[0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md): materialization **сжатого** context card + последней intent card активной workline в system one-liner.

Опционально (фаза S3+): `sedm_phase: perceive | process | perform` в meta сессии — bias промпта («сейчас не dump тестов, собери alternatives»).

Query-first (до массового Read): `codebase_index_search`, `get_code_navigation_context` — Perceive/Process **до** file crawl.

---

## Отклонённые альтернативы

| Альтернатива | Почему отклонена |
|--------------|------------------|
| Вложить SEDM целиком в ADR 0121 IOP | Раздувает парадигму; IOP остаётся communication discipline |
| Оставить SM/CRS как primary UX | Не совместимо с conversation-first и слабым operator story |
| Показывать L0–L4 как primary labels | Протокол, не Perceive; подтверждено UX-болью |
| Отдельный «SEDM panel» в MFD | Ещё одна вкладка; T2 в scope strip ближе к 0172 |
| Обязательный внешний knowledge-graph (Graphify) | MIT-идеи допустимы; зависимость — опциональна, не spine |

---

## Последствия

- UX backlog и VDS DoD **привязываются к фазам SEDM**, не к списку приборов.
- SM/CRS/HCI получают роль **data sources** с явной фазой.
- Новая работа: **context card** (T2) — schema, scope strip, drill-down в CRS/SM.
- Intent card [0173](0173-intercom-intent-card-session-decision-capture.md) — T1 в той же лестнице.
- 0061 — приоритизировать **one-liner Applies**, не полный GPWS v2.

---

## Фазы внедрения

| Фаза | Deliverable | Связь |
|------|-------------|-------|
| **S0** | ADR 0174 + KB mirror | spine |
| **S1** | Context card schema + scope strip projection | [0172 G2](0172-conversation-first-habitat.md#adr0172-phases) |
| **S2** | Intent card event | [0173 P1](0173-intercom-intent-card-session-decision-capture.md) |
| **S3** | Applies one-liner из path map | [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) minimal |
| **S4** | SM: operator summary vs engineer subgraph | [0039](0039-workspace-navigation-affordances.md), [0053](0053-semantic-map-control-flow-pfd.md) |
| **S5** | CRS только как drill-down | [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) |
| **S6** | VDS checklist: экран объявляет SEDM-фазу | [cide-vds-v1.md](../design/cide-vds-v1.md) |

**Приоритет moat:** S1–S2 выше S4–S5 (граф polish).

---

## Открытые вопросы

- Связь с EICAS/Health: advisory только в Evaluate/Perform, не в Perceive prose [0021](0021-pfd-mfd-cockpit-attention-model.md).
- Debounce `context_card_materialized` и политика auto-stale decisions — при реализации S1/S2.

**Закрыто (unified model v1.1+, 2026-07-12):** SEDM internal; T2 = **событие** в [0045](0045-agent-chat-persistence-event-log-and-projections.md); `sedm_phase` → протокол маркеров [WORK/HUMAN]-style (KB §11); SM off by default для operator.

---

## Punchline

> **IOP** — дисциплина намерения. **SEDM** — цикл инженерного решения. **UX** — Surface для Perceive/Perform; SM/CRS — Process под капотом. Без SEDM приборы без пилота.

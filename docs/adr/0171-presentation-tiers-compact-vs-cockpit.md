# ADR 0171: Presentation tiers — Compact (standard IDE) vs Cockpit

**Статус:** Accepted · Implemented (P0–P4)  
**Дата:** 2026-07-10

## Резюме

CIDE проектировалась с **трёхмониторным cockpit** `(P)(F)(M)` и **пространственным scan pattern** (P → F → M). На **1–2 обычных мониторах** (16:9, ноутбук) попытка «впихнуть кабину» даёт узкие колонки, ложные PFD/MFD в `MainGrid` и плохой UX — при том же harness под капотом.

**Принято направление:**

1. Два **presentation tier**: **`compact`** (обычная IDE) и **`cockpit`** (полный P/F/M).
2. **1–2 стандартных экрана** → default **`compact`**: не симулировать трёхзонную кабину колонками в одном окне.
3. **`cockpit`** — **3 монитора** или **cockpit-capable** холст (UltraWide и аналоги с достаточной шириной для фиксированных якорей).
4. **Harness** (MCP, Intercom, AEE, FM, KB in-proc) — **одинаковый** в обоих tier; меняется только **presentation policy** и acceptance по scan/checklist.
5. Авиационная **дисциплина внимания** (ANC, dark cockpit для приборов) — в обоих tier; **пространственный scan** и scan-checklist — **только cockpit**.

[0168](0168-presentation-two-screen-pf-m-layout-policy.md) остаётся полезным **переходным** policy для topology `(P+F)(M)`; этот ADR задаёт **продуктовый tier** выше: на `compact` P+F split **не обязателен**.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation`, topology, host windows |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / Forward / MFD, внимание, scan |
| [0046](0046-presentation-layout-authority-and-cockpit-invariants.md) | `CockpitPresentationLayoutPolicy` |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs IDE chrome |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Intercom vs Editor в Forward |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Harness ≠ habitat; economics |
| [0168](0168-presentation-two-screen-pf-m-layout-policy.md) | 2-screen P+F collapsed — interim внутри cockpit-топологии |
| [0170](0170-intercom-feed-readability-mlp.md) | Читаемость ленты — оба tier |

### Вне ADR

| Документ | Роль |
|----------|------|
| [playbook-layout-presentation-intercom-troubleshooting-v1](https://github.com/KarataevDmitry/agent-notes/blob/main/knowledge/work/projects/door-to-singularity/cascade-ide/playbook-layout-presentation-intercom-troubleshooting-v1.md) | Симптомы layout; § tier |
| [kb-aviation-pfd-mfd-efis-eicas-fundamentals-v1](https://github.com/KarataevDmitry/agent-notes/blob/main/knowledge/worlds/aviation-human-factors/kb-aviation-pfd-mfd-efis-eicas-fundamentals-v1.md) | Метафора PFD/MFD |
| Comet note: cascade attention architecture (agent-notes personal) | Scan pattern как инвариант **cockpit** |

---

## Контекст

### 1.1 North-star vs реальность оператора

| Конфигурация | Канон до ADR | Опыт оператора |
|--------------|--------------|----------------|
| **3 × 16:9** | `(P)(F)(M)`, hosts, full-width Intercom на F | Эталон; scan pattern раскрывается |
| **2 × 16:9** | `(P+F)(M)`, [0168](0168-presentation-two-screen-pf-m-layout-policy.md) | P+F делят экран → «полоска», сырой чат на фоне layout |
| **1 × 16:9** | веса в одном `MainGrid` | Симуляция кабины без пространства |

Операторский feedback (2026-07): CIDE **интересна как harness**, но **много сырого** в Intercom и layout; на **2 мониторах** неудобно по сравнению с привычной IDE.

### 1.2 Число кабелей ≠ tier

**UltraWide** (32:9, 49", два логических canvas) — не «один монитор» в смысле compact, если ширина позволяет **стабильные якоря** с предсказуемым scan. Критерий — **cockpit-capable width**, не `Screen.Count == 3`.

### 1.3 Что не меняется

- Intercom wire, attachments, topics, slash, AEE, in-proc MCP.
- `[workspace] primary_work_surface` — в **cockpit** чаще `intercom` в Forward; в **compact** Intercom — **панель** (side/bottom), редактор — центр.
- Авиация как **язык дисциплины** (не смешивать критичное с побочным) — не отменяется.

---

<a id="adr0171-decision"></a>

## Решение

### 2.1 Два tier

| Tier | Когда (default) | UX-обещание |
|------|-----------------|-------------|
| **`compact`** | 1–2 стандартных монитора; ноутбук; узкий 21:9 | **Обычная IDE**: редактор в центре, Explorer / Problems / Terminal в dock, **Intercom как панель** (как Copilot Chat / боковая панель). **Без** обязательных колонок PFD/MFD в main. |
| **`cockpit`** | 3 монитора `(P)(F)(M)`; или `cockpit_capable` UltraWide | Полный **P / F / M**, host windows, **пространственный scan**, semantic map / deck на своих якорях, dark cockpit для приборов. |

### 2.2 Выбор tier

Настройка (целевая):

```toml
[display.presentation]
tier = "auto"   # auto | compact | cockpit

# Пороги для auto → cockpit на одном физическом экране (эвристика, уточняется)
cockpit_min_total_width_px = 4800
cockpit_min_anchor_width_px = 1280
```

**`auto` (MLP):**

| Условие | Tier |
|---------|------|
| `topology` явно `(P)(F)(M)` и ≥3 physical screens | `cockpit` |
| `(P+F)(M)` или 2 screens | `compact` (MFD — dock/host по выбору, не split P+F в main) |
| 1 screen, width &lt; порога | `compact` |
| 1 screen, width ≥ `cockpit_min_total_width_px` и зоны ≥ `cockpit_min_anchor_width_px` | предложить **`cockpit`** (wizard), не silent |

Оператор всегда может **`tier = cockpit`** или **`compact`** вручную.

### 2.3 Compact — поведение (норматив)

| Элемент | Cockpit (было default mindset) | Compact |
|---------|----------------------------------|---------|
| Main `MainGrid` P/M колонки | 220 / 340 при якорях | **0** — нет «фиктивной кабины» |
| PFD semantic map на боковой полосе | default на 3-screen | **нет**; map / SE — **MFD-страница** или палитра (Ctrl+P) [0167](0167-solution-explorer-ux-go-to-file-and-compact-tree.md) |
| Intercom | Forward full-width | **Side panel** или bottom; comfortable feed [0170](0170-intercom-feed-readability-mlp.md) |
| Scan checklist (acceptance) | обязателен для release cockpit | **не применяется** |
| `CockpitPresentationLayoutPolicy` coercion P/M | инварианты якорей | **relaxed** / bypass для compact tier |
| Host windows Pfd/Mfd | типично | опционально; не default при старте |

**Инвариант compact:** пользователь узнаёт раскладку **VS Code / Rider / Cursor** — один главный canvas, панели по краям.

### 2.4 Cockpit — без изменений north-star

- `(P)(F)(M)` на трёх экранах — эталон [0017](0017-multi-window-workspace-and-agent-surfaces.md).
- Scan pattern P → F → M — **design invariant** (Comet note; [0021](0021-pfd-mfd-cockpit-attention-model.md)).
- [0168](0168-presentation-two-screen-pf-m-layout-policy.md): если оператор **явно** держит cockpit tier на 2 экранах — PFD collapsed, SE/map на MFD; это **исключение**, не default path для новых пользователей.

### 2.5 UltraWide

Один кабель, **cockpit-capable**:

- Три **логические зоны** с фиксированными min-width на одном canvas **или** два logical viewport (OS «two displays on one panel»).
- Wizard: «Cockpit layout on ultrawide» vs «Compact».
- Не смешивать с compact только из-за `Screen.Count == 1`.

### 2.6 Связь с harness (0166)

| Плоскость | Compact | Cockpit |
|-----------|---------|---------|
| Model / FM | ✅ | ✅ |
| Tools / MCP | ✅ | ✅ |
| Memory / KB | ✅ | ✅ |
| Verify / AEE | ✅ | ✅ |
| Lifecycle / checkpoint | ✅ (product hooks) | ✅ + scan ritual |

**Anti-pattern:** откладывать читаемость Intercom [0170](0170-intercom-feed-readability-mlp.md) «пока не cockpit» — **compact** как раз **первый** приоритет UX.

---

<a id="adr0171-phases"></a>

## Фазы

| Фаза | Содержание | Критерий |
|------|------------|----------|
| **P0** | `tier` в settings + **compact defaults** для 1–2 screens; скрыть P/M колонки в main при `compact` | Нет «полосы по центру» на 2 mon без ручного cockpit |
| **P1** | First-run wizard: count screens + width → recommend tier | Новый пользователь не попадает в `(0.25P+0.75F)` silent |
| **P2** | Intercom **side panel** layout в compact; editor primary | Паритет ощущений с «обычной IDE» |
| **P3** | UltraWide cockpit zones на одном canvas | Scan checklist green |
| **P4** | Документация + playbook; deprecate «всё через triple» в onboarding | README / samples |

---

<a id="adr0171-non-goals"></a>

## Не цели

- Удаление cockpit tier или ADR 0017/0021.
- Отдельный продукт «CIDE Lite» — один бинарник, два presentation preset.
- Авто-migrate Cursor users в cockpit на ноутбуке.
- Vim/Emacs keymap — [0169](0169-keymap-contributions-and-pluggable-input-schemes.md).

---

<a id="adr0171-consequences"></a>

## Последствия

- `defaults-settings.toml`: при отсутствии явного `topology` для 1–2 screens — **`tier = compact`**, не `(0.25P + 0.75F) (M)` как единственный default.
- `CockpitPresentationLayoutPolicy`: ветка **`compact`** — не применять coercion «нельзя скрыть P/M якорь» [0046](0046-presentation-layout-authority-and-cockpit-invariants.md).
- Playbook layout: симптом «узкая полоса» на 2 mon → проверить **`tier`**; fix = **compact**, не только collapse PFD [0168](0168-presentation-two-screen-pf-m-layout-policy.md).
- Тесты: `PresentationTier.Compact` → `IsPfdColumnVisible == false`, `IsMfdColumnVisible == false` в main at startup.
- Marketing / README: **cockpit** — power-user / 3-screen; **compact** — default для большинства.

---

<a id="adr0171-history"></a>

## История

| Дата | Событие |
|------|---------|
| 2026-07-10 | Proposed: operator feedback — не впихивать cockpit на 1–2 mon; UltraWide — отдельный кейс; harness общий |
| 2026-07-10 | Accepted · Implemented: tier resolver, compact shell, ultrawide layout, first-run wizard, defaults |

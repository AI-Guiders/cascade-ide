# CIDE Visual Design System (VDS) v1

**Статус:** v1 (visual projection of ADR — не дублирует норматив)  
**Дата:** 2026-07-11  
**Аудитория:** дизайнер, UX, разработка (Skia/AXAML)

**Роль:** VDS переводит уже принятые ADR и handbook в **проверяемые визуальные DoD**. Новую продуктовую философию не вводит.

**Старт:** [cide-design-handbook-v1.md](cide-design-handbook-v1.md) · **Референсы (explore):** agent-notes `cide-vds-reference-matrix-v1.md`

---

## Design thesis

Классические IDE заточены под **написание кода**; сопутствующая информация (решения, intent, «почему так») живёт сбоку и читается плохо. В agentic-цикле **авторинг в основном у агента**; человеку остаётся **восприятие, направление и фиксация решений**. Редактор — по reveal, для проверки и точечных правок.

VDS переводит это в визуальные правила: **бюджет полировки на Surface** (Intercom — читать, ориентироваться, ветвить), chrome и редактор — тихие и периферийные, пока оператор не запросил код. Подробнее: [0172](../adr/0172-conversation-first-habitat.md) · KB `kb-cide-brand-positioning-v1.md`.

---

## 1. Принципы (сжатие ADR)

| ID | Инвариант | Смысл | ADR / design |
|----|-----------|-------|--------------|
| **I1** | **Chrome peripheral** | Оболочка IDE не конкурирует с Forward; объёмные/сложные элементы хрома оттягивают внимание | [0021](../adr/0021-pfd-mfd-cockpit-attention-model.md), [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md) |
| **I2** | **Quiet normal** | В норме тишина; salience только при отклонении. Не «новогодняя ёлка» | [flat-chrome-dark-cockpit-v1.md](flat-chrome-dark-cockpit-v1.md), [0064](../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) |
| **I3** | **Surface-first** | Бюджет читаемости и полировки — Intercom (чтение, навигация, обсуждение), не редактор | [0120](../adr/0120-primary-work-surface-intercom-or-editor.md), [0170](../adr/0170-intercom-feed-readability-mlp.md), [0172](../adr/0172-conversation-first-habitat.md) |
| **I4** | **Editor on reveal** | Редактор — инструмент по attach/reveal, не дефолтный центр внимания | [0120](../adr/0120-primary-work-surface-intercom-or-editor.md), [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) |
| **I5** | **Layers don't mix** | Chrome, Surface (Skia), Instrument (deck) — три палитры/ритма; не один «Figma на всё» | [0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md), handbook §8.1 |

**Одной строкой:** тишина в норме; внимание на коммуникацию и картину; хром и код — по запросу.

---

## 2. Три слоя VDS

```text
Surface VDS      Intercom feed, composer, worklines, scope, tree/timeline
                 Skia-first · приоритет полировки · [0117](../adr/0117-ide-skia-kit.md), [0123](../adr/0123-intercom-full-skia-surface-evolution.md)

Chrome VDS       Shell, MFD pages, modals, settings, toolbars
                 Приглушённый · тонкие границы · [ide-chrome-tokens-v1.md](ide-chrome-tokens-v1.md)

Instrument VDS   Deck primitives, health, annunciator, semantic map
                 Salience on deviation · [0064](../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md)
```

| Слой | Код | Когда ярко |
|------|-----|------------|
| Surface | `Views/SkiaKit/`, `Views/Chat/Skia/` | Активная workline, head ветки, открытый batch |
| Chrome | `Views/UiKit/`, `Features/UiChrome/`, `CascadeTheme.*` | Focus ring, modal, редко |
| Instrument | `Cockpit/PrimitivesKit/` | Warning / alert / EICAS |

---

## 3. Visual DoD (проверка глазами)

### 3.1 Quiet normal (I2)

| ✓ Норма | ✗ Нарушение |
|---------|-------------|
| Фон и chrome в одной тёмной семье, низкий контраст между панелями | Glow, градиенты, неон на chrome |
| Один акцентный hue (активная линия / ветка) | Rainbow hero cards, «ёлка» на весь экран |
| Иконки мелкие, приглушённые (sidebar dimmer) | Крупные цветные CTA без причины |
| Power PNG — **alert/demo**, не baseline UI | Сравнение реального UI с Power poster как acceptance |

### 3.2 Surface-first (I3)

| ✓ | ✗ |
|---|---|
| Prose measure cap, comfortable metrics [0170] | Full-width cards без measure |
| Flat feed, role rail, без пузырей | Bubble chat как default |
| Worklines = строки (title + meta) [0172 S3] | Hero cards overview |
| Scope читается без скролла (когда есть data) | Только лента без tree/scope (moat loss) |

### 3.3 Chrome peripheral (I1)

| ✓ | ✗ |
|---|---|
| Forward ~90% conversation-first canvas | SE/Terminal забирают Forward после `load_solution` [0172 S6] |
| MFD — осознанное переключение | Постоянный «второй спектакль» панелей |
| Compact tier — обычная IDE плотность [0171] | Cockpit колонки на 1–2 мониторах без tier |

---

## 4. Anti-patterns (стоп-кран)

Совпадают с [0172 §6](../adr/0172-conversation-first-habitat.md) и handbook:

- Detail = только flat feed (паритет с линейным agent chat)
- Topics = New Chat
- Hero cards на wide canvas
- Glow/неон на chrome как default
- Poster frame (concept PNG) как **единственный** критерий приёмки G1

---

## 5. Референсы Phase 0 (вне репо)

Матрица продуктовых референсов и ссылок на скрины — в KB:

`agent-notes/knowledge/work/projects/door-to-singularity/cascade-ide/cide-vds-reference-matrix-v1.md`

**За I1–I3:** Slack flat density, Linear calm UI, Mattermost scanability.  
**Паттерны, не клон:** Cursor/Windsurf attach, VS Code sessions.  
**Не baseline:** Power concept glow, Windsurf agent-sidebar-only.

---

## 6. Implementation map (куда кодить)

| VDS элемент | Канон кода / токены |
|-------------|---------------------|
| Chrome colors, radii | `CascadeTheme.*`, `Themes/*.json`, `App.axaml` |
| Intercom feed / composer | `SkiaChatTheme`, `SkiaKitPaintTheme`, [0170](../adr/0170-intercom-feed-readability-mlp.md) |
| Section / inset / chip | `Views/UiKit/`, `cascadeSection`, `cascadeInset` |
| Deck lamps / bars | `Cockpit/PrimitivesKit/`, `DeckPrimitiveKind` |
| Habitat wireframes | `cide-session-graph-habitat-concept-v2.png` (north-star), [0172](../adr/0172-conversation-first-habitat.md) ladder G1–G4 |

**Разрыв сегодня:** `concept-to-implementation-map-v1.md` §4.1 — Fluent tree, плотность Power PNG vs runtime. VDS приоритизирует **Surface** над выравниванием всего chrome под poster.

---

## 7. Фазы VDS (после principles)

| Фаза | Deliverable | Связь ADR |
|------|-------------|-----------|
| **V0** | Principles + matrix (этот doc + KB) | — |
| **V1** | Token sheet v2 (type + spacing + Surface mirror) | [0086](../adr/0086-ui-theme-toml-canonical-json-mcp-wire.md) |
| **V2** | 12 named components (message row, workline row, scope strip, …) | [0117](../adr/0117-ide-skia-kit.md) |
| **V3** | 2 эталонных экрана: Intercom comfortable + MFD settings | handbook §8.4 |
| **V4** | Chrome spike (Semi vs Fluent) — один экран | [0171](../adr/0171-presentation-tiers-compact-vs-cockpit.md) |

**Не блокирует moat:** G2–G4 session graph [0172] важнее pixel-polish chrome.

---

## 8. Связанные документы

| Документ | Роль |
|----------|------|
| [cide-design-handbook-v1.md](cide-design-handbook-v1.md) | Принципы и маршрут дизайнера |
| [ide-chrome-tokens-v1.md](ide-chrome-tokens-v1.md) | Chrome tokens v1 |
| [intercom-design-hub-v1.md](intercom-design-hub-v1.md) | Intercom D1–D9 |
| [concept-to-implementation-map-v1.md](../ui-ux/concept-to-implementation-map-v1.md) | Концепт PNG vs XAML |
| [0172](../adr/0172-conversation-first-habitat.md) | Habitat north-star + implementation ladder |

---

*VDS v1 — projection layer. Норматив поведения остаётся в ADR; при споре с инженерами — ADR wins.*

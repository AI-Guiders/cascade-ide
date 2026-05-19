# Intercom — design hub v1

**Статус:** v1 hub для дизайнеров (не ADR)  
**Аудитория:** UX, продукт, визуал; разработка — для согласования границ доменов.  
**Дата:** 2026-05-19

**Родитель:** [CIDE Design Handbook §5.2](cide-design-handbook-v1.md#52-intercom-канал-темы-composer) · **Каталог design:** [README](README.md)

Intercom пересекает **много доменов** (лента, composer, slash, вложения, редактор, команды, Skia, навигация по темам). Один «чатовый» макет не покрывает картину. Этот хаб задаёт **иерархию документов**, **порядок чтения** и **каталог референс-картинок** (что нарисовать и куда положить).

!!! tip "Сначала прочитай"
    1. [intercom-ux-reference-slack-mattermost-v1.md](intercom-ux-reference-slack-mattermost-v1.md) — продуктовые границы и паттерны Slack/MM.  
    2. Handbook [§2.6](cide-design-handbook-v1.md#26-команды-три-входа-репетиция-выступление-канал) (три входа) и [§2.7](cide-design-handbook-v1.md#27-анти-паттерны-сводка-чего-не-рисуем) (анти-паттерны).  
    3. По домену ниже — макет + ADR только если споришь с инженерами.

---

## Зачем отдельный хаб (и почему «мелочи» важны)

Один и тот же якорь на файл может означать **разное намерение**:

| Намерение | Поверхность | Эффект в IDE |
|-----------|-------------|--------------|
| Payload агенту | `/attach selection`, `[M:…]`, реже path/lines | Смысл в UI; path+lines — resolve @ send в wire |
| Действие в редакторе | `/editor line …` | Select/delete в буфере |
| Открыть файл | `/file open` | Вкладка, без attach |
| Посмотреть контекст | Клик по chip в **ленте** | Open + scroll + **рамка** (не selection по умолчанию) |
| Править фрагмент | Shift+клик / настройка | Selection в редакторе |

Если смешать их в одном affordance, ломаются и UX (случайное удаление), и контракт команд, и MCP. Дизайн здесь — **развести намерения визуально и в copy**, не только в коде.

**Проектирование до макетов:** сложные домены (D4 attach, D6 клик по chip) выгодно **долго проговаривать** с агентом в том же IOP-контуре — снять ветвления, оформить ADR/playbook, потом рисовать PNG и кодировать MVP. Агент — собеседник и спарринг-партнёр, не замена ревью людей — [cascadeide-philosophy-v1.md §8](cascadeide-philosophy-v1.md#8-агент-как-партнёр-для-проектирования-до-кода).

---

## Карта документов (иерархия)

```text
docs/design/
├── cide-design-handbook-v1.md          ← принципы кокпита, §2.6 три входа, §5.2 вход в Intercom
├── intercom-design-hub-v1.md           ← ВЫ ЗДЕСЬ: домены, макеты, порядок работы
├── intercom-ux-reference-slack-mattermost-v1.md   ← норматив UX-направления (черновик)
├── ide-chrome-tokens-v1.md             ← токены оболочки (AXAML chrome вокруг Skia)
└── cascadeide-philosophy-v1.md         ← зачем Intercom = канал сессии

docs/ui-ux/
├── cascade-ide-ui-layout-v1.md         ← зоны PFD/Forward/MFD, имена контролов
└── concept-screens/intercom/           ← референс-PNG (см. § «Макеты»)

docs/adr/  (норматив для разработки — дизайнеру по необходимости)
├── 0080  naming, multi-party, deep links (future)
├── 0072  topic cards, overview/detail
├── 0119  slash + autocomplete в composer
├── 0120  Forward = редактор | Intercom
├── 0123–0127  Skia chrome, spine, tabs, navigator
├── 0124–0125  editor line, file open (≠ attach)
├── 0116, 0045, 0096  сессия, event log, spine
├── 0128-intercom-attachment-anchors-and-code-references.md  ← норматив attach
└── intercom-ux-reference-slack-mattermost-v1.md             ← playbook Slack/MM + attach UX
```

---

## Домены Intercom (что проектировать отдельно)

Каждый домен — **отдельный макет / flow**; не склеивать в один экран «как Telegram».

| # | Домен | Вопрос дизайна | Продукт / playbook | Норматив (ADR) | Референс-макет |
|---|--------|----------------|-------------------|----------------|----------------|
| **D1** | **Chrome & навигация** | Spine, вкладки тем, Navigator, back из detail | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) §навигация | [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0127](../adr/0127-intercom-spine-and-topic-tabs-chrome-navigation.md) Proposed | `intercom-spine-tabs-navigator.png` |
| **D2** | **Лента (feed)** | Flat feed, роли human/agent/system, без balloon | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) §лента | [0057](../adr/0057-chat-surface-pipeline-adoption.md), [0123](../adr/0123-intercom-full-skia-surface-evolution.md) | `intercom-feed-flat-roles.png` |
| **D3** | **Composer** | Поле внизу, текст vs `/`, подсказки | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) §composer | [0119](../adr/0119-chat-slash-commands-intercom-surface.md) | `intercom-composer-slash-popup.png` |
| **D4** | **Вложения (attach)** | **Selection / `[M:…]` first**; path+lines — производные; whole-file для медиа | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) · [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) Proposed | [0124](../adr/0124-slash-parametric-editor-line-commands.md), [0125](../adr/0125-slash-workspace-file-commands-and-dynamic-completion.md), [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md) | `intercom-composer-chips-mid-sentence.png` |
| **D5** | **@ vs `[` vs slash** | `@` — люди; `[path]` и `/attach` — артефакты; не `@cc` | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) §упоминания | [0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) | `intercom-mention-vs-attach.png` |
| **D6** | **Мост в редактор** | Клик по chip: open + scroll + **рамка**; Shift → select | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) §клик | [0080 future](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities) | `intercom-click-reveal-frame-vs-select.png` |
| **D7** | **Три входа команд** | Палитра / Chord / slash — не дублировать мнемоники | [handbook §2.6](cide-design-handbook-v1.md#26-команды-три-входа-репетиция-выступление-канал) | [0013](../adr/0013-command-surface-and-discoverability.md), [0060](../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md) | `intercom-three-surfaces-help.png` |
| **D8** | **Токены & Skia** | Типографика ленты, chip, status; не сырые hex | [ide-chrome-tokens-v1.md](ide-chrome-tokens-v1.md) | [0064](../adr/0064-deck-primitives-visual-language-render-layer-and-palette.md) | опираться на токены + D2–D4 |
| **D9** | **Forward fullscreen** | Редактор **или** Intercom на всю колонку | [handbook §3](cide-design-handbook-v1.md#3-зоны-внимания-pfd--forward--mfd) | [0120](../adr/0120-primary-work-surface-intercom-or-editor.md) Proposed | `intercom-forward-fullscreen.png` |
| **B** | **Внешний контур** | Slack/MM как бэкенд команды — не v1 IDE UI | [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md) слой B | [0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5) | вне scope макетов IDE v1 |

**Сквозной принцип (для всех доменов):** [Dark cockpit](cide-design-handbook-v1.md#25-dark-cockpit-и-тревога-по-делу) — акцент редко; рамка на сообщении только для ошибки / блокера.

---

## Макеты и референс-картинки

**Каталог файлов:** [`docs/ui-ux/concept-screens/intercom/`](../ui-ux/concept-screens/intercom/README.md)

PNG — **не** скриншоты билда без пометки; в имени или README указывать **target state** и домен (D1–D9).

### Чеклист артефактов v1 (что нарисовать)

| Приоритет | Файл (план) | Домен | Содержание кадра |
|-----------|-------------|--------|------------------|
| P0 | `intercom-feed-flat-roles.png` | D2 | 3–4 строки: human, agent, system; **без** пузырей; компактная system-строка |
| P0 | `intercom-composer-chips-mid-sentence.png` | D4 | Текст с **двумя chip** в середине фразы + slash popup `/attach file` |
| P0 | `intercom-click-reveal-frame-vs-select.png` | D6 | Split или два состояния: (a) рамка/gutter band, (b) Shift → selection |
| P1 | `intercom-composer-slash-popup.png` | D3 | `/` → иерархия; пример `/attach` vs `/file open` |
| P1 | `intercom-spine-tabs-navigator.png` | D1 | Spine + вкладки + navigator (target [0127](../adr/0127-intercom-spine-and-topic-tabs-chrome-navigation.md)) |
| P1 | `intercom-mention-vs-attach.png` | D5 | `@ivan` autocomplete **люди**; рядом copy «файл → /attach» |
| P2 | `intercom-forward-fullscreen.png` | D9 | Intercom на всю Forward vs редактор (переключение) |
| P2 | `intercom-three-surfaces-help.png` | D7 | Схема палитра / Chord / slash (можно diagram + мини UI) |

### Связь с существующими скринами

Общий каталог concept-screens: [`../ui-ux/concept-screens/README.md`](../ui-ux/concept-screens/README.md). Старые «чат с пузырями» **не** использовать как эталон — см. [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md).

---

## Порядок работы дизайнера (итерации)

| Этап | Домены | Результат |
|------|--------|-----------|
| **1. Словарь** | D5, D7 | Таблица «что пользователь думает» → какой вход; согласование с handbook §2 |
| **2. Лента + composer** | D2, D3 | Flat feed + slash popup; статусы system |
| **3. Attach + editor bridge** | D4, D6 | Chips, формы вложения, клик → рамка |
| **4. Навигация тем** | D1, D9 | Spine/tabs; fullscreen Forward |
| **5. Polish** | D8 | Токены, плотность, a11y фокуса |

Параллельно с инженерами: не дублировать ADR в Figma — в макете **имена токенов** и ссылка на домен (D4 и т.д.).

---

## Мини-глоссарий (только Intercom)

| Термин | Кратко |
|--------|--------|
| **Chip (вложение)** | Inline-токен в composer/ленте; хранит anchor (path, lines, shape) |
| **attachmentShape** | `whole-file` \| `text-range` \| `selection` |
| **Flat feed** | Строка = имя + время + текст; не messenger balloon |
| **Reveal (из ленты)** | Навигация к якорю с **рамкой**, без selection |
| **Slash namespace** | `/attach`, `/file`, `/editor`, `/build` — разные эффекты |
| **Spine** | Линия продуктовых тем «над чем работаем» |

Полный глоссарий кокпита: [handbook §7](cide-design-handbook-v1.md#7-глоссарий-для-общего-языка-с-командой).

---

## Открытые продуктовые вопросы (трекинг)

Сводка из [intercom-ux-reference](intercom-ux-reference-slack-mattermost-v1.md#открытые-вопросы-для-обсуждения) — дизайнер может вести решения здесь краткой таблицей «решение / дата»:

- Реализация фаз [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) (schema 0045, reveal adornment)  
- Нужен ли `@file` inline после прототипа `/attach` и `[M:…]`  
- Sidebar vs overview при [0120](../adr/0120-primary-work-surface-intercom-or-editor.md)

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | v1 hub: домены D1–D9, иерархия доков, чеклист PNG |
| 2026-05-19 | Ссылка на [ADR 0128](../adr/0128-intercom-attachment-anchors-and-code-references.md). |

*Предложения по структуре — PR в `docs/design/intercom-design-hub-v1.md`.*

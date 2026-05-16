# ADR 0075: Тематический указатель UI (`docs/adr/UI/`) и соглашения по страницам MFD

**Статус:** Proposed  
**Дата:** 2026-04-20  

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0076](0076-ui-ux-principles-hub.md) | центр UI/UX-принципов; связный вводный текст |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | вторичный контур / MFD |
| [0068](0068-deck-row-payload-and-presentation-projection.md) | payload vs проекция |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | keyboard-first, Command Melody |
| [0074](0074-settings-ui-mfd-compact-layout-overflow.md) | плотность и место в MFD |
| [0013](0013-command-surface-and-discoverability.md) | палитра |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id` |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | картотека тем в Intercom, сводка на карточке |
| [0077](0077-tech-principles-hub.md) | TECH — центр принципов (связный текст из канона) |

### Вне ADR

| Документ | Роль |
|----------|------|
| [`TECH/README.md`](TECH/README.md) | `TECH/README.md` |
---

## Контекст

Обсуждения **визуальной подачи** (список vs таблица, скролл, подсказки) без зафиксированного эталона в репозитории дают **трение**: восстановление «как было» по памяти, разные трактовки терминов (в т.ч. продуктовых vs ADR), спор о приоритетах **pointer** vs **keyboard**.

При этом в репо уже есть норматив **payload vs presentation** ([0068](0068-deck-row-payload-and-presentation-projection.md)) и **keyboard-first** входы ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)).

---

<a id="adr0075-p1"></a>

## 1. Решение: папка `docs/adr/UI/`

1. Ввести **тематический указатель** [`docs/adr/UI/README.md`](UI/README.md): таблица ссылок на существующие ADR по теме UI/MFD/chromium/палитра.  
2. **Не** вводить отдельную нумерацию и **не** дублировать текст нормативных ADR внутри `UI/` — канон по-прежнему файлы `docs/adr/NNNN-*.md` и главный [индекс](README.md). Навигационная **карта принципов** (куда смотреть за полным текстом) — [`UI/principles.md`](UI/principles.md).  
3. Подпапка `UI/` — **не** классификация по статусу ADR (см. соглашения в [README § «Соглашения»](README.md)); только удобная навигация по теме.

---

<a id="adr0075-p2"></a>

## 2. Соглашения по страницам вторичного контура (MFD)

Нормативно опираться на:

| Принцип | Источник |
|--------|----------|
| Один **payload** строк (порядок, `command_id`/DTO) меняется в VM/сервисе; **проекция** (карточки, таблица, плотность) — во View без смены семантики строк | [0068](0068-deck-row-payload-and-presentation-projection.md) |
| **Keyboard-first:** смысл команды доступен через палитру / Melody (`c:`) / Chords с тем же `command_id`; **hover-only** не считать единственным каналом для обязательного смысла | [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md), [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) где уместно |
| Узкий **MFD** — ограничение по ширине/высоте; стратегии scroll / компактность / fallback — согласовать с направлением [0074](0074-settings-ui-mfd-compact-layout-overflow.md) для форм и аналогично для «длинных» списков |

**Пример (иллюстрация, не отдельный канон):** страница «готовность окружения» — коллекция `AnnunciatorLampItem`; переключение «компактный / широкий» — только **раскладка** (`EnvironmentReadinessPresentationResolver`), порядок строк — **payload**.

---

<a id="adr0075-p3"></a>

## 3. Последствия

- Агенты и люди могут ссылаться **«см. ADR/UI»** как на [`docs/adr/UI/README.md`](UI/README.md), не смешивая с отменой плоского индекса.  
- Спорные UI-решения по продукту по-прежнему оформляются **отдельным пронумерованным ADR** или правкой существующего, а не только заметкой в `UI/`.

---

## 4. Отклонённые альтернативы

- **Только wiki / только чат** — без ссылки из репо теряется воспроизводимость.  
- **Все UI-ADR только в подпапке `UI/`** — ломает действующее соглашение об именах `NNNN-*.md` в одном каталоге и усложняет поиск по номеру.

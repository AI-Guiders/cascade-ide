# ADR 0133: Commander cockpit — общая модель внимания и instrument deck по роли

**Статус:** Proposed  
**Дата:** 2026-05-19

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | Канон зон: Forward, Pfd, Mfd, Eicas, Hud — **семантика не меняется** |
| [0025](0025-sdk-attention-zones-and-capabilities.md) | `AttentionZoneCanonicalIds` в контрактах; capabilities ↔ зона |
| [0073](0073-pfd-instrument-deck.md) | Каталог приборов; здесь — **deck по роли** Commander |
| [0010](0010-ui-modes-toml-configuration.md) | Пресеты overlay: `pilot` vs `commander` без подмены семантики зон |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Forward = редактор **или** Intercom |
| [0132](0132-intercom-federated-transport-and-multi-client-boundary.md) | Transport, multi-client, паритет ролей; MCC ≠ message store |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom как командный канал |
| [0121](0121-intent-oriented-programming-paradigm.md) | IOP: намерение, верификация, наблюдаемая дельта |

### Вне ADR (playbook / экосистема)

| Документ | Роль |
|----------|------|
| [cide-design-handbook-v1.md](../design/cide-design-handbook-v1.md) | §2.2 иерархия внимания; дизайн-критерии |
| [iop-manifest-v1.md](../iop-manifest-v1.md) | IOP use case; среда команды |
| Mission Control Center (project card) | Web-реализация **Check / Control** (и Companion) — не primary workplace |

## Резюме

**Два яруса продуктов** (нормативно):

| Ярус | Поверхность | Назначение |
|------|-------------|------------|
| **Primary workplace** | **Cascade IDE** (Avalonia) | Pilot и **Commander** — полный кокпит: зоны внимания [0021](0021-pfd-mfd-cockpit-attention-model.md), мультимонитор [0017](0017-multi-window-workspace-and-agent-surfaces.md), in-proc MCP, редактор / reveal |
| **Companion tier (web)** | PWA / браузер | **Companion · Check · Control** — спутник, не замена IDE; тот же Intercom transport [0132](0132-intercom-federated-transport-and-multi-client-boundary.md) |

**Commander** — роль (Lead / PO / QA). **Основное рабочее место Commander — CIDE** (preset `commander`). **Веб** — участие в канале и обзор миссии без претензии на полный кокпит.

| Роль | Primary (CIDE) | Web (3C) |
|------|----------------|----------|
| **Pilot** | Редактор, Roslyn, полный deck | Companion: Intercom read/лёгкий compose; deep link |
| **Commander** | Intercom-first Forward, mission deck, presentation | **Check** + **Control** (+ Companion Intercom); trace, verify, digest |

| Зона (семантика [0021](0021-pfd-mfd-cockpit-attention-model.md)) | Commander **в CIDE** | Web (упрощённо, один viewport) |
|------|----------------------|--------------------------------|
| **Forward** | Intercom (полный) | **Companion** — compose, агент, slash subset |
| **Pfd** | Mission trace, requirements | **Control** — обзор, цели, heatmap |
| **Mfd** | git, KB, digests | **Check** — verify ADR↔код, статус, ссылки в CIDE |

**Принято направление (Proposed):**

1. **Идентификаторы зон** — общий канон (`forward`, `pfd`, `mfd`, …) для SDK и UX-языка ([0025](0025-sdk-attention-zones-and-capabilities.md)); **полный deck и топология** — только primary (CIDE).
2. **Паритет в Intercom** — Lead **не** второго сорта: тот же transport, compose и агент доступны в **Companion** (web) и в **Commander CIDE**; отсутствие мультимонитора на web **не** лишает голоса в канале.
3. **MCC / team-console** — продуктовое имя **Control · Check** (и модуль Companion), **не** «веб-Commander как главный стол».
4. **MVP web** может начать с read-only Check/Control; расширение Companion (compose) — по transport [0132](0132-intercom-federated-transport-and-multi-client-boundary.md), без цели заменить CIDE.

---

## Контекст

В [0132](0132-intercom-federated-transport-and-multi-client-boundary.md) зафиксированы transport и паритет ролей: Lead не должен жить в «втором контуре» (CIDE у dev + MCC без голоса). Отдельно обсуждалось: у CIDE сильны **архитектура внимания**, кокпит, Intercom как центр сессии — это **переносимо** на командира, а не только на разработчика с редактором.

Риск без этого ADR: MCC остаётся **плоским дашбордом** (таблицы, графы), а Lead для агента и slash уходит в **третий продукт**; модель внимания IOP не масштабируется на команду.

---

## Проблема

1. **Разрыв метафор:** dev думает кокпитом (Forward/PFD/MFD); Lead — «открой MCC в браузере» без той же дисциплины внимания.
2. **Дублирование продуктов:** Intercom Web + MCC + опционально CIDE = три UX без общего deck contract.
3. **Недоиспользование SDK:** зоны в [0025](0025-sdk-attention-zones-and-capabilities.md) не связывают CIDE и web Commander явной **ролью** и каталогом приборов.

---

## Решение

### 1. Commander — именованная роль, не «урезанный IDE»

**Commander** — оператор с **полным паритетом** в Intercom (обсуждение, `@`, slash subset, агент как партнёр) и **частичным** паритетом по коду (attach excerpt, трассировка, deep link в CIDE для reveal/commit).

Не путать с:

- **Observer** — read-mostly, без обязательного compose;
- **Pilot** — полный редактор и in-proc MCP.

Роль задаёт **default preset** и **instrument deck**, не ACL transport (ACL — в [0132](0132-intercom-federated-transport-and-multi-client-boundary.md)).

### 2. Instrument deck Commander (нормативный черновик)

Семантика зон — [0021](0021-pfd-mfd-cockpit-attention-model.md). Ниже — **продуктовое** наполнение v0→v1; детали UI — handbook + [0073](0073-pfd-instrument-deck.md).

| Зона | Вопрос (0021) | Приборы Commander (кандидаты) |
|------|---------------|--------------------------------|
| **Forward** | Что делаю **сейчас**? | **Intercom composer** + topic tabs; агент в канале; slash subset; attach chips |
| **Pfd** | **Где** в миссии? | Mission board; trace graph «требование → ADR → ветка → файлы»; ADR/kb indicator ([0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) — кандидат) |
| **Mfd** | Что **вторично**? | Git overview; hybrid search; KB router (read); «open in CIDE»; дайджест SA |
| **Eicas** | Что **тревожит**? | Пробелы трассировки; drift канон/код; failed checks (не toast-шторм) |
| **Hud** | Контекст **на лобовом** | Активная цель релиза / requirement id; краткий статус transport sync |

**Pilot deck** в CIDE не меняется; пресет `commander` в CIDE — **переключение deck** при тех же zone ids ([0010](0010-ui-modes-toml-configuration.md)).

### 3. Архитектура поверхностей

```text
                    ┌─────────────────────────────────────┐
                    │   Shared: zone ids + IOP + transport   │
                    └──────────────────┬──────────────────┘
         ┌───────────────────────────┼───────────────────────────┐
         ▼                           ▼                           ▼
  CIDE preset                  team-console / MCC              (опц.) CIDE
  `commander`                  PWA Commander layout            pilot preset
  Avalonia render              web render                      для dev
         │                           │
         └──────── Forward = Intercom (compose) ──────────────┘
                         same event log [0132]
```

- **Контракты** (`CascadeIDE.Contracts`): роль `commander` в metadata capability/deck descriptor (фаза B — вместе с [0025](0025-sdk-attention-zones-and-capabilities.md) фаза C).
- **Рендер:** CIDE (Avalonia) и MCC (web) — **разные** host; **общая** семантика зон и список instrument id.

### 3.1 Презентация: web vs CIDE (мультимонитор)

**Наблюдение (продуктовое):** на **вебе** (PWA, SPA) **нельзя** в общем случае обещать ту же **мультиоконность по мониторам**, что в CIDE ([0017](0017-multi-window-workspace-and-agent-surfaces.md): `TopLevel`, `presentation`, `MfdHostWindow` / `PfdHostWindow`, `display.screens`). Браузер — по сути **один viewport** на вкладку; `window.open` / Picture-in-Picture — костыли без стабильного плейсмента на второй монитор и без паритета с `presentation`-грамматикой.

| Возможность | CIDE (Avalonia) | MCC / team-console (web) |
|-------------|-----------------|---------------------------|
| Зоны PFD / Forward / MFD **в одном** окне | да | да (split / tabs / responsive layout) |
| **Разнести зоны по мониторам** (`(P+F)(M)` и т.д.) | да ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) | **не v0**; не норма для PWA |
| Сохранение геометрии окон на дисплеях | `settings.toml` / presentation | ограничено браузером |
| Commander с **двумя физическими экранами** | preset `commander` + presentation | **рекомендация:** CIDE или второй браузер вручную — не «из коробки» |

**Нормативно для 0133:**

1. **Семантика зон** (forward, pfd, mfd) — **общая**; **топология экранов** — **не общая** между CIDE и web.
2. **team-console** — имя **единого web-shell** (одна вкладка: Forward + Mission), **не** обещание multi-monitor cockpit. Альтернатива: две вкладки («Intercom» / «Mission») на одном мониторе — хуже, но честнее, чем симуляция 0017 в браузере.
3. **Commander на двух мониторах** (Intercom на одном, trace на другом) — **out of scope** для MCC PWA v0–v1; **in scope** для CIDE preset `commander` + [0017](0017-multi-window-workspace-and-agent-surfaces.md). Remote/phone — см. [0117](0117-remote-operator-surface-multidevice.md) (другая ось).
4. Web Commander фокусируется на **плотности одного экрана** (handbook §2.2: иерархия внутри viewport) и **deep link** в CIDE, когда нужен reveal или вторая физическая поверхность.

**Anti-pattern:** проектировать MCC как «веб-клон 0017» с обязательным выносом MFD на второй монитор.

### 3.2 Web = Companion · Check · Control (не primary workplace)

**Позиция продукта:** браузер/PWA — **спутниковый ярус** IOP-контура. Пользователь **не** должен вынужденно жить в вебе так же, как пилот или командир в **основном** кокпите CIDE.

| Режим (3C) | Вопрос | Типичные приборы | Не обещаем |
|------------|--------|------------------|------------|
| **Companion** | «Я в контексте команды?» | Intercom лента, compose, агент, `@`, slash subset, push | Полный editor, reveal in-proc |
| **Check** | «Верно ли связано и согласовано?» | Trace ADR↔код, attach excerpt, diff summary, QA slash | Commit, правка канона |
| **Control** | «Как идёт миссия?» | Mission board, heatmap, digests, EICAS-style gaps | Мультимонитор, полный instrument deck |

**Связь с ролями:**

- **Commander** в **CIDE** — primary: все зоны + [0017](0017-multi-window-workspace-and-agent-surfaces.md).
- **Commander** на **web** — 3C: достаточно для совещания, телефона, QA-проверки; тяжёлая сессия → **открыть CIDE** (deep link, preset `commander`).
- **Pilot** на web — в основном **Companion** (не отрывать от треда); код — в CIDE.

**MCC (project-id):** маркетинговое имя **Mission Control**; архитектурно — реализация **Control + Check** (и при необходимости Companion) в одном PWA, не дублирующая CIDE.

**Anti-patterns:**

- «Lead работает только в браузере» как **целевой** steady state для Commander.
- «MCC = Commander cockpit» без оговорки **primary = CIDE**.
- Три отдельных web-продукта на каждую C — **не** требуется; достаточно **режимов/вкладок** в одном shell.

### 4. Связь с [0132](0132-intercom-federated-transport-and-multi-client-boundary.md)

| Тема | 0132 | 0133 |
|------|------|------|
| Event log, ACL, wire attach | **Да** | Ссылается |
| `clientKind` telemetry | cide / web / mcc / agent | **`rolePreset`** + **`workplaceTier`**: `primary` \| `companion` |
| MCC read-only в MVP | Допустимо как **фаза** | Check/Control first; Companion compose по мере transport |
| Intercom Web | Transport client | **Companion**-модуль; не primary workplace |

**Anti-pattern (повтор для связки):** MCC с собственным чатом или compose только «в другом приложении» без Forward Commander в той же оболочке.

### 5. Capability matrix (Commander)

| Capability | Commander **CIDE** (primary) | Web **3C** |
|------------|------------------------------|------------|
| compose в Intercom | да (Forward) | да (**Companion**; BFF) |
| slash | полный / subset | subset |
| agent | in-proc MCP | BFF + policy |
| attach re-resolve | в редакторе | excerpt + deep link → CIDE |
| git/kb write | по политике pilot | **нет** (Check/Control) |
| mission trace / heatmap | PFD/MFD full | **Control / Check** |
| multi-monitor presentation | да [0017](0017-multi-window-workspace-and-agent-surfaces.md) | **нет** |

---

## Последствия

### Положительные

- Lead и dev делят **одну ментальную модель** кокпита; onboarding IOP проще.
- MCC получает продуктовую форму, а не «ещё один портал».
- Instrument deck можно эволюционировать по [0073](0073-pfd-instrument-deck.md) без смены transport.

### Отрицательные / риски

- Два рендера (Avalonia + web) — нужен **общий каталог** instrument id и тесты parity compose.
- Пресет `commander` в CIDE может размыть фокус «IDE только для кода» — митигация: default Intercom-first, редактор скрыт/узок.
- **Web без 0017:** Lead с двумя мониторами может почувствовать регресс vs CIDE — митигация: явный positioning (§3.1), CIDE `commander` для power users, опционально ручной второй браузер (не канон).

---

## Фазы (черновик)

| Фаза | Содержание | Где |
|------|------------|-----|
| **0** | Этот ADR + обновление MCC project card | cascade-ide, agent-notes |
| **1** | Документ **Commander instrument catalog** (id → зона → data source) | `docs/design/` или MCC repo |
| **2** | CIDE: TOML preset `commander` (Forward=Intercom, deck stub) | cascade-ide |
| **3** | MCC PWA: layout PFD+Forward+MFD + transport ([0132](0132-intercom-federated-transport-and-multi-client-boundary.md) ф.2–3) | mission-control-center |
| **4** | Contracts: `rolePreset` + deck descriptor в capability map | CascadeIDE.Contracts |

**Зависимости:** transport MVP — [0132](0132-intercom-federated-transport-and-multi-client-boundary.md) ф.2; Commander Forward compose — не откладывать за «только SA-дашборд».

---

## Критерии принятия (Accepted)

- [ ] MCC README описывает **Commander cockpit** и таблицу зон (не только «дашборд SA»).
- [ ] [0132](0132-intercom-federated-transport-and-multi-client-boundary.md) ссылается на этот ADR в §1.1 и фазах.
- [ ] Зафиксирован черновик Commander instrument catalog (минимум 5 instrument id).
- [ ] Решение: team-console **одна** PWA vs два репо — issue/запись в MCC card.

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-19 | Proposed: Commander role; shared attention zones; MCC as cockpit; link 0132 parity. |
| 2026-05-19 | §3.1: web ≠ multi-monitor [0017]; team-console — однооконный shell, не клон presentation. |
| 2026-05-19 | §3.2: web = Companion/Check/Control; primary workplace только CIDE; MCC не главный стол Commander. |

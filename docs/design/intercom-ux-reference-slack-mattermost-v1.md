# Intercom UX: ориентиры Slack / Mattermost (границы v1)

**Статус:** продуктовый playbook (детали attach — **ADR**).  
**Дата:** 2026-05-17  
**Норматив:** attach / якоря — [ADR 0128](../adr/0128-intercom-attachment-anchors-and-code-references.md); fenced / MD в теле — [ADR 0129](../adr/0129-intercom-message-body-markdown-and-fenced-code.md)  
**Связь:** [intercom-design-hub-v1.md](intercom-design-hub-v1.md) · [0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) · [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) · [0119](../adr/0119-chat-slash-commands-intercom-surface.md) · [0057](../adr/0057-chat-surface-pipeline-adoption.md) · [0120](../adr/0120-primary-work-surface-intercom-or-editor.md) · [north-star](north-star-cursor-mcp-cascade-workbench-v1.md)

## Зачем этот документ

**Intercom** в CascadeIDE — не «чат с ботом», а **канал связи в сессии работы** ([0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md)). Пользователи и команды уже знают **Slack** и **Mattermost**: composer внизу, слэш-команды, треды, роли сообщений.

Этот чертёж фиксирует, **какие паттерны** брать как вдохновение для **UX в IDE**, и что **сознательно не копировать**. Attach и code references — **[0128](../adr/0128-intercom-attachment-anchors-and-code-references.md)**; здесь — **playbook** для дизайна и приоритизации.

---

## Два слоя (не смешивать)

| Слой | Что это | Ориентир Slack/MM |
|------|---------|-------------------|
| **A. Intercom в IDE** | Сессия + агент + workspace + MCP; Skia pipeline [0057](../adr/0057-chat-surface-pipeline-adoption.md) | Паттерны **UI и поведения**, не бэкенд |
| **B. Внешний командный контур** | Люди вне IDE, организация, retention, мобилки | Mattermost / Matrix / корп. API — **интеграция**, не переписывание ([0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5)) |

Документ в основном про **слой A**. Слой B — **[ADR 0132](../adr/0132-intercom-federated-transport-and-multi-client-boundary.md)** (transport, Web, MCC, federation) и **[ADR 0142](../adr/0142-intercom-open-wire-pluggable-transports.md)** (открытый wire, pluggable transport, опциональные мосты); **фильтр ленты по роли** — **[ADR 0143](../adr/0143-intercom-feed-participant-lens.md)** (participant lens, не transport channel). Playbook остаётся UX-ориентиром.

---

## Что заимствуем (in scope для UX)

### Composer и command line

- **Поле ввода внизу** (или закреплённый composer в Forward при [0120](../adr/0120-primary-work-surface-intercom-or-editor.md)) — привычная «точка речи» сессии.
- **Слэш-команды** с **иерархическим autocomplete** (`/` → namespace → action), как в Slack — [0119](../adr/0119-chat-slash-commands-intercom-surface.md). Discoverability **через подсказки**, не через запоминание `/br`.
- **Обычный текст** без ведущего `/` уходит агенту; слэш — **локальное** действие IDE/Intercom (Reject неизвестного `/`, без pass-through агенту).

### Навигация по темам (сильнее, чем «просто лента»)

- **Topic cards + overview/detail + back** [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) — аналог «списка каналов/тредов», но заточен под **линии работы** в сессии, а не произвольные комнаты.
- **Одна тема** → default detail; **несколько тем** → default overview карточек.
- **Main line** — одна из карточек, не скрытый режим «без карточек».

### Типы сообщений и «кто в эфире»

| Тип (продуктово) | Примеры | Заметка |
|------------------|---------|---------|
| **Human** | оператор | |
| **Agent** | LLM / ACP | |
| **System** | сборка, git, MCP-статус, ошибка инструмента | визуально отличать от диалога (как system/bot в MM) |
| *(будущее)* **Operator external** | реплика из Mattermost | слой B, синхронизация по контракту |

Минимум v1: заложить **тип сообщения** в модель событий [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md), даже если в UI пока только human + agent.

### Плотность и читаемость ленты

- Хронология **внутри темы**; ветвления — данные [0031](../adr/0031-agent-chat-clarification-batches-and-threading.md), не хаотичные прыжки UI.
- **Карточка темы** со **сводкой** [0096](../adr/0096-intercom-topic-card-summary-and-product-spine.md) — аналог preview последнего сообщения в списке каналов.
- **Product spine** — ортогонален main thread; не смешивать с обычной лентой без явного действия.

#### Лента без «пузырей» (как Slack/MM, не как Telegram)

У **Slack и Mattermost** в канале **нет** messenger-пузырей (цветные «шарики» слева/справа) — плоская **лента**: аватар (опционально) + **имя + время** + текст на фоне канала. У Cascade Intercom **целевое направление — то же**: пузыри **отвлекают** от кода и смысла реплики; лишний визуальный шум в Forward.

| Делаем | Не делаем |
|--------|-----------|
| **Большинство** реплик — одна плоская строка, **без** рамки и без заливки | Оформлять каждое сообщение как «карточку» |
| Роль human / agent / system — **колонка слева** (`SkiaChatFeedRoleRail` + единый **`SkiaChatFeedLayout`**) + тело справа; подряд идущие реплики той же роли — без повтора метки | Залитые rounded **bubble** вокруг каждой реплики |
| Локальные слэши (`/help`, audience self) — метка **«Система»**, не «Справка» | Дублировать роль в meta-строке и сверху тела |
| **Акцент редко:** рамка / inset только когда есть смысл прервать (ошибка, блокер, результат слэша, diff) | Рамка «для красоты» или на каждую реплику агента |
| System / slash-outcome — по умолчанию **компактная строка**; рамка — только при ошибке или явном статусе | Псевдо-диалог «два собеседника в шариках» |

**Правило акцента:** если рамкой/inset помечено **больше малой доли** ленты, акцент **теряет смысл** — как в Dark Cockpit ([handbook §2.5](cide-design-handbook-v1.md)): в норме лента **тихая**, прерывание — по делу.

**Текущее состояние:** в коде/теме ещё есть legacy `message_bubble` / `ChatMessageBubbleBackground` (Avalonia/Skia) — **долг** на эволюцию к flat feed ([0123](../adr/0123-intercom-full-skia-surface-evolution.md)). В макетах для дизайнера — **не** рисовать Telegram/iMessage-пузыри; ориентир — скриншот Slack (плоская лента).

*Исключение термина:* в ADR про слэш иногда «пузырь» = **одна строка результата** `/command` в ленте (не chat-bubble); при рефакторинге лучше «slash outcome row».

### Упоминания и контекст в composer

**Разные пространства имён** — не путать людей, команды IDE и «что уходит агенту вместе с текстом».

| Префикс / путь | Объект | Зачем | Статус |
|----------------|--------|--------|--------|
| **`@…`** | **Только люди** (`@mention`) | Как Slack/MM: участник, уведомление; **не** файлы и не код | v2+ / слой B [0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5) |
| **`/…`** | **Команды IDE** (в т.ч. `/attach …`) | Единый каталог + autocomplete [0119](../adr/0119-chat-slash-commands-intercom-surface.md) | частично Implemented |

**Решение (база):** **`@` — только люди**; артефакты workspace к реплике — через **`/attach …`** в том же slash-каталоге. **`@cc:` не используем.**

#### Что считаем «вложением» (не только «код»)

Для агента полезны **любые файлы решения**: `.cs`, `.md`, `.txt`, `.toml`, `.json`, логи — не только «исходники». Поэтому в slash **не зажимаемся в `/attach code`** как единственный корень.

| Термин в UX | Смысл | Пример |
|-------------|--------|--------|
| **`file`** | Файл в workspace (тип по расширению не важен для пути) | `notes.txt`, `README.md`, `Foo.cs` |
| **`selection`** | Текущее **выделение** в активном редакторе (какой бы ни был файл) | фрагмент из `.txt` или `.cs` |
| ~~`code`~~ как корень slash | Слишком узко и путает с «только .cs» | **не используем** как namespace; в prose можно говорить «код» |

**Параллель с [0125](../adr/0125-slash-workspace-file-commands-and-dynamic-completion.md):**

| Slash | Эффект |
|-------|--------|
| **`/file open`** … | **Открыть** файл в редакторе (навигация) |
| **`/attach file`** … | **Прикрепить** ссылку к **черновику** реплики (контент для агента) |

Один autocomplete по файлам solution можно **переиспользовать** (динамические подсказки пути); разный `command_id` и исход: open vs attach.

#### Привязка к реплике — UX (черновик)

Не путать с **действием в редакторе**:

| Сейчас (Implemented) | Смысл | Пример |
|------------------------|--------|--------|
| **`/editor line select`** / **`delete`** | **Сделать** в редакторе | `/editor line select 5 10` — [0124](../adr/0124-slash-parametric-editor-line-commands.md) |
| **`/file open`** … | **Открыть** файл | [0125](../adr/0125-slash-workspace-file-commands-and-dynamic-completion.md) |
| **`/attach file`** / **`/attach selection`** / **`/attach scope`** | **Вложение** в сообщение агенту | [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) *(фазы 1–3)* |
| **`/editor select code [M:…]`** / **`/editor reveal code […]`** | **Select / reveal** в редакторе без attach | [0131](../adr/0131-editor-slash-select-code-by-bracket-reference.md) *(в коде)* |

**Рекомендуемый namespace:** **`/attach`** с подкомандами (не `/codecontext`).

| Команда (черновик) | Поведение | Wire (идея) |
|--------------------|-----------|-------------|
| **`/attach selection`** | Выделение из **текстового** редактора → chip | `path` + `lineStart`/`lineEnd` + optional excerpt |
| **`/attach file`** + autocomplete | **Файл** → chip; диапазон строк — **только если уместен** | см. §«Формы вложения» |
| **`/attach file <path>`** | Весь файл (обязательно для `.png`, `.dll`, …) | `path` only |
| **`/attach file <path> <start> <end>`** | Участок **текстового** файла | только line-addressable типы |
| *алиас* `selected` → `selection` | Один канон в ADR | — |

#### Формы вложения (не у всего есть «строки»)

**Диапазон строк** имеет смысл только для **line-addressable** артефактов (исходники, `.md`, `.txt`, `.json`, лог…). У **бинарных и медиа** — только **файл целиком**.

| Форма | Когда | Chip в composer (пример) | Wire |
|-------|--------|---------------------------|------|
| **`whole-file`** | `.png`, `.jpg`, `.pdf`, `.dll`, `.zip`, …; или явно «весь файл» | `assets/diagram.png` | `path` |
| **`text-range`** | Текстовый файл + строки или выделение | `Foo.cs L12–18` | `path` + `lineStart`/`lineEnd` |
| **`selection`** | Фрагмент из открытого редактора | как `text-range` | из active editor |

**Пример:** `/attach file docs/wireframe.png` — **без** `<start> <end>`; autocomplete **не предлагает** строки для известных binary/media расширений. Если пользователь ввёл диапазон для `.png` — **ошибка в slash** (как у [0124](../adr/0124-slash-parametric-editor-line-commands.md) I3), не молчаливый no-op.

**Агент:** для `whole-file` + image — отдельная политика (vision / не отправлять / только path) — **не этот документ**; здесь фиксируем только UX attach и wire. В metadata: `attachmentShape` + опционально `contentKind` (`image`, `source`, `text`, …).

#### В середине предложения (несколько вложений)

Типичный запрос: *«Этот метод реализован у нас в **〔код1〕**, а этот — в **〔код2〕**»* — два якоря **внутри** одной реплики, не только в конце.

**Целевой UX composer:** не plain text, а **текст + встроенные chip-токены** вложений (как placeholder в строке; в ленте после send — кликабельные метки, flat feed).

| Способ | Как пользователь делает | Заметка |
|--------|-------------------------|---------|
| **A. `/attach …` в позиции курсора** *(приоритет v2)* | «…в », `/attach selection` или `/attach file Foo.cs 1 20` → **chip в курсор** | Discoverability; autocomplete пути |
| **B. Кнопка «Прикрепить»** | Выделение или файл → chip в курсор | Дублирует A для мыши |
| **D. Скобки `[…]` inline** *(параллельно A/B)* | `[M:Method]` или запасной `[Foo.cs 50 100]` | §«Якорь: блок, не путь»; resolve при send |
| **C. `@file` inline** *(запасной, если D/A неудобны)* | `@file` + autocomplete | **Не** `@cc`; не путать с `@ivan` и `/file open` |

#### Якорь: человек думает **блоком**, не путём и не строками

В голове обычно не `src/Domain/Foo.cs:50–100`, а **«этот метод»**, **«вот этот кусок»**, **«этот тип»**. Путь и номера строк — **технический снимок** для IDE и агента; их можно **вычислить** при send (выделение, Roslyn, активный файл).

| Роль | Что помнит человек | Что хранит wire (канон) |
|------|-------------------|-------------------------|
| **Смысл** | метод / выделение / «этот файл» (png) | `memberKey` и/или excerpt; `attachmentShape` |
| **Производное** | *не обязан* | `file` + `lineStart`/`lineEnd` — **resolve при send** |
| **В ленте** | короткая метка | `GetUserAsync` или `выделение`; путь/строки — **мелко или по hover** |

**Следствие для UX:** приоритет ввода — **`/attach selection`**, кнопка «Прикрепить выделение», **`[M:MethodName]`** с autocomplete; **`[Foo.cs 50 100]`** — запасной и power-user путь, не главная догма.

#### Синтаксис `[…]` (скобки в тексте)

Удобно для **середины предложения** без слэша: *«логика в [M:HandlePayment], а тест в [M:PaymentTests.ShouldFail]»* — без набора пути и строк.

**Не второй тип вложения** — **вторая поверхность ввода** того же anchor. В wire — canonical anchor (member + снимок lines); в UI — метка по **смыслу**, не по диску.

| Форма ввода (черновик) | Смысл | После parse |
|------------------------|--------|-------------|
| `[M:GetUserAsync]` | Member; **F:** из активного файла или autocomplete solution | resolve → lines + `memberKey` |
| `[Foo.cs M:GetUserAsync]` | Member в явном файле | то же |
| `[выделение]` / chip из selection | Текущее выделение редактора | `selection` shape |
| `[Foo.cs 50 100]` | *Запасной* positional ([0124](../adr/0124-slash-parametric-editor-line-commands.md)) | `text-range` |
| `[Foo.cs]` / `[diagram.png]` | Файл целиком (медиа, конфиг) | `whole-file` |

**Отображение в ленте** (пример): `GetUserAsync` · при hover `Foo.cs L50–100 (на момент отправки)` — строки для ориентира, не как главный заголовок.

**Парсер (принципы):**

- Узнаём только **workspace-path-like** токен внутри `[` `]` (сегменты с `/`, расширение `.cs`, `.md`, …) — не каждые скобки в цитате кода.
- **Не** путать с `@mention` (`@` — люди; `[` — артефакты).
- **Не** путать с markdown-ссылкой `[label](url)` — у attach **нет** `(` после `]`.
- Литеральные `[` в тексте — экранирование TBD (`\[` или `[[`), в v1 можно не поддерживать, если редко.

**Composer UX:** при вводе `]` — optional **подсветка** распознанного anchor; Tab на пути — autocomplete как у [0125](../adr/0125-slash-workspace-file-commands-and-dynamic-completion.md). Альтернатива slash: быстрее для тех, кто печатает prose; slash остаётся для discoverability и `selection`.

Пример (человеко-ориентированный ввод **D**):

```text
Оплата у нас в [M:HandlePayment], а регрессия — в [M:PaymentTests.ShouldFail]
                    ↑ member (path resolve при send)              ↑ member
        в ленте:     HandlePayment                                PaymentTests.ShouldFail
        hover:       Foo.cs L120–185 @ send                       PaymentTests.cs L40–62 @ send
```

Пример **запасной** positional (агент, paste, нет Roslyn):

```text
см. [Foo.cs 50 100]
```

**Отправка:** anchors живут **в теле** черновика (как chip или распознанные `[…]`); в ленте — **inline-метки** (без balloon). Агент получает **структурированный** список вложений + prose (offsets в тексте).

#### Внутри метода: «второй `for`», без выделения и без имени

`[M:Foo]` покрывает **весь** `void Foo() { … 500 строк … }`, но не «второй цикл `for`» внутри. Это нормальный случай; не сводить к «запомни строки».

| Подход | Как в UI | Wire / лента |
|--------|----------|--------------|
| **1. Выделить блок** *(H0)* | Расширить выделение до `for` (Expand selection) → `/attach selection` | `selection` + excerpt; метка «фрагмент» или `Foo › for` |
| **2. Каретка + scope** *(H0b, целевой)* | Курсор **внутри** нужного `for` → `/attach scope` или кнопка «Прикрепить блок под курсором» | Roslyn: **innermost** синтаксический span (`ForStatement`, `IfStatement`, …); `scopeKind` + excerpt; в ленте `Foo › for (2)` если в методе несколько |
| **3. Member + prose** *(v1 допустимо)* | `[M:Foo]` в тексте + фраза «второй `for`» | Вложение = весь метод (или excerpt метода) + **prose** как уточнение для агента; у человека в ленте `Foo` + текст реплики |
| **4. Structural sub-anchor** *(v2/v3)* | `[M:Foo S:for:2]` или picker: `Foo` → список `for`/`if` внутри ([0053](../adr/0053-semantic-map-control-flow-pfd.md)) | `memberKey` + `syntaxPath` / индекс однотипных узлов; re-resolve у получателя |
| **5. Positional внутри метода** *(M0)* | `[Foo.cs 220 245]` только если иначе никак | Локальный hint; не primary |

**Рекомендация по продукту:**

- **Не заставлять** помнить строки для «второго for».
- **v1:** H0 (выделил) или **3** (весь `Foo` + сказал словами); параллельно заложить **H0b** (`/attach scope`) — одна команда, каретка в цикле, без ручного выделения 40 строк.
- **v2:** picker структурных узлов внутри `[M:Foo]`; опционально `S:for:2` в грамматике `[…]`.
- **Excerpt обязателен** для безымянных блоков: несколько строк текста @ send — то, что одинаково у отправителя, получателя и агента, даже если lines разъехались.

```text
В [M:Foo] меня смущает второй for — [attach scope @ caret был бы: Foo › for (2)]
```

**Клик по метке `Foo › for (2)`:** re-resolve этот `ForStatement` в **текущем** файле получателя → рамка; не «строка 220 у отправителя».

##### Слои грамматики (смысл → снимок на диске)

**Порядок приоритета для человека** (не «сначала путь, потом member»):

| Слой | Пример ввода | Зачем |
|------|----------------|--------|
| **H0 selection** | `/attach selection`, кнопка, chip | Ноль памяти path/lines — **главный** путь |
| **H0b scope @ caret** | `/attach scope` (каретка внутри `for`) | Блок **без имени**: innermost `for`/`if`/… по Roslyn — см. §«Внутри метода» |
| **H1 member** | `[M:HandlePayment]`, `[Foo.cs M:…]` | Имя метода/члена; path/lines — resolve |
| **H2 whole file** | `[diagram.png]`, `[appsettings.json]` | Когда важен файл, не символ |
| **M0 positional** | `[Foo.cs 50 100]`, `[Foo.cs L:50 100]` | Агент, paste, нет символа, legacy |

**`anchor.notation.canonical`** *(агент/MCP; legacy «L2 field record»)*: `[F:…; M:…; L:…]` — тот же canonical anchor; в ленте для человека рендер **H1**.

| Слой | Пример ввода | Кто печатает | После send |
|------|----------------|--------------|------------|
| **H0–H2** | см. выше | человек | `memberKey` / selection + **resolved** `file` + lines |
| **M0** | `[Foo.cs 50 100]` | агент, power user | `text-range` без member |
| **`anchor.notation.canonical`** | `[F:Foo.cs; M:GetUserAsync]` | агент, шаблоны | то же canonical; UI как H1 |

**Рекомендация по форме readable / canonical (один канон, не два):** см. [naming-layers-v1.md](../design/naming-layers-v1.md) §5.

- Ключи **`F` `M` `L` `T`** (File, Member, Lines, Type) — **опциональные**, порядок **не важен**; разделитель полей — **`;`** или **пробел между парами `Key:value`** (выбрать **один** в ADR).
- **Не** смешивать в продукте две нотации «через запятую `T:, M:`» и «через точку с запятой `type; member:`» — парсер и подсказки раздвоатся.
- Пример **`anchor.notation.readable`** (legacy intercom «L1»): `[Foo.cs M:GetUserAsync]` · `[Foo.cs L:50 100]` · комбо `[Foo.cs M:GetUserAsync L:50 100]` (lines — уточнение, если member размазан по partial).
- Пример **`anchor.notation.canonical`**: `[F:Foo.cs; M:GetUserAsync; L:50-100]` — для агента; в UI ленты рендерить как `Foo.cs › GetUserAsync`.

| Поле | Смысл | Заметка |
|------|--------|---------|
| **F** | Путь в workspace | обязателен, если нет positional `Foo.cs` в начале |
| **L** | `start end` или `start-end` | 1-based inclusive; после **M:** может заполниться автоматически |
| **M** | Имя члена (method/property/…) | **resolve при send** ([0058](../adr/0058-agent-roslyn-mcp-coupling-settings-toml.md)); неоднозначность → ошибка в composer + picker overload |
| **T** | Имя типа (класс) | если **M** не уникален в файле; опционально |

**Стабильность и «две правды» по строкам**

Строки **плывут** не только от коммитов, но и **между участниками одного сообщения**:

| Причина расхождения | Пример |
|---------------------|--------|
| Разный снимок репозитория | у получателя другая ветка / локальные правки |
| Разный контекст IDE | другой активный файл, не тот partial |
| Настройки отображения | перенос, шрифт, масштаб — **видимый** контекст другой; номера строк в файле на диске те же, но «где на экране» — нет |
| Только positional anchor (M0) | отправитель писал `[Foo.cs 50 100]` — получатель открывает **те же** числа, хотя метод уже сдвинулся |

**Вывод для продукта:** номера строк в сообщении — **не общий контракт** между отправителем и получателем (и между человеком и агентом на другом снимке workspace). Это **локальный hint @ send** плюс fallback.

| Что считать «устойчивым» в ленте | Что производное / локальное |
|----------------------------------|-----------------------------|
| **Member** (`M:`), имя типа, **excerpt** текста @ send | `lineStart`/`lineEnd` как единственная правда |
| **Selection snapshot** (хеш/якорь + excerpt) для H0 | «Открой строку 50» без символа |
| **File** для whole-file / медиа | Точный пиксель viewport |

**Поведение у получателя (целевое):**

1. Если в wire есть **`memberKey`** → при клике **re-resolve** в *его* solution → рамка по **текущим** строкам; в UI опционально «было L50–100 @ send, сейчас L48–98».
2. Если только lines (M0) → открыть по снимку + **предупреждение**, если файл не совпадает / диапазон пустой / сильный drift.
3. **Excerpt** (несколько строк текста @ send) — то, что одинаково у всех в ленте; агент может оперировать им, даже если lines разъехались.

В идеале в репо **не должно** быть рассинхрона, но UX **не строим** на предположении, что у всех одинаковые строки — строим на **смысле + excerpt**, строки — для IDE навигации после resolve.

**Клик из ленты:** навигация по **смыслу** (member → текущий диапазон у *этого* получателя); M0-only — best-effort по снимку.

**Не путать:** `M:` = member symbol, не «mention»; люди по-прежнему только `@`.

**Паритет:** тот же диапазон, что `c:els:5:10` и `/editor line select 5 10` — общий binder; разный **эффект**: select = IDE action, attach = payload к сообщению.

#### Клик по вложению в ленте (deep link)

После send inline-метка вложения в тексте реплики — **ссылка-якорь** ([0080 future](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities): открыть, прыгнуть, показать участок). Один и тот же anchor в wire, что при attach.

| Шаг | Поведение (целевое v2) |
|-----|-------------------------|
| 1 | **Открыть** файл в редакторе (или preview для не-текста), сфокусировать вкладку |
| 2 | **Прокрутить** диапазон в видимую область (`text-range` / `selection`) |
| 3 | **Показать участок** — по умолчанию **не** `Selection` в редакторе |

**Почему не выделение по умолчанию:** активное выделение = риск случайно стереть/заменить фрагмент одной клавишей после клика из чата. Навигация «посмотреть, о чём речь» должна быть **безопасной**.

**Рекомендуемый режим v1 для клика из Intercom:** **transient range highlight** — рамка/полоса вокруг строк (или gutter-band), по духу **`highlight_control`** / `AgentHighlightOverlay` ([ui-layout](../ui-ux/cascade-ide-ui-layout-v1.md): рамка поверх, `IsHitTestVisible=false`, не меняет фокус ввода в редакторе). Курсор редактора **не обязан** попадать внутрь диапазона; selection buffer **не трогаем**.

| `attachmentShape` | Клик |
|-------------------|------|
| **`text-range`** / **`selection`** | open + scroll + **рамка** на `lineStart`…`lineEnd` |
| **`whole-file`** (в т.ч. `.png`) | open / preview; **без** строк и без рамки по строкам |

**Опционально (мощный режим):**

| Действие | Эффект |
|----------|--------|
| **Shift+клик** или пункт меню «Выделить в редакторе» | тот же якорь → `SelectInEditor` (как сегодня `go_to_position` для агента) |
| Настройка `[intercom.attachments.code].navigate` | `reveal` *(default)* \| `select` |

**Разделение с агентом:** MCP `go_to_position` сегодня зовёт `SelectInEditor` — уместно, когда агент **готовит правку**. Клик человека из ленты — другой intent (`command_id` вроде `intercom.reveal_attachment` / `editor.reveal_range`), тот же binder строк, **другой presentation mode**. Отдельный ADR: editor range adornment vs reuse CDS attention channel.

**Сейчас в коде:** deep link из ленты и range-frame **не реализованы**; для агента уже есть `go_to_position` → select. Документ фиксирует **целевой UX**, не текущее поведение.

**`@file` vs `@mention`:** если понадобится **(C)** — только зарезервированное `@file`, не произвольный `@notes.txt`.

**Канон:** [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) — не `/attach code`, не `@cc:`; chips в середине предложения; wire [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md).

**Связь:** [0080 future](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities), [0039](../adr/0039-workspace-navigation-affordances.md).

### Входы (ортогональность)

Один `command_id` — несколько поверхностей ([0013](../adr/0013-command-surface-and-discoverability.md), [handbook §2.6](cide-design-handbook-v1.md)):

| Вход | Роль |
|------|------|
| Pointer / hit-target | тактика на карточках |
| **Палитра (Ctrl+Q)** | репетиция: полный каталог, fuzzy-поиск |
| **CascadeChord (Ctrl+K)** + Melody `c:` | выступление: мнемоники **в палитре/аккорде**, не в слэше |
| **Chat slash** + autocomplete | канал сессии: CLI в composer ([0119](../adr/0119-chat-slash-commands-intercom-surface.md) Implemented) |
| MCP / агент | тот же `command_id` [0030](../adr/0030-command-ids-hotkeys-and-ui-registry-layers.md) |

---

## Что не копируем (out of scope / non-goals)

| Не делаем | Почему |
|-----------|--------|
| **Сервер Slack/MM внутри Cascade** | [0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5): отдельный продукт |
| **WebView полного клиента MM/Slack** в IDE без ADR | «Две правды», фокус, SSO — только осознанно |
| **Одна бесконечная лента** как единственный режим | Ломает topic cards [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| **Короткие слэш-мнемоники** (`/br`) | Discoverability только autocomplete; сжатие — `c:` Melody |
| **Messenger-пузыри** (Telegram/iMessage-style) | Отвлекают; у Slack/MM их нет — flat feed; акцент — рамка/inset, см. §«Лента без пузырей» |
| **Реакции, GIF, emoji-first** | Шум для IDE-цикла; отложить |
| **Полный поиск по истории** как в MM Enterprise | v2+; не блокер v1 Intercom |
| **Паритет каждой фичи MM** (threads в threads, workflows) | Берём **ментальную модель**, не feature checklist |
| **`/attach code` как корень** | Узко; любой `.txt`/`.md` — **`/attach file`** |
| **`@cc:`** | Не используем |

---

## Карта паттернов: Slack/MM → Cascade

| Паттерн Slack / Mattermost | В Cascade Intercom |
|----------------------------|-------------------|
| Channel list | Topic overview (карточки) [0072](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md) |
| Thread | Тема / `ThreadNode` [0031](../adr/0031-agent-chat-clarification-batches-and-threading.md) |
| `/command` + picker | [0119](../adr/0119-chat-slash-commands-intercom-surface.md) |
| `@mention` (люди) | Упоминание участника; уведомление — как Slack/MM; v2+ / внешний контур [0080 §5](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-p5) |
| `[path]` / `[path start end]` в тексте | Тот же attach-anchor, что `/attach`; ввод в середине фразы |
| `/attach file` / `/attach selection` | Вложение в реплику; параллель `/file open` [0125](../adr/0125-slash-workspace-file-commands-and-dynamic-completion.md) |
| Клик по file anchor в сообщении | open + scroll; **рамка** диапазона (default), не selection; Shift → select |
| `/editor line …` | Действие в редакторе — [0124](../adr/0124-slash-parametric-editor-line-commands.md) Implemented |
| Сообщение в канале (flat) | Строка ленты: имя + время + текст; **без** balloon |
| Bot / system message | System role + build/git/MCP lines (компактно, не пузырь) |
| Pin / bookmark | Spine, session tree [0116](../adr/0116-intercom-session-tree-and-agent-message-steering.md) |
| Unread badge | IDE Health / уведомления кабины (не дублировать MM) |

---

## Skia и «ощущение Slack»

[0044](../adr/0044-avalonia-host-skia-agent-chat-surface.md) / [0057](../adr/0057-chat-surface-pipeline-adoption.md): вдохновение — **иерархия экранов, composer, типографика, плоская лента + системные строки**, а не HTML-виджеты Slack и не messenger-пузыри. Pipeline **Intent → Layout → Render** сохраняет intent-first [0072 §5](../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md): pointer и слэш не дергают Skia напрямую.

**Визуальные ориентиры v1 (мягкие):**

- human / agent — различимые **роли** (цвет имени, иконка), **без** balloon; **без** рамки на обычной реплике;
- system / slash-outcome — по умолчанию компактная строка; **рамка/inset — исключение** (ошибка, fail, требует действия);
- topic card — заголовок + сводка + метаданные (время, непрочитанное — по мере готовности модели).

---

## Приоритеты внедрения (согласовано с ADR)

| Приоритет | Содержание | ADR |
|-----------|------------|-----|
| P0 | Topic cards, drill-in/back, adaptive default | 0072 |
| P0 | Слэш + **обязательный** autocomplete; без коротких слэш-алиасов | 0119 |
| P1 | Типы сообщений (system/agent/human) в ленте | 0045, 0080 |
| P1 | Intercom-centric Forward (опционально) | 0120 |
| P2 | Attach по [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md) фазы 0–2 | 0045, 0124/0125, 0058 |
| P3 | Внешний MM/Slack как канал команды | 0080 §5, отдельный ADR |

---

## Открытые вопросы

1. **Sidebar vs overview в Forward:** при [0120](../adr/0120-primary-work-surface-intercom-or-editor.md) — карточки тем слева (MM-style) или полноэкранный overview?
2. **Имя в UI:** только «Intercom» или подзаголовок «как в командном чате» на первый релиз? [0080 open](../adr/0080-intercom-naming-and-multi-party-channel-model.md#adr0080-open).
3. **Минимальный набор system-сообщений v1:** только build fail/success или шире (git, index)?
4. **Критерии выбора внешнего провайдера (слой B):** self-host, API, SSO — до ADR интеграции.
5. **0128 implementation:** schema в [0045](../adr/0045-agent-chat-persistence-event-log-and-projections.md); фазы 0–4.
6. *(закрыто)* attach — [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md); fenced vs anchor — [0129](../adr/0129-intercom-message-body-markdown-and-fenced-code.md).
7. **Изображения в prompt агента:** политика vision vs path-only — отдельно от slash UX.
8. *(отложено в [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md))* `@file` inline — после фазы 2 `/attach` + `[path]`.
9. *(закрыто)* L2 `F;M;L` — [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md); `[` не парсить внутри fenced — [0129](../adr/0129-intercom-message-body-markdown-and-fenced-code.md) §5.
10. *(v1 в [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md))* reveal: overlay/gutter ~3 s; adornment — v2.
11. **`/attach scope`:** innermost syntax span; нумерация `for (2)`; picker vs `S:for:2` — фаза 3 [0128](../adr/0128-intercom-attachment-anchors-and-code-references.md).

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-17 | Черновик: Slack/Mattermost как UX-ориентир; границы A/B; in/out of scope; карта паттернов. |
| 2026-05-19 | Лента без messenger-пузырей (flat feed как Slack); акцент редко (рамка/inset); legacy bubble — долг. |
| 2026-05-19 | Разделены `@mention` (люди) и `@cc:` (code context); черновик `selected` / autocomplete файлов. |
| 2026-05-19 | UX attach: `/attach code` vs `/editor line`; паритет binders 0124; `@` только люди, без `@cc:`. |
| 2026-05-19 | Вложения в середине предложения: chip в курсор; запасной `@file` inline (не `@cc`). |
| 2026-05-19 | `/attach file` + `/attach selection` вместо узкого `/attach code`; параллель `/file open`. |
| 2026-05-19 | Формы вложения: `whole-file` (png…) vs `text-range`; строки не у всех типов. |
| 2026-05-19 | Клик по attach: open + scroll + рамка (не selection); Shift → select; ≠ `go_to_position` агента. |
| 2026-05-19 | Inline `[path start end]` — ввод в prose; тот же wire, что `/attach`; отображение `L50–100`. |
| 2026-05-19 | Слои якоря: H0–H2 (смысл) vs M0 (path/lines); wire = resolve @ send; лента — member-first. |
| 2026-05-19 | Строки @ send — не контракт между участниками; re-resolve у получателя; excerpt устойчивее L50–100. |
| 2026-05-19 | Внутри метода: H0b `/attach scope`, member+prose, v2 `S:for:2` / structural picker. |
| 2026-05-19 | Норматив attach → [ADR 0128](../adr/0128-intercom-attachment-anchors-and-code-references.md). |
| 2026-05-19 | Fenced / MD в теле → [ADR 0129](../adr/0129-intercom-message-body-markdown-and-fenced-code.md). |

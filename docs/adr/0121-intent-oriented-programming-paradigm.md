# ADR 0121: Парадигма Intent-Oriented Programming (IOP) — концептуальный фундамент Cascade IDE

**Статус:** Proposed  
**Дата:** 2026-05-17

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0100](0100-project-constitution.md) | Конституция: agent-first, кокпит, общая операционная модель |
| [0013](0013-command-surface-and-discoverability.md) | Палитра, keyboard-first, discoverability команд |
| [0051](0051-intent-based-attention-routing-toml.md) | Intent-based routing внимания (TOML) |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | Intent-first: topic cards, Melody/Chords, `command_id` |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Intercom — канал сессии и намерений |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | Каталог Intent Melody (декларативный слой интентов) |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Слэш в Intercom → тот же `command_id`, что палитра/MCP |
| [0120](0120-primary-work-surface-intercom-or-editor.md) | Якорь Forward: Intercom или редактор — где живёт IOP-цикл |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Паритет git: человек и агент в одном контуре |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | Редактор — source of truth текста; чат — intent/status |

### Вне ADR

| Документ | Роль |
|----------|------|
| [iop-manifest-v1.md](../iop-manifest-v1.md) | Краткий манифест IOP для сайта и онбординга |
| [architecture-policy.md](../architecture-policy.md) | Политика архитектуры, north-star, KB |
| [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) | Команды IDE/MCP — исполнение интентов |
| [intent-melody-language-v1.md](../intent-melody-language-v1.md) | Грамматика `c:` (Melody), не слэши чата |
| [design/north-star-cursor-mcp-cascade-workbench-v1.md](../design/north-star-cursor-mcp-cascade-workbench-v1.md) | Границы «Cursor + MCP + Cascade» |

## Резюме

- Принять **Intent-Oriented Programming (IOP)** — *интенционально-ориентированное программирование* — как **именованную парадигму продукта** Cascade IDE: прежде всего **дисциплина коммуникации** в контуре разработки (**рабочая реализация гипотезы в продукте**), а не замена ООП/ФП.
- Три столпа IOP в CIDE: **намерение вместо ручного синтаксиса** (intent layer), **двухконтурная верификация** (агент синтезирует — человек утверждает diff), **эпистемический контекст** (канон KB и маршрутизация контекста как нормативный слой для агента).
- Публичная формулировка для команды и сайта — [iop-manifest-v1.md](../iop-manifest-v1.md); этот ADR — нормативная привязка к существующим решениям и non-goals.

---

## Контекст

В экосистеме agent-first IDE уже есть «intent-first» в отдельных ADR ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0055](0055-skia-instrument-composition-pipeline.md) Intent→…→Render), каталог **Intent Melody**, слой **Intercom**, паритет **MCP** и **Roslyn**. Не хватает **единого имени парадигмы**, которое:

1. объясняет новичку (в т.ч. на испытательном сроке), *почему* продукт устроен так, а не как «VS + чат»;
2. связывает разрозненные ADR в одну ментальную модель;
3. честно отделяет **гипотезу и рабочую реализацию в продукте** от претензии «единственный стандарт индустрии» или «эталон по спецификации».

Обсуждение с командой (в т.ч. с Атласом) предложило термин **IOP** по аналогии с ООП и ФП. Смысл глубже, чем UI-команды: ИТ в глобальном смысле про **информационный поток** (цели, намерения, процессы, коммуникация, прозрачность); разработка ПО — часть потока, а не весь предмет. Агенты усилили старую истину: без явных намерений и общей картины код и команда расходятся в хаос.

---

## Проблема

1. **Поверхностное чтение IOP:** формулировка «базовая единица — интент» звучит как «ещё слэши», хотя речь о **договорённости о цели** в информационном контуре команды.
2. **Когнитивный потолок:** человек не удерживает 100k+ строк монолита как «один текст в голове»; роль человека — архитектура и верификация, не ручной компилятор синтаксиса.
3. **Разрыв контуров:** без общей парадигмы легко дублировать парсинг команд (слэш в чате vs Melody vs MCP) — см. мотивацию [0119](0119-chat-slash-commands-intercom-surface.md).
4. **Слабый контекст агента:** без канона KB и маршрутизации (`route_context`, playbook'и) интенты «плывут»; нужна явная модель **эпистемических ограничений**, а не только промпт.
5. **Маркетинг vs инженерия:** без ADR термин IOP рискует звучать как декларация «революции» без привязки к коду и статусам ADR.

---

## Решение

<a id="adr0121-p1"></a>

### 1. Определение IOP (в scope Cascade IDE)

**Intent-Oriented Programming (IOP)** — способ организации работы в IDE, где:

- **предмет** — согласованный **информационный поток** (цели, процессы, коммуникация, прозрачность), а не только текст программы;
- **интент** — *именованная договорённость* о намерении или целевом состоянии в этом потоке (не синтаксис и не «ещё один слэш»);
- **исполнение** (в т.ч. генерация кода) делегируется агенту и инфраструктуре (MCP, сборка, Roslyn, git) под **наблюдаемостью** человека;
- **корректность** проверяется по **дельте** (diff, диагностики, тесты) и по **нормативному знанию** (KB), а не только по «сгенерировалось ли что-то».

IOP в CIDE — **дисциплина коммуникации** в agent-first IDE (информационный поток сделан явным и проверяемым). **C#, проекты и редактор остаются source of truth** для текста программы ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md), [0098](0098-semantic-first-document-as-projection.md)).

<a id="adr0121-p2"></a>

### 2. Три столпа IOP в Cascade IDE

| Столп | Смысл | В CIDE (уже есть / в пути) |
|-------|--------|----------------------------|
| **1. Поток и явное намерение** | Согласованный информационный поток; интент = договорённость о цели/состоянии | Intercom, topic cards, KB/ADR; Intent Melody, `command_id`, палитра, [0119](0119-chat-slash-commands-intercom-surface.md) слэши → тот же контур, что MCP; [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) |
| **2. Двухконтурная верификация** | Агент синтезирует; человек — архитектор и арбитр diff | Forward (редактор) / Intercom ([0120](0120-primary-work-surface-intercom-or-editor.md)); Roslyn MCP, build/test MCP, git MCP; human-in-the-loop на merge |
| **3. Эпистемический контекст** | Нормативный слой над кодом — канон KB, router, политики | kb-public, agent-notes, дерево `knowledge/` (каталог `domains/` — **путь в репо**, не термин «домен»); [architecture-policy](../architecture-policy.md), [0100](0100-project-constitution.md) |

«Компилятор интента» в метафоре манифеста — **не один бинарник**, а связка: **Intercom + command surface + MCP + агент + верификация в IDE**.

<a id="adr0121-p3"></a>

### 3. Рабочая реализация в продукте

Cascade IDE — **открытая рабочая реализация** предложенной парадигмы IOP (**экземпляр в продукте**, не эталон по внешней спецификации): стек IDE, Roslyn MCP, agent-notes, kb-public, документируемый на [сайте проекта](https://ai-guiders.github.io/cascade-ide/).

Формулировки уровня «весь мир перейдёт на IOP», «единственный в мире компилятор» или **reference implementation** в смысле ISO/W3C **не** являются частью этого ADR — только **рабочая гипотеза парадигмы** для продукта и сообщества AI-Guiders.

<a id="adr0121-p4"></a>

### 4. Терминология (глоссарий v0)

| Термин | Значение в IOP/CIDE |
|--------|---------------------|
| **Intent** | Именованная договорённость о цели/целевом состоянии в информационном потоке; в CIDE носители — Intercom, KB, `command_id`, Melody, slash (не «атом = слэш») |
| **Intent Melody** | Декларативный/параметрический язык привязки интентов к UI и горячим клавишам |
| **Intercom** | Канал сессии: диалог, topic cards, слэши — лобовая поверхность намерений ([0080](0080-intercom-naming-and-multi-party-channel-model.md)) |
| **Verification loop** | Синтез → diff/диагностики/тесты → принятие или откат человеком |
| **Epistemic context** | KB, agent-notes, router/playbook'и, политики — ограничители смысла для агента |

---

## Non-goals

- **Не** сводить IOP к слэш-командам, палитре или Melody — это поверхности, не парадигма.
- **Не** заменять ООП, ФП или C# в репозитории пользователя «интентами вместо кода».
- **Не** автономный merge в main без human-in-the-loop (см. [0100](0100-project-constitution.md), git-политики).
- **Не** IOP без инфраструктуры верификации (Roslyn/build/test/git) — иначе это только чат.
- **Не** претензия на стандарт ISO/ECMA; IOP здесь — **продуктовая и архитектурная** рамка CIDE.
- **Не** дублировать тело [0119](0119-chat-slash-commands-intercom-surface.md) / [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) — только связующий слой.

---

## Последствия

- Новые фичи command/chat/MCP описываются как **расширение intent surface** + **parity** + **verification**, с отсылкой к столпам IOP.
- Сайт документации: блок на [главной](../index.md), [манифест IOP](../iop-manifest-v1.md), EN-версия в `docs/en/`.
- При **Accepted** — одна строка в [architecture-policy.md](../architecture-policy.md) (цель / позиционирование) и при необходимости глоссарий в [MCP-PROTOCOL.md](../MCP-PROTOCOL.md).
- Онбординг (испытательный срок, контрибьюторы): сначала манифест + [concept-overview](../en/concept-overview.md) / главная, затем ADR по теме.

---

## Статус реализации (на момент Proposed)

| Столп | Зрелость | Комментарий |
|-------|----------|-------------|
| Намерение | Частично Implemented | Melody, палитра, часть MCP; [0119](0119-chat-slash-commands-intercom-surface.md) — Proposed |
| Верификация | Implemented (контур) | Редактор, Roslyn/build/git MCP; полнота UX — по roadmap |
| Эпистемический контекст | Implemented (внешний стек) | kb-public, agent-notes-mcp; интеграция в CIDE — по [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) |

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-17 | Proposed: парадигма IOP, три столпа, манифест, рабочая реализация CIDE. |
| 2026-05-17 | Смягчение позиционирования: «reference implementation» → «рабочая реализация в продукте». |
| 2026-05-17 | IOP: «домены знаний» → канон KB + маршрутизация; `knowledge/domains/` — только путь в репо. |
| 2026-05-17 | Глубина IOP: информационный поток, коммуникация/прозрачность; интент ≠ слэш. |
| 2026-05-17 | Якорная формулировка: IOP = **дисциплина коммуникации** («в коммуникации весь ключ»). |

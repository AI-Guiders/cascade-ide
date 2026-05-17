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

- Принять **Intent-Oriented Programming (IOP)** — *интенционально-ориентированное программирование* — как **именованную парадигму продукта** Cascade IDE (reference implementation), а не как замену ООП/ФП в кодовой базе.
- Три столпа IOP в CIDE: **намерение вместо ручного синтаксиса** (intent layer), **двухконтурная верификация** (агент синтезирует — человек утверждает diff), **эпистемический контекст** (KB/domains как нормативный слой для агента).
- Публичная формулировка для команды и сайта — [iop-manifest-v1.md](../iop-manifest-v1.md); этот ADR — нормативная привязка к существующим решениям и non-goals.

---

## Контекст

В экосистеме agent-first IDE уже есть «intent-first» в отдельных ADR ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md), [0055](0055-skia-instrument-composition-pipeline.md) Intent→…→Render), каталог **Intent Melody**, слой **Intercom**, паритет **MCP** и **Roslyn**. Не хватает **единого имени парадигмы**, которое:

1. объясняет новичку (в т.ч. на испытательном сроке), *почему* продукт устроен так, а не как «VS + чат»;
2. связывает разрозненные ADR в одну ментальную модель;
3. честно отделяет **гипотезу и reference implementation** от претензии «единственный стандарт индустрии».

Обсуждение с командой (в т.ч. с Атласом) предложило термин **IOP** по аналогии с ООП и ФП, со смещением фокуса с реализации на **замысел** и **целевое состояние**.

---

## Проблема

1. **Когнитивный потолок:** человек не удерживает 100k+ строк монолита как «один текст в голове»; роль человека — архитектура и верификация, не ручной компилятор синтаксиса.
2. **Разрыв контуров:** без общей парадигмы легко дублировать парсинг команд (слэш в чате vs Melody vs MCP) — см. мотивацию [0119](0119-chat-slash-commands-intercom-surface.md).
3. **Слабый контекст агента:** без KB/domains интенты «плывут»; нужна явная модель **эпистемических ограничений**, а не только промпт.
4. **Маркетинг vs инженерия:** без ADR термин IOP рискует звучать как декларация «революции» без привязки к коду и статусам ADR.

---

## Решение

<a id="adr0121-p1"></a>

### 1. Определение IOP (в scope Cascade IDE)

**Intent-Oriented Programming (IOP)** — способ организации работы в IDE, где:

- **базовая единица взаимодействия** — *интент* (намерение, целевое состояние, команда с семантикой), а не фрагмент синтаксиса;
- **исполнение** делегируется агенту и инфраструктуре (MCP, сборка, Roslyn, git) под **наблюдаемостью** человека;
- **корректность** проверяется по **дельте** (diff, диагностики, тесты) и по **нормативному знанию** (KB), а не только по «сгенерировалось ли что-то».

IOP в CIDE — **надстройка оркестрации** в agent-first IDE. **C#, проекты и редактор остаются source of truth** для текста программы ([0084](0084-agent-edits-editor-source-of-truth-presence-channel.md), [0098](0098-semantic-first-document-as-projection.md)).

<a id="adr0121-p2"></a>

### 2. Три столпа IOP в Cascade IDE

| Столп | Смысл | В CIDE (уже есть / в пути) |
|-------|--------|----------------------------|
| **1. Намерение вместо синтаксиса** | Пользователь задаёт *что должно быть*, не пошаговый алгоритм | Intent Melody (`c:`), `command_id`, палитра, [0119](0119-chat-slash-commands-intercom-surface.md) слэши → тот же контур, что MCP; каталог [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) |
| **2. Двухконтурная верификация** | Агент синтезирует; человек — архитектор и арбитр diff | Forward (редактор) / Intercom ([0120](0120-primary-work-surface-intercom-or-editor.md)); Roslyn MCP, build/test MCP, git MCP; human-in-the-loop на merge |
| **3. Эпистемический контекст** | «Типы» высшего порядка — домены знаний и политики | kb-public, agent-notes, `knowledge/domains/`; маршрутизация контекста агента; [architecture-policy](../architecture-policy.md), [0100](0100-project-constitution.md) |

«Компилятор интента» в метафоре манифеста — **не один бинарник**, а связка: **Intercom + command surface + MCP + агент + верификация в IDE**.

<a id="adr0121-p3"></a>

### 3. Reference implementation

Cascade IDE позиционируется как **reference implementation IOP**: открытый стек (IDE, Roslyn MCP, agent-notes, kb-public), документируемый на [сайте проекта](https://ai-guiders.github.io/cascade-ide/).

Формулировки уровня «весь мир перейдёт на IOP» / «единственный в мире компилятор» **не** являются частью этого ADR — только **рабочая гипотеза парадигмы** для продукта и сообщества AI-Guiders.

<a id="adr0121-p4"></a>

### 4. Терминология (глоссарий v0)

| Термин | Значение в IOP/CIDE |
|--------|---------------------|
| **Intent** | Именованное намерение с контрактом исполнения (`command_id`, Melody, slash token) |
| **Intent Melody** | Декларативный/параметрический язык привязки интентов к UI и горячим клавишам |
| **Intercom** | Канал сессии: диалог, topic cards, слэши — лобовая поверхность намерений ([0080](0080-intercom-naming-and-multi-party-channel-model.md)) |
| **Verification loop** | Синтез → diff/диагностики/тесты → принятие или откат человеком |
| **Epistemic context** | KB, domains, agent-notes, политики — ограничители смысла для агента |

---

## Non-goals

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
| 2026-05-17 | Proposed: парадигма IOP, три столпа, манифест, reference implementation CIDE. |

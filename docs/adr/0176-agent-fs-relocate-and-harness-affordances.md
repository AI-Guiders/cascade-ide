# ADR 0176: Agent FS relocate и harness affordances (дешёвый правильный edit path)

**Статус:** Proposed  
**Дата:** 2026-07-18

## Резюме

Агент выбирает не «лучший engineering», а **путь наименьшего сопротивления в tool loop**. Пока родные операции — `Write` / `Delete` / `StrReplace`, а `mv` спрятан в Shell, при смене пути побеждает **compose-and-place** (полный rewrite + delete), а не **relocate-and-patch**.

**Решение:** встроенный native tool **`fs_relocate`** (и семейство родственных affordance-tools) в том же слое, где уже живут Write/Delete — CIDE in-proc / AEE ([0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md)), с описанием *prefer this over Write+Delete when only path/placement changes*. Критерий: тело файла **не** обязано проходить через контекст модели; diff = rename (+ микропатч).

Эмпирика одной длинной сессии (Cursor transcript) вскрыла **не один** прокол — каталог ниже; `fs_relocate` — первый клин семейства **Agent Affordance Tools**.

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0148](0148-agent-execution-environment-verification-ladder-and-native-tooling.md) | AEE: native tools > shell escape; latency среды |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | Оркестрация tool surface |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Паритет Cursor ↔ CIDE tool surface |
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | Git через shared core, не raw shell |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Контракты MCP / IdeCommands |
| [0118](0118-agent-notes-core-2-toml-and-knowledge-path.md) | KB path; companion `relocate_knowledge_file` (AN MCP) |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Harness: comfort + token economics |
| [0175](0175-adcm-partition-continuity-pair-and-message-anchors.md) | Continuity; правильный payload тоже affordance |

### Вне ADR

| Документ | Роль |
|----------|------|
| KB `work/projects/.../cascade-ide/note-cognitive-se-environments-historiography-v0.md` | Триггер: перенос domains→work через Write+Delete |
| KB `META/kb-taxonomy-v1.md` | Правило placement; tool должен *дешевить* соблюдение |
| ADR IPSE lesson (historiography note) | Framework tax vs thin tool — здесь thin tool *снимает* обход |

---

## Контекст

### Рамка: налог на героизм (не «агенты тупые»)

Обучение моделей разное; вывод сессии — **не** «агент идиот / плохо обучен». Часто провал **средовой**: tool surface и harness проектировали **под** агента, но **без** него как участника с равным стоянием (см. equal-standing в KB). Тогда агент платит тот же **налог на героизм**, что и человек в кривой IDE: знает правильный путь (`mv` → edit), но среда делает дешёвым обход (Write+Delete, Shell, schema tax).

Следствие для CIDE: чинить **аффордансами и native tools**, не промптом «будь аккуратнее». «Ничего о нас без нас» ([0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md)) распространяется и на **проектирование tool loop** — не только на compact/summary.

### Механизм (почему проигрывает mv)

1. **Аффорданс:** Write/Delete — first-class; `mv` — Shell + дисциплина двух фаз.
2. **Один shot:** compose полный файл на новом пути кажется одной доставкой; move+edit — две фазы.
3. **Контент уже в окне:** после Read полный rewrite «бесплатен» для модели, дорог для git/ревью.
4. **Нет инварианта в tool description:** без явного *relocation → relocate tool* правило «minimal diff» проигрывает привычке.

### Эмпирика сессии (transcript `74b59b6a-…`, 2026-07-18)

Подсчёт вызовов (host / MCP), без полного replay:

| Сигнал | Порядок величины | Прокол |
|--------|------------------|--------|
| `CallMcpTool` ~196 · `GetMcpTools` ~32 | schema tax ~1/6 вызовов | Нет session-cache схем / «man once» |
| `Shell` ~108 vs git MCP ~64 | Shell всё ещё дешёвый escape | Нет жёсткого native git path в host; deploy MCP через robocopy+`Stop-Process` |
| `Write` 17 · `Delete` 2 · `mv` редко | Relocate через compose | Нет `fs_relocate` |
| `read_knowledge_file` 32 · host `Read` 122 | Двойной I/O KB | Нет единого KB-first path; taxonomy не в tool |
| `write_knowledge_file` 10 | Полные тела | Нет `relocate_knowledge_file` / patch-first |
| `WebSearch` 28 · `WebFetch` 15 | Research flood | Нет «digest prior art» / capped research backpack |
| `Stop-Process` / robocopy `.next→live` | Ops MCP руками | Нет `mcp_package_deploy` / hot-swap |
| `route_context` без `workspace_path` | Ошибка → retry | Schema/defaults не подталкивают обязательный arg |
| PowerShell `$PID` read-only | Сломанный deploy | Shell без known-gotchas backpack |

**Наблюдение оператора:** «более логичная цепочка mv → edit на месте, а у агента тонна editing’а» — подтверждено: historiography note перенесена Write+Delete+полный body, не filesystem rename.

---

## Решение

### 1. Native tool `fs_relocate` (CIDE / AEE)

| Параметр | Смысл |
|----------|--------|
| `from`, `to` | Перенос файла или каталога |
| `git_mv` (default true в git worktree) | Prefer `git mv` для истории |
| `patches[]` (optional) | Малые replace **после** move (шапка Domain→project-id) |
| `update_refs` (optional enum: `off` \| `report` \| `apply`) | Скан относительных ссылок в scope; `report` по умолчанию |
| Результат | `{ moved, bytes_unchanged, patches_applied, ref_hits[] }` |

**Инвариант:** если содержимое не менялось, модель **не** должна была сериализовать тело в tool args.

**Tool description (норматив для оркестратора):**  
*When changing only path or KB placement, call `fs_relocate`. Do not Write+Delete the same bytes.*

### 2. Семейство Agent Affordance Tools (backlog того же ADR)

Не раздувать ADR в мега-APSE: каждый пункт — отдельный тонкий tool или флаг AEE, когда дойдёт очередь. Здесь — **карта проколов harness/backpack** из той же сессии:

| ID | Tool / механизм | Дешевит что | Анти-паттерн сейчас |
|----|-----------------|-------------|---------------------|
| **A1** | `fs_relocate` | path change | Write+Delete |
| **A2** | `kb_relocate` (AN MCP) | taxonomy move + link report | host Write в wrong basket |
| **A3** | `kb_place_check` / write gate | domains vs work/projects | молчаливая запись не туда |
| **A4** | MCP schema session cache | повторный `GetMcpTools`/`man` | schema tax |
| **A5** | `mcp_package_deploy` (`.next`→live, stop/hold process) | hot-swap MCP | robocopy + `Stop-Process` + `$PID` traps |
| **A6** | Native git always-on (усиление [0019](0019-shared-git-core-ide-and-git-mcp.md)) | status/diff/commit | Shell git «потому что ближе» |
| **A7** | Research backpack: `prior_art_digest` / capped search | historiography | WebSearch flood |
| **A8** | `route_context` defaults (`workspace_path` from host) | обязательные args | fail→retry |
| **A9** | Shell known-gotchas inject (PS `$PID`, quoting) | escape hatch | повтор одних граблей |
| **A10** | Continuity pair auto-scaffold ([0175](0175-adcm-partition-continuity-pair-and-message-anchors.md)) | Partition payload | ops-only seed |

**MVP этого ADR:** **A1** (+ описание в tool catalog). **A2–A3** — companion в agent-notes, не блокируют A1. Остальное — очередь по боли.

### 3. Где жить

| Слой | Роль |
|------|------|
| **CIDE in-proc / IdeCommand** | A1, A5, A6, A9 — там, где Cursor-like Write/Delete |
| **agent-notes MCP** | A2, A3, частично A8 |
| **Harness inject (0166)** | Короткие reminders только если tool ещё нет; tools > rules |
| **Cursor host** | Вне контроля; паритет через CIDE dogfood |

Принцип: **не чинить правилом то, что чинится аффордансом.**

---

## Последствия

### Плюсы

- Чистый git history на переносах; меньше токенов и тихих правок «заодно».
- Явный product wedge: CIDE как среда, которая **дешевит правильное поведение агента** (cognitive platform ≠ больше Write).
- Карта A2–A10 превращает anecdotal «агент криво» в backlog инструментов.

### Минусы / риски

- Ещё один tool в catalog → нужен чёткий prefer-текст, иначе модель игнорирует.
- `update_refs: apply` опасен без preview — default `report`.
- Путаница с Roslyn `rename` / `move_members` — разные семантики; имена не смешивать.

### Отклонённые альтернативы

| Альтернатива | Почему нет |
|--------------|------------|
| Только rule/skill «всегда mv» | Проигрывает аффордансу (доказано сессией) |
| Только Shell wrapper script | Остаётся вторым классом vs Write |
| «Умный» relocate = rewrite на новом пути | Повторяет баг |
| Мега-APSE file framework | IPSE lesson: framework tax |

---

## Критерий принятия (Acceptance)

1. Агент в CIDE при «перенести note из domains в work/projects» вызывает `fs_relocate`; в diff — rename (+≤N строк патча шапки).
2. Tool description содержит явный prefer над Write+Delete.
3. Тест контракта: relocate без изменения bytes → `bytes_unchanged: true`; тело не в request payload.
4. Карта A2–A10 зафиксирована; минимум один follow-up issue/TK на A2 или A5.

---

## Открытые вопросы

1. Wire: IdeCommand vs loopback MCP tool name — единый id `fs_relocate`?
2. Нужен ли `fs_relocate_dir` отдельно или один tool с kind=file|dir?
3. Связка с pre-flight [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) для `update_refs: apply`?
4. Метрика dogfood: доля Write+Delete пар с near-identical content (детектор анти-паттерна в harness telemetry)?

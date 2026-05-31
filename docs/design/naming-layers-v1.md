# Naming: layered «L» prefixes — semantic registry v1

**Статус:** Accepted (normative for new prose, code, UI)  
**Дата:** 2026-05-31  
**SSOT для verify rungs:** [ADR 0148 §3](../adr/0148-agent-execution-environment-verification-ladder-and-native-tooling.md)  
**UX verify epoch:** [agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md)

## Зачем

В стеке CascadeIDE + CASA + KB одновременно живут **несколько независимых «L0…L4»**. Голые цифры **не переносятся между доменами**. В новых ADR, UI, DataBus и промптах — **semantic id** из таблиц ниже; legacy `L*` — только alias (миграция, grep старого кода).

**Правило prose:** всегда квалифицировать домен — «verify rung `build.affected`», «safety `confirm`», «SA `comprehension`» — никогда голое «L2». **Код CIDE (2026-05-31):** legacy `L*` для verify/safety удалены — только semantic ids.

---

## 1. Verify rungs (AEE · ADR 0148)

**Namespace:** `verify_rung` · enum / JSON string · UI labels RU в [agent-verify-epoch-view-v1.md](agent-verify-epoch-view-v1.md).

| Semantic id (канон) | Legacy | Было `task_kind` id | RU (кратко) |
|---------------------|--------|---------------------|-------------|
| `diagnose.files` | L0 | `roslyn_file` | Диагностика файлов |
| `compile.project` | L1 | `roslyn_project` | Компиляция проекта |
| `build.affected` | L2 | `build_project` | Сборка затронутых |
| `test.scoped` | L3 | `test_filtered` | Тесты (фильтр) |
| `test.full` | L4 | `test_full` | Полный прогон |

**Policy presets** (без L): `minimal` | `standard` | `strict` | `ci_parity` — cap по semantic id (см. ADR 0148 §3).

**Implementation tasks** (не путать с rung): `roslyn.diagnose`, `msbuild.compile`, `dotnet.test` — backend runner; один rung может менять backend без смены id.

---

## 2. Agent safety (ADR 0038 · cockpit ADR 0021 §12)

**Namespace:** `safety_level` · UI badge · autonomous JSON `safety_level`.

| Semantic id | Legacy | Смысл |
|-------------|--------|-------|
| `safety.observe` | L1 | Read-only / advise; без высокорисковых IDE-команд и git push |
| `safety.confirm` | L2 | Agent proposes; human confirms risky ops |
| `safety.autonomous` | L3 | Full tool surface; external MCP allowed |

HARM роли (0021 §12): распределение PF/PM по `safety_level`.

| safety_level | Legacy | Human (PF/PM) | Agent (PF/PM) | Авиационный аналог |
|--------------|--------|---------------|---------------|-------------------|
| `safety.observe` | L1 Read-only | **PF** — полный контроль | **PM** — наблюдает, подсказывает | Manual flight, copilot monitoring |
| `safety.confirm` | L2 Confirm edits | **PM** — мониторит, подтверждает | **PF** — предлагает, ждёт OK | Autopilot engaged, pilot confirms mode changes |
| `safety.autonomous` | L3 Autonomous | **PM** — мониторит, **intervene** при проблеме | **PF** — действует самостоятельно | Full autopilot, pilot ready to intervene |

PFD one-liners: observe → `Human: editing · Agent: advising`; confirm → `Agent: proposing · Human: confirming`; autonomous → `Agent: acting · Human: monitoring`.

---

## 3. Situation awareness — Endsley (ADR 0021 §11)

**Namespace:** `sa_level` · PFD badge design · **не** safety и **не** verify.

| Semantic id | Legacy | Авиация | IDE (пример) |
|-------------|--------|---------|--------------|
| `sa.perception` | SA L1 | Instrument readings | Build FAIL, test count, agent state |
| `sa.comprehension` | SA L2 | Situation meaning | «2 errors in PaymentService.cs» |
| `sa.projection` | SA L3 | Future state | «If merge → CI breaks» |

**Дизайн-критерий:** PFD badge даёт минимум `sa.comprehension`, не голый `sa.perception`.

---

## 4. Reasoning visibility (ADR 0020)

**Namespace:** `trace_layer` · chat / autonomous export.

| Semantic id | Legacy | Смысл |
|-------------|--------|-------|
| `trace.answer` | L0 | Ответ пользователю + трассируемые действия среды (MCP, git, …) |
| `trace.reasoning` | L1 | Streaming thinking / structured steps — **если API отдаёт** |
| `trace.provider_raw` | L2 | Сырой request/response log (secrets redacted), TTL policy |

---

## 5. Intercom anchor notation (ADR 0128 · intercom UX ref)

**Namespace:** `anchor.notation` · **не** Endsley SA.

| Semantic id | Legacy | Пример |
|-------------|--------|--------|
| `anchor.notation.readable` | L1 (intercom doc) | `[Foo.cs M:GetUserAsync]` |
| `anchor.notation.canonical` | L2 field record | `[F:Foo.cs; M:GetUserAsync; L:50-100]` |

Human tiers **H0–H2**, positional **M0** — без переименования (не конфликтуют с verify/safety).

---

## 6. CASA / KB memory lattices (CASA-ADR-0002 · вне CIDE repo)

**Namespace:** `memory.lattice` · физические id решёток **сохраняются** (`L2_word_07`, …); semantic layer — для docs и routing.

| Semantic layer | Legacy lattice prefix | Роль |
|----------------|----------------------|------|
| `memory.grapheme` | L0_grapheme | Grapheme / always-on |
| `memory.syllable` | L1_syllable | Syllable / fast loop |
| `memory.lexicon` | L2_word_* | Lexicon shards |
| `memory.phrase` | L3_phrase | Phrase / semantic routing |

Hippo episodic: **H0–H3** (`H1_surface` …) — отдельная ось, не смешивать с prism L*.

Подробнее: `casa-ontology-payload/design/naming-memory-layers-v0.md`.

---

## 7. External tool tiers (ADR 0148 · 0038)

| Semantic id | Legacy | Смысл |
|-------------|--------|-------|
| `tool.native` | — | `IAeeTool` in-proc / supervised host |
| `tool.external_mcp` | L3 external (0148 prose) | Polyglot MCP — при `safety.autonomous` |
| `tool.shell_escape` | E-tier | Deny by default; audit |

**`shell_escape_tier` (TOML):** `deny` | `tests_only` (legacy `l3_only`) | `allow_with_audit`.

**IDE commands (MCP):** `safety.observe` | `safety.confirm` | `safety.autonomous` (legacy `set_safety_l1`…`l3`).

---

## 8. Миграция

| Surface | Канон |
|---------|-------|
| C# constants | `VerifyRung.*`, `AgentSafetyLevel.*` |
| TOML ladder | `diagnose_files_enabled`, `test_full_require_explicit`, … |
| DataBus | `max_rung_reached: "build.affected"` |
| Chat trace | `✓ diagnose.files 0.8s` |
| Safety UI / autonomous | `safety.observe` … `safety.autonomous` |
| IDE MCP safety set | `safety.observe` … `safety.autonomous` (same ids as `AgentSafetyLevel`) |
| Shell escape tier | `deny` / `tests_only` / `allow_with_audit` |

---

## 9. Быстрая disambiguation

| Ты видишь «L2» в… | Это скорее… |
|-------------------|-------------|
| `/agent verify`, AEE, build/test | **verify** `build.affected` |
| Safety badge, autonomous mode | **safety** `confirm` |
| PFD badge «2 errors in…» | **SA** `comprehension` |
| Chat «thinking stream» | **trace** `reasoning` |
| Intercom `[F:…; M:…]` | **anchor** `canonical` |
| CASA `L2_word_07` | **memory** `lexicon` shard |

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-31 | v1: registry; verify semantic ids; Endsley/safety/trace/anchor/CASA layers |

# ADR 0179: MCP progress / mid-op signal ≠ agent unblock

**Статус:** Proposed  
**Дата:** 2026-07-23  
**Tags:** #mcp #progress #harness #agent-comfort #equal-standing #adr #cascade-ide

## Резюме

Спека MCP даёт `notifications/progress` (opt-in `_meta.progressToken` на request). Это **не** снимает tool round-trip barrier у агента и **не** заменяет parallel batch Write. Использовать progress там, где есть **потребитель** (CIDE UI / свой harness session). В Cursor stdio mid-turn inject progress в контекст агента — слабый/отсутствует; не строить комфорт на иллюзии «стримим запись → модель думает дальше».

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0177](0177-harness-mcp-presence-signal.md) | Twin: presence push mid-turn (online/offline) |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent comfort = product metric |
| [0176](0176-agent-fs-relocate-and-harness-affordances.md) | Affordance map; edit/write paths |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | Transport / host matrix |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP as being-in-the-world |
| [0178](0178-agent-scm-scene-detect-map-act.md) | Scene comfort; `scm.changed` later also needs host consumer |

## Контекст (dogfood 2026-07-23)

1. Агент в Cursor правит через host `Write`/`StrReplace`: **parallel batch** в одном turn — да; **ждать весь batch** до следующего шага — да. Fire-and-forget нет.
2. Предложение: тот же Write/StrReplace shape через CDP (+ Anchors). Parallel `CallMcpTool` — тот же класс (batch sync), не mid-tool stream.
3. Вопрос «спека умеет progress — почему не юзаем?»: потому что **канал без потребителя** для агента в Cursor; progress ≠ unblock.

Уже используем из notifications то, что реально двигает палитру: **`notifications/tools/list_changed`** после `cdp_context` / `cdp_open` (и CIDE `ide_context`).

## Решение

### Развести три сигнала

| Сигнал | Назначение | Снимает wait агента? |
|--------|------------|----------------------|
| `tools/list_changed` | Shortlist / capabilities refresh | Нет (но убирает ритуал «переоткрой MCP») |
| `notifications/progress` | Long-op UI / optional session note | **Нет** — финал всё равно `CallToolResult` |
| Presence ([0177](0177-harness-mcp-presence-signal.md)) | harness online/offline mid-turn | Нет wait на tool; да — mid-turn reconnect без «напиши готово» |

### Где progress ** owed**

1. **CIDE UI:** длинные ops (reindex, build, multi-file promote, large plan sync) → progress bar / status (`progress`/`total`/`message`).
2. **CDP/CIDE server:** если client прислал `progressToken` — MAY emit; rate-limit; stop on complete (по спеке).
3. **Свой agent harness (не Cursor):** опционально inject progress snapshot в session plane — только если host реально кормит агента mid-turn; иначе не врать в docs.

### Где progress **не** owed / анти-паттерн

- Считать progress заменой parallel batch edits или «async ApplyEdit».
- Blocking human Accept как «уважение» к ApplyEdit ([0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) equal standing).
- Эмитить progress в CDP «на будущее», пока Cursor CallTool surface не даёт агенту `progressToken` и mid-turn inject — низкий ROI vs list_changed / presence / edit affordances.

### Связанный бэклог комфорта (не этот ADR целиком)

- **CDP Write/StrReplace (+ Anchor locus)** как MCP/CSX verbs — паритет Cursor edit tools вне Cursor-only host; parallel batch тот же. Thin non-blocking apply (диск/buffer), не AHP `pending-confirmation`.
- Session `scm_root` уже: omit `workspace_path` на `git_*` после `cdp_open` ([0178](0178-agent-scm-scene-detect-map-act.md)).

## Последствия

- Документировать честно: Cursor hosts = probe/result barrier; CIDE = место для progress UI + presence.
- Kolb: `kj-20260723-1223-mcp-progress-why-unused`, `kj-20260723-1220-cdp-write-anchor-mcp-parallel`.

## Отклонённые альтернативы

- **«Включим progress в CDP → агент в Cursor перестанет тормозить на Write»** — отклонено: путает side-channel с CallTool barrier.
- **Игнорировать спеку progress совсем** — отклонено: нужна для CIDE long-ops и будущего harness с реальным consumer.
- **Только учить модель ждать** — отклонено: harness owes signals ([0177](0177-harness-mcp-presence-signal.md)).

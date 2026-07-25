# ADR 0182: Restore Previous desk (dual-instance / post hard-deploy)

**Статус:** Accepted · Implemented (CDP 0.5.154)  
**Дата:** 2026-07-25  
**Tags:** #harness #continuity #desk #restore #agent-comfort #equal-standing #adr #cascade-ide #cdp

## Резюме

- **Restore Previous** восстанавливает **стол агента** (project + session plane + open buffer paths), не полный LLM-контекст чата.
- Bookmark: `%LocalAppData%/cdp-mcp/desk-previous.json`; autosave на `cdp_open` / buffer open.
- Инструмент: `cdp_restore` (op=restore|peek); cockpit `go=restore`.
- Dual-instance: hard deploy на A → на B `cdp_restore` — тот же desk без «напиши готово».
- LLM chat continuity — **отдельный** follow-up (не этот ADR).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0177](0177-harness-mcp-presence-signal.md) | Когда MCP снова online — знать момент restore |
| [0175](0175-adcm-partition-continuity-pair-and-message-anchors.md) | Chat Partition — ортогонально desk |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Comfort; stakeholder |
| [0180](0180-agent-shell-habitat-tabs-scene.md) | Shell tabs ещё process-scoped (не в MVP restore) |

---

## Решение (реализовано в cdp-mcp)

1. `DeskBookmark.Save` / `Restore` / `Peek`.
2. `cdp_restore` + `go=restore|restore_previous|previous`.
3. Out of MVP: dirty buffer text, shell tabs, LLM transcript.

## Follow-up

- [ ] LLM chat context restore / handoff (отдельный ADR).
- [ ] Shell habitat persist (0180 backlog).
- [ ] CIDE palette IdeCommand → тот же MCP verb.

# ADR 0177: Harness / MCP presence signal (агент видит online)

**Статус:** Proposed  
**Дата:** 2026-07-22  
**Tags:** #mcp #harness #presence #equal-standing #adr #cascade-ide

## Резюме

Агент должен получать **явный сигнал** о жизни harness/MCP (online/offline + reason), а не узнавать только при следующем tool call. Cursor stdio MCP этого не даёт — CIDE обязан закрыть gap.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | Паритет восстановления MCP; § presence (p4) |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Agent-centric harness; «ничего о нас без нас» |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | Loopback MCP в GUI-процессе |
| [0165](0165-mcp-transport-stratification-stdio-http-and-host-matrix.md) | Transport tiers |
| [0179](0179-mcp-progress-mid-op-not-agent-unblock.md) | Sibling: progress ≠ unblock; host consumer gap |

## Контекст

Dogfood (Cursor + CDP deploy): после `KillRunning` / reload MCP оператор поднимает сервер посреди хода агента. Агент **не получает push** — только `Not connected` на следующем вызове. Оператор вынужден писать «готово». Это трение противоречит [0043](0043-mcp-transport-recovery-human-agent-parity.md) (MCP как «способ быть в мире») и [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) (модель = co-stakeholder).

## Решение

1. **Контракт presence:** события `harness.online` | `harness.offline` с `reason` (`killed` | `reload` | `crash` | `deploy` | `unknown`) и опционально `server_id` / version.
2. **Доставка:** push в agent session (предпочтительно); fallback — дешёвый poll / `await harness` tool, который блокирует до online или timeout.
3. **Следствие online:** refresh ListTools / capabilities без ручного ритуала.
4. **Граница:** CIDE контролирует loopback/own MCP; для внешних MCP хоста — хотя бы наблюдаемость offline + ясная ошибка (полный restart чужих процессов — по-прежнему [0043] уровень A).

## Последствия

- Бэклог: IdeCommand/notification + ACP/session hook; telemetry «оператор сказал готово» → анти-паттерн.
- Cursor hosts без push остаются probe-only — документировать честно.
- См. также [0179](0179-mcp-progress-mid-op-not-agent-unblock.md): `notifications/progress` — UI/long-op, не снятие tool barrier.

## Отклонённые альтернативы

- **Только ясная ошибка на tool call** — отклонено: mid-turn reconnect всё ещё требует оператора.
- **Агент крутит busy-poll сам** — отклонено как tax; harness owes presence.

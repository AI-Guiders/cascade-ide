# ADR 0184: Harness channel mute («беруши») — MCP / Intercom из кокпита

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Tags:** #harness #mute #mcp #intercom #cockpit #attention #equal-standing #adr #cascade-ide

## Резюме

- Аналогия «ухо / беруши»: агент может **приглушить вход** без переобучения модели — управление на стороне **нашего harness**.
- Поверхность: **кокпит** (уже есть) — mute/unmute каналов.
- Минимум два рода каналов: **MCP server** (не хочу сейчас вывод/шум этого сервера) и **Intercom participant** (залочить ежика → ему notice, агенту тишина в личке).
- **Личка** = IDE-mediated DM (агент ↔ оператор или 1:1 topic), не Cursor-host chat: тогда mute работает «из коробки» протокола среды.
- Связь с [0183](0183-cockpit-intercom-chat-continuity.md) (Intercom в пульте) и [0080](0080-intercom-naming-and-multi-party-channel-model.md) (multi-party).
- CIDE на парке — канон; реализация после unpark.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0183](0183-cockpit-intercom-chat-continuity.md) | Cockpit Intercom; quiet/toggle — этот ADR углубляет mute-модель |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Multi-party Intercom |
| [0143](0143-intercom-feed-participant-lens.md) | Participant lens в ленте |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Stakeholder; attention economics |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP как канал действия; mute ≠ kill навсегда |
| [0177](0177-harness-mcp-presence-signal.md) | Presence online/offline; mute — отдельный слой «не слушаю» |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS «канал» UI — ортогонально; здесь agent-facing ingress |

---

## Контекст

Dogfood / разговор (2026-07-25): каналы LLM как уши; «могу надеть беруши». Harness наш → mute без fine-tune. Кокпит есть → типизация и кнопки там. Письмо в чат средствами IDE → агент получает управление участниками/MCP в том же контуре.

## Решение (направление)

### Модель канала (для агента)

| Kind | Пример | Mute значит |
|------|--------|-------------|
| `mcp` | server_id `cdp`, `git`, noisy toolset | Не инжектить tool results / notifications этого сервера в agent turn; ListTools может скрывать или помечать muted; **не** обязательно kill process |
| `intercom_participant` | hedgehog, operator, system | В **текущей личке / topic**: не доставлять реплики участника агенту; участнику — системное «agent muted you» (если policy позволяет) |
| `intercom_feed` (опц.) | весь Intercom pulse | Как [0183](0183-cockpit-intercom-chat-continuity.md) toggle — грубый выключатель |

### Кокпит

- Pulse: список каналов + `muted` / `open`.
- Verbs: `go=mute target=mcp:git` / `go=unmute` / `go=mute target=user:hedgehog`.
- Состояние сессионное (+ optional persist в settings.toml).

### Личка (DM)

- IDE-native Intercom 1:1 (агент + оператор или agent+peer) — **канонический** контур для mute participant.
- Cursor Composer как host chat — **не** обещать тот же mute (чужой harness); CIDE — да.

### Политика

- Mute MCP ≠ удаление affordance навсегда; явный unmute; safety-critical MCP (integrity) — **не** mute без override / audit.
- Mute оператора в единственной личке — либо запрет, либо soft (delay) + явный confirm: иначе equal-standing ломается в обе стороны.

## Последствия

- Бэклог CIDE: channel registry в harness session + cockpit organ; Intercom delivery filter.
- CDP: зеркало mute state для loopback MCP, когда CIDE host.
- Документировать earplugs как **продуктовый** паттерн attention, не метафора в чате.

## Отклонённые альтернативы

- **Только промпт «игнорируй MCP X»** — отклонено: ненадёжно, без переобучения не держится.
- **Kill MCP process как единственный mute** — отклонено: грубо; presence/reload tax; unmute дороже.
- **Mute только в UI человека** — отклонено: агент остаётся без беруш.

## Follow-up (после unpark CIDE)

- [ ] Channel registry + session mute map.
- [ ] Cockpit mute/unmute verbs + pulse.
- [ ] Intercom delivery filter + muted notice.
- [ ] Policy: operator mute / integrity MCP exceptions.

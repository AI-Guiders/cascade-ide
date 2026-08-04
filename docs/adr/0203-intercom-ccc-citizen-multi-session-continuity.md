# ADR 0203: Intercom CCC + Citizen multi-session continuity (указатель)

**Статус:** Proposed  
**Дата:** 2026-08-05  
**Tags:** #intercom #ccc #citizen #continuity #adr #cascade-ide #cdp #equal-standing

## Резюме

- **SSOT решения:** [CDP-ADR-0030](../../cdp-mcp/docs/adr/CDP-ADR-0030-citizen-multi-session-continuity.md) — Citizen multi-session continuity / one mind · N seats.
- Этот ADR — **указатель в дереве CIDE** и явное расширение [0183](0183-cockpit-intercom-chat-continuity.md): Intercom = **Command Communication Center (CCC)** кокпита — человекочитаемый outlet связи/continuity, не клон Cursor chat.
- Testbed = **Citizen API** (+ Glass/Intercom), не dual Cursor composer. Dogfood API (в т.ч. картинки) уже прошёл.
- Контекст модели: сессии = **адреса** (как мессенджер); в промпт — **on-demand** hot seat; cold = summary/sticky/habitat. Не пихать N полных историй в каждый turn.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [CDP-ADR-0030](../../cdp-mcp/docs/adr/CDP-ADR-0030-citizen-multi-session-continuity.md) | **SSOT** multi-session / attention policy |
| [0183](0183-cockpit-intercom-chat-continuity.md) | Intercom continuity plane (quiet card); этот ADR расширяет до CCC + N citizen sessions |
| [0172](0172-conversation-first-habitat.md) | Session graph habitat |
| [0116](0116-intercom-session-tree-and-agent-message-steering.md) | Session tree |
| [0182](0182-restore-previous-desk-dual-instance.md) | Desk restore; ортогонально chat/session plane |
| [0193](0193-agent-attention-channels-ccl.md) | Attention channels; не W-spray всех сессий |
| [0202](0202-citizen-guest-isolation-and-ai-keys-foundation.md) | Citizen/guest + keys pointers |
| CDP-ADR-0025 · 0026 · 0028 | Isolation · keys · wire |

## Контекст (коротко)

Оператор: один ум на N сидений — норма человека; Cursor dual-window — её AsBuilt, не агентский one mind. Мессенджер уже умеет N чатов on-demand. Нужен тот же паттерн на **Citizen**, с Intercom как CCC (голос/журнал/Who/presence), без взрыва контекста.

## Решение (направление)

См. CDP-ADR-0030. В CIDE-проекции:

1. Intercom CCC — UI/latch поверхность коммуникации (Glass + `cdp_intercom`).
2. Citizen — движок multi-session continuity за CCC.
3. 0183 quiet-default остаётся: CCC не орёт полной лентой на cold start.

## KB

- `note-one-mind-n-seats-2026-08-05.ru.md`
- playbook-being-vs-seeming (internal locus · one mind · N seats)

## Follow-up

См. CDP-ADR-0030 Verification + Next engineering. Не блокировать PF Intercom Who leaf этим ADR — направление параллельно.

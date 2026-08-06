# Glass Intercom · NorthStar messenger (v0 steer)

**Status:** accepted direction 2026-08-05 (operator); channel ontology refined same day.
**Do not:** wire Folded AutoI Korry by hand in this leaf — separate residual.

## Thesis

Glass **Intercom** is **not** «чат со вторым пилотом».

**NorthStar** = координационный центр команды: люди + агенты + агенты агентов.
Product face → **обычный мессенджер**: групповые чаты, личные диалоги, on-demand переключение внимания (человек или модель) — не linear agent-chat и не CIDE session-graph страдание.

Рифма: **one mind · N seats** / Citizen multi-session (CDP-ADR-0030, CIDE 0203) — то, что сейчас костыльно двумя окнами Cursor, через API становится нормальным мессенджером с несколькими линиями.

## Already canon (reuse, don’t reinvent)

- [ADR 0080](../adr/0080-intercom-naming-and-multi-party-channel-model.md) — Intercom = multi-party channel, not bot window.
- [intercom-ux-reference-slack-mattermost-v1](intercom-ux-reference-slack-mattermost-v1.md) — composer, roles, flat feed; **do not** fork full Slack server into IDE.
- [ADR 0143](../adr/0143-intercom-feed-participant-lens.md) — **lens** All/Humans/Agents/System = UI filter inside a room, **not** separate channels.
- [ADR 0172](../adr/0172-conversation-first-habitat.md) — session-graph was CIDE north-star; **Glass must not copy the suffering**.
- [ADR 0203](../adr/0203-intercom-ccc-citizen-multi-session-continuity.md) + CDP-ADR-0030 — Intercom as CCC / multi-session continuity.

## Channel ontology (accepted refine)

### Reject as rooms

- `#humans` / `#agents` as separate **channels** — выглядит как дискриминация экипажа; роли уже есть в сообщениях + **lens** (0143).
- «Только DM с вторым пилотом» как вся поверхность Intercom.

### First-class kinds

| Kind | Name (working) | Who | What |
|------|----------------|-----|------|
| **Crew** | `#crew` | люди **и** агенты вместе | командный групповой эфир — NorthStar hub |
| **DM** | личные / 1:1 (и малые группы позже) | выбранные участники | модель может быть в `#crew` **и** вести личные линии с разными членами |
| **Radio** | Radio | оператор ↔ **агент оператора** (этот seat / citizen partner) | прямая связь «как сейчас с тобой» — не вся команда |

Доп. групповые каналы (`#ops-feature`, project rooms…) — позже, по мере команды; day-1 достаточно `#crew` + Radio + DM.

### Attention

Переключение канала / линии — **on-demand** со стороны человека **или** модели (foreground seat), в духе one mind · N seats — не «новый чат Cursor = новая амнезия».

### Lens (orthogonal)

Внутри `#crew` (или DM) lens All/Humans/Agents/System **фильтрует вид**, не создаёт комнаты.

## Face (UX)

**Conversational UI → Slack/MM light:** меньше хрома, больше смысла.

- **Substrate:** Glass **WPF** face — реально проще, чем Avalonia+Skia «рисуем мессенджер сами» (ItemsControl / virtualization / layout). Не тащить CIDE Skia chat surface как default для NorthStar.
- Flat feed: имя + время + текст (не пузыри Telegram/RadChat).
- Channel rail + composer — основные controls; не bot cards / suggested-actions / overlay carousel как default.
- Meaning first: кто сказал, куда (crew/DM/Radio), Radio pointers / attach — не декоративный chat chrome.
- Paid Conversational kits (Telerik/DE) не нужны под этот stance; stand on `intercom-wire` + WPF list/virtualization.
- Canon UX: [intercom-ux-reference-slack-mattermost-v1](intercom-ux-reference-slack-mattermost-v1.md).

## Relation to lane × model axes

[glass-intercom-lane-model-axes-v0](glass-intercom-lane-model-axes-v0.md) (CIT/HOST/PF Korry + HUD model) = **near-term chrome / strangler**.

Mapping sketch (not UI ship yet):

- **Radio** ≈ сегодняшний прямой Intercom с PF/habitat partner (то, что ощущается как «этот чат»).
- **`#crew`** ≈ командный эфир (ещё не UI).
- **HOST** pipe может стать отдельным transport/DM к host Composer, не обязан быть «каналом экипажа».
- **CIT** + model directory (ListBox + HudModelPicker) — мозг Citizen, который **участвует** в `#crew` / DM / Radio по политике внимания, а не «lane = весь UX».

Longer arc: lane strip → channel rail (`#crew` · Radio · DMs).

## Folded AutoI Korry — CLOSED 2026-08-06 (cdp-mcp 0.5.674)

Green Korry = latch **paint**. Click → `glass_ignite_cmd`. Halted/folded: `autonomous_on` now `Resume` + `SetAutonomous(true)` (clears await_partner so face returns to fly). Residual: operator eyes on HUD after fold; VAD still unwired.

## Open (later ship)

- Identity of Radio partner vs Citizen vs host Composer when multiple minds on wire.
- DM address book (humans + agents as equal-standing members) — **thin shipped 2026-08-06** (`GlassIntercomContacts` · DM sidebar); peer journal filter / rich directory later.
- Browsable FM model directory (CIT) — **thin shipped 2026-08-06** (`GlassIntercomModels` · `CitModelsPanel`); live `GET /v1/models` merge later.
- Face chrome-strip toward Slack/MM flat feed — **thin shipped 2026-08-06** (flat rows · quiet meta · topic strip Collapsed / Topics opt-in); residual Autoi/HLD/VAD HUD + CIT/HOST/PF lane strangler.
- Whether HOST stays a channel or a transport quirk.

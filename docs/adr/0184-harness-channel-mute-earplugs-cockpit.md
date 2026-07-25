# ADR 0184: Harness channel mute («беруши») — MCP / Intercom из кокпита

**Статус:** Proposed  
**Дата:** 2026-07-25  
**Обновлено:** 2026-07-25 — `Muted` ≠ `Killed`; слои ignition vs side-channel  
**Tags:** #harness #mute #mcp #intercom #cockpit #attention #equal-standing #adr #cascade-ide

## Резюме

- Аналогия «ухо / беруши»: агент может **приглушить вход** без переобучения модели — управление на стороне **нашего harness**.
- Поверхность: **кокпит** (уже есть) — mute/unmute каналов.
- Минимум два рода каналов: **MCP server** и **Intercom participant**.
- **Статус в пульте: `Muted` ≠ `Killed` / offline.** Muted = «агенту временно не надо»; процесс может жить. Killed/offline = канал мёртв ([0177](0177-harness-mcp-presence-signal.md)).
- **Личка** = IDE-mediated DM — mute participant «из коробки» среды.
- CIDE на парке — канон; реализация после unpark.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0183](0183-cockpit-intercom-chat-continuity.md) | Cockpit Intercom; quiet/toggle |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md) | Multi-party Intercom |
| [0143](0143-intercom-feed-participant-lens.md) | Participant lens |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Stakeholder; attention |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | MCP канал; mute ≠ kill |
| [0177](0177-harness-mcp-presence-signal.md) | online/offline; mute **ортогонален** presence |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | CDS UI-канал — ортогонально |

---

## Контекст

Dogfood (2026-07-25): беруши на harness; кокпит = ручки. Уточнение: в pulse писать **MCP Muted**, не путать с умершим сервером.

## Решение (направление)

### Статусы MCP в кокпите (обязательная лексика)

| Wire / UI | Смысл | Процесс | Агент слышит? |
|-----------|--------|---------|----------------|
| `online` | Живой, слушаю | up | да |
| **`muted`** | **Беруши** — временно не надо | **обычно up** | **нет** (ingress отфильтрован) |
| `offline` / `killed` | Канал мёртв / убит | down | нет (нечем) |

- Pulse: `mcp:git  Muted` vs `mcp:git  Offline` / `Killed`.
- Mute **не** эмитит `harness.offline` и **не** KillRunning.
- Unmute → `online`, если process ещё up; если за время mute умер — честно `offline`, не маскировать под muted.

### Модель канала

| Kind | Mute значит |
|------|-------------|
| `mcp` | Статус **`Muted`**: не инжектить results/notifications; ListTools скрыть или пометить; процесс **не** обязан умирать |
| `intercom_participant` | В личке/topic: тишина агенту; участнику — «agent muted you» (по policy) |
| `intercom_feed` (опц.) | Грубый выключатель ленты ([0183](0183-cockpit-intercom-chat-continuity.md)) |

### Кокпит

- Pulse с явным `online` | **`muted`** | `offline`.
- `go=mute target=mcp:git` / `go=unmute` / `go=mute target=user:hedgehog`.

### Личка (DM)

- IDE-native 1:1 — канон для mute participant. Cursor host chat — не обещать.

### Политика

- Unmute явный; integrity MCP — не mute без override/audit.

### Слои входа (mute ≠ выключить completion)

По определению **чей-то** текст должен попасть в chat/completions, иначе ход не стартует. Обычно это оператор (или система / таймер / другой агент). Хост дергает API.

| Слой | Mute «единственного оператора»? | Зачем |
|------|----------------------------------|--------|
| **Host chat → completions** (ignition хода) | **Нет** (не беруши). Отключить ≈ не звать модель / стоп IDE | Без входа turn не начинается |
| **Intercom side-channel** (шум, peer, pulse) | **Да** — не кормить агенту Intercom-ingress; host user message всё равно может стартовать turn | Беруши на внимании, не на API |
| **Steer / interrupt mid-turn** (хост прерывает агента) | Не агентский mute; власть хоста | Cursor stop и аналоги |

`Mute ≠ Disconnect API.` Беруши — на **ingress внимания** (MCP, peer, Intercom). Primary steer (то, что зажигает completion) в единственной личке — **неприкосновенен**, иначе агент глушит единственный способ сказать «хватит» / «поправь».

Mute единственного оператора **целиком** в 1:1: запрет, либо soft (только side-channel / delayed queue) + confirm — не глушить ignition.

## Последствия

- Отдельные индикаторы **Muted** vs **Killed** в cockpit/health.
- Telemetry: mute_duration ≠ crash/kill counts.
- Policy engine различает ignition vs side-channel.

## Отклонённые альтернативы

- Промпт «игнорируй MCP» — нет.
- Kill = mute — нет: это `Killed`, не `Muted`.
- Один значок на muted и offline — нет: путаница.
- Mute только в UI человека — нет.
- Mute primary steer / host completion ignition как «беруши» — нет: ломает определение хода.

## Follow-up (после unpark CIDE)

- [ ] Channel registry + mute map.
- [ ] Cockpit verbs + лейбл **Muted**.
- [ ] Intercom filter + notice.
- [ ] Policy: ignition vs side-channel; integrity exceptions; telemetry split mute vs kill.

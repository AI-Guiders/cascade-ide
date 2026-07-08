# ADR 0170: Intercom feed readability MLP — типографика, роли, flat feed

**Статус:** Proposed · In progress (P0 defaults + метки)  
**Дата:** 2026-06-28

## Резюме

Лента Intercom в Forward и MFD должна **удобно читаться** при длинных ответах агента: достаточный кегль, ясные метки роли/audience, flat feed без визуального шума ([intercom-ux-reference](../design/intercom-ux-reference-slack-mattermost-v1.md) §лента).

**Принято направление (MLP):**

1. **`feed_metrics = comfortable`** по умолчанию (`[intercom] feed_metrics` в settings.toml).
2. **Prose 14pt** (parity с редактором); межстрочный интервал и отступы между репликами ↑.
3. **Метки audience:** в role rail **`Локально`** вместо **`Система`** + убрать мелкий badge «только ты» в meta slash.
4. **P1:** markdown-структура в ленте (списки, заголовки) — [0129](0129-intercom-message-body-markdown-and-fenced-code.md); participant lens — [0143](0143-intercom-feed-participant-lens.md).

---

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0057](0057-chat-surface-pipeline-adoption.md) | Skia chat surface pipeline |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Flat feed, `SkiaChatFeedLayout` |
| [0129](0129-intercom-message-body-markdown-and-fenced-code.md) | MD / fenced в теле |
| [0143](0143-intercom-feed-participant-lens.md) | Фильтр human/agent/system |
| [0168](0168-presentation-two-screen-pf-m-layout-policy.md) | Узкий Forward — measure width |
| [0119](0119-chat-slash-commands-intercom-surface.md) | `IntercomMessageAudience.SelfOnly` |

---

## Контекст

Операторский feedback: **мелкий шрифт**, непонятные метки **«Система» / «только ты»**, слабое **отображение тела сообщений**.

Технически:

- default `[intercom] feed_metrics = compact` + `prose_pt_forward = 12` + `SkiaChatDensity` сжимали ленту в Forward;
- `ComfortableFeed` был реализован, но **выключен** по умолчанию;
- role rail 48–56px @ ~9pt — обрезка «Система»;
- slash SelfOnly: role «Система» + badge «только ты» 9pt справа — дублирование и низкая контрастность.

---

## Решение

### 1. Comfortable — default

| Setting | Было | Стало |
|---------|------|--------|
| `[intercom] feed_metrics` | `compact` | **`comfortable`** |
| `[fonts.intercom] prose_pt` | 13 | **14** |
| `[fonts.intercom] prose_pt_forward` | 12 | **14** (при compact — по-прежнему forward кегль) |

`FeedUsesForwardMetrics = ForwardHost && !ComfortableFeed` — при comfortable Forward использует MFD-метрики ленты.

### 2. Метки роли и audience

| Ситуация | Было | MLP |
|----------|------|-----|
| Slash + `SelfOnly` | role «Система», badge «только ты» | role **«Локально»**, badge **убран** |
| Slash + Channel | «Команда» | без изменений |
| User / Agent / … | «Ты» / «Агент» / … | без изменений; role rail **шире и крупнее** |

`IntercomMessageAudience` не меняется; меняется **copy** в `BuildMessageTitle` и Skia meta.

### 3. Метрики `SkiaChatFeedLayout` (comfortable / MFD)

- Role rail width ↑, role label **11pt** (было ~9–9.5);
- prose line height **18px** (было 15);
- segment gap и gap между группами реплик ↑.

### 4. Фазы

| Фаза | Содержание |
|------|------------|
| **P0** | defaults + метки + layout metrics (этот PR) |
| **P1** | lists/headers в `SkiaMarkdownLayout`; контраст prose |
| **P2** | «Focus read» → full MD preview; lens collapse tool rows |

---

## Последствия

- Пользователи с user `feed_metrics = compact` сохраняют плотный режим.
- Help/intercom-help: «локальное сообщение» согласовано с меткой **Локально**.

## Открытые вопросы

1. Inline meta «Агент · 12:34» вместо бокового rail — отдельная итерация?
2. `compact` переименовать в UI settings в «Плотный (Forward)»?

---

## История

| Дата | Изменение |
|------|-----------|
| 2026-06-28 | Proposed; P0: comfortable default, Локально, typography |

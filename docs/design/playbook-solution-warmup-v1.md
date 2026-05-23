# Playbook: solution warm-up v1

ADR: [0141](../adr/0141-solution-scoped-warmup-orchestration.md)

## Что прогревается при open solution

| Id | Когда | Где кэш |
|----|-------|---------|
| `bracket.active_file` | Сразу (P0) | `BracketMemberCompletionProvider` in-memory |
| `hci.poke` | Сразу (отдельно) | HCI SQLite + observer → symbol sidecar |
| `intercom.feed_anchors` | После HCI | Chips в ленте (in-memory) |
| `intercom.bracket` + `roslyn_l1` (open tabs) | После P1, параллельно ≤N | Bracket + `IntercomAttachmentRoslynWorkspaceCache` |

## Настройки

TOML `[solution_warmup]` в `settings.toml` (см. `defaults-settings.toml`).

`show_background_status_on_pfd` (по умолчанию `true`) — мастер-выключатель полосы warm-up/HCI.

Размещение по зонам — instrument **`workspace_background_status_v1`** (alias `background_status`) в **`[display.instruments]`** (ADR 0050, оверлей-слоты, не `pfd_primary`):

| Ключ | Зона |
|------|------|
| `pfd_status_strip` | колонка PFD (над картой / обозревателем) |
| `forward_status_strip` | Forward (над чатом / редактором) |

Значения: `background_status` (показать) или `none` (скрыть в этой зоне). По умолчанию в `defaults-settings.toml` — обе зоны `background_status`. Текст: «Indexing…» / «Warming…»; после успеха полоса скрывается (мин. ~400 ms); при ошибке — caution, клик → HCI.

## Для агента

- UI **не ждёт** `Ready` — только lazy/debounce fallback.
- Смена solution **отменяет** предыдущий прогон (`Cancelled`).
- Symbol sidecar при in-proc HCI **не** вызывать `ScheduleRebuildAfterHybridIndex` (observer 0.1.2+); fallback только если HCI выключен.

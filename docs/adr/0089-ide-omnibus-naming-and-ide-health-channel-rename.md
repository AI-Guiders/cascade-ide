# ADR 0089: Именование омнибуса агента (`get_ide_state`) и канал **IDE Health** (вместо Workspace Health)

**Статус:** Accepted  
**Дата:** 2026-04-23  

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | паритет отладки — **отдельное** решение; этот ADR **не** меняет семантику DAP/snapshot, только имена и границы терминов |
| [0036](0036-cds-channel-compositor-surface-pipeline.md) | канал → CDS → композитор → surface; **конвейер** тот же |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | контракты, тесты |
| [0052](0052-agent-contract-cli-and-snapshot-tests.md) | снапшоты контракта агента |

### Вне ADR

| Документ | Роль |
|----------|------|
| [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) | MCP PROTOCOL |
**Резюме:** снять путаницу между **workspace = каталог/репо** и **снимок состояния IDE** для агента; снять путаницу между **Workspace Health** (полоса наблюдаемости) и «workspace на диске». Решение: **переименовать** MCP-омнибус `get_workspace_state` → **`get_ide_state`**; **переименовать** продуктово и в коде канал **Workspace Health** → **IDE Health** (конкретные идентификаторы — по соглашению репо, напр. `IdeHealth*`).

---

<a id="adr0089-context"></a>

## 1. Контекст

1. Сегодня **`get_workspace_state`** фактически отдаёт **снимок ручки IDE** (solution, редактор, брейкпоинты, `debug`, сборка, панели, `cockpit_surface` …), а не «только путь к папке». Имя **вводит в заблуждение** при обсуждении с агентами и в документации.
2. **Workspace Health** в кокпите — про **сборку / тесты / отладку / git** в полосе; слово *workspace* снова тянет смысл «директория проекта», хотя речь о **среде разработки в IDE**.
3. Реализация **единого debug-snapshot** ([0002](0002-debug-human-agent-parity.md)) **ортогональна** этому ADR: сначала или параллельно можно ввести `DebugSnapshot`, читая его и под старым именем омнибуса, пока **этот** ADR не переименует тул.

<a id="adr0089-decision"></a>

## 2. Решение

1. **MCP / `IdeCommands`:** публичное имя инструмента и `command_id` — **`get_ide_state`**, тул MCP — **`ide_get_ide_state`** (см. [MCP-PROTOCOL.md](../MCP-PROTOCOL.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)); внутренний метод — **`IIdeMcpActions.GetIdeStateAsync`**.
2. **Канал health:** переименовать **Workspace Health** → **IDE Health**: неймспейсы `Cockpit/Channels/…`, типы `IWorkspaceHealthChannel` → `IIdeHealthChannel` (или иное единообразное имя), провайдер, композитор, строки UI, ссылки в ADR/README. **Семантика channel → CDS → compositor** из [0036](0036-cds-channel-compositor-surface-pipeline.md) **не** меняется.
3. **Документация и тесты:** [MCP-PROTOCOL.md](../MCP-PROTOCOL.md), [architecture-migration.md](../architecture-migration.md) при ссылке на тул, golden/approved JSON из [0052](0052-agent-contract-cli-and-snapshot-tests.md), при необходимости — одна строка в [architecture-policy.md](../architecture-policy.md).

<a id="adr0089-ui-scope"></a>

## 3. UI: что входит и что нет

- **Входит (минимально):** всё, что пользователь **читает** как «Workspace Health» или старое имя омнибуса в подсказках/доках — **заменить формулировки** на **IDE Health** / `get_ide_state` (ResX, строки в `Cockpit`, подписи полосы и т.д.). Это **тот же** контрол/композиция, **другое имя** (терминологическая правка, не фича).
- **Не входит:** новая вёрстка, смена слотов PFD/MFD, новый «дизайн» health-полосы, сценарии отладки в UI — это **другие ADR** (в т.ч. [0002](0002-debug-human-agent-parity.md) для **паритета отладки**: глифы, панель, привязка к snapshot — **не** из 0089).

<a id="adr0089-non-goals"></a>

## 4. Границы (прочее, что сюда не входит)

- **Не** дублировать [0002](0002-debug-human-agent-parity.md): не описывать здесь DAP, `DebugSnapshot`, удаление `show_debug_*` — только **именование** и **читаемость** границ «IDE vs workspace на диске».
- **Не** менять **CDS** и **топологию** регионов; только **подписи/имена**, где это чисто терминология.

<a id="adr0089-consequences"></a>

## 5. Последствия

- **Breaking change** для внешних клиентов MCP, которые вызывали `ide_get_workspace_state` / `get_workspace_state`: обновить на `ide_get_ide_state` / `get_ide_state`; алиасов нет.
- Крупный, но **механический** рефакторинг в `Cockpit/Channels` и строках — по возможности **отдельные логические коммиты** (омнибус MCP vs переименование канала vs доки).

<a id="adr0089-rejected"></a>

## 6. Отклонённые альтернативы

- **Оставить** `get_workspace_state` **без** синонима «IDE» — отклонено: накопленная путаница в обсуждениях CIDE.
- **Переименовать только** омнибус **без** health-канала — допустим как **фаза 1**; полное выравнивание терминов — цель ADR.

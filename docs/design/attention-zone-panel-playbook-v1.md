# Playbook: зона внимания ↔ панель shell ↔ SDK

**Статус:** чертёж v1.  
**Связь:** [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) (семантика), [ADR 0025](../adr/0025-sdk-attention-zones-and-capabilities.md) (контракты), [0010](../adr/0010-ui-modes-toml-configuration.md) (overlay).

## Зачем

Чтобы после фразы «эта поверхность в **PFD**» был **следующий шаг без догадок**: какой **id панели**, где **дефолт зоны** в рантайме, что **переопределяет** `workspace.toml`.

## Три слоя (не смешивать)

| Слой | Что это | Где живёт |
|------|---------|-----------|
| Семантика зоны | «Где в модели внимания» (PFD / Forward / …) | `PrimaryAttentionZoneId` в `UiSurfaceCapabilityDescriptor`, константы `AttentionZoneCanonicalIds` |
| Панель shell | Стабильный ключ панели в сетке / доке | `HostAttentionPanelId` ↔ `AttentionPanelCanonicalIds` / `AttentionPanelIds` |
| Презентация | Видимость, размер, вкладки | TOML overlay ([0010](../adr/0010-ui-modes-toml-configuration.md)), Axaml |

## Дефолтная карта панель → зона

Источник в коде: `AttentionZonePanelRuntime.BuildDefaultMap()` (после загрузки workspace накладываются переопределения из `attention_zone_panels`).

| Панель (`AttentionPanelIds`) | Дефолтная зона (канонический id) | Примечание |
|------------------------------|-------------------------------------|------------|
| `solution_explorer` | `pfd` | контекст workspace слева |
| `chat_panel` | `mfd` | |
| `git` | `mfd` | |
| `terminal_dock` | `mfd` | |
| `editor` | `forward` | лобовое |
| `editor_hud` | `hud` | слой над редактором, не отдельная колонка |

EICAS как **канал** оповещений в этой таблице не как отдельная строка-панель — см. ADR 0021.

## Что делать при добавлении UI surface

1. Выбрать **канонический id зоны** (`AttentionZoneCanonicalIds`) для ментальной модели.
2. Если поверхность = **конкретная панель** дока: задать **`HostAttentionPanelId`** тем же строковым id, что в таблице выше (или добавить новый id в `AttentionPanelCanonicalIds`, `AttentionPanelIds`, дефолт в `AttentionZonePanelRuntime`).
3. Заполнить **оба** поля (`PrimaryAttentionZoneId` + `HostAttentionPanelId`) — тогда при `BuildMap()` проверка **`CapabilityAttentionConsistency`** сравнит с текущей картой рантайма и выведет предупреждение в Debug при рассинхроне.
4. Для переопределения **места** панели пользователем — только **TOML** (`attention_zone_panels`), не подмена семантики в SDK.

## Чего этот playbook не делает

- Не задаёт пиксели и не заменяет разметку Axaml.
- Не заставляет вешать зону на команды без привязки к панели.

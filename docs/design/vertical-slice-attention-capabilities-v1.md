# Вертикальный срез: зоны внимания + capabilities (проверка)

**Статус:** процедура v1.  
**Связь:** [attention-zone-panel-playbook-v1](attention-zone-panel-playbook-v1.md), [ADR 0025](../adr/0025-sdk-attention-zones-and-capabilities.md).

## Что проверяем

Сквозная цепочка: **`UiChromeCapabilitiesModule`** регистрирует поверхность **Solution Explorer** с парой `PrimaryAttentionZoneId` = `pfd` и `HostAttentionPanelId` = `solution_explorer` → при **`BuildMap()`** проверка **`CapabilityAttentionConsistency`** не находит рассинхрона → в **JSON-дампе** capability-map видны оба поля.

## Автоматическая проверка

В репозитории:

```text
dotnet test CascadeIDE.Tests/CascadeIDE.Tests.csproj -c Debug --filter "FullyQualifiedName~VerticalSliceAttentionCapabilitiesTests"
```

Тест **`VerticalSliceAttentionCapabilitiesTests`** поднимает тот же дескриптор через модуль и реестр.

## Ручная проверка в IDE

1. Собрать и запустить CascadeIDE (`Debug`).
2. Выполнить команду **Dump capabilities map to file** (категория Debug/Diagnostics; capability `docs.markdown.dump_capabilities`).
3. В выводе/логе посмотреть путь к файлу `capabilities-*.json` в каталоге настроек → `diagnostics\`.
4. Открыть JSON и найти объект в **`uiSurfaces`** с **`id`**: `ui.chrome.surface.solution_explorer`.
5. Убедиться:
   - **`primaryAttentionZoneId`** = `pfd`
   - **`hostAttentionPanelId`** = `solution_explorer`
6. (Опционально) В окне **Вывод** отладчика при старте/дампе не должно быть строк `CapabilityAttentionConsistency:` с этой поверхностью (при корректной паре их нет).

## Негативный сценарий (по желанию)

Временно выставить в `UiChromeCapabilitiesModule` для той же поверхности `PrimaryAttentionZoneId = mfd` при неизменном `HostAttentionPanelId` → при следующем **`BuildMap()`** в Debug появится предупреждение о несовпадении → вернуть `pfd`.

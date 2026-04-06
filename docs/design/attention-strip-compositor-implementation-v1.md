# Полоса телеметрии и композитор внимания — implementation map (v1)

**Статус:** живой чертёж (не ADR). **Решения и термины** — в [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) (PFD/MFD/EICAS, ARINC 661-идеи). Здесь — **где в коде** и **что дальше**, чтобы не раздувать ADR.

---

## 1. Идея в одном абзаце

Несколько источников (сборка, тесты, отладка, git) **подают состояние и строки**; **один** слой (`AttentionStripCompositor`) задаёт **порядок и состав** сегментов для нижней полосы телеметрии. Разметка: [`TelemetryStripView`](../../Views/TelemetryStripView.axaml) (Balanced/Focus vs Power cockpit).

---

## 2. Карта файлов

| Компонент | Путь | Роль |
|-----------|------|------|
| Снимок входов | `Features/UiChrome/AttentionStripInputSnapshot.cs` | `AttentionStripInputSnapshot` + `AttentionStripSegmentInput` (build/tests/debug/git). Точка расширения без раздувания сигнатур. |
| Композитор | `Features/UiChrome/AttentionStripCompositor.cs` | `Rebuild(ObservableCollection<AttentionStripSegment>, AttentionStripInputSnapshot)`; порядок: Build → Tests → Debug → Git; `IsBuildRunning` только на сегменте Build. |
| Модель сегмента | `Features/UiChrome/AttentionStripSegment.cs` | `LineText` (полная строка), `CockpitShort` (Power), флаги для шаблона. |
| Источник enum | `Features/UiChrome/AttentionStripSource.cs` | `Build`, `Tests`, `Debug`, `Git`. |
| Форматирование строк | `Features/UiChrome/AttentionStripTelemetryFormat.cs` | Статические сегменты `BuildSegment` / `TestsSegment` / `DebugSegment` / `GitSegment` и `Compose(...)` — чистая логика без VM/DAP; удобно для юнит-тестов. |
| Провайдер снимка | `Features/UiChrome/IAttentionStripTelemetryProvider.cs`, `AttentionStripTelemetryProvider.cs` | `GetSnapshot()` собирает входы (build/tests/DAP/instrumentation/git из `UiChromeViewModel`) в `AttentionStripInputSnapshot`. `MainWindowViewModel` не знает текст каждой строки по отдельности — только держит провайдер и передаёт снимок в композитор. |
| VM | `ViewModels/MainWindowViewModel.AttentionStrip.cs` | `RebuildAttentionStrip()` вызывает `AttentionStripCompositor.Rebuild(AttentionStripSegments, _attentionStripTelemetry.GetSnapshot())`. |
| Инвалидация | `ViewModels/MainWindowViewModel.LayoutNotifications.cs` | `RebuildAttentionStrip` при смене телеметрии build/tests/debug. |
| Git-строки | `Features/UiChrome/UiChromeViewModel.cs` | `TelemetryGitText`, `TelemetryGitCockpitShort`; подписка в `MainWindowViewModel` на `Chrome.PropertyChanged`. |
| Свойства для UI | `ViewModels/MainWindowViewModel.Presentation.cs` | `TelemetryBuild*` / `TelemetryTests*` / `TelemetryDebug*` читают сегменты из `_attentionStripTelemetry.GetSnapshot()`; флаги сессии отладки по-прежнему из DAP. |
| UI | `Views/TelemetryStripView.axaml` | `ItemsControl` по `AttentionStripSegments`; разные шаблоны для Power vs остальные режимы. |
| Тесты | `CascadeIDE.Tests/AttentionStripCompositorTests.cs`, `AttentionStripTelemetryFormatTests.cs` | Композитор: порядок, `IsBuildRunning`. Формат: сегменты и `Compose` для снимка. |

---

## 3. Поток данных (кратко)

1. Состояние меняется (сборка, тесты, DAP, git, …).
2. Свойства `Telemetry*` уведомляют UI (частично через `[NotifyPropertyChangedFor]`, частично явный `OnPropertyChanged` для отладки).
3. `RebuildAttentionStrip()` берёт снимок через `IAttentionStripTelemetryProvider.GetSnapshot()` (внутри — делегаты/DAP/`UiChromeViewModel` + `AttentionStripTelemetryFormat`) и вызывает `AttentionStripCompositor.Rebuild`.
4. `AttentionStripSegments` обновляется; привязка к `TelemetryStripView`.

Альтернативная реализация провайдера (агент, MCP, моки в тестах VM) подменяет только сбор снимка, не композитор и не разметку полосы.

---

## 4. Статус vs ADR 0021 (первая строка таблицы ARINC)

| Идея ADR | В коде сейчас |
|----------|----------------|
| Один композитор «стекла», много источников | Да: один `Rebuild` + снимок входов. |
| Источники не владеют отдельным слоем toast без правил | Частично: строки централизованы; отдельные toast-цепочки не сводились сюда. |
| EICAS / Warning–Caution–Advisory | Нет: сегменты без уровня приоритета; полоса не EICAS-лента. |
| Декларативный merge из TOML | Частично: видимость полосы через capabilities/режимы; **порядок/состав** сегментов пока не из конфига. |

---

## 5. Следующие шаги (приоритет на усмотрение продукта)

1. **Пустые / placeholder-сегменты** — правило показа (всегда 4 слота vs только непустые) и согласование с Dark Cockpit (ADR §6).
2. **Приоритет / сортировка** — поля уровня в снимке или отдельный шаг ранжирования перед `Rebuild` (связь с §5 ADR).
3. **Канал EICAS** — отдельные сегменты или вторая полоса в том же контуре композитора, без нового «слоя уведомлений вне правил».
4. **Провайдер телеметрии** — сделано: `IAttentionStripTelemetryProvider`, `AttentionStripTelemetryProvider`, `AttentionStripTelemetryFormat` (см. таблицу выше).
5. **Конфиг** — какие сегменты включены в каком UI-режиме (расширение capabilities или TOML).

---

## 6. Связанные документы

- [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) — модель внимания, зоны, EICAS, метрики.
- [ADR 0010](../adr/0010-ui-modes-toml-configuration.md) — режимы UI и capabilities.
- [`cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md) — раскладка окон.

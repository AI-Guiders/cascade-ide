# Полоса телеметрии и композитор внимания — implementation map (v1)

**Статус:** живой чертёж (не ADR). **Решения и термины** — в [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) (PFD/MFD/EICAS, ARINC 661-идеи). Здесь — **где в коде** и **что дальше**, чтобы не раздувать ADR.

---

## 1. Термины (глоссарий)

| Термин | Смысл |
|--------|--------|
| **Зона PFD / MFD / EICAS** | Семантическая роль участка UI из ADR 0021: первичный контекст, вторичные потоки, канал оповещений. **Где** на экране задаётся **пресетом** (TOML/capabilities), не перетаскиванием в сессии. |
| **Снимок телеметрии внимания** | `AttentionStripInputSnapshot`: нормализованные входы (build/tests/debug/git) до композитора. Не привязан к форме «полоски». |
| **Композитор смысла (semantic)** | `AttentionStripCompositor`: из снимка собирает упорядоченные `AttentionStripSegment` (порядок, флаги вроде `IsBuildRunning`). Отвечает за **состав каналов**, не за пиксели и не за зону PFD/MFD. |
| **Раскладка зоны / страницы (chrome layout)** | Куда на экране попадают блоки: полоса снизу, сетка на странице MFD, карточка в PFD. Задаётся **пресетом** и шаблонами (AXAML) и/или отдельным слоем в коде; рабочее имя в дизайне — *compositor страницы зоны* / *display page layout*. Только **геометрия контейнера** в зоне, не дублирует порядок build/tests — тот уже зафиксирован композитором смысла. |
| **Поверхность (surface)** | Способ **показать** те же сегменты: полоса, страница, карточка в хроме и т.д. Выбор Strip vs Page (и зона размещения) — **настройки пользователя / пресета**; **снимок и композитор смысла от этого не зависят**: одни и те же данные, разный только host UI. |
| **Strip (полоса)** | Конкретная поверхность: узкая горизонтальная полоса на всю ширину контейнера — текущая реализация [`TelemetryStripView`](../../Views/TelemetryStripView.axaml). |
| **Page (страница)** | Поверхность в зоне PFD или MFD: полноэкранная/вкладка с тем же содержанием по смыслу, но без ограничения «одна строка на сегмент»; переход осознанный, не замена scan по полоске. |
| **Канал EICAS** | Оповещения по приоритету (Warning / Caution / Advisory) — в ADR отдельно от «телеметрии контура работы»; может быть полосой, списком, оверлеем (см. §5 ADR 0021). Не смешивать терминологически с build/tests/debug/git, если только пресет явно не объединяет их визуально. |

Имена в коде (`AttentionStrip*`, `TelemetryStripView`) исторически говорят о **полосе**; при появлении страницы/другой поверхности имеет смысл в коде постепенно говорить о **telemetry surface** или оставить композитор смысла нейтральным по имени, а «Strip» только в типе нижней полосы.

**Показ и данные развязаны:** кто-то выберет **Strip** (статус всегда внизу), кто-то **Page** (та же телеметрия на странице зоны) — это решение **настроек**, не ветвление логики снимка. Strip отнимает высоту у лобового; Page не отнимает ту же полосу, но требует перехода взгляда — пользователь сам решает, что важнее, **не меняя** источники данных и `AttentionStripCompositor`.

---

## 2. Идея в одном абзаце

Несколько источников (сборка, тесты, отладка, git) **подают состояние и строки**; **один** слой (`AttentionStripCompositor`) задаёт **порядок и состав** сегментов — независимо от того, пользователь включил **Strip** или **Page**. Текущая разметка полосы: [`TelemetryStripView`](../../Views/TelemetryStripView.axaml) (Balanced/Focus vs Power cockpit).

---

## 3. Карта файлов

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
| Хост полосы (нижняя поверхность) | `Views/WorkspaceTelemetrySurfaceHostView.axaml` | Сетка колонок как у `MainGrid` (0–4); вложенный `TelemetryStripView`. Включение: `ShowTelemetryStrip` (`telemetry_strip` + `TelemetryUiSurface.BottomStrip` в capabilities). |
| UI полосы | `Views/TelemetryStripView.axaml` | `ItemsControl` по `AttentionStripSegments`; разные шаблоны для Power vs остальные режимы. |
| Тесты | `CascadeIDE.Tests/AttentionStripCompositorTests.cs`, `AttentionStripTelemetryFormatTests.cs` | Композитор: порядок, `IsBuildRunning`. Формат: сегменты и `Compose` для снимка. |

---

## 4. Поток данных (кратко)

1. Состояние меняется (сборка, тесты, DAP, git, …).
2. Свойства `Telemetry*` уведомляют UI (частично через `[NotifyPropertyChangedFor]`, частично явный `OnPropertyChanged` для отладки).
3. `RebuildAttentionStrip()` берёт снимок через `IAttentionStripTelemetryProvider.GetSnapshot()` (внутри — делегаты/DAP/`UiChromeViewModel` + `AttentionStripTelemetryFormat`) и вызывает `AttentionStripCompositor.Rebuild`.
4. `AttentionStripSegments` обновляется; привязка к `TelemetryStripView` (через `WorkspaceTelemetrySurfaceHostView` в `MainWindow`).

Альтернативная реализация провайдера (агент, MCP, моки в тестах VM) подменяет только сбор снимка, не композитор и не разметку полосы.

---

## 5. Статус vs ADR 0021 (первая строка таблицы ARINC)

| Идея ADR | В коде сейчас |
|----------|----------------|
| Один композитор «стекла», много источников | Да: один `Rebuild` + снимок входов. |
| Источники не владеют отдельным слоем toast без правил | Частично: строки централизованы; отдельные toast-цепочки не сводились сюда. |
| EICAS / Warning–Caution–Advisory | Нет: сегменты без уровня приоритета; полоса не EICAS-лента. |
| Декларативный merge из TOML | Частично: видимость полосы через capabilities/режимы; **порядок/состав** сегментов пока не из конфига. |

---

## 6. Следующие шаги (приоритет на усмотрение продукта)

1. **Пустые / placeholder-сегменты** — правило показа (всегда 4 слота vs только непустые) и согласование с Dark Cockpit (ADR §6).
2. **Приоритет / сортировка** — поля уровня в снимке или отдельный шаг ранжирования перед `Rebuild` (связь с §5 ADR).
3. **Канал EICAS** — отдельные сегменты или вторая полоса в том же контуре композитора, без нового «слоя уведомлений вне правил».
4. **Провайдер телеметрии** — сделано: `IAttentionStripTelemetryProvider`, `AttentionStripTelemetryProvider`, `AttentionStripTelemetryFormat` (см. таблицу выше).
5. **Конфиг** — какие сегменты включены в каком UI-режиме (расширение capabilities или TOML).
6. **Раскладка зоны без нижней полосы** — пресеты/шаблоны, где телеметрия не strip (страница MFD, блок PFD); общий контур: тот же снимок + `AttentionStripCompositor`, другой host UI и раскладка страницы зоны.

---

## 7. Связанные документы

- [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md) — модель внимания, зоны, EICAS, метрики.
- [ADR 0010](../adr/0010-ui-modes-toml-configuration.md) — режимы UI и capabilities.
- [`cascade-ide-ui-layout-v1.md`](../ux/cascade-ide-ui-layout-v1.md) — раскладка окон.

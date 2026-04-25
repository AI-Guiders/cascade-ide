# P/F/M: геометрия зон и Skia-волны (1–3)

**Статус:** Wave 1–2 — в продукте как основа и опциональный отладочный слой; Wave 3 — *in progress* (mount-инструмент и style).

Один сюжет: от **топологии зон** из строки презентации до **Skia-слоя** с контентом инструмента — без смешивания с «мультиоконностью как таковой» (это [ADR 0017](../adr/0017-multi-window-workspace-and-agent-surfaces.md)) и без дублирования [черновика про оверлеи vs поверхности](../design/skia-surfaces-vs-overlays-v1.md).

---

## Обзор волн

| Волна | Что даёт | Ключи / точки входа | Примечание |
|-------|-----------|---------------------|------------|
| **1** | Топология колонок **P / Forward / M** на главном окне: видимость регионов, ширины, связь с пресетом и хостом MFD | `[display].presentation` / `zone_screen_layout`, грамматика `[presentation_grammar]`, разбор в `PresentationParser`, колонки `MainGrid` через `PresentationMainGridColumnDefinitions` и композитор shell | Источник истины — **ADR 0017**; Skia здесь **не обязателен** — это Avalonia-layout и модель намерений (expand/collapse регионов). |
| **2** | **Отладочные контуры** зон поверх текущего layout — проверить геометрию без смены контента слотов | `[display].show_skia_zone_geometry_overlay` → `DisplaySettings.ShowSkiaZoneGeometryOverlay`, привязки `IsSkiaZoneGeometryOverlayPfdVisible` и т.д., оверлеи `SkiaZoneGeometryOverlayPfd` / `Forward` / `Mfd` в `MainWindow.axaml`, аналог в `MfdHostWindow.axaml` | Включено временно для валидации; по умолчанию выключено. |
| **3** | **Mount-слой** инструмента (контент карточки и т.п.) в зоне **поверх** базового контента слота, плюс декларативный style/registry | `[display].use_skia_instrument_mount`, `instrument_mount_style`, `[[display.instrument_mount_style_rules]]` — см. раздел ниже | Ортогонально карте «какой `instrument_id` в слоте» ([ADR 0050](../adr/0050-declarative-instrument-zone-placement-toml.md)). |

Волны **накладываются**: 1 задаёт *где* рамки зон; 2 — *нарисовать рамки для глаз*; 3 — *что показать в mount поверх* при уже разрешённой геометрии.

---

## Волна 1 — топология зон (без Skia-инструмента)

- Строка **`presentation`** (и синоним `zone_screen_layout`) задаёт **сколько групп экранов** и **какие якоря** в каждой; парсер и инварианты — ADR 0017, код: `Services/Presentation/PresentationParser.cs`, `PresentationMainGridColumnDefinitions`.
- **Композитор shell** (`MainWindowShellSurfaceCompositor` и связанный ввод) определяет, **видны ли** колонки PFD / Forward / MFD в `MainGrid`, ширины MFD и т.д. — это *intent геометрии*, не CDS и не mount.
- Топология **main + отдельный MFD-хост** (`AttentionLayoutSurfaceKind`, подавление колонки MFD в main при открытом `MfdHostWindow`) — всё ещё волна 1, см. ADR 0017 §про хост.

---

## Волна 2 — overlay контуров зон (отладка геометрии)

- Назначение: **визуально сверить** границы P/F/M с моделью презентации**, не меняя** привязанный к слотам контент.
- Реализация: полупрозрачные **Border**-оверлеи и флаги видимости из VM (`ShowSkiaZoneGeometryOverlay`, `IsSkiaZoneGeometryOverlay*`).
- Не путать с **волной 3**: контуры — только рамки; mount — данные инструмента (IDE Health и т.д.).

---

## Волна 3 — mount-инструмент и style (*реализовано / in progress*)

### Цель волны 3

Перенос не только геометрии shell, но и **содержимого инструмента** в управляемый mount-слой (Skia-style пайплайн и общий host), с **минимально безопасным** шагом и фичей по умолчанию выключенной.

### Реализовано

1. Первый реальный **instrument-content** в mount-слое:
   - `Views/MainWindow.axaml`
   - компактная PFD-карта на живых данных VM (`build/tests/debug/safety`).
2. Интеграция в PFD зону:
   - `Views/MainWindow.axaml`
   - overlay поверх текущего контента (`SolutionExplorerView`), без замены базовой панели.
3. Флаг в пользовательских настройках:
   - `[display].use_skia_instrument_mount`
   - модель: `DisplaySettings`, прокси в VM: `UseSkiaInstrumentMount`.
4. Второй реальный mini-instrument в mount-слое для MFD:
   - `Views/MfdHostWindow.axaml`
   - компактная MFD-карта на тех же живых данных VM для проверки паритета отдельного MFD-хоста.
5. Единый **mount-layer host**:
   - `Views/ZoneInstrumentMountView.axaml(.cs)`
   - PFD и MFD монтируют один и тот же host-control с параметрами темы.
6. Декларативный выбор mini-инструмента:
   - `instrument_id` + `slot_id` + `mount_style` в `ZoneInstrumentMountView`.
   - `Views/MainWindow.axaml` и `Views/MfdHostWindow.axaml` передают декларативные параметры.
   - `ZoneInstrumentMountPolicy` резолвит скин/заголовок по style (`instrument_mount_v1`) и slot.
7. Источник `mount_style` в настройках:
   - `[display].instrument_mount_style`
   - VM-proxy: `MainWindowViewModel.InstrumentMountStyle`
   - `MainWindow` и `MfdHostWindow` bind-ят `MountStyle` из VM.
8. Style-registry:
   - `[[display.instrument_mount_style_rules]]` c полями `surface_id`, `slot_id`, `instrument_id`, `mount_style`.
   - резолв в VM: `ResolveInstrumentMountStyle(surface_id, slot_id, instrument_id)` с приоритетом поверхности, затем global `*`, внутри слоя: `exact -> slot/* -> */instrument -> */* -> fallback`.
   - `MainWindow`/`MfdHostWindow` bind-ят slot-specific style (`PfdInstrumentMountStyle`, `MfdInstrumentMountStyle`).
9. Резолв style: **Strategy + Specification**:
   - `IInstrumentMountPolicyResolver` + `SettingsBackedInstrumentMountPolicyResolver`.
   - `InstrumentMountPolicyRuleMatchesSpecification`.
10. Eligibility-gate для rollout по метрикам:
    - `RolloutMetricsEligibilitySpecification` и поля `[display]` (`enforce_instrument_mount_style_eligibility`, score-пороги, `require_instrument_mount_style_scores`).
11. Placement-spec для host-surface:
    - `InstrumentPlacementSpecification` + `CockpitInstrumentPlacementRules`.
    - `MainWindowHostSurfaceCompositor` монтирует инструмент только при прохождении placement-rule.
12. Валидация конфигурации:
    - `DisplaySettingsValidationSpecification` при `SettingsService.Load()`.

### Принцип rollout (волна 3)

- Не ломаем текущий UX: mount **поверх** существующего содержимого слота.
- Фича **выключена по умолчанию** (`use_skia_instrument_mount`).
- Цель итерации — отладить pipeline контентного overlay на реальных данных, не ломая маршрутизацию инструментов в shell.

### Следующий шаг (волна 3)

1. Перевести content-binding с фиксированных VM-полей на декларативный контракт payload (через `instrument_id` и typed data-source), чтобы mount не знал про конкретные поля `IdeHealth*`.
2. Перенести resolver из VM в отдельный surface/style service рядом с `CockpitInstrumentDescriptor`, а не только в UI binding.

---

## История файла

| Было | Стало |
|------|--------|
| `pfm-zone-geometry-wave3.md` (только волна 3) | Этот документ: **волны 1–3** в одном месте, чтобы не плодить три черновика по одной оси. |

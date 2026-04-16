# P/F/M Zone Geometry — Wave 3

**Статус:** поставка **wave 3** (preview + единый mount + policy/placement по слотам) — **закрыта** (реализовано ниже, пп. 1–12). **Follow-up** (декларативный payload для mount, единый `surface_id` с композитором, резолв policy рядом с host-surface) — **закрыт**, см. [§ Закрытый follow-up](#wave3-follow-up-done).

## Цель

Начать перенос не только геометрии, но и содержимого инструментов на Skia-style отрисовку, с минимальным безопасным шагом.

## Реализовано (wave 3 — scope закрыт)

1. Добавлен первый реальный instrument-content preview:
   - `Views/MainWindow.axaml`
   - компактная PFD-карта на живых данных VM (`build/tests/debug/safety`).
2. Интеграция в PFD зону:
   - `Views/MainWindow.axaml`
   - overlay поверх текущего контента (`WorkspaceNavigationMapView`), без замены базовой панели.
3. Флаг в пользовательских настройках:
   - `[display].use_skia_instrument_wave3_preview`
   - модель: `DisplaySettings`, прокси в VM: `UseSkiaInstrumentWave3Preview`.
4. Добавлен второй реальный mini-instrument preview для MFD:
   - `Views/MfdHostWindow.axaml`
   - компактная MFD-карта на тех же данных; видимость превью в отдельном окне — `IsMfdHostWindowWorkspaceHealthMountVisible` (паритет с PFD при вынесенном вторичном контуре).
5. Введён единый mount-layer host для mini-инструмента:
   - `Views/ZoneInstrumentMountView.axaml(.cs)`
   - PFD и MFD монтируют один и тот же host-control; скин/заголовок по `InstrumentId`/`SlotId`/`SlotPolicy` (`ZoneInstrumentMountPolicy`).
6. Декларативный контракт mount (без привязки к полям главного VM):
   - типы `WorkspaceHealthStatusMountPayload` / `WorkspaceHealthStatusMountContext` (`Cockpit/Composition/HostSurface/`);
   - `ZoneInstrumentMountView`: `DataContext` — контекст, строки — `Payload.*` (build/tests/debug/safety);
   - `MainWindow` / `MfdHostWindow`: `DataContext` mount через `ElementName` окна (видимость и контекст не конфликтуют с compiled binding).
7. Источник `slot_policy` вынесен из hardcode в настройки:
   - `[display].instrument_mount_slot_policy`
   - VM-proxy: `MainWindowViewModel.InstrumentMountSlotPolicy`
   - slot-specific policy для отладки/прокси: `PfdInstrumentMountSlotPolicy` / `MfdInstrumentMountSlotPolicy` — совпадают с `SlotPolicy` в соответствующем `WorkspaceHealthStatusMountContext` (единый резолв через фабрику).
8. Policy-registry для декларативного резолва:
   - `[[display.instrument_mount_policy_rules]]` c полями `surface_id`, `slot_id`, `instrument_id`, `slot_policy`.
   - Резолв согласован с кадром хоста: один и тот же **`MainWindowHostSurfaceIds`** (`DockedGrid` / `PlusMfdHostTopLevel`), что и `MainWindowHostSurfaceCompositor`, CDS (`CockpitSurfaceSnapshotBuilder`) и `MountPolicyRuntimeSurfaceId` в VM.
   - Приоритет правил: сначала runtime-поверхность (`ActiveAttentionLayoutSurface`), затем global (`surface_id = "*"`); внутри слоя: `exact → slot/* → */instrument → */* → fallback`.
9. Резолв policy выделен в Strategy + Specification:
   - `IInstrumentMountPolicyResolver` + `SettingsBackedInstrumentMountPolicyResolver`;
   - сборка контекста mount: `WorkspaceHealthMountContextFactory.Create(...)` (resolver + `surface_id` + slot + payload).
   - match-логика правила: `InstrumentMountPolicyRuleMatchesSpecification`.
10. Добавлен eligibility-gate для rollout policy по метрикам:
   - `RolloutMetricsEligibilitySpecification` проверяет SA/perf/workload для rule.
   - gate управляется полями `[display]`:
     `enforce_instrument_mount_policy_eligibility`,
     `instrument_mount_policy_min_sa_score`,
     `instrument_mount_policy_min_performance_score`,
     `instrument_mount_policy_max_workload_score`,
     `require_instrument_mount_policy_scores`.
11. Для host-surface добавлен placement-spec слой (instrument routing by surface/slot/safety):
   - `InstrumentPlacementSpecification` + `CockpitInstrumentPlacementRules` (допустимые поверхности — те же `MainWindowHostSurfaceIds`, что и у композитора).
   - `MainWindowHostSurfaceCompositor` монтирует инструмент только при прохождении placement-rule.
12. Добавлена validation-спецификация конфигурации:
   - `DisplaySettingsValidationSpecification` проверяет score-диапазоны и обязательные поля rules.
   - вызывается при `SettingsService.Load()`; нарушения логируются в diagnostics.

## Принцип rollout

- Не ломаем текущий UX: preview только поверх существующего содержимого.
- Фича выключена по умолчанию.
- Цель итерации — отладить pipeline контентного overlay на реальных данных, не затрагивая маршрутизацию инструментов.

<a id="wave3-follow-up-done"></a>

## Закрытый follow-up (после закрытия scope wave 3)

Сделано в коде:

1. **Декларативный payload:** `WorkspaceHealthStatusMountPayload` в разметке mount; нет биндингов на `WorkspaceHealth*` поля главного VM в `ZoneInstrumentMountView`.
2. **Единый `surface_id`:** `MainWindowHostSurfaceIds` — композитор, CDS, placement rules и резолв mount-policy используют одни и те же строковые константы.
3. **Policy рядом с compositor:** `WorkspaceHealthMountContextFactory` + `IInstrumentMountPolicyResolver`; VM отдаёт зависимости и payload, не дублирует ad-hoc резолв для контекста.
4. **Уведомления при смене MFD host window:** `MfdHostShellOpenInvalidatedPropertyNames` в `SetMfdHostWindowShellOpen` — один список имён свойств для `PropertyChanged`.

Отдельной **wave 4** в репозитории не заведено; логичное направление «вперёд» — настоящий Skia-backend слотов (вместо Avalonia-overlay), если появится продуктовый приоритет.

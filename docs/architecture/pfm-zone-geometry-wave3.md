# P/F/M Zone Geometry — Wave 3

Статус: in progress.

## Цель

Начать перенос не только геометрии, но и содержимого инструментов на Skia-style отрисовку, с минимальным безопасным шагом.

## Реализовано

1. Добавлен первый реальный instrument-content preview:
   - `Views/MainWindow.axaml`
   - компактная PFD-карта на живых данных VM (`build/tests/debug/safety`).
2. Интеграция в PFD зону:
   - `Views/MainWindow.axaml`
   - overlay поверх текущего контента (`SolutionExplorerView`), без замены базовой панели.
3. Флаг в пользовательских настройках:
   - `[display].use_skia_instrument_wave3_preview`
   - модель: `DisplaySettings`, прокси в VM: `UseSkiaInstrumentWave3Preview`.
4. Добавлен второй реальный mini-instrument preview для MFD:
   - `Views/MfdHostWindow.axaml`
   - компактная MFD-карта на тех же живых данных VM (`build/tests/debug/safety`) для проверки паритета отдельного MFD-хоста.
5. Введён единый mount-layer host для mini-инструмента:
   - `Views/ZoneInstrumentMountView.axaml(.cs)`
   - PFD и MFD теперь монтируют один и тот же host-control с параметрами темы, а не дублируют разметку.
6. Добавлен декларативный слой выбора mini-инструмента:
   - `instrument_id` + `slot_id` + `slot_policy` в `ZoneInstrumentMountView`.
   - `Views/MainWindow.axaml` и `Views/MfdHostWindow.axaml` больше не задают ручную тему/заголовок, а передают декларативные параметры.
   - `ZoneInstrumentMountPolicy` резолвит скин/заголовок по policy (`wave3_preview_v1`) и slot.

## Принцип rollout

- Не ломаем текущий UX: preview только поверх существующего содержимого.
- Фича выключена по умолчанию.
- Цель итерации — отладить pipeline контентного overlay на реальных данных, не затрагивая маршрутизацию инструментов.

## Следующий шаг

1. Перевести content-binding с фиксированных VM-полей на декларативный контракт payload (через `instrument_id` и typed data-source), чтобы mount не знал про конкретные поля `WorkspaceHealth*`.
2. Добавить slot-policy registry из настроек/grammar (`presentation`) вместо hardcoded `wave3_preview_v1`.

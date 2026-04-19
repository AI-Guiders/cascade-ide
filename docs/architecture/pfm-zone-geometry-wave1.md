# P/F/M Zone Geometry — Wave 1

Статус: done.

## Цель

Стабилизировать геометрию зон `PFD | Forward | MFD` как прямую функцию строки `presentation`, чтобы текущий контур рендера опирался на единый детерминированный расчёт.

## Что сделано

1. Введён единый слой нормализации долей:
   - `Services/Presentation/PresentationZoneWeights.cs`
   - нормализует веса в группе экрана;
   - детерминирует сумму до `1.0`.
2. Введён DTO кадра геометрии:
   - `Services/Presentation/PresentationMainGridLayoutFrame.cs`
   - `Services/Presentation/PresentationMainGridLayoutFrameBuilder.cs`
3. `PresentationMainGridColumnDefinitions` переведён на централизованный расчёт через `PresentationMainGridLayoutFrameBuilder`.
4. Применение колонок `MainGrid` остаётся централизованным через `ApplyMainGridColumnDefinitions` в `MainWindow.PresentationLayout.axaml.cs` (строка из VM).
5. Добавлены базовые тесты:
   - `CascadeIDE.Tests/PresentationZoneWeightsTests.cs`.
   - `CascadeIDE.Tests/PresentationMainGridLayoutFrameBuilderTests.cs`.
6. Добавлен debug-only snapshot log геометрии:
   - `Views/MainWindow.PresentationLayout.axaml.cs` (`[MainGridGeometry] ...` в `Debug.WriteLine`);
   - лог включает `presentation`, строку колонок, число зон и нормализованные веса;
   - лог пишется только при изменении сигнатуры кадра (без спама повторов).

## Правила v1

- Если в группе экрана указаны коэффициенты, геометрия строится по ним (после нормализации).
- Если коэффициенты не указаны, сохраняется исторический `Default` для `MainGrid` (без изменения поведения текущих пресетов).
- Смешение якорей с коэффициентами и без коэффициентов в одной группе не допускается.

## Решение по rollout

- Для Wave 1 dual-path/feature-flag не вводится.
- Причина: внутренний контур с одним потребителем; приоритет — скорость поставки и упрощение.
- Контроль качества: debug-only geometry snapshot log + ручная визуальная валидация пресетов.

## Продолжение

- Следующий этап закрыт отдельным документом: `docs/architecture/pfm-zone-geometry-wave2.md`.

## Визуальная валидация

- Preset `presentation = "(0.5P+0.5F)(M)"` подтверждён вручную в debug-сборке: на главном экране зона `P` и `F` визуально 50/50, `M` вынесена в отдельный экран.

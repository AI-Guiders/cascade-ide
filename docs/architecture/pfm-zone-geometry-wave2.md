# P/F/M Zone Geometry — Wave 2

Статус: done.

## Цель

Довести геометрию до явных zone-bounds и добавить визуальный preview контуров зон без изменения контента.

## Что сделано

1. Расширен DTO кадра геометрии:
   - `PresentationMainGridLayoutFrame` теперь содержит `ZoneBounds`.
   - `PresentationZoneBound` описывает `Zone`, `StartNormalized`, `WidthNormalized`.
2. Расчёт bounds встроен в `PresentationMainGridLayoutFrameBuilder`:
   - bounds строятся для первого экрана в порядке якорей.
3. Добавлен preview контуров зон в `MainWindow.axaml`:
   - overlay `SkiaZonePreviewPfd` / `SkiaZonePreviewForward` / `SkiaZonePreviewMfd`;
   - не меняет содержимое зон, только визуально подсвечивает границы.
4. Добавлена настройка пользователя:
   - `[display].use_skia_zone_geometry_preview` (модель `DisplaySettings`).
   - VM-свойства видимости preview учитывают текущую геометрию колонок.
5. Усилен debug snapshot log:
   - `[MainGridGeometry]` теперь логирует и `bounds=[...]`.
6. Тесты дополнены проверкой zone-bounds:
   - `PresentationMainGridLayoutFrameBuilderTests`.

## Решение по rollout

- Полный dual-path для Wave 2 не вводится.
- Причина: внутренний продукт с одним потребителем; важнее скорость и простота.
- Контроль: debug snapshot + ручная визуальная валидация.

## Definition of Done

- Геометрия зон доступна не только как строка колонок, но и как явные bounds DTO.
- Preview контуров можно включить через пользовательскую настройку без изменений контента.
- Сборка и таргетные тесты presentation-слоя проходят.

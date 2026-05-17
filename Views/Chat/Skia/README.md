# Skia chat entities

## Два пути «карточек»

| Сущность | Рендер | Когда |
|----------|--------|--------|
| **Topic** (`SkiaChatTopicCard`) | `SkiaKit.SkiaSectionedCard` — секции Тема / Теги / Саммари; `SkiaTileGridLayout` | Картотека тем в overview |
| **Product spine** (`SkiaChatBubbleEntity` + `CardPanel`) | `SkiaChatBubbleRenderer` | Сквозная линия продукта над лентой |

Topic и spine **намеренно разделены**: topic = drill-in по `ThreadId`; spine = ортогональная линия ([ADR 0096](../../../docs/adr/0096-intercom-topic-card-summary-and-product-spine.md)).

## Пайплайн

`ChatSurfaceSnapshot` → `ChatSurfaceEntityFactory` (Creator) → `ISkiaChatEntity` → `SkiaChatLayoutEngine` → `SkiaChatSurfaceControl`.

Примитивы карточек/сетки/темы: [`Views/SkiaKit/README.md`](../../SkiaKit/README.md).

UI-строки: `ChatProductSpinePresentation`, `ChatThreadOverviewPresentation` (Features/Chat).

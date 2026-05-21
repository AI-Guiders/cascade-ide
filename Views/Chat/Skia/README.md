# Skia chat entities

## Два пути «карточек»

| Сущность | Рендер | Когда |
|----------|--------|--------|
| **Topic** (`SkiaChatTopicCard`) | `SkiaKit.SkiaSectionedCard` — секции Тема / Теги / Саммари; `SkiaTileGridLayout` | Картотека тем в overview |
| **Product spine** (`SkiaChatBubbleEntity` + `CardPanel`) | `SkiaChatBubbleRenderer` | Сквозная линия продукта над лентой |

Topic и spine **намеренно разделены**: topic = drill-in по `ThreadId`; spine = ортогональная линия ([ADR 0096](../../../docs/adr/0096-intercom-topic-card-summary-and-product-spine.md)).

## Лента сообщений (flat feed)

| Сущность | `SkiaChatBubbleKind` | Когда |
|----------|----------------------|--------|
| **Сообщения** (`SkiaChatMessageFeedEntity`) | `Feed` | user / agent / thinking / tool — role rail + тело через **`SkiaChatFeedLayout`** ([ADR 0123](../../../docs/adr/0123-intercom-full-skia-surface-evolution.md)) |
| **Slash outcome** (`SkiaChatSlashCommandEntity`) | отдельный flat row | `/help`, `/build run`, … |
| **Clarification** | `Feed` | пакеты уточнений |
| **Навигация** (заголовок ветки, «Назад к темам») | `Feed` | компактные meta-строки |

`SkiaChatBubbleKind.Standard` остаётся для legacy/особых случаев; **не** для обычных реплик в detail-ленте.

## Пайплайн

`ChatSurfaceSnapshot` → `ChatSurfaceEntityFactory` (Creator) → `ISkiaChatEntity` → `SkiaChatLayoutEngine` → `SkiaChatSurfaceControl`.

Примитивы карточек/сетки/темы: [`Views/SkiaKit/README.md`](../../SkiaKit/README.md).

UI-строки: `ChatProductSpinePresentation`, `ChatThreadOverviewPresentation` (Features/Chat).

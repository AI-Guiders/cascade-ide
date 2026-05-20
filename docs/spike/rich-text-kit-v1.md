# Spike: RichTextKit (Topten.RichTextKit) в Intercom Skia

**Ветка:** `feature/RichTextKit` → merge в `develop`  
**Дата:** 2026-05-20  
**Статус:** принято, feature-flag снят  
**Лицензия:** MIT — при необходимости форк по образцу [AvaloniaEdit](https://github.com/KarataevDmitry/AvaloniaEdit)

## Цель

Проверить, что layout/рендер inline и block markdown в Intercom можно отдать **RichTextKit**, не переписывая весь Intercom.

## Что вошло в продукт

- NuGet: `Topten.RichTextKit` 0.4.167
- `Views/Chat/Skia/SkiaRichTextKitMarkdown.cs` — inline + block (`TryMeasureDocument`)
- `SkiaChatBubbleRenderer` — RTK для body всех `SkiaChatBubbleKind`; title/footer — `DrawText`
- Slash `/help` detail, composer, mono code strips — RTK без флага
- `SkiaChatRenderLimits` — `MaxProseBodyLines` 128, `MaxDocumentRows` 256
- Тесты: `CascadeIDE.Tests/SkiaRichTextKitMarkdownTests.cs`

## Ограничения (ADR 0129)

- Полный Markdig в scroll **не** цель
- Заголовки `##` в **сообщениях ленты** (inline path) — plain; в slash detail и `.md` help — block path с heading styles
- Composer: каретка — эвристика `WrapLines`, не RTK hit-test

## Риски

- Транзитивный SkiaSharp 2.88 от RTK vs Avalonia 12.x — следить за runtime
- Релизы RTK редкие — fork MIT допустим

## Решение

**Берём NuGet.** Spike завершён, `SkiaRichTextKitFeature` удалён.

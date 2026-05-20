# Spike: RichTextKit (Topten.RichTextKit) в Intercom Skia

**Ветка:** `feature/RichTextKit` (от `develop`)  
**Дата:** 2026-05-20  
**Лицензия:** MIT — при необходимости форк по образцу [AvaloniaEdit](https://github.com/KarataevDmitry/AvaloniaEdit)

## Цель

Проверить, что layout/рендер inline markdown в ленте Intercom (`SkiaChatBubbleKind.Feed`) можно отдать **RichTextKit**, не переписывая весь Intercom.

## Что сделано

- NuGet: `Topten.RichTextKit` 0.4.167
- `Views/Chat/Skia/SkiaRichTextKitMarkdown.cs` — subset parser тот же (`SkiaMarkdownLayout.ParseInline`)
- `SkiaRichTextKitFeature.UseForIntercomFeedBody` (default `true` на ветке)
- `SkiaChatBubbleRenderer` — RTK только для **Feed** body; title/footer — как раньше
- Тесты: `CascadeIDE.Tests/SkiaRichTextKitMarkdownTests.cs`

## Ограничения (ADR 0129)

- Полный Markdig в scroll **не** цель spike
- Composer (`SkiaComposerStrip`) — пока без RTK

## Риски

- Транзитивный SkiaSharp 2.88 от RTK vs Avalonia 12.x — следить за runtime
- Релизы RTK редкие (последний NuGet 2024-03) — fork MIT допустим

## Следующие шаги

1. Визуально сравнить Feed в IDE (RTK on/off через `SkiaRichTextKitFeature`)
2. Composer spike или `SkiaSharp.HarfBuzz` only
3. Решение: остаться на NuGet / форк `AIGuiders.RichTextKit`

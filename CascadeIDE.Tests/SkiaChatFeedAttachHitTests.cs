#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaChatFeedAttachHitTests
{
    [Fact]
    public void ComputeFeedLinkHitRect_is_narrower_than_full_segment()
    {
        var segment = new SKRect(12, 80, 400, 104);
        var metrics = new SkiaChatBubbleMetrics(
            [new SkiaMarkdownLine([new SkiaMarkdownRun("[TryResolveFile]", SkiaMarkdownStyle.Link)])],
            Footer: null,
            TitleHeight: 0,
            FooterHeight: 0,
            LineHeight: 15);

        var hit = SkiaChatBubbleRenderer.ComputeFeedLinkHitRect(segment, "[TryResolveFile]", metrics);

        Assert.True(hit.Width < segment.Width - 40f);
        Assert.True(hit.Left > segment.Left);
        Assert.InRange(hit.Top, segment.Top, segment.Bottom);
    }

    [Fact]
    public void HitRegistry_find_prefers_attach_reveal_over_later_row_hit()
    {
        var registry = new SkiaChatHitRegistry();
        registry.RegisterControlRect(
            new Avalonia.Rect(0, 0, 400, 80),
            new SkiaChatHit(0, null, ResetDetailMode: false));
        registry.RegisterControlRect(
            new Avalonia.Rect(12, 20, 200, 24),
            new SkiaChatHit(0, null, ResetDetailMode: false, RevealAttachment: new AttachmentAnchor { File = "a.cs" }));

        var index = registry.FindIndex(new Avalonia.Point(20, 28));

        Assert.Equal(1, index);
        Assert.True(registry.TryGetHit(index, out var hit));
        Assert.NotNull(hit.RevealAttachment);
        Assert.True(SkiaChatHitRegistry.WantsHandCursor(hit));
    }

    [Fact]
    public void HitRegistry_content_rect_scroll_maps_to_control_viewport()
    {
        const float chromeTop = 48f;
        const float scrollOffset = 120f;
        var scrollInContext = scrollOffset - chromeTop;
        var registry = new SkiaChatHitRegistry();
        registry.RegisterContentRect(
            new SKRect(12, 200, 372, 220),
            scrollInContext,
            new SkiaChatHit(0, null, ResetDetailMode: false, RevealAttachment: new AttachmentAnchor { File = "a.cs" }));

        var controlY = chromeTop - scrollOffset + 200f;
        var controlPoint = new Avalonia.Point(20, controlY + 5);
        Assert.Equal(0, registry.FindIndex(controlPoint));
        Assert.True(registry.TryGetHit(0, out var hit));
        Assert.NotNull(hit.RevealAttachment);
    }

    [Fact]
    public void Attach_chip_classify_resolved_degraded_failed_pending()
    {
        var resolved = new AttachmentAnchor
        {
            File = "src/Foo.cs",
            ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeResolved,
        };
        Assert.Equal(
            IntercomAttachLinkVisualStatus.Resolved,
            SkiaIntercomAttachLinkChip.Classify(resolved, messagePending: false));

        var degraded = resolved with { ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeMemberNotFound };
        Assert.Equal(
            IntercomAttachLinkVisualStatus.Degraded,
            SkiaIntercomAttachLinkChip.Classify(degraded, messagePending: false));

        var failed = resolved with
        {
            File = "missing.cs",
            ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeFileMissing,
        };
        Assert.Equal(
            IntercomAttachLinkVisualStatus.Failed,
            SkiaIntercomAttachLinkChip.Classify(failed, messagePending: false));

        Assert.Equal(
            IntercomAttachLinkVisualStatus.Pending,
            SkiaIntercomAttachLinkChip.Classify(resolved, messagePending: true));
    }

    [Fact]
    public void RegisterFeedMarkdownLinkHits_registers_narrow_link_rects()
    {
        var registry = new SkiaChatHitRegistry();
        var drawContext = new SkiaChatDrawContext
        {
            Canvas = null!,
            Theme = SkiaChatTheme.DarkFallback,
            ContentLeft = 12,
            ContentWidth = 360,
            ScrollOffset = 0,
            ItemIndex = 0,
            HoveredItemIndex = -1,
            SelectedMessageIndex = -1,
            HitRegistry = registry,
        };

        var segment = new SKRect(12, 40, 372, 80);
        var metrics = new SkiaChatBubbleMetrics(
            [
                new SkiaMarkdownLine(
                [
                    new SkiaMarkdownRun("see ", SkiaMarkdownStyle.Plain),
                    new SkiaMarkdownRun("Run", SkiaMarkdownStyle.Link),
                    new SkiaMarkdownRun(" ok", SkiaMarkdownStyle.Plain),
                ]),
            ],
            Footer: null,
            TitleHeight: 0,
            FooterHeight: 0,
            LineHeight: 15);

        var anchor = new AttachmentAnchor { File = "Foo.cs", MemberKey = "Run", DisplayLabel = "Run" };
        SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHits(
            drawContext,
            segment,
            metrics,
            messageIndex: 3,
            linkText => string.Equals(linkText, "Run", StringComparison.Ordinal) ? anchor : null);

        Assert.Equal(1, registry.Count);
        Assert.Equal(0, registry.FindIndex(new Avalonia.Point(50, 48)));
        Assert.Equal(-1, registry.FindIndex(new Avalonia.Point(12, 40)));
        Assert.True(registry.TryGetHit(0, out var hit));
        Assert.Equal(3, hit.MessageIndex);
        Assert.Equal("Run", hit.RevealAttachment?.MemberKey);
    }

    [Fact]
    public void HitRegistry_find_prefers_chrome_action_over_row_hit()
    {
        var registry = new SkiaChatHitRegistry();
        registry.RegisterControlRect(
            new Avalonia.Rect(0, 0, 400, 80),
            new SkiaChatHit(0, null, ResetDetailMode: false));
        registry.RegisterControlRect(
            new Avalonia.Rect(20, 20, 80, 24),
            new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.OverviewToggle));

        Assert.Equal(1, registry.FindIndex(new Avalonia.Point(40, 30)));
    }

    [Fact]
    public void HitRegistry_contains_pointer_action_for_wheel_routing()
    {
        var registry = new SkiaChatHitRegistry();
        registry.RegisterControlRect(
            new Avalonia.Rect(0, 500, 800, 120),
            new SkiaChatHit(null, null, ResetDetailMode: false, PointerAction: SkiaChatPointerAction.ComposerFocus));

        Assert.True(registry.ContainsPointerAction(new Avalonia.Point(100, 550), SkiaChatPointerAction.ComposerFocus));
        Assert.False(registry.ContainsPointerAction(new Avalonia.Point(100, 200), SkiaChatPointerAction.ComposerFocus));
    }

    [Fact]
    public void Link_body_measure_uses_single_link_run_for_bracket_label()
    {
        var ctx = new SkiaChatMeasureContext(48, 360);
        var spec = new SkiaChatBubbleSpec(
            "",
            "[TryResolveFile]",
            null,
            SkiaChatBubbleKind.Feed,
            SkiaBubbleFillRole.MessageAssistant,
            SkiaChatBodyTone.Link,
            false,
            false,
            false,
            0);

        var metrics = SkiaChatBubbleRenderer.Measure(ctx, spec);
        Assert.Single(metrics.ContentLines);
        Assert.Single(metrics.ContentLines[0].Runs);
        Assert.Equal(SkiaMarkdownStyle.Link, metrics.ContentLines[0].Runs[0].Style);
    }
}

using CascadeIDE.Views.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaIntercomCommandDeckLayoutTests
{
    [Fact]
    public void Deck_popup_stays_inside_deck_not_above_composer_only_stack()
    {
        const float width = 480f;
        const float bottom = 600f;
        var deck = SkiaIntercomCommandDeckLayout.Compute(
            width,
            bottom,
            showComposer: true,
            showCommandLine: true,
            commandLinePreview: "preview",
            composerText: "hi",
            showSlashPopup: true,
            slashRowCount: 4);

        Assert.True(deck.HasDeck);
        Assert.True(deck.DeckBounds.Bottom <= bottom + 0.01f);
        Assert.True(deck.DeckBounds.Top >= deck.SlashPopupBounds.Top - 0.01f);
        Assert.True(deck.SlashPopupBounds.Bottom <= deck.CommandLineBounds.Top + 0.01f);
        Assert.True(deck.CommandLineBounds.Bottom <= deck.ComposerBounds.Top + 0.01f);
        Assert.InRange(deck.TotalHeight, deck.DeckBounds.Height - 0.01f, deck.DeckBounds.Height + 0.01f);
    }

    [Fact]
    public void MeasureTotalHeight_matches_deck_layout_on_surface()
    {
        const float width = 420f;
        const float bottom = 800f;
        var measured = SkiaIntercomCommandDeckLayout.MeasureTotalHeight(
            width,
            showComposer: true,
            showCommandLine: true,
            commandLinePreview: null,
            composerText: "/intercom anchor list",
            showSlashPopup: true,
            slashRowCount: 2);
        var deck = SkiaIntercomCommandDeckLayout.Compute(
            width,
            bottom,
            showComposer: true,
            showCommandLine: true,
            commandLinePreview: null,
            composerText: "/intercom anchor list",
            showSlashPopup: true,
            slashRowCount: 2);

        Assert.Equal(measured, deck.TotalHeight, precision: 2);
        Assert.Equal(bottom - measured, deck.DeckBounds.Top, precision: 2);
    }

    [Fact]
    public void Composer_layout_wrapper_includes_command_line_and_popup()
    {
        var width = 420f;
        var composerOnly = SkiaIntercomComposerLayout.MeasureBottomChromeHeight(
            showComposer: true,
            showSlashPopup: false,
            slashRowCount: 0,
            composerText: "hello",
            surfaceWidth: width);
        var fullDeck = SkiaIntercomComposerLayout.MeasureBottomChromeHeight(
            showComposer: true,
            showSlashPopup: true,
            slashRowCount: 3,
            composerText: "hello",
            surfaceWidth: width,
            showCommandLine: true,
            commandLinePreview: "x");

        Assert.True(fullDeck > composerOnly);
    }
}

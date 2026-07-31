#nullable enable
using Avalonia;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    private void DrawIntercomBottomChrome(
        SKCanvas canvas,
        float width,
        float height,
        SkiaChatTheme theme,
        float layoutScale)
    {
        if (!ShowIntercomComposer)
        {
            _deckBounds = default;
            _composerBounds = default;
            _commandLineBounds = default;
            _slashPopupBounds = default;
            _sendButtonBounds = default;
            return;
        }

        RebuildSlashRows();
        var fonts = IntercomFonts;
        var deck = SkiaIntercomCommandDeckLayout.Compute(
            width,
            height,
            showComposer: true,
            showCommandLine: ShowCockpitCommandLine,
            commandLinePreview: CommandLinePreview,
            composerText: ComposerText ?? "",
            showSlashPopup: IsSlashAutocompleteVisible && _slashRows.Count > 0,
            slashRowCount: _slashRows.Count,
            composerPreeditText: ComposerPreeditText,
            showSlashHierarchyHeader: ShowSlashHierarchyHeader,
            fonts.ResolveComposerPt(FeedUsesForwardMetrics),
            fonts.ResolveComposerLineHeight(FeedUsesForwardMetrics),
            composerSlashPreview: ComposerPreview,
            composerSlashPreviewFontSize: fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics),
            commandLineFontSize: fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
            commandLinePreviewFontSize: fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));

        _deckBounds = deck.DeckBounds;
        _composerBounds = deck.ComposerBounds;
        _commandLineBounds = deck.CommandLineBounds;
        _slashPopupBounds = deck.SlashPopupBounds;

        if (deck.HasDeck)
            SkiaIntercomCommandDeckLayout.DrawDeckChrome(canvas, _deckBounds, theme);

        if (_slashPopupBounds.Width > 0)
        {
            if (_slashRows.Count != _slashPopupLastRowCount)
            {
                _slashPopupLastRowCount = _slashRows.Count;
                _slashPopupScrollOffset = 0;
            }

            var selected = SelectedSlashSuggestionIndex;
            if (selected < 0 && _slashRows.Count > 0)
                selected = 0;
            _slashPopupScrollOffset = SkiaPopupList.EnsureSelectionVisible(
                selected,
                _slashPopupScrollOffset,
                _slashRows.Count);

            SkiaPopupList.Draw(
                canvas,
                _slashPopupBounds,
                theme,
                _slashRows,
                selected,
                _slashPopupScrollOffset,
                layoutScale,
                SlashAutocompletePathPrefix,
                SlashAutocompleteNextStep,
                SlashAutocompleteBreadcrumb);
        }
        else
        {
            _slashPopupScrollOffset = 0;
            _slashPopupLastRowCount = 0;
        }

        if (_commandLineBounds.Width > 0)
        {
            var cclCaret = _commandLineFocused && IsKeyboardFocusWithin;
            SkiaCommandLineStrip.Draw(
                canvas,
                _commandLineBounds,
                theme,
                CommandLineText ?? "/",
                CommandLinePreview,
                CommandLinePreviewKind,
                "/intercom … · /anchor peek …",
                IsComposerEnabled,
                CommandLineCaretIndex,
                _commandLineSelectionAnchor,
                cclCaret,
                cclCaret && _composerCaretBlinkVisible,
                _commandLineScrollOffsetX,
                fonts.ResolveCommandLinePt(FeedUsesForwardMetrics),
                fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics));
        }

        var showCaret = !_commandLineFocused && IsComposerEnabled && IsKeyboardFocusWithin;
        var composerPreviewPt = fonts.ResolveCommandLinePreviewPt(FeedUsesForwardMetrics);
        SkiaComposerStrip.Draw(
            canvas,
            _composerBounds,
            theme,
            ComposerText ?? "",
            ComposerPreeditText,
            ComposerPlaceholder,
            IsComposerEnabled,
            ComposerCaretIndex,
            showCaret,
            showCaret && _composerCaretBlinkVisible,
            fonts.ResolveComposerPt(FeedUsesForwardMetrics),
            fonts.ResolveComposerLineHeight(FeedUsesForwardMetrics),
            out _sendButtonBounds,
            out _,
            _composerSelectionAnchor,
            _composerScrollOffsetY,
            ComposerPreview,
            ComposerPreviewKind,
            composerPreviewPt);

        RegisterComposerPointerHits();
    }
}

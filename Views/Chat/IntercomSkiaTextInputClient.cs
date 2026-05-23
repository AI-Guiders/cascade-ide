#nullable enable
using Avalonia;
using Avalonia.Input.TextInput;
using Avalonia.Media;

namespace CascadeIDE.Views.Chat;

/// <summary>IME/ввод для Skia composer и поля поиска Topic Navigator (ADR 0123 фаза 2).</summary>
internal sealed class IntercomSkiaTextInputClient : TextInputMethodClient
{
    private readonly SkiaChatSurfaceControl _host;

    public IntercomSkiaTextInputClient(SkiaChatSurfaceControl host) => _host = host;

    public override Visual TextViewVisual => _host;

    public override bool SupportsPreedit => !_host.IsNavigatorSearchInputActive;

    public override bool SupportsSurroundingText => true;

    public override string SurroundingText =>
        _host.IsNavigatorSearchInputActive
            ? _host.TopicNavigatorSearchQuery ?? ""
            : _host.IsCommandLineInputActive
                ? _host.CommandLineText ?? "/"
                : _host.ComposerText ?? "";

    public override TextSelection Selection
    {
        get
        {
            var text = SurroundingText;
            if (_host.IsNavigatorSearchInputActive)
            {
                var navCaret = Math.Clamp(_host.NavigatorSearchCaretIndex, 0, text.Length);
                return new TextSelection(navCaret, navCaret);
            }

            var caret = _host.IsCommandLineInputActive
                ? Math.Clamp(_host.CommandLineCaretIndex, 0, text.Length)
                : Math.Clamp(_host.ComposerCaretIndex, 0, text.Length);
            var anchor = _host.IsCommandLineInputActive
                ? Math.Clamp(_host.CommandLineSelectionAnchor, 0, text.Length)
                : Math.Clamp(_host.ComposerSelectionAnchor, 0, text.Length);
            if (anchor == caret)
                return new TextSelection(caret, caret);

            var start = Math.Min(anchor, caret);
            var end = Math.Max(anchor, caret);
            return new TextSelection(start, end);
        }
        set
        {
            var len = SurroundingText.Length;
            var start = Math.Clamp(value.Start, 0, len);
            var end = Math.Clamp(value.End, 0, len);
            if (_host.IsNavigatorSearchInputActive)
            {
                _host.NavigatorSearchCaretIndex = start;
                return;
            }

            if (_host.IsCommandLineInputActive)
            {
                _host.CommandLineCaretIndex = end;
                if (start != end)
                    _host.SetCommandLineSelectionAnchor(start);
                else
                    _host.CollapseCommandLineSelection();

                return;
            }

            _host.ComposerCaretIndex = end;
            if (start != end)
                _host.SetComposerSelectionAnchor(start);
            else
                _host.CollapseComposerSelection();
        }
    }

    public override Rect CursorRectangle =>
        _host.IsNavigatorSearchInputActive
            ? _host.GetNavigatorSearchCaretScreenRect()
            : _host.IsCommandLineInputActive
                ? _host.GetCommandLineCaretScreenRect()
                : _host.GetComposerCaretScreenRect();

    public void NotifyTextChanged() => RaiseSurroundingTextChanged();

    public void NotifyCursorMoved() => RaiseCursorRectangleChanged();

    public override void SetPreeditText(string? preedit)
    {
        if (_host.IsNavigatorSearchInputActive || _host.IsCommandLineInputActive)
            return;

        _host.ComposerPreeditText = string.IsNullOrEmpty(preedit) ? null : preedit;
        NotifyCursorMoved();
        _host.InvalidateVisual();
    }
}

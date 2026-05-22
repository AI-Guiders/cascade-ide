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
            : _host.ComposerText ?? "";

    public override TextSelection Selection
    {
        get
        {
            var text = SurroundingText;
            var caret = _host.IsNavigatorSearchInputActive
                ? Math.Clamp(_host.NavigatorSearchCaretIndex, 0, text.Length)
                : Math.Clamp(_host.ComposerCaretIndex, 0, text.Length);
            if (_host.IsNavigatorSearchInputActive)
                return new TextSelection(caret, caret);

            var anchor = Math.Clamp(_host.ComposerSelectionAnchor, 0, text.Length);
            if (anchor == caret)
                return new TextSelection(caret, caret);

            var start = Math.Min(anchor, caret);
            var end = Math.Max(anchor, caret);
            return new TextSelection(start, end);
        }
        set
        {
            var caret = Math.Clamp(value.Start, 0, SurroundingText.Length);
            if (_host.IsNavigatorSearchInputActive)
                _host.NavigatorSearchCaretIndex = caret;
            else
                _host.ComposerCaretIndex = caret;
        }
    }

    public override Rect CursorRectangle =>
        _host.IsNavigatorSearchInputActive
            ? _host.GetNavigatorSearchCaretScreenRect()
            : _host.GetComposerCaretScreenRect();

    public void NotifyTextChanged() => RaiseSurroundingTextChanged();

    public void NotifyCursorMoved() => RaiseCursorRectangleChanged();

    public override void SetPreeditText(string? preedit)
    {
        if (_host.IsNavigatorSearchInputActive)
            return;

        _host.ComposerPreeditText = string.IsNullOrEmpty(preedit) ? null : preedit;
        NotifyCursorMoved();
        _host.InvalidateVisual();
    }
}

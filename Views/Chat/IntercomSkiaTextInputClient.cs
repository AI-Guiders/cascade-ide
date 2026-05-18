#nullable enable
using Avalonia;
using Avalonia.Input.TextInput;
using Avalonia.Media;

namespace CascadeIDE.Views.Chat;

/// <summary>IME/ввод для Skia composer (ADR 0123 фаза 2).</summary>
internal sealed class IntercomSkiaTextInputClient : TextInputMethodClient
{
    private readonly SkiaChatSurfaceControl _host;

    public IntercomSkiaTextInputClient(SkiaChatSurfaceControl host) => _host = host;

    public override Visual TextViewVisual => _host;

    public override bool SupportsPreedit => true;

    public override bool SupportsSurroundingText => true;

    public override string SurroundingText => _host.ComposerText ?? "";

    public override TextSelection Selection
    {
        get
        {
            var caret = Math.Clamp(_host.ComposerCaretIndex, 0, SurroundingText.Length);
            return new TextSelection(caret, caret);
        }
        set => _host.ComposerCaretIndex = Math.Clamp(value.Start, 0, SurroundingText.Length);
    }

    public override Rect CursorRectangle => _host.GetComposerCaretScreenRect();

    public void NotifyTextChanged() => RaiseSurroundingTextChanged();

    public void NotifyCursorMoved() => RaiseCursorRectangleChanged();

    public override void SetPreeditText(string? preedit)
    {
        _host.ComposerPreeditText = string.IsNullOrEmpty(preedit) ? null : preedit;
        NotifyCursorMoved();
        _host.InvalidateVisual();
    }
}

#nullable enable
using System.IO;
using System.Windows.Media;
using CDP.GlassCockpit.Windows.TextMate;
using ICSharpCode.AvalonEdit;
using TextMateSharp.Grammars;

namespace CDP.GlassCockpit.Windows;

/// <summary>TextMateSharp Dark+ on WPF AvalonEdit — port of AvaloniaEdit.TextMate glue.</summary>
internal sealed class GlassAvalonEditTextMate : IDisposable
{
    readonly TextEditor _editor;
    readonly RegistryOptions _registry = new(ThemeName.DarkPlus);
    AvalonEditTextMate.Installation? _installation;

    public GlassAvalonEditTextMate(TextEditor editor) => _editor = editor;

    public bool ApplyForPath(string path)
    {
        _installation ??= _editor.InstallTextMate(_registry, initCurrentDocument: true, OnException);

        var ext = Path.GetExtension(path);
        var language = _registry.GetLanguageByExtension(ext);
        if (language is null)
            return false;

        var scope = _registry.GetScopeByLanguageId(language.Id);
        if (string.IsNullOrEmpty(scope))
            return false;

        // TextMate owns coloring — stock XSHD would fight scopes.
        _editor.SyntaxHighlighting = null;
        _installation.SetGrammar(scope);
        ApplyThemeChrome();
        return true;
    }

    void ApplyThemeChrome()
    {
        if (_installation is null)
            return;

        if (_installation.TryGetThemeColor("editor.background", out var bg) &&
            TryParseBrush(bg, out var bgBrush))
            _editor.Background = bgBrush;

        if (_installation.TryGetThemeColor("editor.foreground", out var fg) &&
            TryParseBrush(fg, out var fgBrush))
            _editor.Foreground = fgBrush;

        if (_installation.TryGetThemeColor("editorLineNumber.foreground", out var ln) &&
            TryParseBrush(ln, out var lnBrush))
            _editor.LineNumbersForeground = lnBrush;

        if (_installation.TryGetThemeColor("editor.selectionBackground", out var sel) &&
            TryParseBrush(sel, out var selBrush))
            _editor.TextArea.SelectionBrush = selBrush;
    }

    static bool TryParseBrush(string? color, out SolidColorBrush brush)
    {
        brush = null!;
        if (string.IsNullOrWhiteSpace(color))
            return false;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(Normalize(color))!;
            brush = new SolidColorBrush(c);
            brush.Freeze();
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string Normalize(string color)
    {
        if (color.Length != 9 || color[0] != '#')
            return color;
        // #RRGGBBAA → #AARRGGBB for WPF
        return $"#{color[7]}{color[8]}{color[1]}{color[2]}{color[3]}{color[4]}{color[5]}{color[6]}";
    }

    static void OnException(Exception ex) =>
        System.Diagnostics.Debug.WriteLine($"[Glass TextMate] {ex}");

    public void Dispose()
    {
        _installation?.Dispose();
        _installation = null;
    }
}

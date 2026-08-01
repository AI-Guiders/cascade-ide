#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using System.Buffers;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Themes;
using TmFontStyle = TextMateSharp.Themes.FontStyle;

namespace CDP.GlassCockpit.Windows.TextMate;

sealed class TextMateColoringTransformer :
    GenericLineTransformer,
    IModelTokensChangedListener,
    IDisposable
{
    readonly object _lock = new();
    readonly TextView _textView;
    readonly Action<Exception>? _exceptionHandler;
    bool _isDisposed;
    Theme? _theme;
    IGrammar? _grammar;
    TMModel? _model;
    TextDocument? _document;
    bool _areVisualLinesValid;
    int _firstVisibleLineIndex = -1;
    int _lastVisibleLineIndex = -1;
    Dictionary<int, Brush> _brushes = new();

    public TextMateColoringTransformer(TextView textView, Action<Exception>? exceptionHandler)
        : base(exceptionHandler)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        _exceptionHandler = exceptionHandler;
        _textView.VisualLinesChanged += TextView_VisualLinesChanged;
    }

    public void SetModel(TextDocument? document, TMModel? model)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            ThrowIfDisposed();
            _areVisualLinesValid = false;
            _document = document;
            _model = model;
            if (_grammar is not null && _model is not null)
                _model.SetGrammar(_grammar);
        }
    }

    void TextView_VisualLinesChanged(object? sender, EventArgs e)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        try
        {
            if (!_textView.VisualLinesValid || _textView.VisualLines.Count == 0)
                return;
            lock (_lock)
            {
                if (Volatile.Read(ref _isDisposed))
                    return;
                _areVisualLinesValid = true;
                _firstVisibleLineIndex = _textView.VisualLines[0].FirstDocumentLine.LineNumber - 1;
                _lastVisibleLineIndex = _textView.VisualLines[^1].LastDocumentLine.LineNumber - 1;
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    public void Dispose()
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        lock (_lock)
        {
            if (Volatile.Read(ref _isDisposed))
                return;
            Volatile.Write(ref _isDisposed, true);
            _theme = null;
            _grammar = null;
            _model = null;
            _document = null;
            _brushes = null!;
        }

        _textView.VisualLinesChanged -= TextView_VisualLinesChanged;
    }

    public void SetTheme(Theme theme)
    {
        ThrowIfDisposed();
        var map = theme.GetColorMap();
        var newBrushes = new Dictionary<int, Brush>();
        foreach (var color in map)
        {
            var id = theme.GetColorId(color);
            var brush = new SolidColorBrush(ParseColor(NormalizeColor(color)));
            brush.Freeze();
            newBrushes[id] = brush;
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            _theme = theme;
            _brushes = newBrushes;
        }
    }

    public void SetGrammar(IGrammar? grammar)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            ThrowIfDisposed();
            _grammar = grammar;
            _model?.SetGrammar(grammar);
        }
    }

    protected override void TransformLine(DocumentLine line, ITextRunConstructionContext context)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        try
        {
            TMModel? model;
            TextDocument? document;
            Theme? theme;
            Dictionary<int, Brush>? brushes;
            lock (_lock)
            {
                if (Volatile.Read(ref _isDisposed))
                    return;
                model = _model;
                document = _document;
                theme = _theme;
                brushes = _brushes;
            }

            if (model is null || document is null || theme is null || brushes is null)
                return;

            var lineNumber = line.LineNumber;
            var tokens = model.GetLineTokens(lineNumber - 1);
            if (tokens is null || tokens.Count == 0)
                return;

            var transformsInLine = ArrayPool<ForegroundTextTransformation>.Shared.Rent(tokens.Count);
            try
            {
                GetLineTransformations(lineNumber, tokens, transformsInLine, model, document, theme, brushes);
                for (var i = 0; i < tokens.Count; i++)
                    transformsInLine[i]?.Transform(this, line);
            }
            finally
            {
                ArrayPool<ForegroundTextTransformation>.Shared.Return(transformsInLine, clearArray: true);
            }
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    void GetLineTransformations(
        int lineNumber,
        List<TMToken> tokens,
        ForegroundTextTransformation[] transformations,
        TMModel model,
        TextDocument document,
        Theme theme,
        Dictionary<int, Brush> brushes)
    {
        var lineOffset = document.GetLineByNumber(lineNumber).Offset;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var nextToken = i + 1 < tokens.Count ? tokens[i + 1] : null;
            var startIndex = token.StartIndex;
            var endIndex = nextToken?.StartIndex ?? model.GetLines().GetLineLength(lineNumber - 1);
            if (startIndex >= endIndex || token.Scopes is null || token.Scopes.Count == 0)
            {
                transformations[i] = null!;
                continue;
            }

            var foreground = 0;
            var background = 0;
            TmFontStyle fontStyle = 0;
            foreach (var themeRule in theme.Match(token.Scopes))
            {
                if (foreground == 0 && themeRule.foreground > 0)
                    foreground = themeRule.foreground;
                if (background == 0 && themeRule.background > 0)
                    background = themeRule.background;
                if (fontStyle == 0 && themeRule.fontStyle > 0)
                    fontStyle = themeRule.fontStyle;
            }

            transformations[i] ??= new ForegroundTextTransformation();
            transformations[i].ColorMap = brushes;
            transformations[i].ExceptionHandler = _exceptionHandler;
            transformations[i].StartOffset = lineOffset + startIndex;
            transformations[i].EndOffset = lineOffset + endIndex;
            transformations[i].ForegroundColor = foreground;
            transformations[i].BackgroundColor = background;
            transformations[i].FontStyle = fontStyle;
        }
    }

    public void ModelTokensChanged(ModelTokensChangedEvent e)
    {
        if (e.Ranges is null || Volatile.Read(ref _isDisposed))
            return;

        TMModel? model;
        TextDocument? document;
        bool areVisualLinesValid;
        int firstVisibleLineIndex;
        int lastVisibleLineIndex;
        lock (_lock)
        {
            if (Volatile.Read(ref _isDisposed))
                return;
            model = _model;
            document = _document;
            areVisualLinesValid = _areVisualLinesValid;
            firstVisibleLineIndex = _firstVisibleLineIndex;
            lastVisibleLineIndex = _lastVisibleLineIndex;
        }

        if (model is null || model.IsStopped || document is null)
            return;

        var firstChangedLineIndex = int.MaxValue;
        var lastChangedLineIndex = -1;
        foreach (var range in e.Ranges)
        {
            firstChangedLineIndex = Math.Min(range.FromLineNumber - 1, firstChangedLineIndex);
            lastChangedLineIndex = Math.Max(range.ToLineNumber - 1, lastChangedLineIndex);
        }

        if (areVisualLinesValid)
        {
            var changedLinesAreNotVisible =
                (firstChangedLineIndex < firstVisibleLineIndex && lastChangedLineIndex < firstVisibleLineIndex) ||
                (firstChangedLineIndex > lastVisibleLineIndex && lastChangedLineIndex > lastVisibleLineIndex);
            if (changedLinesAreNotVisible)
                return;
        }

        _textView.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            if (Volatile.Read(ref _isDisposed))
                return;

            var firstLineIndexToRedraw = Math.Max(firstChangedLineIndex, firstVisibleLineIndex);
            var lastLineIndexToRedrawLine = Math.Min(lastChangedLineIndex, lastVisibleLineIndex);
            var totalLines = document.Lines.Count - 1;
            firstLineIndexToRedraw = Math.Clamp(firstLineIndexToRedraw, 0, totalLines);
            lastLineIndexToRedrawLine = Math.Clamp(lastLineIndexToRedrawLine, 0, totalLines);

            if (!areVisualLinesValid || lastLineIndexToRedrawLine < firstLineIndexToRedraw)
            {
                _textView.Redraw();
                return;
            }

            var firstLineToRedraw = document.Lines[firstLineIndexToRedraw];
            var lastLineToRedraw = document.Lines[lastLineIndexToRedrawLine];
            _textView.Redraw(
                firstLineToRedraw.Offset,
                lastLineToRedraw.Offset + lastLineToRedraw.TotalLength - firstLineToRedraw.Offset,
                DispatcherPriority.Background);
        });
    }

    void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed))
            throw new ObjectDisposedException(nameof(TextMateColoringTransformer));
    }

    static Color ParseColor(string color) =>
        (Color)ColorConverter.ConvertFromString(color)!;

    static string NormalizeColor(string color)
    {
        if (color.Length != 9)
            return color;
        Span<char> normalized = stackalloc char[]
        {
            '#', color[7], color[8], color[1], color[2], color[3], color[4], color[5], color[6]
        };
        return normalized.ToString();
    }
}

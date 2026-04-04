using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>Подключает к редактору автодополнение, подсказки по параметрам и подсветку вхождений. Всё в фоне с debounce.</summary>
public sealed class EditorIntelligence
{
    private readonly TextEditor _editor;
    private readonly CSharpLanguageService _languageService;
    private readonly Func<(string? filePath, string sourceText)> _getContext;
    private CancellationTokenSource? _completionCts;
    private DispatcherTimer? _completionDebounce;
    private DispatcherTimer? _highlightDebounce;
    private DispatcherTimer? _signatureDebounce;
    private Popup? _completionPopup;
    private ListBox? _completionList;
    private IReadOnlyList<CSharpLanguageService.CompletionItem> _lastCompletionItems = [];
    private int _completionStartOffset;
    private Popup? _signaturePopup;
    private TextBlock? _signatureText;
    private ReferencesHighlightRenderer? _highlightRenderer;
    private const int CompletionDebounceMs = 120;
    private const int HighlightDebounceMs = 280;
    private const int SignatureDebounceMs = 180;

    public EditorIntelligence(
        TextEditor editor,
        CSharpLanguageService languageService,
        Func<(string? filePath, string sourceText)> getContext)
    {
        _editor = editor;
        _languageService = languageService;
        _getContext = getContext;
    }

    public void Attach()
    {
        var textArea = _editor.TextArea;
        if (textArea is null) return;

        _highlightRenderer = new ReferencesHighlightRenderer();
        textArea.TextView.BackgroundRenderers?.Add(_highlightRenderer);

        textArea.KeyDown += OnKeyDown;
        textArea.KeyUp += OnKeyUp;
        textArea.Caret.PositionChanged += OnCaretPositionChanged;

        _completionDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CompletionDebounceMs) };
        _completionDebounce.Tick += (_, _) => { _completionDebounce.Stop(); TriggerCompletion(); };
        _highlightDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HighlightDebounceMs) };
        _highlightDebounce.Tick += (_, _) => { _highlightDebounce.Stop(); UpdateHighlight(); };
        _signatureDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SignatureDebounceMs) };
        _signatureDebounce.Tick += (_, _) => { _signatureDebounce.Stop(); UpdateSignature(); };
    }

    /// <summary>Отвязка перед переключением на другой <see cref="TextEditor"/> (другая вкладка).</summary>
    public void Detach()
    {
        _completionCts?.Cancel();
        CloseCompletion();
        _signaturePopup?.SetCurrentValue(Popup.IsOpenProperty, false);

        _completionDebounce?.Stop();
        _highlightDebounce?.Stop();
        _signatureDebounce?.Stop();

        var textArea = _editor.TextArea;
        if (textArea is not null)
        {
            textArea.KeyDown -= OnKeyDown;
            textArea.KeyUp -= OnKeyUp;
            textArea.Caret.PositionChanged -= OnCaretPositionChanged;
            if (_highlightRenderer is not null && textArea.TextView.BackgroundRenderers is { } renderers)
                renderers.Remove(_highlightRenderer);
        }

        _highlightRenderer = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            StartCompletionDebounce();
            return;
        }
        if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
        {
            StartCompletionDebounce();
            return;
        }
        if (_completionPopup?.IsOpen == true)
        {
            if (e.Key == Key.Escape)
            {
                CloseCompletion();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                CommitCompletion();
                e.Handled = true;
            }
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_completionPopup?.IsOpen == true && e.Key != Key.Escape && e.Key != Key.Enter && e.Key != Key.Tab)
            StartCompletionDebounce();
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        _highlightDebounce?.Stop();
        _highlightDebounce?.Start();
        _signatureDebounce?.Stop();
        _signatureDebounce?.Start();
    }

    private void StartCompletionDebounce()
    {
        _completionCts?.Cancel();
        _completionDebounce?.Stop();
        _completionDebounce?.Start();
    }

    private static bool IsCompletionWordChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '@';

    private (int line, int column) GetCaretLineColumn()
    {
        var doc = _editor.Document;
        var offset = _editor.TextArea.Caret.Offset;
        if (offset < 0 || offset > doc.TextLength) return (1, 1);
        var line = doc.GetLineByOffset(offset);
        return (line.LineNumber, offset - line.Offset + 1);
    }

    private void TriggerCompletion()
    {
        var (filePath, sourceText) = _getContext();
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        var (line, column) = GetCaretLineColumn();
        var doc = _editor.Document;
        var caretOffset = _editor.TextArea.Caret.Offset;
        var start = caretOffset;
        while (start > 0 && IsCompletionWordChar(doc.GetCharAt(start - 1)))
            start--;
        _completionStartOffset = start;
        _completionCts = new CancellationTokenSource();
        var ct = _completionCts.Token;

        Task.Run(() =>
        {
            var items = _languageService.GetCompletionItems(filePath, sourceText, line, column, ct);
            if (ct.IsCancellationRequested || items.Count == 0) return;
            UiScheduler.Default.Post(() =>
            {
                if (_completionCts?.IsCancellationRequested == true) return;
                _lastCompletionItems = items;
                ShowCompletionPopup(items);
            });
        }, ct);
    }

    private void ShowCompletionPopup(IReadOnlyList<CSharpLanguageService.CompletionItem> items)
    {
        if (_completionPopup is null)
        {
            _completionList = new ListBox
            {
                MinWidth = 280,
                MaxHeight = 220,
                Background = Avalonia.Media.Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 180, 180, 180))
            };
            _completionList.DoubleTapped += (_, _) => CommitCompletion();
            _completionPopup = new Popup
            {
                Child = _completionList,
                Placement = PlacementMode.AnchorAndGravity,
                PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft,
                PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomLeft
            };
            ((ISetLogicalParent)_completionPopup).SetParent(_editor);
        }

        _completionList!.ItemsSource = items.Select(i => i.DisplayText).ToList();
        _completionList.SelectedIndex = 0;

        var caret = _editor.TextArea.Caret;
        var rect = _editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(caret.Line, caret.Column), AvaloniaEdit.Rendering.VisualYPosition.LineTop) - _editor.TextArea.TextView.ScrollOffset;
        _completionPopup.PlacementRect = new Rect(rect.X + 8, rect.Y + 20, 0, 0);
        _completionPopup.IsOpen = true;
    }

    private void CloseCompletion()
    {
        _completionPopup?.SetCurrentValue(Popup.IsOpenProperty, false);
    }

    private void CommitCompletion()
    {
        var idx = _completionList?.SelectedIndex ?? -1;
        if (idx < 0 || idx >= _lastCompletionItems.Count)
        {
            CloseCompletion();
            return;
        }
        var item = _lastCompletionItems[idx];
        var doc = _editor.Document;
        var start = _completionStartOffset;
        var end = _editor.TextArea.Caret.Offset;
        if (end < start) (start, end) = (end, start);
        doc.Replace(start, end - start, item.InsertText);
        CloseCompletion();
    }

    private void UpdateHighlight()
    {
        var (filePath, sourceText) = _getContext();
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            _highlightRenderer?.SetSpans([]);
            return;
        }
        var (line, column) = GetCaretLineColumn();
        _completionCts?.Cancel();
        var cts = new CancellationTokenSource();
        Task.Run(() =>
        {
            var spans = _languageService.GetHighlightSpans(filePath, sourceText, line, column, cts.Token);
            UiScheduler.Default.Post(() =>
            {
                _highlightRenderer?.SetSpans(spans);
                _editor.TextArea.TextView.Redraw();
            });
        }, cts.Token);
    }

    private void UpdateSignature()
    {
        var (filePath, sourceText) = _getContext();
        if (string.IsNullOrEmpty(filePath) || !sourceText.Contains('(')) return;
        var (line, column) = GetCaretLineColumn();
        Task.Run(() =>
        {
            var sig = _languageService.GetSignatureHelp(filePath, sourceText, line, column);
            UiScheduler.Default.Post(() =>
            {
                if (string.IsNullOrEmpty(sig))
                {
                    _signaturePopup?.SetCurrentValue(Popup.IsOpenProperty, false);
                    return;
                }
                if (_signaturePopup is null)
                {
                    _signatureText = new TextBlock
                    {
                        FontFamily = "Consolas,monospace",
                        FontSize = 12,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 60, 60, 60)),
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 255, 252, 220)),
                        Padding = new Thickness(6, 4),
                        MaxWidth = 420
                    };
                    _signaturePopup = new Popup
                    {
                        Child = _signatureText,
                        Placement = PlacementMode.AnchorAndGravity,
                        PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft,
                        PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomLeft
                    };
                    ((ISetLogicalParent)_signaturePopup).SetParent(_editor);
                }
                _signatureText!.Text = sig;
                var caret = _editor.TextArea.Caret;
                var rect = _editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(caret.Line, caret.Column), AvaloniaEdit.Rendering.VisualYPosition.LineBottom) - _editor.TextArea.TextView.ScrollOffset;
                _signaturePopup!.PlacementRect = new Rect(rect.X + 8, rect.Y + 2, 0, 0);
                _signaturePopup.IsOpen = true;
            });
        });
    }

    private sealed class ReferencesHighlightRenderer : IBackgroundRenderer
    {
        private readonly List<TextViewSegment> _segments = [];
        private readonly object _lock = new();

        public KnownLayer Layer => KnownLayer.Background;

        public void SetSpans(IReadOnlyList<Microsoft.CodeAnalysis.Text.TextSpan> spans)
        {
            lock (_lock)
            {
                _segments.Clear();
                foreach (var s in spans)
                    _segments.Add(new TextViewSegment(s.Start, s.Length));
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView.Document is null) return;
            var brush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(80, 180, 220, 255));
            lock (_lock)
            {
                foreach (var seg in _segments)
                {
                    foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
                        drawingContext.DrawRectangle(brush, null, rect);
                }
            }
        }
    }

    private sealed class TextViewSegment(int offset, int length) : AvaloniaEdit.Document.ISegment
    {
        public int Offset { get; } = offset;
        public int Length { get; } = length;
        int AvaloniaEdit.Document.ISegment.EndOffset => Offset + Length;
    }
}

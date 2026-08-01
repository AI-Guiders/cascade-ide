#nullable enable

using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;

namespace CDP.GlassCockpit.Windows;

/// <summary>AE chrome CIDE never shipped: folding + search + indent + current line.</summary>
internal sealed class GlassAvalonEditChrome : IDisposable
{
    readonly TextEditor _editor;
    readonly FoldingManager _folding;
    readonly DispatcherTimer _foldTimer;
    readonly BraceFoldingStrategy _brace = new();
    readonly XmlFoldingStrategy _xml = new();
    bool _xmlMode;

    public GlassAvalonEditChrome(TextEditor editor)
    {
        _editor = editor;
        SearchPanel.Install(editor.TextArea);
        _folding = FoldingManager.Install(editor.TextArea);
        editor.Options.HighlightCurrentLine = true;
        editor.Options.ConvertTabsToSpaces = true;
        editor.Options.IndentationSize = 4;
        editor.TextArea.IndentationStrategy =
            new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(editor.Options);

        _foldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _foldTimer.Tick += (_, _) =>
        {
            _foldTimer.Stop();
            UpdateFoldings();
        };
        editor.TextChanged += (_, _) =>
        {
            _foldTimer.Stop();
            _foldTimer.Start();
        };
    }

    public void SetModeForPath(string? path)
    {
        var ext = string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetExtension(path);
        _xmlMode = ext is ".xml" or ".xaml" or ".axaml" or ".csproj" or ".props" or ".targets" or ".config";
        UpdateFoldings();
    }

    void UpdateFoldings()
    {
        try
        {
            if (_xmlMode)
                _xml.UpdateFoldings(_folding, _editor.Document);
            else
                _brace.UpdateFoldings(_folding, _editor.Document);
        }
        catch
        {
            // folding is best-effort
        }
    }

    public void Dispose()
    {
        _foldTimer.Stop();
        FoldingManager.Uninstall(_folding);
    }
}

/// <summary>Minimal brace folding (AvalonEdit sample adapt).</summary>
internal sealed class BraceFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, ICSharpCode.AvalonEdit.Document.TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var stack = new Stack<int>();
        var text = document.Text;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '{')
                stack.Push(i);
            else if (c == '}' && stack.Count > 0)
            {
                var start = stack.Pop();
                if (i - start > 2)
                    foldings.Add(new NewFolding(start, i + 1));
            }
        }
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        manager.UpdateFoldings(foldings, -1);
    }
}

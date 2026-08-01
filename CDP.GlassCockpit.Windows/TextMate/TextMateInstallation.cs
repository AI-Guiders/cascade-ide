#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace CDP.GlassCockpit.Windows.TextMate;

static class AvalonEditTextMate
{
    public static Installation InstallTextMate(
        this TextEditor editor,
        IRegistryOptions registryOptions,
        bool initCurrentDocument = true,
        Action<Exception>? exceptionHandler = null) =>
        new(editor, registryOptions, initCurrentDocument, exceptionHandler);

    public sealed class Installation : IDisposable
    {
        readonly object _lock = new();
        readonly Registry _textMateRegistry;
        readonly TextEditor _editor;
        readonly bool _ownsTransformer;
        Action<Exception>? _exceptionHandler;
        TextEditorModel? _editorModel;
        IGrammar? _grammar;
        TMModel? _tmModel;
        TextMateColoringTransformer? _transformer;
        ReadOnlyDictionary<string, string>? _themeColorsDictionary;
        bool _isDisposed;

        public IRegistryOptions RegistryOptions { get; }
        public event EventHandler<Installation>? AppliedTheme;

        public Installation(
            TextEditor editor,
            IRegistryOptions registryOptions,
            bool initCurrentDocument = true,
            Action<Exception>? exceptionHandler = null)
        {
            RegistryOptions = registryOptions ?? throw new ArgumentNullException(nameof(registryOptions));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _exceptionHandler = exceptionHandler;
            _textMateRegistry = new Registry(registryOptions);

            _transformer = _editor.TextArea.TextView.LineTransformers
                .OfType<TextMateColoringTransformer>()
                .FirstOrDefault();
            if (_transformer is null)
            {
                _transformer = new TextMateColoringTransformer(_editor.TextArea.TextView, _exceptionHandler);
                _editor.TextArea.TextView.LineTransformers.Add(_transformer);
                _ownsTransformer = true;
            }

            SetTheme(registryOptions.GetDefaultTheme());
            editor.DocumentChanged += OnEditorOnDocumentChanged;
            if (initCurrentDocument)
                OnEditorOnDocumentChanged(editor, EventArgs.Empty);
        }

        public void SetGrammar(string scopeName)
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                ThrowIfDisposed();
                SetGrammarInternal(_textMateRegistry.LoadGrammar(scopeName));
            }

            _editor.TextArea.TextView.Redraw();
        }

        void SetGrammarInternal(IGrammar? grammar)
        {
            _grammar = grammar;
            _transformer!.SetGrammar(_grammar);
        }

        public bool TryGetThemeColor(string colorKey, out string? colorString)
        {
            ThrowIfDisposed();
            var dict = Volatile.Read(ref _themeColorsDictionary);
            if (dict is null)
                throw new ObjectDisposedException(nameof(Installation));
            return dict.TryGetValue(colorKey, out colorString);
        }

        public void SetTheme(IRawTheme theme)
        {
            ThrowIfDisposed();
            EventHandler<Installation>? appliedTheme;
            lock (_lock)
            {
                ThrowIfDisposed();
                _textMateRegistry.SetTheme(theme);
                var registryTheme = _textMateRegistry.GetTheme();
                _transformer!.SetTheme(registryTheme);
                _tmModel?.InvalidateLine(0);
                _editorModel?.InvalidateViewPortLines();
                _themeColorsDictionary = registryTheme.GetGuiColorDictionary();
                appliedTheme = AppliedTheme;
            }

            appliedTheme?.Invoke(this, this);
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _isDisposed))
                return;

            TextEditorModel? editorModel;
            TMModel? tmModel;
            TextMateColoringTransformer? transformer;
            lock (_lock)
            {
                if (Volatile.Read(ref _isDisposed))
                    return;
                Volatile.Write(ref _isDisposed, true);
                editorModel = _editorModel;
                _editorModel = null;
                tmModel = _tmModel;
                _tmModel = null;
                transformer = _transformer;
                _transformer = null;
                _grammar = null;
                _themeColorsDictionary = null;
                _exceptionHandler = null;
                AppliedTheme = null;
            }

            _editor.DocumentChanged -= OnEditorOnDocumentChanged;
            editorModel?.Dispose();
            if (tmModel is not null)
            {
                if (transformer is not null)
                    tmModel.RemoveModelTokensChangedListener(transformer);
                tmModel.Dispose();
            }

            if (_ownsTransformer && transformer is not null)
            {
                _editor.TextArea.TextView.LineTransformers.Remove(transformer);
                transformer.Dispose();
            }
            else
            {
                transformer?.SetModel(null, null);
            }
        }

        void OnEditorOnDocumentChanged(object? sender, EventArgs args)
        {
            if (Volatile.Read(ref _isDisposed))
                return;
            lock (_lock)
            {
                if (Volatile.Read(ref _isDisposed))
                    return;
                try
                {
                    _editorModel?.Dispose();
                    if (_tmModel is not null)
                    {
                        if (_transformer is not null)
                            _tmModel.RemoveModelTokensChangedListener(_transformer);
                        _tmModel.Dispose();
                    }

                    _editorModel = new TextEditorModel(_editor.TextArea.TextView, _editor.Document, _exceptionHandler);
                    _tmModel = new TMModel(_editorModel);
                    _tmModel.SetGrammar(_grammar);
                    _transformer!.SetModel(_editor.Document, _tmModel);
                    _tmModel.AddModelTokensChangedListener(_transformer);
                }
                catch (Exception ex)
                {
                    _exceptionHandler?.Invoke(ex);
                }
            }
        }

        void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed))
                throw new ObjectDisposedException(nameof(Installation));
        }
    }
}

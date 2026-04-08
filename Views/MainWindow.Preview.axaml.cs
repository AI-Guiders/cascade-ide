using System.IO;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using CascadeIDE.Features.UiChrome;
using TextMateSharp.Grammars;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private void EnsurePreviewWindow()
    {
        if (_previewWindow is not null)
            return;
        _previewVm = new ViewModels.MarkdownPreviewWindowViewModel();
        _previewWindow = new MarkdownPreviewWindow { DataContext = _previewVm };
        _previewWindow.Closed += (_, _) =>
        {
            _previewWindow = null;
            _previewVm?.DetachFromEditor();
        };
    }

    private void ShowMarkdownPreviewWindow(string title, string content)
    {
        EnsurePreviewWindow();
        _previewVm!.SetContent(title, content);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }

    private void ShowMarkdownPreviewForEditor()
    {
        if (DataContext is not ViewModels.MainWindowViewModel mainVm)
            return;

        if (MarkdownPreviewPlacementRuntime.Current == MarkdownPreviewPlacement.ForwardSplit
            && TryGetActiveDockDocumentView()?.FindControl<Grid>("EditorContentGrid") is not null
            && mainVm.IsMarkdownFile)
        {
            UpdateMarkdownPreviewColumn(true);
            UpdateInlineMarkdownPreview();
            return;
        }

        // separate_window, mfd (пока без вкладки в MFD), forward_split без сетки — отдельное окно.
        EnsurePreviewWindow();
        _previewVm!.AttachToEditor(mainVm);
        _previewWindow!.Show(this);
        _previewWindow.Activate();
    }

    private void SyncFromViewModel()
    {
        var editor = TryGetActiveDockEditor();
        if (editor is null || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        _suppressEditorSync = true;
        try
        {
            var desired = vm.EditorText ?? "";
            if (!string.Equals(editor.Document.Text, desired, StringComparison.Ordinal))
                editor.Document.Text = desired;
            ApplyGrammarByFilePath(editor, vm.CurrentFilePath);
        }
        finally
        {
            _suppressEditorSync = false;
        }
    }

    private void ApplyGrammarByFilePath(TextEditor editor, string? filePath)
    {
        if (_registryOptions is null)
        {
            LogHighlight("ApplyGrammarByFilePath: skipped (_registryOptions is null).");
            return;
        }

        if (!_textMateByEditor.TryGetValue(editor, out var installation))
        {
            LogHighlight("ApplyGrammarByFilePath: skipped (no TextMate installation for this editor).");
            return;
        }

        var ext = string.IsNullOrEmpty(filePath) ? "" : Path.GetExtension(filePath).ToLowerInvariant();
        if (!Services.EditorLanguageSupport.ExtensionToGrammarExtension.TryGetValue(ext, out var grammarExt))
        {
            LogHighlight($"ApplyGrammarByFilePath: no grammar mapping for ext='{ext}' file='{filePath ?? "<null>"}'.");
            return;
        }

        try
        {
            var lang = _registryOptions.GetLanguageByExtension(grammarExt);
            var scope = _registryOptions.GetScopeByLanguageId(lang.Id);
            installation.SetGrammar(scope);
            LogHighlight($"ApplyGrammarByFilePath: OK file='{filePath ?? "<null>"}' ext='{ext}' grammarExt='{grammarExt}' langId='{lang.Id}' scope='{scope}'.");
        }
        catch (Exception ex)
        {
            // грамматика не в бандле — не меняем подсветку
            LogHighlight($"ApplyGrammarByFilePath: FAILED file='{filePath ?? "<null>"}' ext='{ext}' grammarExt='{grammarExt}': {ex}");
        }
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        if (_suppressEditorSync || DataContext is not ViewModels.MainWindowViewModel vm)
            return;

        var editor = sender as TextEditor ?? TryGetActiveDockEditor();
        if (editor is null)
            return;

        vm.EditorText = editor.Document.Text;
    }
}

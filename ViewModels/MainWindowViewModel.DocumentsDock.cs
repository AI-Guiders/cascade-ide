using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Threading;
using CascadeIDE.Models;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Открыть файл выбранного узла после паузы, чтобы не реагировать на двойное срабатывание/мигание выбора в дереве.</summary>
    private async Task OpenFileAfterDebounceAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(OpenFileDebounceMs, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var value = SelectedSolutionItem;
            if (value?.FullPath is not { } path || !File.Exists(path))
                return;
            var normalizedPath = Path.GetFullPath(path);
            // Уже открыт этот файл и контент загружен — не затирать (защита от сбоя выбора в дереве при появлении превью .md)
            if (string.Equals(CurrentFilePath, normalizedPath, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(EditorText))
                return;
            IsLoadingCurrentFile = true;
            OpenOrActivateDocument(normalizedPath);
            IsLoadingCurrentFile = false;
        });
    }

    /// <summary>Выделить в дереве решения узел, соответствующий текущему открытому файлу (после ide_open_file и т.п.).</summary>
    private void SyncSelectedSolutionItemToCurrentFile()
    {
        var current = CurrentFilePath;
        if (string.IsNullOrEmpty(current))
            return;
        var normalized = Path.GetFullPath(current);
        var item = FindSolutionItemByPath(SolutionRoots, normalized);
        if (item is not null)
            SelectedSolutionItem = item;
    }

    partial void OnSelectedDocumentChanged(OpenDocumentViewModel? value)
    {
        _selectedDocumentContentSubscription?.Dispose();
        _selectedDocumentContentSubscription = null;

        _isSwitchingDocument = true;
        try
        {
            if (value is null)
            {
                CurrentFilePath = null;
                EditorText = "";
                return;
            }

            CurrentFilePath = value.FilePath;
            EditorText = value.Content;
            SyncSelectedSolutionItemToCurrentFile();

            _selectedDocumentContentSubscription = ObservePropertyChanged(value, nameof(OpenDocumentViewModel.Content), () =>
            {
                if (_isSwitchingDocument)
                    return;
                // EditorText is used by MCP tools; keep it synced even if editor binds indirectly.
                if (!string.Equals(EditorText, value.Content, StringComparison.Ordinal))
                    EditorText = value.Content ?? "";
            });
        }
        finally
        {
            _isSwitchingDocument = false;
        }
    }

    partial void OnEditorTextChanged(string value)
    {
        if (_isSwitchingDocument || SelectedDocument is null)
            return;

        SelectedDocument.Content = value ?? "";
        SelectedDocument.IsDirty = !string.Equals(SelectedDocument.Content, SelectedDocument.OriginalContent, StringComparison.Ordinal);
        OnPropertyChanged(nameof(EditorTextGroup2));
        OnPropertyChanged(nameof(EditorTextGroup3));
    }

    partial void OnCurrentFilePathChanged(string? value) => RefreshComplexityBadgeFromCurrentFile();

    /// <summary>Прокси «сложности» для task cockpit: число строк текущего файла на диске (при переключении документа).</summary>
    private void RefreshComplexityBadgeFromCurrentFile()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentFilePath) || !File.Exists(CurrentFilePath))
            {
                ComplexityBadge = 0;
                return;
            }

            const int maxLines = 95_000;
            var lines = 0;
            using var sr = new StreamReader(CurrentFilePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            while (sr.ReadLine() is not null)
            {
                lines++;
                if (lines >= maxLines)
                    break;
            }

            ComplexityBadge = lines;
        }
        catch
        {
            ComplexityBadge = 0;
        }
    }

    private void OpenOrActivateDocument(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        var normalized = Path.GetFullPath(filePath);
        var existing = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, normalized, StringComparison.OrdinalIgnoreCase));
        var targetGroup = Math.Clamp(ActiveEditorGroup, 1, 3);
        if (existing is null)
        {
            var text = SafeReadFile(normalized);
            existing = new OpenDocumentViewModel(normalized, Path.GetFileName(normalized), text)
            {
                GroupIndex = targetGroup
            };
            OpenDocuments.Add(existing);
            GetGroupCollection(targetGroup).Add(existing);

            DockDocuments.Add(new DockDocumentViewModel(existing)
            {
                Id = normalized,
                Title = existing.DisplayTitle
            });
        }
        else
        {
            if (string.IsNullOrEmpty(existing.OriginalContent))
            {
                // If a previous read failed and we opened an "empty" document, try to re-load it
                // so the editor isn't blank for regular files.
                var text = SafeReadFile(normalized);
                existing.ReloadContent(text);
            }

            if (existing.GroupIndex != targetGroup)
            {
                MoveDocumentToGroupInternal(existing, targetGroup);
            }
        }

        ActivateDocumentInternal(existing);
        // Rebuild after setting DockActiveDocument so DockControl sees ActiveDockable.
        RebuildAndReinitDockLayout();
    }

    private static string SafeReadFile(string path)
    {
        // File IO is best-effort; editor should show content whenever possible.
        // Some Windows setups (locks / transient FS issues) can cause File.ReadAllText to throw,
        // so we use a FileStream with sharing and a tiny retry.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
            catch
            {
                return "";
            }
        }

        return "";
    }

    partial void OnDockActiveDocumentChanged(IDockable? value)
    {
        if (value is DockDocumentViewModel d)
            ActivateDocumentInternal(d.Doc);
    }

    private Dock.Model.Core.IDock BuildDockLayout()
    {
        var documents = new DocumentDock
        {
            Id = "DocumentsDock",
            Title = "Documents",
            IsCollapsable = false,
            VisibleDockables = DockFactory.CreateList<Dock.Model.Core.IDockable>(DockDocuments.ToArray()),
            ActiveDockable = DockActiveDocument,
            CanCreateDocument = false
        };

        var root = DockFactory.CreateRootDock();
        root.Id = "RootDock";
        root.VisibleDockables = DockFactory.CreateList<Dock.Model.Core.IDockable>(documents);
        root.DefaultDockable = documents;
        root.ActiveDockable = documents;
        return root;
    }

    private void RebuildAndReinitDockLayout()
    {
        // DockLayout.VisibleDockables берётся из DockDocuments.ToArray() во время построения.
        // Поэтому после добавления/удаления документов нужно заново инициализировать DockFactory,
        // иначе UI может показывать "No document open".
        DockLayout = BuildDockLayout();
        DockFactory.InitLayout(DockLayout);
        OnPropertyChanged(nameof(DockLayout));
    }

    private static IDisposable ObservePropertyChanged(INotifyPropertyChanged obj, string propertyName, Action onChanged)
    {
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
                onChanged();
        };
        obj.PropertyChanged += handler;
        return new Subscription(() => obj.PropertyChanged -= handler);
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }

    private ObservableCollection<OpenDocumentViewModel> GetGroupCollection(int group) =>
        group switch
        {
            2 => Group2Documents,
            3 => Group3Documents,
            _ => Group1Documents
        };

    private void ActivateDocumentInternal(OpenDocumentViewModel doc)
    {
        ActiveEditorGroup = doc.GroupIndex;
        switch (doc.GroupIndex)
        {
            case 2:
                SelectedDocumentGroup2 = doc;
                break;
            case 3:
                SelectedDocumentGroup3 = doc;
                break;
            default:
                SelectedDocument = doc;
                break;
        }

        var dockDoc = DockDocuments
            .OfType<DockDocumentViewModel>()
            .FirstOrDefault(d => string.Equals(d.Doc.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));
        if (dockDoc is not null && !ReferenceEquals(DockActiveDocument, dockDoc))
            DockActiveDocument = dockDoc;
    }

    private void MoveDocumentToGroupInternal(OpenDocumentViewModel doc, int targetGroup)
    {
        var normalizedGroup = Math.Clamp(targetGroup, 1, 3);
        if (doc.GroupIndex == normalizedGroup)
            return;

        var sourceCollection = GetGroupCollection(doc.GroupIndex);
        sourceCollection.Remove(doc);
        var targetCollection = GetGroupCollection(normalizedGroup);
        if (!targetCollection.Contains(doc))
            targetCollection.Add(doc);

        doc.GroupIndex = normalizedGroup;

        if (SelectedDocument == doc && normalizedGroup != 1)
            SelectedDocument = Group1Documents.FirstOrDefault();
        if (SelectedDocumentGroup2 == doc && normalizedGroup != 2)
            SelectedDocumentGroup2 = Group2Documents.FirstOrDefault();
        if (SelectedDocumentGroup3 == doc && normalizedGroup != 3)
            SelectedDocumentGroup3 = Group3Documents.FirstOrDefault();

        ActivateDocumentInternal(doc);
    }

    private static SolutionItem? FindSolutionItemByPath(IEnumerable<SolutionItem> items, string fullPath)
    {
        foreach (var node in items)
        {
            if (node.FullPath is not null && string.Equals(Path.GetFullPath(node.FullPath), fullPath, StringComparison.OrdinalIgnoreCase))
                return node;
            var found = FindSolutionItemByPath(node.Children, fullPath);
            if (found is not null)
                return found;
        }
        return null;
    }
}
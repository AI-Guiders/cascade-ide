using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Factory = Dock.Model.Mvvm.Factory;

namespace CascadeIDE.Features.Documents;

/// <summary>
/// Открытые документы, группы редакторов и Dock MDI — вынесено из <see cref="ViewModels.MainWindowViewModel"/>.
/// </summary>
public sealed partial class DocumentsWorkspaceViewModel : ObservableObject
{
    public const int OpenFileDebounceMs = 100;

    private readonly MainWindowViewModel _host;
    private readonly SolutionWorkspaceViewModel _workspace;
    private readonly Action _notifyReopenClosedCommand;
    private readonly Stack<string> _recentlyClosedDocumentPaths = new();
    private int _recentlyClosedDocumentCount;
    private bool _isSwitchingDocument;
    private IDisposable? _selectedDocumentContentSubscription;

    public DocumentsWorkspaceViewModel(
        MainWindowViewModel host,
        SolutionWorkspaceViewModel workspace,
        Action notifyReopenClosedCommand)
    {
        _host = host;
        _workspace = workspace;
        _notifyReopenClosedCommand = notifyReopenClosedCommand;
        OpenDocuments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasOpenDocuments));
    }

    public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; } = [];

    public bool HasOpenDocuments => OpenDocuments.Count > 0;

    public int RecentlyClosedDocumentCount => _recentlyClosedDocumentCount;

    public IFactory DockFactory { get; private set; } = null!;

    public IDock DockLayout { get; private set; } = null!;

    public ObservableCollection<IDockable> DockDocuments { get; } = [];

    [ObservableProperty]
    private IDockable? _dockActiveDocument;

    [ObservableProperty]
    private OpenDocumentViewModel? _selectedDocument;

    [ObservableProperty]
    private OpenDocumentViewModel? _selectedDocumentGroup2;

    [ObservableProperty]
    private OpenDocumentViewModel? _selectedDocumentGroup3;

    [ObservableProperty]
    private int _activeEditorGroup = 1;

    public ObservableCollection<OpenDocumentViewModel> Group1Documents { get; } = [];
    public ObservableCollection<OpenDocumentViewModel> Group2Documents { get; } = [];
    public ObservableCollection<OpenDocumentViewModel> Group3Documents { get; } = [];

    public void InitializeDock()
    {
        DockFactory = new Factory();
        DockLayout = BuildDockLayout();
        DockFactory.InitLayout(DockLayout);
    }

    /// <summary>Сброс вкладок и дока при загрузке нового решения (пути/selection чистит хост).</summary>
    public void ClearForNewSolution()
    {
        Group1Documents.Clear();
        Group2Documents.Clear();
        Group3Documents.Clear();
        OpenDocuments.Clear();
        DockDocuments.Clear();
        DockActiveDocument = null;
        _recentlyClosedDocumentPaths.Clear();
        _recentlyClosedDocumentCount = 0;
        _notifyReopenClosedCommand();
        SelectedDocument = null;
        SelectedDocumentGroup2 = null;
        SelectedDocumentGroup3 = null;
        RebuildAndReinitDockLayout();
    }

    /// <summary>Открыть файл выбранного узла после паузы, чтобы не реагировать на двойное срабатывание/мигание выбора в дереве.</summary>
    public async Task OpenFileAfterDebounceAsync(CancellationToken ct)
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
            var value = _workspace.SelectedSolutionItem;
            if (value?.FullPath is not { } path || !File.Exists(path))
                return;
            var normalizedPath = Path.GetFullPath(path);
            if (string.Equals(_host.CurrentFilePath, normalizedPath, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(_host.EditorText))
                return;
            _host.IsLoadingCurrentFile = true;
            OpenOrActivateDocument(normalizedPath);
            _host.IsLoadingCurrentFile = false;
        });
    }

    public void OpenOrActivateDocument(string filePath)
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
                var text = SafeReadFile(normalized);
                existing.ReloadContent(text);
            }

            if (existing.GroupIndex != targetGroup)
                MoveDocumentToGroupInternal(existing, targetGroup);
        }

        ActivateDocumentInternal(existing);
        RebuildAndReinitDockLayout();
    }

    public void ActivateDocumentInternal(OpenDocumentViewModel doc)
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

    public void MoveDocumentToGroupInternal(OpenDocumentViewModel doc, int targetGroup)
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

    public ObservableCollection<OpenDocumentViewModel> GetGroupCollection(int group) =>
        group switch
        {
            2 => Group2Documents,
            3 => Group3Documents,
            _ => Group1Documents
        };

    public void RebuildAndReinitDockLayout()
    {
        DockLayout = BuildDockLayout();
        DockFactory.InitLayout(DockLayout);
        OnPropertyChanged(nameof(DockLayout));
    }

    public void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            return;

        var index = OpenDocuments.IndexOf(doc);
        GetGroupCollection(doc.GroupIndex).Remove(doc);
        OpenDocuments.Remove(doc);
        var dockDoc = DockDocuments
            .OfType<DockDocumentViewModel>()
            .FirstOrDefault(d => string.Equals(d.Doc.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));
        if (dockDoc is not null)
            DockDocuments.Remove(dockDoc);
        _recentlyClosedDocumentPaths.Push(doc.FilePath);
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        _notifyReopenClosedCommand();

        if (OpenDocuments.Count == 0)
        {
            SelectedDocument = null;
            SelectedDocumentGroup2 = null;
            SelectedDocumentGroup3 = null;
            return;
        }

        SelectedDocument = Group1Documents.FirstOrDefault();
        SelectedDocumentGroup2 = Group2Documents.FirstOrDefault();
        SelectedDocumentGroup3 = Group3Documents.FirstOrDefault();

        var next =
            SelectedDocument
            ?? SelectedDocumentGroup2
            ?? SelectedDocumentGroup3
            ?? OpenDocuments[Math.Clamp(index, 0, OpenDocuments.Count - 1)];

        ActivateDocumentInternal(next);
        RebuildAndReinitDockLayout();
    }

    public void ReopenLastClosedDocument()
    {
        if (_recentlyClosedDocumentPaths.Count == 0)
            return;
        var path = _recentlyClosedDocumentPaths.Pop();
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        _notifyReopenClosedCommand();
        OpenOrActivateDocument(path);
    }

    public bool CanReopenClosedDocument() => _recentlyClosedDocumentCount > 0;

    /// <summary>При вводе в основном редакторе — обновить активный документ группы 1.</summary>
    public void ApplyEditorTextFromHost(string value)
    {
        if (_isSwitchingDocument || SelectedDocument is null)
            return;

        SelectedDocument.Content = value ?? "";
        SelectedDocument.IsDirty = !string.Equals(SelectedDocument.Content, SelectedDocument.OriginalContent, StringComparison.Ordinal);
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
                _host.CurrentFilePath = null;
                _host.EditorText = "";
                return;
            }

            _host.CurrentFilePath = value.FilePath;
            _host.EditorText = value.Content;
            SyncSelectedSolutionItemToCurrentFile();

            _selectedDocumentContentSubscription = ObservePropertyChanged(value, nameof(OpenDocumentViewModel.Content), () =>
            {
                if (_isSwitchingDocument)
                    return;
                if (!string.Equals(_host.EditorText, value.Content, StringComparison.Ordinal))
                    _host.EditorText = value.Content ?? "";
            });
        }
        finally
        {
            _isSwitchingDocument = false;
        }
    }

    partial void OnDockActiveDocumentChanged(IDockable? value)
    {
        if (value is DockDocumentViewModel d)
            ActivateDocumentInternal(d.Doc);
    }

    private IDock BuildDockLayout()
    {
        var documents = new DocumentDock
        {
            Id = "DocumentsDock",
            Title = "Documents",
            IsCollapsable = false,
            VisibleDockables = DockFactory.CreateList<IDockable>(DockDocuments.ToArray()),
            ActiveDockable = DockActiveDocument,
            CanCreateDocument = false
        };

        var root = DockFactory.CreateRootDock();
        root.Id = "RootDock";
        root.VisibleDockables = DockFactory.CreateList<IDockable>(documents);
        root.DefaultDockable = documents;
        root.ActiveDockable = documents;
        return root;
    }

    private void SyncSelectedSolutionItemToCurrentFile()
    {
        var current = _host.CurrentFilePath;
        if (string.IsNullOrEmpty(current))
            return;
        var normalized = Path.GetFullPath(current);
        var item = FindSolutionItemByPath(_workspace.SolutionRoots, normalized);
        if (item is not null)
            _workspace.SelectedSolutionItem = item;
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

    private static string SafeReadFile(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
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
}

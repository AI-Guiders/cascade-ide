using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
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
    private readonly Stack<string> _recentlyClosedDocumentPaths = new();
    private int _recentlyClosedDocumentCount;
    private bool _isSwitchingDocument;
    private IDisposable? _selectedDocumentContentSubscription;

    public DocumentsWorkspaceViewModel(MainWindowViewModel host, SolutionWorkspaceViewModel workspace)
    {
        _host = host;
        _workspace = workspace;
        OpenDocuments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasOpenDocuments));
    }

    public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; } = [];

    public bool HasOpenDocuments => OpenDocuments.Count > 0;

    public int RecentlyClosedDocumentCount => _recentlyClosedDocumentCount;

    public IFactory DockFactory { get; private set; } = null!;

    public IDock DockLayout { get; private set; } = null!;

    public ObservableCollection<IDockable> DockDocuments { get; } = [];

    /// <summary>Снимки открытых вкладок для MCP <c>get_open_document_text</c>; только с UI-потока.</summary>
    public List<IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot> CollectIdeMcpOpenDocumentTabSnapshots()
    {
        var list = new List<IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot>();
        foreach (var item in DockDocuments)
        {
            if (item is not DockDocumentViewModel dvm)
                continue;
            var doc = dvm.Doc;
            list.Add(new IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot(doc.FilePath, doc.Content, doc.IsDirty));
        }

        return list;
    }

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
        NotifyReopenClosedCanExecuteChanged();
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

        await UiScheduler.Default.InvokeAsync(() =>
        {
            var value = _workspace.SelectedSolutionItem;
            if (value?.FullPath is not { } path)
                return;
            if (!SolutionTreePath.TryGetFullPath(path, out var normalizedPath) || !File.Exists(normalizedPath))
                return;
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
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (!SolutionTreePath.TryGetFullPath(filePath, out var normalized) || !File.Exists(normalized))
            return;
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
        _host.RevealEditorForOpenedDocument();
    }

    /// <summary>
    /// Для reveal с карты/anchor: не пересобирать dock, если файл уже открыт (ADR 0130).
    /// Новый файл — полный <see cref="OpenOrActivateDocument"/>.
    /// </summary>
    public void ActivateDocumentForReveal(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        if (!SolutionTreePath.TryGetFullPath(filePath, out var normalized) || !File.Exists(normalized))
            return;

        var existing = OpenDocuments.FirstOrDefault(d =>
            string.Equals(d.FilePath, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            OpenOrActivateDocument(normalized);
            return;
        }

        if (EditorTextCoordinateUtilities.PathsReferToSameFile(_host.CurrentFilePath, normalized))
            return;

        ActivateDocumentInternal(existing);
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

    public void CloseDocumentByPath(string? filePath)
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
        NotifyReopenClosedCanExecuteChanged();

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
        NotifyReopenClosedCanExecuteChanged();
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
        _host.NotifyAgentEnvironmentDocumentWrite(SelectedDocument.FilePath);
    }

    /// <summary>MCP <c>apply_edit</c>: правка в модели любой открытой вкладки; при необходимости открывает файл.</summary>
    public string ApplyMcpEditToDocument(
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string newText)
    {
        if (!SolutionTreePath.TryGetFullPath(filePath, out var normalized))
            return JsonSerializer.Serialize(new { error = "invalid_path", message = "Некорректный file_path." });

        var doc = FindOpenDocument(normalized);
        if (doc is null)
        {
            if (!File.Exists(normalized))
                return JsonSerializer.Serialize(new { error = "not_found", message = "Файл не найден.", file_path = normalized });

            OpenOrActivateDocument(normalized);
            doc = FindOpenDocument(normalized);
            if (doc is null)
                return JsonSerializer.Serialize(new { error = "open_failed", message = "Не удалось открыть файл.", file_path = normalized });
        }

        if (!IdeMcpEditorOrchestrator.TryReplaceTextRange(
                doc.Content, startLine, startColumn, endLine, endColumn, newText, out var updated))
            return JsonSerializer.Serialize(new { error = "invalid_range", message = "Некорректный диапазон line/column.", file_path = doc.FilePath });

        doc.Content = updated;
        doc.IsDirty = !string.Equals(doc.Content, doc.OriginalContent, StringComparison.Ordinal);
        _host.NotifyAgentEnvironmentDocumentWrite(doc.FilePath);

        if (IsActiveDocumentForHost(doc))
            _host.EditorText = updated;

        return "OK";
    }

    /// <summary>MCP <c>save_document</c>: запись буфера открытой вкладки или явного content на диск.</summary>
    public string SaveDocumentToDisk(string? filePath, string? content)
    {
        var workspace = _host.McpGetWorkspacePath();

        if (!string.IsNullOrWhiteSpace(content))
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return JsonSerializer.Serialize(new { error = "no_path", message = "file_path обязателен при записи content." });

            if (!WorkspaceDocumentFileIo.TryResolvePath(workspace, null, filePath, out var target, out var resolveError))
                return JsonSerializer.Serialize(new { error = "resolve_failed", message = resolveError });

            if (!WorkspaceDocumentFileIo.TryWriteText(target, content, createIfMissing: true, out var writeError))
                return JsonSerializer.Serialize(new { error = "write_failed", message = writeError, file_path = target });

            var open = FindOpenDocument(target);
            if (open is not null)
            {
                open.ReloadContent(content);
                if (IsActiveDocumentForHost(open))
                    _host.EditorText = open.Content;
            }

            _host.NotifyAgentEnvironmentDocumentWrite(target);
            return JsonSerializer.Serialize(new { file_path = target, bytes = Encoding.UTF8.GetByteCount(content) });
        }

        var path = string.IsNullOrWhiteSpace(filePath) ? _host.CurrentFilePath : filePath.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return JsonSerializer.Serialize(new { error = "no_path", message = "Нет открытого файла и file_path не задан." });

        if (!WorkspaceDocumentFileIo.TryResolvePath(workspace, null, path, out var normalized, out var resolveErr))
            return JsonSerializer.Serialize(new { error = "resolve_failed", message = resolveErr });

        var doc = FindOpenDocument(normalized);
        if (doc is null)
            return JsonSerializer.Serialize(new { error = "not_open", message = "Файл не открыт; передай content для записи на диск.", file_path = normalized });

        if (!WorkspaceDocumentFileIo.TryWriteText(normalized, doc.Content, createIfMissing: false, out var diskError))
            return JsonSerializer.Serialize(new { error = "write_failed", message = diskError, file_path = normalized });

        doc.ReloadContent(doc.Content);
        _host.NotifyAgentEnvironmentDocumentWrite(normalized);
        return JsonSerializer.Serialize(new { file_path = normalized, bytes = Encoding.UTF8.GetByteCount(doc.Content) });
    }

    private OpenDocumentViewModel? FindOpenDocument(string normalizedPath) =>
        OpenDocuments.FirstOrDefault(d =>
            EditorTextCoordinateUtilities.PathsReferToSameFile(d.FilePath, normalizedPath));

    private bool IsActiveDocumentForHost(OpenDocumentViewModel doc) =>
        !string.IsNullOrEmpty(_host.CurrentFilePath)
        && EditorTextCoordinateUtilities.PathsReferToSameFile(_host.CurrentFilePath, doc.FilePath);

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
        if (!SolutionTreePath.TryGetFullPath(current, out var normalized))
            return;
        var item = SolutionTreePath.FindItemByFullPath(_workspace.SolutionRoots, normalized);
        if (item is not null)
            _workspace.SelectedSolutionItem = item;
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

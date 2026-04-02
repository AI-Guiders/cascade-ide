using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;
using CascadeIDE.Services.Lsp;
using CascadeIDE.ViewModels;
using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services;

/// <summary>
/// Диагностики Roslyn по всем открытым .cs: debounce, кэш по пути, список для панели Problems, событие для перерисовки вкладок.
/// </summary>
public sealed class WorkspaceDiagnosticsCoordinator : IDisposable
{
    private readonly CSharpLanguageService _language;
    private readonly ProblemsPanelViewModel _problems;
    private readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(400);
    private readonly ConcurrentDictionary<string, byte> _watchedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, List<EditorDiagnosticStrip>> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.OrdinalIgnoreCase);

    private MainWindowViewModel? _vm;
    private CSharpLspDiagnosticsHost? _lspHost;
    private bool _disposed;

    public WorkspaceDiagnosticsCoordinator(CSharpLanguageService language, ProblemsPanelViewModel problems)
    {
        _language = language;
        _problems = problems;
    }

    public event Action? DiagnosticsChanged;

    public void Attach(MainWindowViewModel vm)
    {
        _vm = vm;
        vm.Documents.OpenDocuments.CollectionChanged += OnOpenDocumentsChanged;
        foreach (var d in vm.Documents.OpenDocuments)
            WatchDocument(d);
        foreach (var d in vm.Documents.OpenDocuments)
            TrySchedule(d.FilePath, d.Content);
    }

    /// <summary>Подключить один C# LSP-процесс (или null — только парсер Roslyn).</summary>
    public void SetLspDiagnosticsHost(CSharpLspDiagnosticsHost? host)
    {
        if (ReferenceEquals(_lspHost, host))
            return;
        if (_lspHost is not null)
            _lspHost.DiagnosticsChanged -= OnLspHostDiagnosticsChanged;
        _lspHost = host;
        if (_lspHost is not null)
            _lspHost.DiagnosticsChanged += OnLspHostDiagnosticsChanged;

        if (_lspHost is { IsActive: true })
        {
            foreach (var kv in _pending)
            {
                if (_pending.TryRemove(kv.Key, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }
        }
        else if (_vm is not null)
        {
            foreach (var d in _vm.Documents.OpenDocuments)
                TrySchedule(d.FilePath, d.Content);
        }

        RebuildProblemsList();
        DiagnosticsChanged?.Invoke();
    }

    private void OnLspHostDiagnosticsChanged()
    {
        RebuildProblemsList();
        DiagnosticsChanged?.Invoke();
    }

    public IReadOnlyList<EditorDiagnosticStrip> GetStripsForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        if (_lspHost is { IsActive: true } && filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return _lspHost.GetStripsForFile(filePath);
        var key = NormalizePath(filePath);
        lock (_cacheLock)
        {
            return _byPath.TryGetValue(key, out var list) ? list : [];
        }
    }

    /// <summary>Найти диагностику, покрывающую offset в документе (для tooltip).</summary>
    public static EditorDiagnosticStrip? HitTest(IReadOnlyList<EditorDiagnosticStrip> strips, int offset)
    {
        foreach (var s in strips)
        {
            var end = s.Start + s.Length;
            if (offset >= s.Start && offset < end)
                return s;
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_vm is not null)
            _vm.Documents.OpenDocuments.CollectionChanged -= OnOpenDocumentsChanged;
        foreach (var cts in _pending.Values)
            cts.Cancel();
        _pending.Clear();
    }

    private void OnOpenDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (OpenDocumentViewModel d in e.NewItems)
                WatchDocument(d);
        }

        if (e.OldItems is not null)
        {
            foreach (OpenDocumentViewModel d in e.OldItems)
            {
                UnwatchDocument(d);
                var key = NormalizePath(d.FilePath);
                lock (_cacheLock)
                {
                    _byPath.Remove(key);
                }

                if (_pending.TryRemove(key, out var cts))
                    cts.Cancel();
            }
        }

        RebuildProblemsList();
        DiagnosticsChanged?.Invoke();
    }

    private void WatchDocument(OpenDocumentViewModel doc)
    {
        if (!_watchedPaths.TryAdd(doc.FilePath, 0))
            return;
        doc.PropertyChanged += OnDocumentPropertyChanged;
        TrySchedule(doc.FilePath, doc.Content);
    }

    private void UnwatchDocument(OpenDocumentViewModel doc)
    {
        if (!_watchedPaths.TryRemove(doc.FilePath, out _))
            return;
        doc.PropertyChanged -= OnDocumentPropertyChanged;
    }

    private void OnDocumentPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OpenDocumentViewModel.Content))
            return;
        if (sender is not OpenDocumentViewModel doc)
            return;
        TrySchedule(doc.FilePath, doc.Content);
    }

    private void TrySchedule(string filePath, string text)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;

        if (_lspHost is { IsActive: true })
        {
            _lspHost.ScheduleDocumentSync(filePath, text);
            return;
        }

        var key = NormalizePath(filePath);
        if (_pending.TryGetValue(key, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pending[key] = cts;
        var token = cts.Token;
        var snapshot = text ?? "";

        _ = DebouncedComputeAsync(key, filePath, snapshot, token);
    }

    private async Task DebouncedComputeAsync(string key, string filePath, string textSnapshot, CancellationToken ct)
    {
        try
        {
            await Task.Delay(_debounce, ct).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        IReadOnlyList<Diagnostic> raw;
        try
        {
            raw = await Task.Run(() => _language.GetDiagnosticsForFile(filePath, textSnapshot, ct), ct).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        var len = textSnapshot.Length;
        var strips = new List<EditorDiagnosticStrip>(raw.Count);
        foreach (var d in raw)
        {
            if (d.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
                continue;
            if (!d.Location.IsInSource)
                continue;
            var s = d.Location.SourceSpan;
            if (s.Start < 0 || s.Start >= len)
                continue;
            var spanLen = s.Length <= 0 ? 1 : s.Length;
            if (s.Start + spanLen > len)
                spanLen = len - s.Start;
            if (spanLen <= 0)
                continue;
            var lp = d.Location.GetLineSpan().StartLinePosition;
            strips.Add(new EditorDiagnosticStrip(
                s.Start,
                spanLen,
                d.Severity,
                d.Id,
                d.GetMessage(),
                lp.Line + 1,
                lp.Character + 1));
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (ct.IsCancellationRequested)
                return;
            var doc = _vm?.Documents.OpenDocuments.FirstOrDefault(d => NormalizePath(d.FilePath) == key);
            if (doc is not null && !string.Equals(doc.Content, textSnapshot, StringComparison.Ordinal))
                return;

            lock (_cacheLock)
            {
                _byPath[key] = strips;
            }

            RebuildProblemsList();
            DiagnosticsChanged?.Invoke();
        }, DispatcherPriority.Background);
    }

    private void RebuildProblemsList()
    {
        if (_vm is null)
            return;
        var rows = new List<ProblemListItem>();
        lock (_cacheLock)
        {
            foreach (var doc in _vm.Documents.OpenDocuments)
            {
                if (!doc.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                IReadOnlyList<EditorDiagnosticStrip> list;
                if (_lspHost is { IsActive: true })
                    list = _lspHost.GetStripsForFile(doc.FilePath);
                else
                {
                    var key = NormalizePath(doc.FilePath);
                    if (!_byPath.TryGetValue(key, out var roslynList))
                        continue;
                    list = roslynList;
                }

                foreach (var s in list)
                {
                    rows.Add(new ProblemListItem(
                        doc.FilePath,
                        s.Line1,
                        s.Column1,
                        s.Severity == DiagnosticSeverity.Error ? "error" : "warning",
                        s.Id,
                        s.Message));
                }
            }
        }

        rows.Sort((a, b) =>
        {
            var c = string.Compare(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;
            c = a.Line.CompareTo(b.Line);
            return c != 0 ? c : a.Column.CompareTo(b.Column);
        });

        _problems.ReplaceItems(rows);
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path);
}

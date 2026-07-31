#nullable enable
using System.IO;
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Documents;

public sealed partial class DocumentsWorkspaceViewModel
{
    public void ApplyEditorTextFromHost(string value)
    {
        if (_isSwitchingDocument)
            return;

        var doc = ResolveHostEditedDocument();
        if (doc is null)
            return;

        ApplyEditorTextToDocument(doc, value);
    }

    /// <summary>Monaco / MCP: правка конкретной открытой вкладки по пути.</summary>
    public void ApplyEditorTextForDocument(string filePath, string value)
    {
        if (_isSwitchingDocument || string.IsNullOrWhiteSpace(filePath))
            return;

        if (!SolutionTreePath.TryGetFullPath(filePath, out var normalized))
            normalized = filePath;

        var doc = FindOpenDocument(normalized);
        if (doc is null)
            return;

        ApplyEditorTextToDocument(doc, value);
    }

    /// <summary>Monaco: правка по ссылке на открытую вкладку (без повторного resolve пути).</summary>
    public void ApplyEditorTextToOpenDocument(OpenDocumentViewModel doc, string value)
    {
        if (_isSwitchingDocument)
            return;

        ApplyEditorTextToDocument(doc, value);
    }

    /// <summary>Синхронизировать <see cref="OpenDocumentViewModel.Content"/> из <see cref="MainWindowViewModel.EditorText"/> (перед сборкой и т.п.).</summary>
    public void SyncActiveEditorBufferFromHost()
    {
        if (_isSwitchingDocument)
            return;

        var doc = ResolveHostEditedDocument();
        if (doc is null)
            return;

        ApplyEditorTextToDocument(doc, _host.EditorText ?? "");
    }

    /// <summary>Записать на диск все грязные открытые вкладки (после <see cref="SyncActiveEditorBufferFromHost"/>).</summary>
    public int SaveDirtyOpenDocumentsToDisk()
    {
        var saved = 0;
        foreach (var doc in OpenDocuments)
        {
            if (!doc.IsDirty)
                continue;
            if (!WorkspaceDocumentFileIo.TryWriteText(doc.FilePath, doc.Content, createIfMissing: false, out _))
                continue;
            doc.ReloadContent(doc.Content);
            _host.NotifyAgentEnvironmentDocumentWrite(doc.FilePath);
            Features.Cdp.CdpDiskSyncProjector.Instance?.PublishHumanSave(doc.FilePath);
            saved++;
        }

        return saved;
    }

    /// <summary>Roslyn refactorings: apply multi-file text changes from Monaco (move type, rename, extract interface).</summary>
    public void ApplyRoslynWorkspaceChanges(IReadOnlyList<CideEditorDocumentTextChange> changes)
    {
        foreach (var change in changes)
        {
            if (!SolutionTreePath.TryGetFullPath(change.FilePath, out var normalized))
                continue;

            if (!string.IsNullOrWhiteSpace(change.PreviousFilePath)
                && SolutionTreePath.TryGetFullPath(change.PreviousFilePath, out var oldPath)
                && !string.Equals(oldPath, normalized, StringComparison.OrdinalIgnoreCase))
            {
                var renameDir = Path.GetDirectoryName(normalized);
                if (renameDir is not null)
                    Directory.CreateDirectory(renameDir);
                if (File.Exists(oldPath))
                    File.Move(oldPath, normalized, overwrite: true);
            }

            var dir = Path.GetDirectoryName(normalized);
            if (dir is not null)
                Directory.CreateDirectory(dir);

            if (change.IsNewFile || !File.Exists(normalized))
            {
                WorkspaceDocumentFileIo.TryWriteText(normalized, change.Text, createIfMissing: true, out _);
                if (FindOpenDocument(normalized) is null)
                    OpenOrActivateDocument(normalized);
            }

            var doc = FindOpenDocument(normalized);
            if (doc is not null)
                ApplyEditorTextToDocument(doc, change.Text);
            else
                WorkspaceDocumentFileIo.TryWriteText(normalized, change.Text, createIfMissing: true, out _);

            var open = doc ?? FindOpenDocument(normalized);
            if (open is not null && IsActiveDocumentForHost(open))
                _host.EditorText = change.Text;

            _host.NotifyAgentEnvironmentDocumentWrite(normalized);
        }
    }

    private void ApplyEditorTextToDocument(OpenDocumentViewModel doc, string value)
    {
        var text = value ?? "";
        if (string.Equals(doc.Content, text, StringComparison.Ordinal))
            return;

        doc.Content = text;
        doc.IsDirty = !string.Equals(doc.Content, doc.OriginalContent, StringComparison.Ordinal);
        _host.NotifyAgentEnvironmentDocumentWrite(doc.FilePath);
    }

    private OpenDocumentViewModel? ResolveHostEditedDocument()
    {
        if (DockActiveDocument is DockDocumentViewModel dockDoc)
            return dockDoc.Doc;

        if (!string.IsNullOrEmpty(_host.CurrentFilePath))
            return FindOpenDocument(_host.CurrentFilePath);

        return ActiveEditorGroup switch
        {
            2 => SelectedDocumentGroup2,
            3 => SelectedDocumentGroup3,
            _ => SelectedDocument,
        };
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
            Features.Cdp.CdpDiskSyncProjector.Instance?.PublishHumanSave(target);
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
        Features.Cdp.CdpDiskSyncProjector.Instance?.PublishHumanSave(normalized);
        return JsonSerializer.Serialize(new { file_path = normalized, bytes = Encoding.UTF8.GetByteCount(doc.Content) });
    }
}


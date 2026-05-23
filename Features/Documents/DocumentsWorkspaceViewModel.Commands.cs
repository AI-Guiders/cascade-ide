using System.Linq;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Documents;

/// <summary>Relay: вкладки документов и группы.</summary>
public sealed partial class DocumentsWorkspaceViewModel
{
    [RelayCommand]
    private void ActivateDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            OpenOrActivateDocument(filePath);
        else
            ActivateDocumentInternal(doc);
    }

    [RelayCommand]
    private void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        CloseDocumentByPath(filePath);
    }

    [RelayCommand]
    private void TogglePinDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private void MoveDocumentToGroup1(string? filePath) => MoveDocumentToGroup(filePath, 1);

    [RelayCommand]
    private void MoveDocumentToGroup2(string? filePath) => MoveDocumentToGroup(filePath, 2);

    [RelayCommand]
    private void MoveDocumentToGroup3(string? filePath) => MoveDocumentToGroup(filePath, 3);

    [RelayCommand(CanExecute = nameof(CanReopenClosedDocument))]
    private void ReopenClosedDocument() => ReopenLastClosedDocument();

    private void MoveDocumentToGroup(string? filePath, int groupIndex)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, groupIndex);
    }

    private void NotifyReopenClosedCanExecuteChanged() =>
        ReopenClosedDocumentCommand.NotifyCanExecuteChanged();
}

using System.Linq;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Relay: вкладки документов и группы.</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void ActivateDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            Documents.OpenOrActivateDocument(filePath);
        else
            Documents.ActivateDocumentInternal(doc);
    }

    [RelayCommand]
    private void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        Documents.CloseDocument(filePath);
    }

    [RelayCommand]
    private void TogglePinDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private void MoveDocumentToGroup1(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 1);
    }

    [RelayCommand]
    private void MoveDocumentToGroup2(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 2);
    }

    [RelayCommand]
    private void MoveDocumentToGroup3(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = Documents.OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            Documents.MoveDocumentToGroupInternal(doc, 3);
    }

    [RelayCommand(CanExecute = nameof(CanReopenClosedDocument))]
    private void ReopenClosedDocument() => Documents.ReopenLastClosedDocument();

    private bool CanReopenClosedDocument() => Documents.CanReopenClosedDocument();
}

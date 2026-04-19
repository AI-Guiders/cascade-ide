using System.IO;
using System.Text;

namespace CascadeIDE.ViewModels;

/// <summary>Документы / dock.</summary>
public partial class MainWindowViewModel
{
    partial void OnEditorTextChanged(string value)
    {
        Documents.ApplyEditorTextFromHost(value);
        OnPropertyChanged(nameof(EditorTextGroup2));
        OnPropertyChanged(nameof(EditorTextGroup3));
    }

    partial void OnCurrentFilePathChanged(string? value)
    {
        UpdateSemanticMapCaretOffset(null);
        RefreshComplexityBadgeFromCurrentFile();
        RefreshEditorHudBannerFromDiagnostics();
        ScheduleWorkspaceNavigationMapRefresh();
    }

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
}

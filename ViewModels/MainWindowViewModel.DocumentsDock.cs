using System.Text;
using CascadeIDE.Features.UiChrome;

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
        RefreshLocBadgeFromCurrentFile();
        RefreshEditorHudBannerFromDiagnostics();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    /// <summary>LOC для task cockpit: непустые строки текущего файла на диске (как <c>get_code_metrics</c> <c>loc</c>), при смене документа.</summary>
    private void RefreshLocBadgeFromCurrentFile()
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentFilePath) || !File.Exists(CurrentFilePath))
            {
                LocBadge = 0;
                LocTierLabel = "";
                return;
            }

            var text = File.ReadAllText(CurrentFilePath, Encoding.UTF8);
            var n = SourceLineMetrics.CountNonEmptyLines(text);
            LocBadge = n;
            LocTierLabel = n > 0 ? LocLimitsRuntime.TierFor(n).ToString() : "";
        }
        catch
        {
            LocBadge = 0;
            LocTierLabel = "";
        }
    }
}

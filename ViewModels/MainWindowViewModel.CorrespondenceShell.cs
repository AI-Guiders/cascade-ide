using CascadeIDE.Models;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>Страница Correspondence (CRS) в оболочке Mfd (ADR 0156).</summary>
public partial class MainWindowViewModel
{
    [RelayCommand]
    private void CloseCorrespondencePage()
    {
        if (CurrentMfdShellPage != MfdShellPage.Correspondence)
            return;

        foreach (var p in MfdShellPageOrder)
        {
            if (p == MfdShellPage.Correspondence)
                continue;
            if (IsMfdShellPageAllowed(p))
            {
                CurrentMfdShellPage = p;
                return;
            }
        }

        CurrentMfdShellPage = MfdShellPage.Chat;
    }
}

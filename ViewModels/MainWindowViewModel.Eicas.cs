using System.Collections.ObjectModel;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>Канал EICAS / CAS — отдельно от полосы телеметрии контура работы (ADR 0021, вариант A).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные оповещения для <see cref="Views.EicasAlertsBarView"/>; источник — <see cref="EicasCompositor"/>.</summary>
    public ObservableCollection<EicasMessage> EicasMessages { get; } = new();

    private void RebuildEicas()
    {
        EicasCompositor.Rebuild(EicasMessages, _eicasFeed.GetMessages());
        OnPropertyChanged(nameof(ShowEicasAlertsBar));
        OnPropertyChanged(nameof(ShowWorkspaceChromeBand));
        OnPropertyChanged(nameof(ShowWorkspaceBottomChrome));
    }
}

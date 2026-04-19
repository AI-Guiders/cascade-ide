using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Composition.Eicas;

namespace CascadeIDE.ViewModels;

/// <summary>Канал EICAS / CAS — отдельно от полосы телеметрии контура работы (ADR 0021, вариант A).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные оповещения для <see cref="Views.EicasAlertsBarView"/>; источник — <see cref="EicasMessageSorter"/>.</summary>
    public ObservableCollection<EicasMessage> EicasMessages { get; } = new();

    private void RebuildEicas()
    {
        EicasMessageSorter.Rebuild(EicasMessages, _eicasFeed.GetMessages());
        OnPropertyChanged(nameof(ShowEicasAlertsBar));
        OnPropertyChanged(nameof(ShowWorkspaceChromeBand));
        OnPropertyChanged(nameof(ShowWorkspaceBottomChrome));
    }
}

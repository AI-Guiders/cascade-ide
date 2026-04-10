using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>
/// Горизонтальная полоса оповещений канала EICAS/CAS (W/C/A), v1 над полосой Workspace Health. Семантика канала — ADR 0021 §5.
/// </summary>
public partial class EicasAlertsBarView : UserControl
{
    public EicasAlertsBarView()
    {
        InitializeComponent();
    }
}

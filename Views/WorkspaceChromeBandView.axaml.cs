using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>
/// Полоса хрома над нижним доком: оповещения EICAS (если включены) и полоса Workspace Health (<see cref="WorkspaceHealthStripView"/>).
/// </summary>
public partial class WorkspaceChromeBandView : UserControl
{
    public WorkspaceChromeBandView() => InitializeComponent();
}

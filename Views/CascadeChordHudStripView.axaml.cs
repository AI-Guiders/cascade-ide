using Avalonia.Controls;
using Avalonia.Interactivity;
using CascadeIDE.Models.Shell;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

public partial class CascadeChordHudStripView : UserControl
{
    public CascadeChordHudStripView() => InitializeComponent();

    private void OnChordPopupClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.NotifyCascadeChordDropdownDismissed();
    }

    private void OnChordSuggestionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || sender is not ListBox lb || DataContext is not MainWindowViewModel vm)
            return;
        if (e.AddedItems[0] is CascadeChordOverlaySuggestion item)
        {
            vm.PickCascadeChordSuggestion(item);
            lb.SelectedItem = null;
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;

namespace CascadeIDE.Views;

public partial class InstallModelDialog : Window
{
    public InstallModelDialog()
    {
        InitializeComponent();
    }

    private void OnRecommendedItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Button { DataContext: Models.RecommendedModel model }
            && DataContext is ViewModels.InstallModelDialogViewModel vm)
            vm.SelectRecommendedCommand.Execute(model);
    }
}

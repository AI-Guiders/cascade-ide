using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CascadeIDE.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        OpenSolutionButton.Click += OnOpenSolutionClick;
    }

    private async void OnOpenSolutionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть решение",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Решение") { Patterns = ["*.slnx", "*.sln"] }
            ]
        });
        if (files.Count > 0 && DataContext is ViewModels.MainWindowViewModel vm)
        {
            var path = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
            vm.LoadSolution(path);
        }
    }
}
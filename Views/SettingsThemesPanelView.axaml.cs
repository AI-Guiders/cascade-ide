using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.Themes, "Тема IDE", "Оформление", order: 0)]
public partial class SettingsThemesPanelView : UserControl
{
    public SettingsThemesPanelView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        ThemeFilesList.SelectionChanged += OnThemeFileSelectionChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ThemeFilesList.ItemTemplate = new FuncDataTemplate<SettingsThemeCatalog.ThemeFileEntry>(
            (entry, _) => new TextBlock { Text = entry?.DisplayName ?? "", Padding = new Avalonia.Thickness(4, 2) });
        ThemeFilesList.ItemsSource = SettingsThemeCatalog.DiscoverBundled();
    }

    private async void OnThemeFileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeFilesList.SelectedItem is not SettingsThemeCatalog.ThemeFileEntry entry)
            return;
        if (DataContext is not MainWindowViewModel vm)
            return;

        await UiThemeApply.ApplyOnUiThreadAsync(UiThemeApply.GetThemeJsonFromFile(entry.FullPath));
    }
}

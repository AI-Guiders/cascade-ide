using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private async Task<string?> ShowOpenThemeFileDialogAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть файл темы",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        if (files.Count == 0)
            return null;
        return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
    }

    private async Task ShowOpenSolutionDialogAsync()
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

    private Task<string> ShowConfirmationDialogAsync(string message, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration ctr = default;
        if (cancellationToken.CanBeCanceled)
        {
            ctr = cancellationToken.Register(() => tcs.TrySetResult(Services.ConfirmationResponses.Cancel));
        }

        Dispatcher.UIThread.Post(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetResult(Services.ConfirmationResponses.Cancel);
                return;
            }

            try
            {
                var result = await ShowConfirmationDialogOnUiThreadAsync(message).ConfigureAwait(true);
                tcs.TrySetResult(result);
            }
            catch
            {
                tcs.TrySetResult(Services.ConfirmationResponses.Cancel);
            }
            finally
            {
                ctr.Dispose();
            }
        });

        return tcs.Task;
    }

    private async Task<string> ShowConfirmationDialogOnUiThreadAsync(string message)
    {
        var dialog = new Window
        {
            Title = "Подтверждение",
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var text = string.IsNullOrWhiteSpace(message) ? "Подтвердить действие?" : message;
        var result = Services.ConfirmationResponses.Cancel;

        var okButton = new Button { Content = "OK", MinWidth = 90 };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 90 };

        okButton.Click += (_, _) =>
        {
            result = Services.ConfirmationResponses.Ok;
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            result = Services.ConfirmationResponses.Cancel;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { okButton, cancelButton }
                }
            }
        };

        await dialog.ShowDialog(this);

        return result;
    }

    private async void ShowAbout()
    {
        var w = new Window { Title = "О программе", Width = 400, Height = 180 };
        var okCommand = new RelayCommand(() => w.Close());
        w.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "CascadeIDE", FontWeight = FontWeight.SemiBold, FontSize = 16 },
                new TextBlock
                {
                    Text = "Тонкий клиент для управления агентом (MCP). Файл, вид, терминал, обозреватель решения.",
                    TextWrapping = TextWrapping.Wrap
                },
                new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, Command = okCommand }
            }
        };
        await w.ShowDialog(this);
    }

    private void ShowSettingsWindow()
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var w = new SettingsWindow { DataContext = vm };
        w.Show(this);
    }

    private async Task ShowInstallModelDialogAsync(ViewModels.MainWindowViewModel mainVm)
    {
        mainVm.SelectedOllamaModel = mainVm.LastSelectedRealModel ?? mainVm.OllamaModels.FirstOrDefault();
        var dialog = new InstallModelDialog();
        dialog.DataContext = new ViewModels.InstallModelDialogViewModel(
            new Services.OllamaService(),
            () => dialog.Close());
        dialog.Closed += (_, _) => _ = mainVm.RefreshOllamaAsync();
        await dialog.ShowDialog(this);
    }
}

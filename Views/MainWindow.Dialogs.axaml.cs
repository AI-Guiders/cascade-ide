using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CascadeIDE.Services;
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

    private async Task<string?> ShowPickDebugTargetAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Отладка: выбери .dll или .exe (цель не подставляется автоматически)",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Сборка .NET") { Patterns = ["*.dll", "*.exe"] }
            ]
        };
        if (DataContext is ViewModels.MainWindowViewModel vm && !string.IsNullOrEmpty(vm.Workspace.SolutionPath))
        {
            var defaultDll = BreakpointsFileService.GetDefaultDebugTargetPath(vm.Workspace.SolutionPath);
            var binDir = Path.GetDirectoryName(defaultDll);
            if (!string.IsNullOrEmpty(binDir) && Directory.Exists(binDir))
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(binDir).ConfigureAwait(true);
                if (folder != null)
                    options.SuggestedStartLocation = folder;
            }
        }

        var files = await StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
            return null;
        return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
    }

    private Task<int?> ShowAttachProcessIdAsync() => AttachProcessPidDialog.ShowAsync(this);

    private async Task ShowInfoDialogAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Сообщение" : title,
            Width = 480,
            MinHeight = 140,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var ok = new Button { Content = "OK", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(message) ? "" : message,
                    TextWrapping = TextWrapping.Wrap
                },
                ok
            }
        };

        await dialog.ShowDialog(this);
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

    private async Task ShowOpenFolderDialogAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Открыть папку как workspace",
            AllowMultiple = false
        };
        if (DataContext is ViewModels.MainWindowViewModel vmStart && !string.IsNullOrEmpty(vmStart.Workspace.SolutionPath))
        {
            var start = DialogStartDirectoryFromWorkspacePath(vmStart.Workspace.SolutionPath);
            if (!string.IsNullOrEmpty(start))
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(start).ConfigureAwait(true);
                if (folder is not null)
                    options.SuggestedStartLocation = folder;
            }
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0 || DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var path = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
        if (string.IsNullOrEmpty(path))
            return;
        vm.LoadSolution(path);
    }

    /// <summary>Каталог для старта диалога: рядом с .sln или внутри открытой папки.</summary>
    private static string? DialogStartDirectoryFromWorkspacePath(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return null;
        try
        {
            var p = Path.GetFullPath(workspacePath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p);
            if (Directory.Exists(p))
                return p;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private async Task ShowOpenFileDialogAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Открыть файл",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Все файлы") { Patterns = ["*"] },
                new FilePickerFileType("Текст и код")
                {
                    Patterns =
                    [
                        "*.cs", "*.xaml", "*.axaml", "*.json", "*.toml", "*.md", "*.xml", "*.txt",
                        "*.csproj", "*.sln", "*.slnx", "*.css", "*.js", "*.ts"
                    ]
                }
            ]
        };
        if (DataContext is ViewModels.MainWindowViewModel vmStart && !string.IsNullOrEmpty(vmStart.Workspace.SolutionPath))
        {
            var dir = DialogStartDirectoryFromWorkspacePath(vmStart.Workspace.SolutionPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var folder = await StorageProvider.TryGetFolderFromPathAsync(dir).ConfigureAwait(true);
                if (folder != null)
                    options.SuggestedStartLocation = folder;
            }
        }

        var files = await StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0 || DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var path = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        var normalized = Path.GetFullPath(path);
        vm.Documents.OpenOrActivateDocument(normalized);
    }

    private async Task<string?> ShowSaveExpandedMarkdownDialogAsync(string? currentMarkdownPath)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Export expanded Markdown",
            DefaultExtension = "md",
            FileTypeChoices =
            [
                new FilePickerFileType("Markdown") { Patterns = ["*.md"] },
                new FilePickerFileType("Все файлы") { Patterns = ["*"] }
            ]
        };

        if (!string.IsNullOrWhiteSpace(currentMarkdownPath))
        {
            try
            {
                var full = Path.GetFullPath(currentMarkdownPath);
                var dir = Path.GetDirectoryName(full);
                var file = Path.GetFileNameWithoutExtension(full);
                options.SuggestedFileName = string.IsNullOrWhiteSpace(file) ? "export.expanded.md" : $"{file}.expanded.md";
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    var folder = await StorageProvider.TryGetFolderFromPathAsync(dir).ConfigureAwait(true);
                    if (folder is not null)
                        options.SuggestedStartLocation = folder;
                }
            }
            catch
            {
                // ignore invalid path
            }
        }

        var fileOut = await StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(true);
        if (fileOut is null)
            return null;
        return fileOut.TryGetLocalPath() ?? fileOut.Path.LocalPath;
    }

    private Task<string> ShowConfirmationDialogAsync(string message, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration ctr = default;
        if (cancellationToken.CanBeCanceled)
        {
            ctr = cancellationToken.Register(() => tcs.TrySetResult(Services.ConfirmationResponses.Cancel));
        }

        UiScheduler.Default.Post(async () =>
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

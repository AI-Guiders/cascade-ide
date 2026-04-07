using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.Services;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private static void LogHighlight(string message)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var logDir = Path.Combine(baseDir, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "editor-highlight-log.txt");
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] {message}{Environment.NewLine}";
            lock (HighlightLogLock)
            {
                File.AppendAllText(logPath, line);
            }
        }
        catch
        {
            // Never crash UI because of debug logging.
        }
    }

    /// <summary>Подсветить эффективный контрол (по имени или под курсором). Оверлей скрывается через 3 с.</summary>
    private string ShowHighlightForControl(string? name)
    {
        Control? control;
        if (!string.IsNullOrWhiteSpace(name))
        {
            control = UiControlAppearance.FindControlByNameAcrossAllWindows(this, name.Trim());
            if (control is null)
                return $"Control not found: {name}.";
        }
        else
        {
            control = UiAgentHighlight.FindControlUnderCursorAnyWindow(this);
            if (control is null)
                return "No control under cursor. Specify name from ide_get_ui_layout.";
        }

        return UiAgentHighlight.ShowForControl(control);
    }

    private async void OnDocumentTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ViewModels.OpenDocumentViewModel doc)
            return;
        if (!e.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
            return;

        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(DataFormat.Text, doc.FilePath));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void OnGroupTabsDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.Text) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnGroup1TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 1);
    private void OnGroup2TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 2);
    private void OnGroup3TabsDrop(object? sender, DragEventArgs e) => MoveDroppedDocumentToGroup(e, 3);

    private void MoveDroppedDocumentToGroup(DragEventArgs e, int group)
    {
        if (DataContext is not ViewModels.MainWindowViewModel vm)
            return;
        var path = e.DataTransfer.TryGetText();
        if (string.IsNullOrWhiteSpace(path))
            return;

        switch (group)
        {
            case 2:
                if (vm.MoveDocumentToGroup2Command.CanExecute(path))
                    vm.MoveDocumentToGroup2Command.Execute(path);
                break;
            case 3:
                if (vm.MoveDocumentToGroup3Command.CanExecute(path))
                    vm.MoveDocumentToGroup3Command.Execute(path);
                break;
            default:
                if (vm.MoveDocumentToGroup1Command.CanExecute(path))
                    vm.MoveDocumentToGroup1Command.Execute(path);
                break;
        }
    }
}

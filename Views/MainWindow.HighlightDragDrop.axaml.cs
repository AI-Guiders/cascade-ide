using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
            control = Services.UiControlAppearance.FindControlByName(this, name.Trim());
            if (control is null)
                return $"Control not found: {name}.";
        }
        else
        {
            var over = (this as IInputRoot)?.PointerOverElement;
            control = over as Control ?? FindAncestorControl(over as Visual);
            if (control is null)
                return "No control under cursor. Specify name from ide_get_ui_layout.";
        }

        var root = this as Visual;
        if (root is null)
            return "No visual root.";
        var topLeft = control.TranslatePoint(new Point(0, 0), root);
        if (topLeft is not { } pt)
            return "Could not get control position.";
        var w = control.Bounds.Width;
        var h = control.Bounds.Height;

        var overlay = this.FindControl<Border>("AgentHighlightOverlay");
        if (overlay is null)
            return "Highlight overlay not found.";
        Canvas.SetLeft(overlay, pt.X);
        Canvas.SetTop(overlay, pt.Y);
        overlay.Width = w;
        overlay.Height = h;
        overlay.IsVisible = true;

        _highlightHideTimer?.Dispose();
        _highlightHideTimer = DispatcherTimer.RunOnce(() =>
        {
            overlay.IsVisible = false;
            _highlightHideTimer = null;
        }, TimeSpan.FromSeconds(3));

        return "OK";
    }

    private static Control? FindAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
            if (v is Control c)
                return c;
        return null;
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

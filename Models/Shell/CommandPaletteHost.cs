namespace CascadeIDE.Models.Shell;

/// <summary>Где показывать палитру команд при нескольких TopLevel (ADR 0017).</summary>
public enum CommandPaletteHost
{
    MainWindow,
    PfdHost,
    MfdHost,
    /// <summary>Окно сплита P+M — тот же <see cref="ViewModels.MainWindowViewModel"/>, отдельный TopLevel.</summary>
    PmSplitHost,
}

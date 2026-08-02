#nullable enable

using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>Forward peeled UserControl fields so existing MainWindow surfaces keep short names.</summary>
public partial class MainWindow
{
    Border? PaletteOverlay => Overlays?.PaletteOverlay;
    TextBox? PaletteQuery => Overlays?.PaletteQuery;
    ListBox? PaletteList => Overlays?.PaletteList;
    Border? ChordOverlay => Overlays?.ChordOverlay;
    TextBox? ChordQuery => Overlays?.ChordQuery;
    ListBox? ChordList => Overlays?.ChordList;

    Border? MfdTerminalHost => MfdHosts?.MfdTerminalHost;
    TextBlock? TerminalShellLabel => MfdHosts?.TerminalShellLabel;
    TextBox? TerminalInput => MfdHosts?.TerminalInput;
    TextBox? TerminalOutput => MfdHosts?.TerminalOutput;

    Border? MfdBuildHost => MfdHosts?.MfdBuildHost;
    TextBlock? BuildStatusLabel => MfdHosts?.BuildStatusLabel;
    TextBox? BuildOutput => MfdHosts?.BuildOutput;

    Border? MfdTestsHost => MfdHosts?.MfdTestsHost;
    TextBlock? TestsStatusLabel => MfdHosts?.TestsStatusLabel;
    TextBox? TestsOutput => MfdHosts?.TestsOutput;

    Border? MfdGitHost => MfdHosts?.MfdGitHost;
    TextBlock? GitStatusLabel => MfdHosts?.GitStatusLabel;
    TextBox? GitOutput => MfdHosts?.GitOutput;
}

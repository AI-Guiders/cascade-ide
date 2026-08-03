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
    EasyWindowsTerminalControl.EasyTerminalControl? TerminalVt => MfdHosts?.TerminalVt;

    Border? MfdBuildHost => MfdHosts?.MfdBuildHost;
    TextBlock? BuildStatusLabel => MfdHosts?.BuildStatusLabel;
    TextBox? BuildOutput => MfdHosts?.BuildOutput;

    Border? MfdTestsHost => MfdHosts?.MfdTestsHost;
    TextBlock? TestsStatusLabel => MfdHosts?.TestsStatusLabel;
    TextBox? TestsOutput => MfdHosts?.TestsOutput;

    Border? MfdGitHost => MfdHosts?.MfdGitHost;
    TextBlock? GitStatusLabel => MfdHosts?.GitStatusLabel;
    ListBox? GitList => MfdHosts?.GitList;
    TextBox? GitOutput => MfdHosts?.GitOutput;

    Border? MfdProblemsHost => MfdHosts?.MfdProblemsHost;
    TextBlock? ProblemsStatusLabel => MfdHosts?.ProblemsStatusLabel;
    ListBox? ProblemsList => MfdHosts?.ProblemsList;

    Border? MfdRelatedFilesHost => MfdHosts?.MfdRelatedFilesHost;
    TextBlock? RelatedStatusLabel => MfdHosts?.RelatedStatusLabel;
    ListBox? RelatedList => MfdHosts?.RelatedList;

    Border? MfdSemanticMapHost => MfdHosts?.MfdSemanticMapHost;
    TextBlock? SemanticStatusLabel => MfdHosts?.SemanticStatusLabel;
    ListBox? SemanticList => MfdHosts?.SemanticList;
    GlassSemanticMapSkia? SemanticSkia => MfdHosts?.SemanticSkia;

    Border? MfdCorrespondenceHost => MfdHosts?.MfdCorrespondenceHost;
    TextBlock? CorrespondenceStatusLabel => MfdHosts?.CorrespondenceStatusLabel;
    ListBox? CorrespondenceReverseList => MfdHosts?.CorrespondenceReverseList;
    ListBox? CorrespondenceForwardList => MfdHosts?.CorrespondenceForwardList;

    Border? MfdMarkdownHost => MfdHosts?.MfdMarkdownHost;
    TextBlock? MarkdownStatusLabel => MfdHosts?.MarkdownStatusLabel;
    TextBox? MarkdownOutput => MfdHosts?.MarkdownOutput;

    Border? MfdDebugStackHost => MfdHosts?.MfdDebugStackHost;
    TextBlock? DebugStatusLabel => MfdHosts?.DebugStatusLabel;
    ListBox? DebugStackList => MfdHosts?.DebugStackList;
    ListBox? DebugLocalsList => MfdHosts?.DebugLocalsList;

    Border? MfdWebAiHost => MfdHosts?.MfdWebAiHost;
    TextBox? WebAiUrl => MfdHosts?.WebAiUrl;
    TextBlock? WebAiStatusLabel => MfdHosts?.WebAiStatusLabel;
    Microsoft.Web.WebView2.Wpf.WebView2? WebAiView => MfdHosts?.WebAiView;
}

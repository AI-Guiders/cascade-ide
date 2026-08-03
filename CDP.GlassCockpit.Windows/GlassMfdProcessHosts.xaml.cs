#nullable enable

using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

public partial class GlassMfdProcessHosts : UserControl
{
    public GlassMfdProcessHosts()
    {
        InitializeComponent();
    }

    public void Wire(MainWindow host)
    {
        TerminalRestartBtn.Click += host.TerminalRestart_OnClick;
        BuildRunBtn.Click += host.BuildRun_OnClick;
        BuildCancelBtn.Click += host.BuildCancel_OnClick;
        BuildClearBtn.Click += host.BuildClear_OnClick;
        TestsRunBtn.Click += host.TestsRun_OnClick;
        TestsCancelBtn.Click += host.TestsCancel_OnClick;
        TestsClearBtn.Click += host.TestsClear_OnClick;
        GitStatusBtn.Click += host.GitStatus_OnClick;
        GitCancelBtn.Click += host.GitCancel_OnClick;
        GitClearBtn.Click += host.GitClear_OnClick;
        GitList.SelectionChanged += host.GitList_OnSelectionChanged;
        ProblemsRefreshBtn.Click += host.ProblemsRefresh_OnClick;
        ProblemsClearBtn.Click += host.ProblemsClear_OnClick;
        ProblemsList.MouseDoubleClick += host.ProblemsList_OnMouseDoubleClick;
        RelatedRefreshBtn.Click += host.RelatedRefresh_OnClick;
        RelatedList.MouseDoubleClick += host.RelatedList_OnMouseDoubleClick;
        SemanticRefreshBtn.Click += host.SemanticRefresh_OnClick;
        SemanticList.MouseDoubleClick += host.SemanticList_OnMouseDoubleClick;
        CorrespondenceRefreshBtn.Click += host.CorrespondenceRefresh_OnClick;
        CorrespondenceReverseList.MouseDoubleClick += host.CorrespondenceReverse_OnMouseDoubleClick;
        CorrespondenceForwardList.MouseDoubleClick += host.CorrespondenceForward_OnMouseDoubleClick;
        MarkdownRefreshBtn.Click += host.MarkdownRefresh_OnClick;
        DebugRefreshBtn.Click += host.DebugRefresh_OnClick;
        DebugStackList.MouseDoubleClick += host.DebugStack_OnMouseDoubleClick;
        WebAiGoBtn.Click += host.WebAiGo_OnClick;
        WebAiUrl.KeyDown += host.WebAiUrl_OnKeyDown;
    }
}

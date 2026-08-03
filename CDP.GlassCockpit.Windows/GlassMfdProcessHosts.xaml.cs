#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        TerminalInput.KeyDown += host.TerminalInput_OnKeyDown;
        BuildRunBtn.Click += host.BuildRun_OnClick;
        BuildCancelBtn.Click += host.BuildCancel_OnClick;
        BuildClearBtn.Click += host.BuildClear_OnClick;
        TestsRunBtn.Click += host.TestsRun_OnClick;
        TestsCancelBtn.Click += host.TestsCancel_OnClick;
        TestsClearBtn.Click += host.TestsClear_OnClick;
        GitStatusBtn.Click += host.GitStatus_OnClick;
        GitCancelBtn.Click += host.GitCancel_OnClick;
        GitClearBtn.Click += host.GitClear_OnClick;
        ProblemsRefreshBtn.Click += host.ProblemsRefresh_OnClick;
        ProblemsClearBtn.Click += host.ProblemsClear_OnClick;
        ProblemsList.MouseDoubleClick += host.ProblemsList_OnMouseDoubleClick;
    }
}

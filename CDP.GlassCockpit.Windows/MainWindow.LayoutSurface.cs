#nullable enable

using System.Windows;

namespace CDP.GlassCockpit.Windows;

/// <summary>Session layout, host windows sync, Forward primary_work_surface (ADR 0120).</summary>
public partial class MainWindow
{
    readonly GlassHostWindows _hosts;
    bool _hostsReady;

    void ApplyLayoutFromSession()
    {
        WpfMainGridColumns.Apply(MainGrid, _session.Layout.ColumnDefinitions);
        TopologyBadge.Text = _session.Layout.Topology;
        ChromeHintChip.Text = "CFG";
        ChromeHintChip.Tip =
            $"settings.toml · {_session.Settings.Workspace.PrimaryWorkSurface} · tier={_session.Settings.Display.Presentation.Tier}" +
            (_session.Layout.ParseOk ? "" : $" · parse fail: {_session.Layout.ParseError}");
        SyncHostWindows();
    }

    void SyncHostWindows()
    {
        if (!_hostsReady)
            return;
        _hosts.Sync(_session.Layout.Flags);
    }

    void ApplyPrimaryWorkSurface()
    {
        if (_session.IsIntercomForward)
        {
            // ADR 0120: Intercom owns Forward; docs open on M · Editor.
            ForwardTitle.Text = "F · Intercom";
            ForwardEditorRow.Height = new GridLength(0);
            ForwardSplitRow.Height = new GridLength(0);
            ForwardIntercomRow.Height = new GridLength(1, GridUnitType.Star);
            ForwardEditorHost.Visibility = Visibility.Collapsed;
            ForwardSplit.Visibility = Visibility.Collapsed;
            IntercomSurface.Visibility = Visibility.Visible;
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor");
        }
        else
        {
            ForwardTitle.Text = "F · Editor";
            ForwardEditorRow.Height = new GridLength(1, GridUnitType.Star);
            ForwardSplitRow.Height = new GridLength(0);
            ForwardIntercomRow.Height = new GridLength(0);
            ForwardEditorHost.Visibility = Visibility.Visible;
            ForwardSplit.Visibility = Visibility.Collapsed;
            IntercomSurface.Visibility = Visibility.Collapsed;
            MountEditor(ForwardEditorHost);
            RefreshMfdEditorVisibility();
        }
    }
}

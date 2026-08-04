#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap — Skia graph + arch board instrument (ADR 0196).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassSemanticMapGraph.Node> _semanticItems = new();
    readonly ObservableCollection<GlassArchBoardGlance.RoleLine> _archRoles = new();
    GlassSemanticMapGraph.Graph _semanticGraph = new(null, [], []);
    bool _semanticSkiaWired;

    void RefreshMfdSemanticVisibility()
    {
        if (MfdSemanticMapHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "SemanticMap", StringComparison.OrdinalIgnoreCase);
        MfdSemanticMapHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        EnsureSemanticSkiaWired();

        if (show)
        {
            if (SemanticList is not null && !ReferenceEquals(SemanticList.ItemsSource, _semanticItems))
                SemanticList.ItemsSource = _semanticItems;
            if (SemanticArchRoleList is not null && !ReferenceEquals(SemanticArchRoleList.ItemsSource, _archRoles))
                SemanticArchRoleList.ItemsSource = _archRoles;
            if (_semanticItems.Count == 0 || (SemanticArchCardsPanel?.Items.Count ?? 0) == 0)
                RefreshSemanticItems();
            else
                PushSemanticGraph();
        }
    }

    bool IsSemanticHostActive()
    {
        if (MfdSemanticMapHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "SemanticMap", StringComparison.OrdinalIgnoreCase)
               && MfdSemanticMapHost.Visibility == Visibility.Visible;
    }

    internal void SemanticRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshSemanticItems();

    internal void SemanticList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SemanticList?.SelectedItem is not GlassSemanticMapGraph.Node item)
            return;
        OpenSemanticNode(item.FilePath);
    }

    internal void SemanticArchRole_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SemanticArchRoleList?.SelectedItem is not GlassArchBoardGlance.RoleLine role)
            return;
        StatusText.Text = $"glass · arch · {role.Role} · {role.Status} · {role.Id}";
    }

    void EnsureSemanticSkiaWired()
    {
        if (_semanticSkiaWired || SemanticSkia is null)
            return;
        SemanticSkia.NodeActivated += OpenSemanticNode;
        _semanticSkiaWired = true;
    }

    void OpenSemanticNode(string path)
    {
        OpenCodeFile(path);
        StatusText.Text = $"glass · semantic · {Path.GetFileName(path)}";
    }

    void RefreshSemanticItems()
    {
        _semanticGraph = GlassSemanticMapGraph.Collect(_session.WorkspaceRoot, _editorPath, maxNodes: 96);
        _semanticItems.Clear();
        foreach (var n in _semanticGraph.Nodes)
            _semanticItems.Add(n);
        PushSemanticGraph();

        var arch = GlassArchBoardGlance.TryProbe(_session.WorkspaceRoot);
        if (SemanticArchCardsPanel is not null)
        {
            SemanticArchCardsPanel.Items.Clear();
            if (arch is { } snap)
            {
                foreach (var chip in GlassArchBoardGlance.BuildInstrument(snap))
                    SemanticArchCardsPanel.Items.Add(CreateDeckCard(chip));
            }
        }

        _archRoles.Clear();
        if (arch is { } a)
        {
            foreach (var r in a.Roles.Take(24))
                _archRoles.Add(r);
        }

        if (SemanticStatusLabel is not null)
        {
            var hops = _semanticGraph.Nodes.Count > 0
                ? $"h{_semanticGraph.Nodes.Max(n => n.Hop)}"
                : "h0";
            var archPart = arch?.StatusLine ?? "arch · no board";
            SemanticStatusLabel.Text =
                $"semantic · skia {_semanticGraph.Nodes.Count} · {hops} · {_semanticGraph.Edges.Count}e · {archPart}";
        }
    }

    void PushSemanticGraph()
    {
        EnsureSemanticSkiaWired();
        SemanticSkia?.SetGraph(_semanticGraph);
    }
}

#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD HybridIndex — instrument cards (HCI/DOCS/FRESH) + scope Skia map (Shared-SSOT).
/// </summary>
public partial class MainWindow
{
    readonly ObservableCollection<string> _hybridScopeLines = new();
    GlassSemanticMapGraph.Graph _hybridGraph = new(null, [], []);
    bool _hybridSkiaWired;

    void RefreshMfdHybridIndexVisibility()
    {
        if (MfdHybridIndexHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase);
        MfdHybridIndexHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        EnsureHybridSkiaWired();

        if (show && HybridScopeList is not null && !ReferenceEquals(HybridScopeList.ItemsSource, _hybridScopeLines))
            HybridScopeList.ItemsSource = _hybridScopeLines;

        if (show && (HybridIndexCardsPanel?.Items.Count ?? 0) == 0)
            RefreshHybridIndexBody();
        else if (show)
            PushHybridGraph();
    }

    bool IsHybridIndexHostActive()
    {
        if (MfdHybridIndexHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase)
               && MfdHybridIndexHost.Visibility == Visibility.Visible;
    }

    internal void HybridIndexRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshHybridIndexBody();

    void EnsureHybridSkiaWired()
    {
        if (_hybridSkiaWired || HybridSkia is null)
            return;
        HybridSkia.NodeActivated += OpenHybridScopeNode;
        _hybridSkiaWired = true;
    }

    void OpenHybridScopeNode(string path)
    {
        if (Directory.Exists(path))
        {
            StatusText.Text = $"glass · hci · scope · {Path.GetFileName(path)}";
            return;
        }

        OpenCodeFile(path);
        StatusText.Text = $"glass · hci · {Path.GetFileName(path)}";
    }

    void RefreshHybridIndexBody()
    {
        var live = GlassHybridIndexGlance.TryProbeLive(_session.WorkspaceRoot);
        if (HybridIndexCardsPanel is not null)
        {
            HybridIndexCardsPanel.Items.Clear();
            if (live is { } status)
            {
                foreach (var chip in GlassHybridIndexGlance.BuildInstrument(status))
                    HybridIndexCardsPanel.Items.Add(CreateDeckCard(chip));
            }
        }

        var ready = live is { DatabaseExists: true, DocumentCount: > 0 }
                    && string.IsNullOrWhiteSpace(live.Value.LastReindexError);
        _hybridGraph = GlassHybridIndexGlance.BuildScopeMap(_session.WorkspaceRoot, ready);
        PushHybridGraph();

        _hybridScopeLines.Clear();
        if (live is { } s)
        {
            _hybridScopeLines.Add($"docs · {s.DocumentCount}{(s.DocumentCountMayBeStale ? " · stale" : "")}");
            _hybridScopeLines.Add($"state · {s.ReindexState ?? "—"}");
            if (!string.IsNullOrWhiteSpace(s.WorkspaceRoot))
                _hybridScopeLines.Add($"ws · {Path.GetFileName(s.WorkspaceRoot.TrimEnd('\\', '/'))}");
            if (!string.IsNullOrWhiteSpace(s.DatabasePath))
                _hybridScopeLines.Add($"db · {Path.GetFileName(s.DatabasePath)}");
            if (!string.IsNullOrWhiteSpace(s.LastReindexError))
                _hybridScopeLines.Add($"err · {s.LastReindexError}");
            foreach (var n in _hybridGraph.Nodes.Where(n => n.Hop == 1).Take(16))
                _hybridScopeLines.Add($"· {Path.GetFileName(n.FilePath)}");
        }
        else
        {
            _hybridScopeLines.Add("hci · workspace root unavailable");
        }

        if (HybridIndexStatusLabel is not null)
        {
            HybridIndexStatusLabel.Text = live is null
                ? "hci · unavailable"
                : $"hci · map · {_hybridGraph.Nodes.Count}n/{_hybridGraph.Edges.Count}e · docs {live.Value.DocumentCount}";
        }
    }

    void PushHybridGraph()
    {
        EnsureHybridSkiaWired();
        HybridSkia?.SetGraph(_hybridGraph);
    }
}

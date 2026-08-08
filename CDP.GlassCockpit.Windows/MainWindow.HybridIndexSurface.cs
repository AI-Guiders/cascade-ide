#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD HybridIndex — instrument cards + scope map + search/reindex hand (Ready-to-Interact).
/// </summary>
public partial class MainWindow
{
    readonly ObservableCollection<object> _hybridScopeLines = new();
    GlassSemanticMapGraph.Graph _hybridGraph = new(null, [], []);
    bool _hybridSkiaWired;
    bool _hybridReindexBusy;

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

    internal void HybridIndexSearch_OnClick(object sender, RoutedEventArgs e) => RunHybridIndexSearch();

    internal void HybridIndexSearchBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunHybridIndexSearch();
            e.Handled = true;
        }
    }

    internal void HybridIndexReindex_OnClick(object sender, RoutedEventArgs e) => StartHybridIndexReindex();

    internal void HybridScopeList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HybridScopeList?.SelectedItem is GlassHybridIndexStatusProbe.SearchHitRow hit
            && !string.IsNullOrWhiteSpace(hit.Path))
        {
            OpenCodeFile(hit.Path, hit.LineStart > 0 ? hit.LineStart : null);
            StatusText.Text = $"glass · hci · search · {Path.GetFileName(hit.Path)}";
        }
    }

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

    /// <summary>SoftFL: HCI status always paints short workspace leaf so fixture vs repo scope is Face-honest.</summary>
    string ShortHybridWs()
    {
        var ws = _session.WorkspaceRoot;
        if (string.IsNullOrWhiteSpace(ws))
            return "—";
        return Path.GetFileName(ws.TrimEnd('\\', '/'));
    }

    void RunHybridIndexSearch()
    {
        var q = HybridIndexSearchBox?.Text ?? "";
        // Workspace-root DB key (same as MCP without solution_path). Do not pass .sln —
        // HybridIndex Core scopes a separate DB per (workspace, solution_path).
        var result = GlassHybridIndexStatusProbe.TrySearch(_session.WorkspaceRoot, q);
        _hybridScopeLines.Clear();
        var wsTag = ShortHybridWs();

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            if (HybridScopeList is not null)
                HybridScopeList.DisplayMemberPath = string.Empty;
            _hybridScopeLines.Add($"search · {result.Error}");
            if (HybridIndexStatusLabel is not null)
                HybridIndexStatusLabel.Text = $"hci · search · {result.Error} · ws={wsTag}";
            return;
        }

        if (result.Hits.Count == 0)
        {
            if (HybridScopeList is not null)
                HybridScopeList.DisplayMemberPath = string.Empty;
            _hybridScopeLines.Add($"search · 0 hits · ws={wsTag}");
        }
        else
        {
            if (HybridScopeList is not null)
                HybridScopeList.DisplayMemberPath = nameof(GlassHybridIndexStatusProbe.SearchHitRow.Display);
            foreach (var hit in result.Hits)
                _hybridScopeLines.Add(hit);
        }

        if (HybridIndexStatusLabel is not null)
            HybridIndexStatusLabel.Text =
                $"hci · search · {result.Hits.Count} hits · {q.Trim()} · ws={wsTag}";
    }

    void StartHybridIndexReindex()
    {
        if (_hybridReindexBusy)
            return;

        var ws = _session.WorkspaceRoot;
        if (string.IsNullOrWhiteSpace(ws))
        {
            if (HybridIndexStatusLabel is not null)
                HybridIndexStatusLabel.Text = "hci · reindex · workspace unavailable";
            return;
        }

        var wsTag = ShortHybridWs();
        _hybridReindexBusy = true;
        if (HybridIndexStatusLabel is not null)
            HybridIndexStatusLabel.Text = $"hci · reindex · running… · ws={wsTag}";

        _ = Task.Run(() =>
        {
            var result = GlassHybridIndexStatusProbe.TryReindex(ws);
            Dispatcher.BeginInvoke(() =>
            {
                _hybridReindexBusy = false;
                if (HybridIndexStatusLabel is not null)
                    HybridIndexStatusLabel.Text = result.Ok
                        ? $"hci · {result.Message} · ws={wsTag}"
                        : $"hci · reindex · fail · {result.Message} · ws={wsTag}";
                StatusText.Text = result.Ok
                    ? $"glass · hci · {result.Message} · ws={wsTag}"
                    : $"glass · hci · reindex fail · ws={wsTag}";
                RefreshHybridIndexBody(forceScopeLines: true);
            });
        });
    }

    void RefreshHybridIndexBody(bool forceScopeLines = false)
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

        var keepSearchHits = !forceScopeLines
            && _hybridScopeLines.Count > 0
            && _hybridScopeLines[0] is GlassHybridIndexStatusProbe.SearchHitRow;

        if (!keepSearchHits)
        {
            _hybridScopeLines.Clear();
            if (HybridScopeList is not null)
                HybridScopeList.DisplayMemberPath = string.Empty;
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
        }

        if (HybridIndexStatusLabel is not null && !_hybridReindexBusy && !keepSearchHits)
        {
            var wsTag = ShortHybridWs();
            HybridIndexStatusLabel.Text = live is null
                ? $"hci · unavailable · ws={wsTag}"
                : $"hci · map · {_hybridGraph.Nodes.Count}n/{_hybridGraph.Edges.Count}e · docs {live.Value.DocumentCount} · ws={wsTag}";
        }
    }

    void PushHybridGraph()
    {
        EnsureHybridSkiaWired();
        HybridSkia?.SetGraph(_hybridGraph);
    }
}

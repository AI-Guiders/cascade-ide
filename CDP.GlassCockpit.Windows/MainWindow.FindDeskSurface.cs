#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>find_desk-LATEST + local /search → MFD FindDesk Face (unpin SoftFL; RelatedFiles stays refactor).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<LatchPaint.FindDeskHitView> _findDeskRows = new();
    bool _findDeskHandsWired;

    void InitFindDeskFace()
    {
        if (MfdFindDeskList is not null)
            MfdFindDeskList.ItemsSource = _findDeskRows;
        EnsureFindDeskHandsWired();
        _latches.SoftInstrumentChanged += OnSoftInstrumentForFindDesk;
        TryHydrateFindDeskFace();
    }

    void EnsureFindDeskHandsWired()
    {
        if (_findDeskHandsWired)
            return;
        if (MfdFindDeskList is not null)
        {
            MfdFindDeskList.MouseDoubleClick += (_, e) =>
            {
                FindDeskOpenSelected();
                e.Handled = true;
            };
            MfdFindDeskList.PreviewKeyDown += (_, e) =>
            {
                if (e.Key is not (Key.Enter or Key.Return))
                    return;
                FindDeskOpenSelected();
                e.Handled = true;
            };
        }

        _findDeskHandsWired = true;
    }

    void OnSoftInstrumentForFindDesk(string organId, string? _)
    {
        if (!organId.Equals("find_desk", StringComparison.OrdinalIgnoreCase))
            return;
        Dispatcher.BeginInvoke(TryHydrateFindDeskFace, DispatcherPriority.Background);
    }

    void TryHydrateFindDeskFace()
    {
        try
        {
            var path = CdpHabitatPaths.GetLatchPath("find_desk-LATEST.json");
            if (!File.Exists(path))
                return;
            ApplyFindDeskLatch(path);
        }
        catch
        {
            /* best-effort */
        }
    }

    void ApplyFindDeskLatch(string path)
    {
        var raw = File.ReadAllText(path);
        var view = LatchPaint.PaintFindDesk(raw);
        if (view is null)
            return;

        if (view.Hits.Count > 0)
        {
            _findDeskRows.Clear();
            foreach (var hit in view.Hits)
                _findDeskRows.Add(hit);
        }

        PaintFindDeskStatus(view);
        RefreshFindDeskVisibility();
    }

    void PaintFindDeskStatus(LatchPaint.FindDeskView? view = null)
    {
        var status = view?.StatusLine
                     ?? (FindDeskStatusLabel?.Text ?? $"find · {_findDeskRows.Count}");
        if (FindDeskStatusLabel is not null)
            FindDeskStatusLabel.Text = status;

        if (string.Equals(CurrentMfdPage(), "FindDesk", StringComparison.OrdinalIgnoreCase))
        {
            if (MfdBody is not null && _findDeskRows.Count == 0)
            {
                MfdBody.Text = view is { Active: true }
                    ? $"{status}\n{view.Pulse ?? ""}\n/search pattern · DoubleClick hit → open"
                    : "find · idle — /search pattern · SoftInstrument find_desk latch";
            }

            StatusText.Text = $"glass · {status} · {DateTime.Now:HH:mm:ss}";
        }
    }

    void RefreshFindDeskVisibility()
    {
        var on = string.Equals(CurrentMfdPage(), "FindDesk", StringComparison.OrdinalIgnoreCase);
        if (MfdFindDeskHost is not null)
            MfdFindDeskHost.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on && MfdBody is not null)
            MfdBody.Visibility = _findDeskRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void FindDeskOpenSelected()
    {
        if (MfdFindDeskList?.SelectedItem is not LatchPaint.FindDeskHitView hit)
            return;
        if (string.IsNullOrWhiteSpace(hit.Path) || !File.Exists(hit.Path))
        {
            StatusText.Text = $"glass · find · missing {hit.Path}";
            return;
        }

        OpenCodeFile(hit.Path, hit.LineNumber);
        StatusText.Text = $"glass · find · {hit.Display} · {DateTime.Now:HH:mm:ss}";
    }

    string RunWorkspaceSearchToFindDesk(string pattern)
    {
        var root = _session.WorkspaceRoot;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return "search · no workspace root";

        var needle = pattern.Trim().Trim('"');
        if (needle.Length == 0)
            return "usage: /search pattern";

        var hits = GlassWorkspaceTextSearch.Search(root, needle);
        _findDeskRows.Clear();
        foreach (var h in hits)
        {
            var label = $"{Path.GetFileName(h.FullPath)}:{h.LineNumber}  {h.PreviewText}".Trim();
            _findDeskRows.Add(new LatchPaint.FindDeskHitView(h.FullPath, h.LineNumber, h.PreviewText, label));
        }

        SelectMfdPage("FindDesk", sticky: true);
        var view = new LatchPaint.FindDeskView(
            Active: true,
            Pulse: $"find · /search · {needle}",
            Op: "search",
            Where: "workspace",
            Query: needle,
            HitCount: _findDeskRows.Count,
            Hits: _findDeskRows.ToList(),
            StatusLine: $"find · {needle} · {_findDeskRows.Count} · workspace");
        PaintFindDeskStatus(view);
        RefreshFindDeskVisibility();
        return view.StatusLine;
    }
}

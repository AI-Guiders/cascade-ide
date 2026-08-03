#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.Features.Cdp;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD DebugStack — live spectator + DAP command latch from debug_desk.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassDebugDeskLatchReader.StackFrame> _debugStackFrames = new();
    int _debugSelectedFrameIndex;

    void RefreshMfdDebugVisibility()
    {
        if (MfdDebugStackHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "DebugStack", StringComparison.OrdinalIgnoreCase);
        MfdDebugStackHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            RefreshDebugSpectator();
    }

    bool IsDebugHostActive()
    {
        if (MfdDebugStackHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "DebugStack", StringComparison.OrdinalIgnoreCase)
               && MfdDebugStackHost.Visibility == Visibility.Visible;
    }

    internal void DebugRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshDebugSpectator();

    internal void DebugContinue_OnClick(object sender, RoutedEventArgs e) =>
        SendDapCommand(GlassDapCommandBridge.Continue);

    internal void DebugStepInto_OnClick(object sender, RoutedEventArgs e) =>
        SendDapCommand(GlassDapCommandBridge.StepInto);

    internal void DebugStepOver_OnClick(object sender, RoutedEventArgs e) =>
        SendDapCommand(GlassDapCommandBridge.StepOver);

    internal void DebugStepOut_OnClick(object sender, RoutedEventArgs e) =>
        SendDapCommand(GlassDapCommandBridge.StepOut);

    void SendDapCommand(string command)
    {
        if (!GlassDapCommandBridge.TryPublish(command))
        {
            StatusText.Text = $"glass · debug · cmd fail · {command}";
            return;
        }

        StatusText.Text = $"glass · debug · {command} · {DateTime.Now:HH:mm:ss}";
    }

    internal void DebugStack_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DebugStackList?.SelectedItem is not GlassDebugDeskLatchReader.StackFrame frame)
            return;

        _debugSelectedFrameIndex = frame.Index;
        if (frame.Index == _lastLocalsFrameIndex)
            return;

        if (!GlassDapCommandBridge.TryPublishVariables(frame.Index))
        {
            StatusText.Text = $"glass · debug · frame {frame.Index} · locals req fail";
            return;
        }

        StatusText.Text = $"glass · debug · frame {frame.Index} · locals requested";
    }

    internal void DebugStack_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DebugStackList?.SelectedItem is not GlassDebugDeskLatchReader.StackFrame frame)
            return;
        if (frame.File is null || !File.Exists(frame.File))
            return;
        OpenCodeFile(frame.File, frame.Line);
    }

    void OnDebugDeskLatchChanged()
    {
        if (IsDebugHostActive())
            RefreshDebugSpectator();
    }

    int _lastLocalsFrameIndex = -1;

    void RefreshDebugSpectator()
    {
        if (DebugStackList is null || DebugLocalsList is null)
            return;

        if (!ReferenceEquals(DebugStackList.ItemsSource, _debugStackFrames))
            DebugStackList.ItemsSource = _debugStackFrames;

        _debugStackFrames.Clear();
        DebugLocalsList.Items.Clear();

        var path = CdpHabitatPaths.GetLatchPath("debug_desk-LATEST.json");
        var raw = CdpLatchIo.TryReadAllTextIfExists(path);
        if (raw is null)
        {
            if (DebugStatusLabel is not null)
                DebugStatusLabel.Text = "debug · live · no latch";
            _debugStackFrames.Add(new GlassDebugDeskLatchReader.StackFrame(0, "(no DAP session · live latch)", null, 0));
            return;
        }

        try
        {
            var snap = GlassDebugDeskLatchReader.Read(raw);
            foreach (var f in snap.Stack)
                _debugStackFrames.Add(f);

            _lastLocalsFrameIndex = snap.LocalsFrameIndex;
            foreach (var v in snap.Locals)
                DebugLocalsList.Items.Add($"{v.Name} = {v.Value}");

            if (_debugStackFrames.Count == 0)
            {
                if (snap.Pulse is { Length: > 0 })
                    _debugStackFrames.Add(new GlassDebugDeskLatchReader.StackFrame(0, snap.Pulse, null, 0));
                else
                    _debugStackFrames.Add(new GlassDebugDeskLatchReader.StackFrame(
                        0,
                        snap.Stopped ? "(stopped · frames pending enrich)" : "(latch idle · no frames)",
                        null,
                        0));
            }
            else if (_debugSelectedFrameIndex >= 0 && _debugSelectedFrameIndex < _debugStackFrames.Count)
                DebugStackList.SelectedIndex = _debugSelectedFrameIndex;

            if (DebugLocalsList.Items.Count == 0)
            {
                if (snap.Verdict is { Length: > 0 })
                    DebugLocalsList.Items.Add($"verdict = {snap.Verdict}");
                else if (_debugStackFrames.Count > 0 && snap.LocalsFrameIndex != _debugSelectedFrameIndex)
                    DebugLocalsList.Items.Add($"(frame {_debugSelectedFrameIndex} · locals pending)");
            }

            if (DebugStatusLabel is not null)
            {
                var mode = snap.Stack.Count > 0 ? "live" : "latch";
                var stopBit = snap.Stopped ? "stopped" : "run";
                var dapBit = snap.ActiveDap ? "dap" : "idle";
                DebugStatusLabel.Text =
                    $"debug · {mode} · {stopBit} · {dapBit} · frames {snap.Stack.Count} · bp={snap.BpCount}";
            }
        }
        catch (Exception ex)
        {
            _debugStackFrames.Add(new GlassDebugDeskLatchReader.StackFrame(0, ex.Message, null, 0));
            if (DebugStatusLabel is not null)
                DebugStatusLabel.Text = "debug · latch parse fail";
        }
    }
}

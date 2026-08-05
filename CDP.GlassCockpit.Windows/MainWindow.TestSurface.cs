#nullable enable

using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Tests — redirected dotnet test log + fail ListBox + pass/fail summary.</summary>
public partial class MainWindow
{
    const int MaxTestChars = 200_000;

    GlassRedirectedTest? _testRunner;
    readonly StringBuilder _testBuffer = new();
    readonly ObservableCollection<GlassTestOutputParse.FailRow> _testFails = new();

    void RefreshMfdTestsVisibility()
    {
        if (MfdTestsHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Tests", StringComparison.OrdinalIgnoreCase);
        MfdTestsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && TestsFailList is not null && !ReferenceEquals(TestsFailList.ItemsSource, _testFails))
            TestsFailList.ItemsSource = _testFails;

        if (show && TestsStatusLabel is not null && _testRunner is not { IsRunning: true })
            TestsStatusLabel.Text = "redirected · idle";
    }

    bool IsTestsHostActive()
    {
        if (MfdTestsHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Tests", StringComparison.OrdinalIgnoreCase)
               && MfdTestsHost.Visibility == Visibility.Visible;
    }

    void DisposeTestsSession()
    {
        if (_testRunner is null)
            return;

        _testRunner.TextReceived -= OnTestsText;
        _testRunner.Dispose();
        _testRunner = null;
    }

    void OnTestsText(string chunk) =>
        Dispatcher.BeginInvoke(() => AppendTestsText(chunk));

    void AppendTestsText(string chunk)
    {
        if (TestsOutput is null || string.IsNullOrEmpty(chunk))
            return;

        _testBuffer.Append(chunk);
        if (_testBuffer.Length > MaxTestChars)
            _testBuffer.Remove(0, _testBuffer.Length - MaxTestChars);

        TestsOutput.Text = _testBuffer.ToString();
        TestsOutput.CaretIndex = TestsOutput.Text.Length;
        TestsOutput.ScrollToEnd();
        RefreshTestParse();
    }

    void RefreshTestParse()
    {
        var text = _testBuffer.ToString();
        _testFails.Clear();
        foreach (var row in GlassTestOutputParse.ParseFails(text))
            _testFails.Add(row);

        RefreshEditorSituRibbon();

        if (TestsStatusLabel is null || _testRunner is { IsRunning: true })
            return;

        var summary = GlassTestOutputParse.ParseSummary(text);
        if (summary.Total > 0)
            TestsStatusLabel.Text = $"redirected · {summary.Label}";
    }

    internal void TestsRun_OnClick(object sender, RoutedEventArgs e)
    {
        if (_testRunner is { IsRunning: true })
            return;

        DisposeTestsSession();
        _testBuffer.Clear();
        _testFails.Clear();
        if (TestsOutput is not null)
            TestsOutput.Text = "";

        _testRunner = new GlassRedirectedTest();
        _testRunner.TextReceived += OnTestsText;
        _testRunner.Exited += code =>
            Dispatcher.BeginInvoke(() =>
            {
                AppendTestsText($"\n┌ exited · {code} ┐\n");
                RefreshTestParse();
                RefreshEditorSituRibbon();
                if (TestsStatusLabel is not null)
                {
                    var summary = GlassTestOutputParse.ParseSummary(_testBuffer.ToString());
                    TestsStatusLabel.Text = summary.Total > 0
                        ? $"redirected · done · {code} · {summary.Label}"
                        : $"redirected · done · {code}";
                }
            });

        try
        {
            if (TestsStatusLabel is not null)
                TestsStatusLabel.Text = "redirected · testing";
            _testRunner.Start(_session.WorkspaceRoot ?? Environment.CurrentDirectory);
            if (TestsStatusLabel is not null)
                TestsStatusLabel.Text = $"redirected · {_testRunner.DisplayTarget}";
        }
        catch (Exception ex)
        {
            AppendTestsText($"┌ start fail · {ex.Message} ┐\n");
            if (TestsStatusLabel is not null)
                TestsStatusLabel.Text = "redirected · fail";
        }
    }

    internal void TestsCancel_OnClick(object sender, RoutedEventArgs e)
    {
        _testRunner?.Cancel();
        if (TestsStatusLabel is not null)
            TestsStatusLabel.Text = "redirected · cancel";
    }

    internal void TestsClear_OnClick(object sender, RoutedEventArgs e)
    {
        _testBuffer.Clear();
        _testFails.Clear();
        if (TestsOutput is not null)
            TestsOutput.Text = "";
        if (TestsStatusLabel is not null)
            TestsStatusLabel.Text = "redirected · idle";
    }
}

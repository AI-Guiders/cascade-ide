#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;
using CDP.GlassCockpit.Windows.UiKit;

namespace CDP.GlassCockpit.Windows;

/// <summary>Human HERE + SoftOrgan situations → steps (nested SoftFL Soft:QRH + HereNext).</summary>
public partial class MainWindow
{
    OperatorSituation? _guideSituation;
    int _guideStepIndex;
    bool _guideStepsMode;

    OperatorHereLocus ProbeHereLocus()
    {
        var root = _session.WorkspaceRoot;
        var hasProject = !string.IsNullOrWhiteSpace(root)
            && (Directory.Exists(Path.Combine(root, ".git"))
                || Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly).Length > 0
                || Directory.Exists(Path.Combine(root, ".cdp"))
                || Directory.Exists(Path.Combine(root, ".cascade-ide")));
        return new OperatorHereLocus(
            CabinUp: IsLoaded && IsVisible,
            WorkspaceRoot: root,
            HasProjectSignals: hasProject,
            EditorPath: _editorPath,
            MfdPage: CurrentMfdPage());
    }

    void RefreshOperatorGuideChrome()
    {
        var soft = SoftOrganFaceHandbook.IsSoftOrganGlancePage(CurrentMfdPage());
        if (OperatorHereLine is not null)
        {
            OperatorHereLine.Visibility = soft ? Visibility.Visible : Visibility.Collapsed;
            if (soft)
                OperatorHereLine.Text = OperatorSituationCatalog.FormatHereLine(ProbeHereLocus());
        }

        if (OperatorGuideStepsHost is null)
            return;

        if (!soft || !_guideStepsMode || _guideSituation is null)
        {
            OperatorGuideStepsHost.Visibility = Visibility.Collapsed;
            if (GlanceCardsScroll is not null)
                GlanceCardsScroll.Visibility = Visibility.Visible;
            return;
        }

        OperatorGuideStepsHost.Visibility = Visibility.Visible;
        if (GlanceCardsScroll is not null)
            GlanceCardsScroll.Visibility = Visibility.Collapsed;

        var s = _guideSituation;
        var steps = s.Steps;
        if (_guideStepIndex < 0)
            _guideStepIndex = 0;
        if (_guideStepIndex >= steps.Count)
            _guideStepIndex = Math.Max(0, steps.Count - 1);

        if (GuideSituationTitle is not null)
            GuideSituationTitle.Text = s.Title;
        if (GuideWhenLabel is not null)
            GuideWhenLabel.Text = s.When;
        if (GuideStepIndex is not null)
            GuideStepIndex.Text = steps.Count == 0
                ? "шагов нет"
                : $"шаг {_guideStepIndex + 1} / {steps.Count}";
        if (GuideStepBody is not null)
            GuideStepBody.Text = steps.Count == 0 ? "—" : steps[_guideStepIndex].Text;

        var step = steps.Count == 0 ? null : steps[_guideStepIndex];
        if (GuideDoBtn is not null)
            GuideDoBtn.IsEnabled = !string.IsNullOrWhiteSpace(step?.CommandId);
        if (GuidePrevBtn is not null)
            GuidePrevBtn.IsEnabled = _guideStepIndex > 0;
        if (GuideNextBtn is not null)
            GuideNextBtn.IsEnabled = _guideStepIndex < steps.Count - 1;
    }

    void EnterGuideSituation(string situationId, bool autoHere = false)
    {
        var s = OperatorSituationCatalog.Find(situationId);
        if (s is null && autoHere)
            s = OperatorSituationCatalog.PickHere(ProbeHereLocus());
        if (s is null)
            return;

        _guideSituation = s;
        _guideStepIndex = 0;
        _guideStepsMode = true;
        RefreshOperatorGuideChrome();
        if (GlanceCardsStatusLabel is not null)
            GlanceCardsStatusLabel.Text = $"guide · {s.Id} · steps {_guideSituation.Steps.Count}";
        StatusText.Text = $"glass · guide · {s.Title} · {_guideSituation.Steps.Count} steps · {DateTime.Now:HH:mm:ss}";
    }

    void ExitGuideSteps()
    {
        _guideStepsMode = false;
        _guideSituation = null;
        _guideStepIndex = 0;
        RefreshOperatorGuideChrome();
        RefreshGlanceCardsBody();
    }

    internal void GuideList_OnClick(object sender, RoutedEventArgs e) => ExitGuideSteps();

    internal void GuidePrev_OnClick(object sender, RoutedEventArgs e)
    {
        if (_guideSituation is null || _guideStepIndex <= 0)
            return;
        _guideStepIndex--;
        RefreshOperatorGuideChrome();
    }

    internal void GuideNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (_guideSituation is null)
            return;
        if (_guideStepIndex >= _guideSituation.Steps.Count - 1)
            return;
        _guideStepIndex++;
        RefreshOperatorGuideChrome();
    }

    internal void GuideDo_OnClick(object sender, RoutedEventArgs e)
    {
        if (_guideSituation is null || _guideStepIndex < 0 || _guideStepIndex >= _guideSituation.Steps.Count)
            return;
        var cmd = _guideSituation.Steps[_guideStepIndex].CommandId;
        if (string.IsNullOrWhiteSpace(cmd))
            return;
        RunPaletteEntry(cmd);
    }

    FrameworkElement CreateSoftOrganSituationCard(GlassGlanceChip chip)
    {
        var card = GlassDeckCard.FromChip(chip);
        card.Cursor = Cursors.Hand;
        var id = chip.Label;
        card.MouseLeftButtonUp += (_, e) =>
        {
            EnterGuideSituation(id);
            e.Handled = true;
        };
        return card;
    }
}

namespace CascadeIDE.Models;

/// <summary>Типографика UI. TOML: <c>[fonts]</c> с вложенными <c>[fonts.intercom]</c>, <c>[fonts.editor]</c>. Значения — из <c>Settings/defaults-settings.toml</c> + user merge.</summary>
public sealed class FontsSettings
{
    public IntercomFontsSettings Intercom { get; set; } = new();

    public EditorFontsSettings Editor { get; set; } = new();
}

/// <summary>Skia-лента Intercom. TOML: <c>[fonts.intercom]</c>.</summary>
public sealed class IntercomFontsSettings
{
    /// <summary>Базовый prose (pt) для шкалы <see cref="MetricsScale"/>; совпадает с заводским <c>prose_pt</c> в defaults-settings.toml.</summary>
    public const float ProseBaselinePt = 11f;

    public double ProsePt { get; set; }

    /// <summary>Prose (pt) в лобовом Forward (<c>primary_work_surface = intercom</c>).</summary>
    public double ProsePtForward { get; set; }

    public string ProseFamily { get; set; } = "";

    public string MonoFamily { get; set; } = "";

    public string ChipFamily { get; set; } = "";

    public string ChipIdFamily { get; set; } = "";

    public string SlashMetaFamily { get; set; } = "";

    public string SlashArgsFamily { get; set; } = "";

    public string RoleFamily { get; set; } = "";

    public string GutterFamily { get; set; } = "";

    public double ComposerPt { get; set; }

    public double CommandLinePt { get; set; }

    public double ChromeTitlePt { get; set; }

    public double ChromeSubtitlePt { get; set; }

    public double ChromeHeadingPt { get; set; }

    public double CardTitlePt { get; set; }

    public double PanelTitlePt { get; set; }

    public double PanelSubtitlePt { get; set; }

    public double PanelLabelPt { get; set; }

    public double PanelInputPt { get; set; }

    public double PanelBodyPt { get; set; }

    public float ResolveProsePt(bool forwardHost) =>
        (float)(forwardHost ? ProsePtForward : ProsePt);

    public float MetricsScale(bool forwardHost) =>
        ResolveProsePt(forwardHost) / ProseBaselinePt;

    public float ResolveComposerPt(bool forwardHost) =>
        (float)ComposerPt;

    public float ResolveComposerLineHeight(bool forwardHost) =>
        ResolveComposerPt(forwardHost) * (17f / 12f);

    public float ResolveCommandLinePt(bool forwardHost) =>
        (float)CommandLinePt;

    public float ResolveCommandLinePreviewPt(bool forwardHost) =>
        ResolveCommandLinePt(forwardHost) * (10f / 12f);

    public float ResolveCommandLineLineHeight(bool forwardHost) =>
        ResolveCommandLinePt(forwardHost) * (22f / 12f);

    public float ResolveChromeTitlePt() => (float)ChromeTitlePt;

    public float ResolveChromeSubtitlePt() => (float)ChromeSubtitlePt;

    public float ResolveChromeHeadingPt() => (float)ChromeHeadingPt;

    public float ResolveCardTitleLineHeight(bool forwardHost) =>
        (float)CardTitlePt;

    public float ResolvePanelTitlePt() => (float)PanelTitlePt;

    public float ResolvePanelSubtitlePt() => (float)PanelSubtitlePt;

    public float ResolvePanelLabelPt() => (float)PanelLabelPt;

    public float ResolvePanelInputPt() => (float)PanelInputPt;

    public float ResolvePanelBodyPt() => (float)PanelBodyPt;

    public string ResolveProseFamily() => ProseFamily.Trim();

    public string ResolveMonoFamily() => MonoFamily.Trim();

    public string ResolveChipFamily() => ChipFamily.Trim();

    public string ResolveChipIdFamily() => ChipIdFamily.Trim();

    public string ResolveSlashMetaFamily() => SlashMetaFamily.Trim();

    public string ResolveSlashArgsFamily() => SlashArgsFamily.Trim();

    public string ResolveRoleFamily() => RoleFamily.Trim();

    public string ResolveGutterFamily() => GutterFamily.Trim();
}

/// <summary>Редактор кода (AvaloniaEdit). TOML: <c>[fonts.editor]</c>.</summary>
public sealed class EditorFontsSettings
{
    public double SizePt { get; set; }

    public string Family { get; set; } = "";

    public float ResolveSizePt() => (float)SizePt;

    public string ResolveFamily() => Family.Trim();
}

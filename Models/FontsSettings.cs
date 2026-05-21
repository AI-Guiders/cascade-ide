namespace CascadeIDE.Models;

/// <summary>Типографика UI. TOML: <c>[fonts]</c> с вложенными <c>[fonts.intercom]</c>, <c>[fonts.editor]</c>.</summary>
public sealed class FontsSettings
{
    public IntercomFontsSettings Intercom { get; set; } = new();

    public EditorFontsSettings Editor { get; set; } = new();
}

/// <summary>Skia-лента Intercom. TOML: <c>[fonts.intercom]</c>.</summary>
public sealed class IntercomFontsSettings
{
    /// <summary>Prose (pt) для MFD Chat и прочих не-Forward хостов. 0 = 11.</summary>
    public double ProsePt { get; set; }

    /// <summary>Prose (pt), когда чат в лобовом Forward (<c>primary_work_surface = intercom</c>). 0 = как <see cref="ProsePt"/> или 10.5. Не про ширину колонки — узкие колонки только в совмещённой топологии (P+M+F).</summary>
    public double ProsePtForward { get; set; }

    /// <summary>Семейство prose (Skia). Пусто = Segoe UI. Несколько через запятую — fallback слева направо.</summary>
    public string ProseFamily { get; set; } = "";

    /// <summary>Моноширинный текст (code blocks, slash args). Пусто = Cascadia Mono, Consolas.</summary>
    public string MonoFamily { get; set; } = "";

    /// <summary>Attach-chip label. Пусто = Segoe UI.</summary>
    public string ChipFamily { get; set; } = "";

    /// <summary>Суффикс id на chip (a:…). Пусто = Consolas.</summary>
    public string ChipIdFamily { get; set; } = "";

    /// <summary>Slash meta (путь команды). Пусто = Consolas Bold.</summary>
    public string SlashMetaFamily { get; set; } = "";

    /// <summary>Slash args. Пусто = Consolas.</summary>
    public string SlashArgsFamily { get; set; } = "";

    /// <summary>Role rail. Пусто = Segoe UI Bold.</summary>
    public string RoleFamily { get; set; } = "";

    /// <summary>Номера в gutter. Пусто = Cascadia Mono, Consolas.</summary>
    public string GutterFamily { get; set; } = "";

    /// <summary>Skia composer: ввод и placeholder. 0 = ~prose×12/11, не ниже 12.</summary>
    public double ComposerPt { get; set; }

    /// <summary>Cockpit Command Line над composer. 0 = как <see cref="ComposerPt"/>.</summary>
    public double CommandLinePt { get; set; }

    /// <summary>Заголовок toolbar Forward («Intercom»). 0 = 13.</summary>
    public double ChromeTitlePt { get; set; }

    /// <summary>Подзаголовок toolbar (тема / линия). 0 = 10.</summary>
    public double ChromeSubtitlePt { get; set; }

    /// <summary>Крупные chrome-заголовки («Картотека тем»). 0 = 14.</summary>
    public double ChromeHeadingPt { get; set; }

    /// <summary>Заголовки карточек в ленте (spine, overview). 0 = 16×scale(prose).</summary>
    public double CardTitlePt { get; set; }

    /// <summary>MFD: «Agent Chat» и заголовки уточнений. 0 = <see cref="ResolveChromeHeadingPt"/>.</summary>
    public double PanelTitlePt { get; set; }

    /// <summary>MFD: подзаголовки секции. 0 = <see cref="ResolveChromeSubtitlePt"/>.</summary>
    public double PanelSubtitlePt { get; set; }

    /// <summary>MFD: «Диалог», Expander spine. 0 = subtitle+1.</summary>
    public double PanelLabelPt { get; set; }

    /// <summary>MFD: TextBox (текст и placeholder). 0 = <see cref="ResolveComposerPt"/>(false).</summary>
    public double PanelInputPt { get; set; }

    /// <summary>MFD: текст вопроса в уточнениях. 0 = <see cref="ResolveProsePt"/>(false).</summary>
    public double PanelBodyPt { get; set; }

    public float ResolveProsePt(bool forwardHost)
    {
        if (forwardHost && ProsePtForward > 0)
            return (float)ProsePtForward;
        if (ProsePt > 0)
            return (float)ProsePt;
        return forwardHost ? 10.5f : 11f;
    }

    public float MetricsScale(bool forwardHost) =>
        ResolveProsePt(forwardHost) / 11f;

    public float ResolveComposerPt(bool forwardHost) =>
        ComposerPt > 0
            ? (float)ComposerPt
            : Math.Max(12f, ResolveProsePt(forwardHost) * (12f / 11f));

    public float ResolveComposerLineHeight(bool forwardHost) =>
        ResolveComposerPt(forwardHost) * (17f / 12f);

    public float ResolveCommandLinePt(bool forwardHost) =>
        CommandLinePt > 0 ? (float)CommandLinePt : ResolveComposerPt(forwardHost);

    public float ResolveCommandLinePreviewPt(bool forwardHost) =>
        ResolveCommandLinePt(forwardHost) * (10f / 12f);

    public float ResolveChromeTitlePt() =>
        ChromeTitlePt > 0 ? (float)ChromeTitlePt : 13f;

    public float ResolveChromeSubtitlePt() =>
        ChromeSubtitlePt > 0 ? (float)ChromeSubtitlePt : 10f;

    public float ResolveChromeHeadingPt() =>
        ChromeHeadingPt > 0 ? (float)ChromeHeadingPt : 14f;

    public float ResolveCardTitleLineHeight(bool forwardHost) =>
        CardTitlePt > 0 ? (float)CardTitlePt : 16f * MetricsScale(forwardHost);

    public float ResolvePanelTitlePt() =>
        PanelTitlePt > 0 ? (float)PanelTitlePt : ResolveChromeHeadingPt();

    public float ResolvePanelSubtitlePt() =>
        PanelSubtitlePt > 0 ? (float)PanelSubtitlePt : ResolveChromeSubtitlePt();

    public float ResolvePanelLabelPt() =>
        PanelLabelPt > 0 ? (float)PanelLabelPt : Math.Max(11f, ResolvePanelSubtitlePt() + 1f);

    public float ResolvePanelInputPt() =>
        PanelInputPt > 0 ? (float)PanelInputPt : ResolveComposerPt(forwardHost: false);

    public float ResolvePanelBodyPt() =>
        PanelBodyPt > 0 ? (float)PanelBodyPt : ResolveProsePt(forwardHost: false);

    public string ResolveProseFamily() => ResolveFamilyList(ProseFamily, "Segoe UI");

    public string ResolveMonoFamily() => ResolveFamilyList(MonoFamily, "Cascadia Mono,Consolas");

    public string ResolveChipFamily() => ResolveFamilyList(ChipFamily, "Segoe UI");

    public string ResolveChipIdFamily() => ResolveFamilyList(ChipIdFamily, "Consolas");

    public string ResolveSlashMetaFamily() => ResolveFamilyList(SlashMetaFamily, "Consolas");

    public string ResolveSlashArgsFamily() => ResolveFamilyList(SlashArgsFamily, "Consolas");

    public string ResolveRoleFamily() => ResolveFamilyList(RoleFamily, "Segoe UI");

    public string ResolveGutterFamily() => ResolveFamilyList(GutterFamily, "Cascadia Mono,Consolas");

    private static string ResolveFamilyList(string? configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
}

/// <summary>Редактор кода (AvaloniaEdit). TOML: <c>[fonts.editor]</c>.</summary>
public sealed class EditorFontsSettings
{
    /// <summary>Размер (pt). 0 = 13.</summary>
    public double SizePt { get; set; }

    /// <summary>Семейство (как в AXAML FontFamily). Пусто = Consolas,Cascadia Code,monospace.</summary>
    public string Family { get; set; } = "";

    public float ResolveSizePt() =>
        SizePt > 0 ? (float)SizePt : 13f;

    public string ResolveFamily() =>
        string.IsNullOrWhiteSpace(Family)
            ? "Consolas,Cascadia Code,monospace"
            : Family.Trim();
}

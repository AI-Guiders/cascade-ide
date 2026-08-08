#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>ADR 0014 SoftFL — human situations → ordered steps (not label deck / not markdown wall).</summary>
public sealed record OperatorGuideStep(string Text, string? CommandId = null);

public sealed record OperatorSituation(
    string Id,
    string Title,
    string When,
    string Family,
    string Tone,
    IReadOnlyList<OperatorGuideStep> Steps);

/// <summary>Live locus for HERE line + auto-pick situation.</summary>
public sealed record OperatorHereLocus(
    bool CabinUp,
    string? WorkspaceRoot,
    bool HasProjectSignals,
    string? EditorPath,
    string? MfdPage);

public static class OperatorSituationCatalog
{
    public static IReadOnlyList<OperatorSituation> All { get; } =
    [
        new(
            "cabin-start",
            "Кабина только что стартовала",
            "когда: окно Glass открыто, ещё не ясно что делать",
            "qrh",
            "ok",
            [
                new("Открой HERE/NEXT (Ctrl+K → hn) — строка HERE скажет, где ты."),
                new("Если проект не виден — выбери ситуацию «Нужно открыть проект»."),
                new("Если кабина тупит — «Кабина зависла»."),
                new("Справочник ситуаций Soft:QRH (Ctrl+Q → QRH) — список, не поиск вслепую."),
            ]),
        new(
            "open-project",
            "Нужно открыть проект",
            "когда: нет workspace / нет .sln / не знаешь какой репо",
            "qrh",
            "warn",
            [
                new("Ctrl+O → P проект / D папка (или F файл). Timeout → файл.", "open_family"),
                new("Ctrl+K → os / od / or — решение · папка · недавние.", "open_solution"),
                new("Ctrl+K → wh — WorkspaceHealth: ROOT / GIT / SLN → READY.", "mfd_workspace_health"),
                new("HERE/NEXT — HERE должен показать имя проекта.", "mfd_here_next"),
            ]),
        new(
            "where-am-i",
            "Не понятно где я в IDE",
            "когда: много панелей, не ясно что дальше",
            "qrh",
            "ok",
            [
                new("Смотри HERE сверху: проект · файл · страница MFD."),
                new("PLAN — что сейчас летает у агента (не твой QRH)."),
                new("Если файл открыт — Editor situ (WHY / ROLE / APPLIES)."),
                new("Soft:QRH — выбери ситуацию глазами, жми Дальше по шагам.", "soft_qrh"),
            ]),
        new(
            "hung-glass",
            "Кабина зависла / не отвечает",
            "когда: окно не кликается, Capture висит, Responding=False",
            "qrh",
            "warn",
            [
                new("Не долби Capture/PrintWindow — это усугубляет hang."),
                new("Убей CDP.GlassCockpit.Windows и подними Release exe заново."),
                new("После рестарта — HERE/NEXT, проверь Responding.", "mfd_here_next"),
                new("Если снова SoftOrgan — Soft:QRH, не MarkdownPreview.", "soft_qrh"),
            ]),
        new(
            "not-connected",
            "CDP Not connected",
            "когда: агент/инструменты пишут Not connected, CdpMcp.exe может ещё жить",
            "ecl",
            "warn",
            [
                new("Из внешнего терминала: Recover-CdpSeatRemount.ps1 -Seat cdp."),
                new("Дождись remount Cursor MCP — не правь mcp.json руками."),
                new("Потом cdp_health / cdp_pressure recall."),
                new("Двойной seat = два -Seat подряд, не -NudgeAllSeats."),
            ]),
        new(
            "hard-deploy",
            "Нужен hard deploy CDP",
            "когда: publish KillRunning, CDP умрёт вместе с shell",
            "ecl",
            "warn",
            [
                new("Deploy только из внешнего terminal_* / WT — не из cdp_shell_*."),
                new("После kill — per-seat nudge (cdp или cdp-debug), не оба сразу."),
                new("Long-run jobs, которые должны пережить CDP — terminal_*."),
            ]),
        new(
            "path-mutate",
            "Править код в habitat",
            "когда: правишь .cs/.md в CDP, не в Cursor Write",
            "ecl",
            "warn",
            [
                new("Mutate через cdp_buffer / cockpit / sniper — не Cursor Write."),
                new("Relative path → ProjectRoot после cdp_open."),
                new("C# правки — prefer edit_op=anchor [F:;M:;K:]."),
            ]),
        new(
            "sa-pulse",
            "Что сейчас на alert / EICAS",
            "когда: видишь tip/clr/ack/list и не знаешь читать ли",
            "alert",
            "ok",
            [
                new("clr / ack / list — SoftKeys EICAS, не handbook search."),
                new("PreferSurface alert — смотри тело канала, не только chrome."),
                new("Ситуации и шаги — Soft:QRH / HERE/NEXT, не пустой Markdown."),
            ]),
        new(
            "prefer-surface",
            "Смотрю не туда (chrome ≠ body)",
            "когда: кликнула chrome/chip, а смысл в другом seat",
            "alert",
            "meta",
            [
                new("OneOf / PreferSurface — активный канал sit/world/alert."),
                new("MFD страница (M · …) — тело инструмента."),
                new("HERE/NEXT склеивает locus + следующий шаг.", "mfd_here_next"),
            ]),
    ];

    public static IReadOnlyList<OperatorSituation> ForFamily(string family, string? filter = null)
    {
        var fam = (family ?? "").Trim().ToLowerInvariant();
        var list = All.Where(s => s.Family == fam).ToList();
        if (fam is "here" or "herenext")
            list = All.ToList();
        return Filter(list, filter);
    }

    public static OperatorSituation? Find(string? id) =>
        All.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));

    public static OperatorSituation PickHere(OperatorHereLocus locus)
    {
        if (!locus.HasProjectSignals || string.IsNullOrWhiteSpace(locus.WorkspaceRoot))
            return Find("open-project")!;
        if (string.IsNullOrWhiteSpace(locus.EditorPath))
            return Find("cabin-start")!;
        return Find("where-am-i")!;
    }

    public static string FormatHereLine(OperatorHereLocus locus)
    {
        var project = "—";
        if (!string.IsNullOrWhiteSpace(locus.WorkspaceRoot))
        {
            try
            {
                project = Path.GetFileName(locus.WorkspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch
            {
                project = locus.WorkspaceRoot;
            }
        }

        var file = string.IsNullOrWhiteSpace(locus.EditorPath)
            ? "—"
            : Path.GetFileName(locus.EditorPath);
        var mfd = string.IsNullOrWhiteSpace(locus.MfdPage) ? "—" : locus.MfdPage;
        var cabin = locus.CabinUp ? "up" : "down";
        return $"HERE · cabin {cabin} · project {project} · file {file} · MFD {mfd}";
    }

    public static GlassGlanceChip ToChip(OperatorSituation s) =>
        new(s.Id, s.Title, s.Tone);

    static IReadOnlyList<OperatorSituation> Filter(IReadOnlyList<OperatorSituation> items, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return items;

        var q = filter.Trim();
        return items
            .Where(s =>
                s.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.When.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Steps.Any(st => st.Text.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}

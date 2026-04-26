using CascadeIDE.Cockpit;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Два именованных instrument deck для страницы «готовность окружения» (ADR 0063): одна и та же упорядоченная композиция ячеек,
/// различается представление — полоса ламп vs текстовые карточки/таблица (без пользовательского TOML).
/// </summary>
public static class EnvironmentReadinessInstrumentDeck
{
    /// <summary>Якорь вторичного контура (страница MFD), не слот Pfd/Mfd главной сетки.</summary>
    public const string SemanticAnchorId = "mfd_shell_environment_readiness";

    public const string CompactDeckId = "environment_readiness_compact_lamps_v1";
    public const string TextualDeckId = "environment_readiness_textual_detail_v1";

    /// <summary>Порядок ячеек совпадает с <see cref="CascadeIDE.Features.EnvironmentReadiness.Application.EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync"/>.</summary>
    public static readonly IReadOnlyList<string> OrderedCellIds =
    [
        EnvironmentReadinessCellIds.DevToolsSection,
        EnvironmentReadinessCellIds.Agent,
        EnvironmentReadinessCellIds.CSharpLsp,
        EnvironmentReadinessCellIds.MarkdownLsp,
        EnvironmentReadinessCellIds.DotnetSdk,
        EnvironmentReadinessCellIds.EnvSection,
        EnvironmentReadinessCellIds.AgentNotesFile,
        EnvironmentReadinessCellIds.AgentNotesCanonPath,
        EnvironmentReadinessCellIds.NetcoreDbgPath,
    ];

    /// <summary>Сетка ламп в одну строку (компактный glance).</summary>
    public static InstrumentDeckDescriptor CompactLampStrip { get; } = new(
        CompactDeckId,
        SemanticAnchorId,
        InstrumentDeckLayoutPattern.Grid,
        OrderedCellIds);

    /// <summary>Те же id; детальный текст — в <see cref="AnnunciatorLampItem.Detail"/>.</summary>
    public static InstrumentDeckDescriptor TextualDetail { get; } = new(
        TextualDeckId,
        SemanticAnchorId,
        InstrumentDeckLayoutPattern.Grid,
        OrderedCellIds);
}

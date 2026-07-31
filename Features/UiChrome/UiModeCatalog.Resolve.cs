using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome.Application;

namespace CascadeIDE.Features.UiChrome;

public static partial class UiModeCatalog
{
    private static ResolvedMode ResolveMode(
        string modeId,
        string uiModesDirectory,
        Dictionary<string, ResolvedMode> memo,
        List<string> stack)
    {
        if (memo.TryGetValue(modeId, out var cached))
            return cached;

        if (stack.Exists(x => string.Equals(x, modeId, StringComparison.OrdinalIgnoreCase)))
        {
            global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: inherits cycle at {modeId}");
            var fb = ResolvedMode.FromRegistry("Flight");
            memo[modeId] = fb;
            return fb;
        }

        stack.Add(modeId);
        try
        {
            UiModeFileToml? file = null;
            if (TryReadUiModesFile(uiModesDirectory, modeId + ".toml", out var modeTomlText))
            {
                try
                {
                    file = CascadeTomlSerializer.Deserialize<UiModeFileToml>(modeTomlText);
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: {modeId}.toml parse error — {ex.Message}");
                }
            }

            var inherits = string.IsNullOrWhiteSpace(file?.Meta?.Inherits) ? null : file!.Meta!.Inherits!.Trim();

            ResolvedMode? parentResolved = null;
            UiModeLayoutSpec baseSpec;
            if (inherits is not null)
            {
                parentResolved = ResolveMode(inherits, uiModesDirectory, memo, stack);
                baseSpec = parentResolved.Spec;
            }
            else
                baseSpec = UiModeLayoutRegistry.Get(modeId);

            var merged = MergeSpec(baseSpec, file);
            var family = ResolveFamily(modeId, file, inherits, parentResolved);
            var chatPx = ResolveChatWidth(modeId, file, inherits, parentResolved, merged);
            var showTaskBar = ResolveShowTaskBar(file, inherits, parentResolved, family);
            var capabilities = ResolveCapabilities(file, inherits, parentResolved, family);
            var windowTitle = ResolveWindowTitle(file, inherits, parentResolved);

            var result = new ResolvedMode(merged, family, chatPx, showTaskBar, capabilities, windowTitle);
            memo[modeId] = result;
            return result;
        }
        finally
        {
            stack.RemoveAt(stack.Count - 1);
        }
    }

    /// <summary>
    /// Явный <c>mfd_region_expanded_width_pixels</c> в файле режима; иначе при <c>inherits</c> — ширина уже разрешённого родителя;
    /// иначе — глобальные метрики и правило Power / AgentChat / остальные (<see cref="UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels"/>).
    /// </summary>
    private static int ResolveChatWidth(
        string modeId,
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved,
        UiModeLayoutSpec merged)
    {
        _ = merged;
        if (file?.Layout?.MfdRegionExpandedWidthPixels is { } w && w >= 0)
            return w;

        if (inherits is not null && parentResolved is not null)
            return parentResolved.MfdRegionExpandedWidthPx;

        return UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels(modeId);
    }

    private static bool ResolveShowTaskBar(
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved,
        UiModeFamily family)
    {
        if (file?.Layout?.ActiveTaskStrip is { } st)
            return st;

        if (inherits is not null && parentResolved is not null)
            return parentResolved.ShowTaskBar;

        return DefaultShowTaskBarForFamily(family);
    }

    private static IdeHealthUiSurface ResolveIdeHealthSurface(string? fromFile, IdeHealthUiSurface inherited)
    {
        if (string.IsNullOrWhiteSpace(fromFile))
            return inherited;
        var v = fromFile.Trim();
        if (string.Equals(v, "dedicated_page", StringComparison.OrdinalIgnoreCase))
            return IdeHealthUiSurface.DedicatedPage;
        if (string.Equals(v, "bottom_strip", StringComparison.OrdinalIgnoreCase))
            return IdeHealthUiSurface.BottomStrip;
        return inherited;
    }

    private static UiModeCapabilities ResolveCapabilities(
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved,
        UiModeFamily family)
    {
        var baseCaps = inherits is not null && parentResolved is not null
            ? parentResolved.Capabilities
            : UiModeCapabilities.DefaultsForFamily(family);

        if (file is null)
            return baseCaps;

        var modeFile = file!;

        var cap = modeFile.Capabilities;
        var span = baseCaps.IdeHealthMainColumnSpan;
        if (cap?.IdeHealthMainColumnSpan is { } s && s >= 1 && s <= 12)
            span = s;

        var surface = ResolveIdeHealthSurface(cap?.IdeHealthSurface, baseCaps.IdeHealthSurface);

        return new UiModeCapabilities(
            QuickActions: cap?.QuickActions ?? baseCaps.QuickActions,
            AgentOperationsPanel: cap?.AgentOperationsPanel ?? baseCaps.AgentOperationsPanel,
            AgentTrace: cap?.AgentTrace ?? baseCaps.AgentTrace,
            AutonomousAgentTelemetry: cap?.AutonomousAgentTelemetry ?? baseCaps.AutonomousAgentTelemetry,
            IdeHealthOnTerminalTab: cap?.IdeHealthOnTerminalTab
                ?? baseCaps.IdeHealthOnTerminalTab,
            IdeHealthMainColumnSpan: span,
            InstrumentationTabs: cap?.InstrumentationTabs ?? baseCaps.InstrumentationTabs,
            HypothesesTab: cap?.HypothesesTab ?? baseCaps.HypothesesTab,
            RiskSummaryCard: cap?.RiskSummaryCard ?? baseCaps.RiskSummaryCard,
            ResultSummaryCard: cap?.ResultSummaryCard ?? baseCaps.ResultSummaryCard,
            IdeHealthStripVisible: cap?.IdeHealthStrip ?? baseCaps.IdeHealthStripVisible,
            IdeHealthSurface: surface,
            ProblemsPanelVisible: cap?.ProblemsPanel ?? baseCaps.ProblemsPanelVisible,
            EicasAlertsBarEnabled: cap?.EicasAlertsBar ?? baseCaps.EicasAlertsBarEnabled);
    }

    private static string? ResolveWindowTitle(UiModeFileToml? file, string? inherits, ResolvedMode? parentResolved)
    {
        var title = file?.Meta?.MainWindowTitle;
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        if (inherits is not null && parentResolved is not null)
            return parentResolved.WindowTitleOverride;

        return null;
    }

    private static UiModeFamily ResolveFamily(
        string modeId,
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved)
    {
        if (TryParseFamily(file?.Meta?.Family) is { } explicitFamily)
            return explicitFamily;

        if (inherits is not null && parentResolved is not null)
            return parentResolved.Family;

        return BuiltinFamily(modeId);
    }

    private static UiModeLayoutSpec MergeSpec(UiModeLayoutSpec baseSpec, UiModeFileToml? o)
    {
        if (o is null)
            return baseSpec;

        return new UiModeLayoutSpec(
            PfdRegionExpanded: o.Layout?.PfdRegionExpanded ?? baseSpec.PfdRegionExpanded,
            BuildOutputVisible: o.Layout?.BuildOutputVisible ?? baseSpec.BuildOutputVisible,
            TerminalVisible: o.Layout?.TerminalVisible ?? baseSpec.TerminalVisible,
            MfdRegionExpanded: o.Layout?.MfdRegionExpanded ?? baseSpec.MfdRegionExpanded,
            EditorGroupCount: o.Layout?.EditorGroupCount ?? baseSpec.EditorGroupCount,
            ThemeSlot: ParseThemeSlot(o.Meta?.ThemeSlot) ?? baseSpec.ThemeSlot,
            SelectTerminalTabWhenTerminalShown: o.Layout?.SelectTerminalTabWhenTerminalShown
                ?? baseSpec.SelectTerminalTabWhenTerminalShown,
            InstrumentationDockVisible: o.Layout?.InstrumentationDockVisible ?? baseSpec.InstrumentationDockVisible);
    }

    private static UiModeThemeSlot? ParseThemeSlot(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (string.Equals(s, nameof(UiModeThemeSlot.CursorLike), StringComparison.OrdinalIgnoreCase))
            return UiModeThemeSlot.CursorLike;
        if (string.Equals(s, nameof(UiModeThemeSlot.Dark), StringComparison.OrdinalIgnoreCase))
            return UiModeThemeSlot.Dark;
        if (string.Equals(s, nameof(UiModeThemeSlot.PowerCockpit), StringComparison.OrdinalIgnoreCase))
            return UiModeThemeSlot.PowerCockpit;
        global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: unknown theme_slot — {s}");
        return null;
    }

    private static UiModeFamily? TryParseFamily(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (string.Equals(s, nameof(UiModeFamily.Focus), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Focus;
        if (string.Equals(s, nameof(UiModeFamily.Balanced), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Balanced;
        if (string.Equals(s, nameof(UiModeFamily.Power), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Power;
        if (string.Equals(s, nameof(UiModeFamily.AgentChat), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.AgentChat;
        if (string.Equals(s, nameof(UiModeFamily.Debug), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Debug;
        if (string.Equals(s, nameof(UiModeFamily.Flight), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Flight;
        if (string.Equals(s, nameof(UiModeFamily.Editor), StringComparison.OrdinalIgnoreCase))
            return UiModeFamily.Editor;
        global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: unknown family — {s}");
        return null;
    }

    private static UiModeFamily BuiltinFamily(string normalizedMode) =>
        normalizedMode switch
        {
            "Focus" => UiModeFamily.Focus,
            "Editor" => UiModeFamily.Editor,
            "Balanced" => UiModeFamily.Balanced,
            "Power" => UiModeFamily.Power,
            "AgentChat" => UiModeFamily.AgentChat,
            "Debug" => UiModeFamily.Debug,
            "Flight" => UiModeFamily.Flight,
            _ => UiModeFamily.Flight,
        };

    public static UiModeLayoutSpec GetSpec(string normalizedMode)
    {
        lock (Gate)
        {
            if (Specs.TryGetValue(normalizedMode, out var spec))
                return spec;
            return UiModeLayoutRegistry.Get(normalizedMode);
        }
    }

    public static int GetMfdRegionExpandedWidthPixels(string normalizedMode)
    {
        lock (Gate)
        {
            if (MfdRegionExpandedWidths.TryGetValue(normalizedMode, out var w))
                return w;
            return UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels(normalizedMode);
        }
    }

    /// <summary>Видимость полосы Task Cockpit (данные режима после TOML; вне каталога — по семье Debug).</summary>
    public static bool GetShowTaskBar(string normalizedMode)
    {
        lock (Gate)
        {
            if (ShowTaskBarByMode.TryGetValue(normalizedMode, out var show))
                return show;
        }

        return DefaultShowTaskBarForFamily(GetFamily(normalizedMode));
    }

    /// <summary>Capabilities режима после TOML; вне каталога — <see cref="UiModeCapabilities.DefaultsForFamily"/>.</summary>
    public static UiModeCapabilities GetCapabilities(string normalizedMode)
    {
        lock (Gate)
        {
            if (CapabilitiesByMode.TryGetValue(normalizedMode, out var c))
                return c;
        }

        return UiModeCapabilities.DefaultsForFamily(GetFamily(normalizedMode));
    }

    /// <summary>Полный заголовок окна из TOML; <see langword="null"/> — использовать встроенные строки по семье.</summary>
    public static string? GetWindowTitleOverride(string normalizedMode)
    {
        lock (Gate)
        {
            if (WindowTitleOverrideByMode.TryGetValue(normalizedMode, out var t))
                return t;
        }

        return null;
    }

    public static bool TryGetFamily(string normalizedMode, out UiModeFamily family)
    {
        lock (Gate)
        {
            return Families.TryGetValue(normalizedMode, out family);
        }
    }

    /// <summary>Нормализует id к каноническому виду из индекса; неизвестный режим → Flight.</summary>
    public static string NormalizeUiMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "Flight";

        lock (Gate)
        {
            foreach (var id in _orderedModeIds)
            {
                if (string.Equals(id, mode, StringComparison.OrdinalIgnoreCase))
                    return id;
            }
        }

        return "Flight";
    }

    public static UiModeFamily GetFamily(string normalizedMode)
    {
        lock (Gate)
        {
            if (Families.TryGetValue(normalizedMode, out var f))
                return f;
        }

        return BuiltinFamily(normalizedMode);
    }
}

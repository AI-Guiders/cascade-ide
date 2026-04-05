using Tomlyn;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Загруженные из <c>UiModes/*.toml</c> режимы (ADR 0010). При ошибке или отсутствии файлов — встроенный <see cref="UiModeLayoutRegistry"/>.
/// </summary>
public static class UiModeCatalog
{
    private static readonly object Gate = new();
    private static bool _initialized;
    private static IReadOnlyList<string> _orderedModeIds = UiModeLayoutRegistry.OrderedModeIds;
    private static readonly Dictionary<string, UiModeLayoutSpec> Specs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeFamily> Families = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> ChatExpandedWidths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> ShowTaskBarByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeCapabilities> CapabilitiesByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string?> WindowTitleOverrideByMode = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsInitialized
    {
        get
        {
            lock (Gate)
                return _initialized;
        }
    }

    public static IReadOnlyList<string> OrderedModeIds
    {
        get
        {
            lock (Gate)
                return _orderedModeIds;
        }
    }

    /// <summary>Инициализация до первого <see cref="MainWindowViewModel"/> и любых вызовов нормализации режима.</summary>
    public static void Initialize(string? uiModesDirectoryOverride = null)
    {
        lock (Gate)
        {
            if (_initialized)
                return;
            try
            {
                var dir = uiModesDirectoryOverride ?? Path.Combine(AppContext.BaseDirectory, "UiModes");
                LoadFromDirectory(dir);
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: fallback to built-in registry ({ex.Message})");
                ApplyBuiltinOnly();
            }

            _initialized = true;
        }
    }

    /// <summary>Для тестов: сброс и повторная загрузка.</summary>
    public static void ResetForTests()
    {
        lock (Gate)
        {
            _initialized = false;
            _orderedModeIds = UiModeLayoutRegistry.OrderedModeIds;
            Specs.Clear();
            Families.Clear();
            ChatExpandedWidths.Clear();
            ShowTaskBarByMode.Clear();
            CapabilitiesByMode.Clear();
            WindowTitleOverrideByMode.Clear();
            UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();
        }
    }

    private static void LoadFromDirectory(string uiModesDirectory)
    {
        Specs.Clear();
        Families.Clear();
        ChatExpandedWidths.Clear();
        ShowTaskBarByMode.Clear();
        CapabilitiesByMode.Clear();
        WindowTitleOverrideByMode.Clear();
        UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();

        if (!Directory.Exists(uiModesDirectory))
        {
            global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: directory missing — {uiModesDirectory}");
            ApplyBuiltinOnly();
            return;
        }

        var indexPath = Path.Combine(uiModesDirectory, "index.toml");
        if (!File.Exists(indexPath))
        {
            global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: index.toml missing — {indexPath}");
            ApplyBuiltinOnly();
            return;
        }

        UiModesIndexToml? index;
        try
        {
            index = Toml.ToModel<UiModesIndexToml>(File.ReadAllText(indexPath));
        }
        catch (Exception ex)
        {
            global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: index.toml parse error — {ex.Message}");
            ApplyBuiltinOnly();
            return;
        }

        if (index is null || index.SchemaVersion < 1 || index.Modes is null || index.Modes.Count == 0)
        {
            global::System.Diagnostics.Debug.WriteLine("UiModeCatalog: index invalid or empty modes");
            ApplyBuiltinOnly();
            return;
        }

        var workspacePath = Path.Combine(uiModesDirectory, "workspace.toml");
        if (File.Exists(workspacePath))
        {
            try
            {
                var w = Toml.ToModel<UiWorkspaceToml>(File.ReadAllText(workspacePath));
                UiWorkspaceLayoutRuntimeMetrics.ApplyWorkspaceToml(w);
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: workspace.toml ignored — {ex.Message}");
            }
        }

        _orderedModeIds = index.Modes
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .ToList();

        var memo = new Dictionary<string, ResolvedMode>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in _orderedModeIds)
            ResolveMode(id, uiModesDirectory, memo, []);

        foreach (var id in _orderedModeIds)
        {
            if (!memo.TryGetValue(id, out var r))
            {
                global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: mode not resolved — {id}");
                r = ResolvedMode.FromRegistry(id);
            }

            Specs[id] = r.Spec;
            Families[id] = r.Family;
            ChatExpandedWidths[id] = r.ChatExpandedWidthPx;
            ShowTaskBarByMode[id] = r.ShowTaskBar;
            CapabilitiesByMode[id] = r.Capabilities;
            WindowTitleOverrideByMode[id] = r.WindowTitleOverride;
        }

        foreach (var required in UiModeLayoutRegistry.OrderedModeIds)
        {
            if (!Specs.ContainsKey(required))
                global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: required mode id missing from data — {required}");
        }
    }

    private static void ApplyBuiltinOnly()
    {
        _orderedModeIds = UiModeLayoutRegistry.OrderedModeIds;
        foreach (var id in _orderedModeIds)
        {
            var fam = BuiltinFamily(id);
            Specs[id] = UiModeLayoutRegistry.Get(id);
            Families[id] = fam;
            ChatExpandedWidths[id] = UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels(id);
            ShowTaskBarByMode[id] = DefaultShowTaskBarForFamily(fam);
            CapabilitiesByMode[id] = UiModeCapabilities.DefaultsForFamily(fam);
            WindowTitleOverrideByMode[id] = null;
        }
    }

    private static bool DefaultShowTaskBarForFamily(UiModeFamily family) => !family.IsDebugFamily();

    private sealed record ResolvedMode(
        UiModeLayoutSpec Spec,
        UiModeFamily Family,
        int ChatExpandedWidthPx,
        bool ShowTaskBar,
        UiModeCapabilities Capabilities,
        string? WindowTitleOverride)
    {
        public static ResolvedMode FromRegistry(string id)
        {
            var fam = BuiltinFamily(id);
            return new ResolvedMode(
                UiModeLayoutRegistry.Get(id),
                fam,
                UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels(id),
                DefaultShowTaskBarForFamily(fam),
                UiModeCapabilities.DefaultsForFamily(fam),
                null);
        }
    }

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
            var fb = ResolvedMode.FromRegistry("Balanced");
            memo[modeId] = fb;
            return fb;
        }

        stack.Add(modeId);
        try
        {
            UiModeFileToml? file = null;
            var path = Path.Combine(uiModesDirectory, modeId + ".toml");
            if (File.Exists(path))
            {
                try
                {
                    file = Toml.ToModel<UiModeFileToml>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: {modeId}.toml parse error — {ex.Message}");
                }
            }

            var inherits = string.IsNullOrWhiteSpace(file?.Inherits) ? null : file!.Inherits!.Trim();

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
    /// Явный <c>chat_expanded_width_pixels</c> в файле режима; иначе при <c>inherits</c> — ширина уже разрешённого родителя;
    /// иначе — глобальные метрики и правило Power / AgentChat / остальные (<see cref="UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels"/>).
    /// </summary>
    private static int ResolveChatWidth(
        string modeId,
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved,
        UiModeLayoutSpec merged)
    {
        _ = merged;
        if (file?.ChatExpandedWidthPixels is { } w && w >= 0)
            return w;

        if (inherits is not null && parentResolved is not null)
            return parentResolved.ChatExpandedWidthPx;

        return UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels(modeId);
    }

    private static bool ResolveShowTaskBar(
        UiModeFileToml? file,
        string? inherits,
        ResolvedMode? parentResolved,
        UiModeFamily family)
    {
        if (file?.ActiveTaskStrip is { } st)
            return st;

        if (inherits is not null && parentResolved is not null)
            return parentResolved.ShowTaskBar;

        return DefaultShowTaskBarForFamily(family);
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

        var span = baseCaps.TelemetryMainColumnSpan;
        if (file.TelemetryMainColumnSpan is { } s && s >= 1 && s <= 12)
            span = s;

        return new UiModeCapabilities(
            QuickActions: file.QuickActions ?? baseCaps.QuickActions,
            AgentOperationsPanel: file.AgentOperationsPanel ?? baseCaps.AgentOperationsPanel,
            AgentTrace: file.AgentTrace ?? baseCaps.AgentTrace,
            AutonomousAgentTelemetry: file.AutonomousAgentTelemetry ?? baseCaps.AutonomousAgentTelemetry,
            TelemetryOnTerminalTab: file.TelemetryOnTerminalTab
                ?? baseCaps.TelemetryOnTerminalTab,
            TelemetryMainColumnSpan: span,
            InstrumentationTabs: file.InstrumentationTabs ?? baseCaps.InstrumentationTabs,
            HypothesesTab: file.HypothesesTab ?? baseCaps.HypothesesTab,
            RiskSummaryCard: file.RiskSummaryCard ?? baseCaps.RiskSummaryCard,
            ResultSummaryCard: file.ResultSummaryCard ?? baseCaps.ResultSummaryCard);
    }

    private static string? ResolveWindowTitle(UiModeFileToml? file, string? inherits, ResolvedMode? parentResolved)
    {
        var title = file?.MainWindowTitle;
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
        if (TryParseFamily(file?.Family) is { } explicitFamily)
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
            SolutionExplorerVisible: o.SolutionExplorerVisible ?? baseSpec.SolutionExplorerVisible,
            BuildOutputVisible: o.BuildOutputVisible ?? baseSpec.BuildOutputVisible,
            TerminalVisible: o.TerminalVisible ?? baseSpec.TerminalVisible,
            ChatPanelExpanded: o.ChatPanelExpanded ?? baseSpec.ChatPanelExpanded,
            EditorGroupCount: o.EditorGroupCount ?? baseSpec.EditorGroupCount,
            ThemeSlot: ParseThemeSlot(o.ThemeSlot) ?? baseSpec.ThemeSlot,
            SelectTerminalTabWhenTerminalShown: o.SelectTerminalTabWhenTerminalShown
                ?? baseSpec.SelectTerminalTabWhenTerminalShown);
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
        global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: unknown family — {s}");
        return null;
    }

    private static UiModeFamily BuiltinFamily(string normalizedMode) =>
        normalizedMode switch
        {
            "Focus" => UiModeFamily.Focus,
            "Balanced" => UiModeFamily.Balanced,
            "Power" => UiModeFamily.Power,
            "AgentChat" => UiModeFamily.AgentChat,
            "Debug" => UiModeFamily.Debug,
            _ => UiModeFamily.Balanced,
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

    public static int GetChatPanelExpandedWidthPixels(string normalizedMode)
    {
        lock (Gate)
        {
            if (ChatExpandedWidths.TryGetValue(normalizedMode, out var w))
                return w;
            return UiModeLayoutRegistry.GetChatPanelExpandedWidthPixels(normalizedMode);
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

    /// <summary>Нормализует id к каноническому виду из индекса; неизвестный режим → Balanced.</summary>
    public static string NormalizeUiMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "Balanced";

        lock (Gate)
        {
            foreach (var id in _orderedModeIds)
            {
                if (string.Equals(id, mode, StringComparison.OrdinalIgnoreCase))
                    return id;
            }
        }

        return "Balanced";
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

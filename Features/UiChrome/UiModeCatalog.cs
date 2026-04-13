using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CascadeIDE.Services;

namespace CascadeIDE.Features.UiChrome;

/// <summary>Откуда взялся текущий список UI-режимов (для диагностики MCP и деплоя).</summary>
public enum UiModesBundleSource
{
    /// <summary>До <see cref="UiModeCatalog.Initialize"/> или после <see cref="UiModeCatalog.ResetForTests"/>.</summary>
    Unknown,
    /// <summary>Встроенный <see cref="UiModeLayoutRegistry"/> (нет TOML-бандла или ошибка загрузки).</summary>
    BuiltinRegistry,
    /// <summary>Успешно загружен <c>UiModes/index.toml</c> и режимы из каталога.</summary>
    TomlBundle,
}

/// <summary>
/// Загруженные из <c>UiModes/*.toml</c> режимы (ADR 0010): сначала файлы в каталоге (или override), иначе те же пути как встроенные ресурсы сборки. При ошибке или полном отсутствии данных — встроенный <see cref="UiModeLayoutRegistry"/>.
/// </summary>
public static class UiModeCatalog
{
    private static readonly object Gate = new();
    private static bool _initialized;
    private static UiModesBundleSource _bundleSource;
    private static IReadOnlyList<string> _orderedModeIds = UiModeLayoutRegistry.OrderedModeIds;
    private static readonly Dictionary<string, UiModeLayoutSpec> Specs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeFamily> Families = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> ChatExpandedWidths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> ShowTaskBarByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeCapabilities> CapabilitiesByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string?> WindowTitleOverrideByMode = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Снимок <c>UiModes/workspace.toml</c> из бандла для merge с репозиторием (ADR 0021 §2.1).</summary>
    private static UiWorkspaceToml? _bundleWorkspaceToml;

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

    /// <summary>Источник текущего списка режимов (TOML-бандл или встроенный fallback).</summary>
    public static UiModesBundleSource ActiveBundleSource
    {
        get
        {
            lock (Gate)
                return _bundleSource;
        }
    }

    /// <summary>
    /// JSON для MCP: пути к <c>UiModes</c>, наличие <c>index.toml</c>/<c>Flight.toml</c>, <see cref="ActiveBundleSource"/>, список id в меню (почему может не быть Flight).
    /// </summary>
    public static string GetDiagnosticsJson()
    {
        var baseDir = AppContext.BaseDirectory;
        var uiModesDir = Path.Combine(baseDir, "UiModes");
        var indexPath = Path.Combine(uiModesDir, "index.toml");
        var flightPath = Path.Combine(uiModesDir, "Flight.toml");

        bool initialized;
        UiModesBundleSource src;
        string[] ordered;
        string[] builtinIds;
        lock (Gate)
        {
            initialized = _initialized;
            src = _bundleSource;
            ordered = _orderedModeIds.ToArray();
            builtinIds = UiModeLayoutRegistry.OrderedModeIds.ToArray();
        }

        var flightInMenu = ordered.Any(static x => string.Equals(x, "Flight", StringComparison.OrdinalIgnoreCase));
        string? hint = null;
        if (!flightInMenu)
        {
            hint = src switch
            {
                UiModesBundleSource.BuiltinRegistry =>
                    "Режимы из встроенного списка (Flight нет). Проверь папку UiModes рядом с exe и корректность index.toml.",
                UiModesBundleSource.TomlBundle =>
                    "Flight нет в списке modes в index.toml (или файл Flight.toml не используется для id).",
                _ =>
                    initialized
                        ? "Источник бандла неизвестен; сравни ordered_mode_ids с builtin_registry_fallback_ids."
                        : "Каталог режимов ещё не инициализирован.",
            };
        }

        return JsonSerializer.Serialize(new
        {
            app_base_directory = baseDir,
            ui_modes_directory = uiModesDir,
            ui_modes_directory_exists = Directory.Exists(uiModesDir),
            index_toml_path = indexPath,
            index_toml_exists = File.Exists(indexPath),
            flight_toml_exists = File.Exists(flightPath),
            ui_mode_catalog_initialized = initialized,
            bundle_source = src.ToString(),
            ordered_mode_ids = ordered,
            builtin_registry_fallback_ids = builtinIds,
            flight_listed_in_menu = flightInMenu,
            hint,
        });
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
            _bundleWorkspaceToml = null;
            _bundleSource = UiModesBundleSource.Unknown;
            UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();
            AttentionZonePanelRuntime.ResetToCodeDefaults();
            MarkdownPreviewPlacementRuntime.ResetToCodeDefaults();
        }
    }

    /// <summary>
    /// Накладывает <c>.cascade/workspace.toml</c> из корня открытого решения на метрики и <c>attention_zone_panels</c> бандла.
    /// Вызывать с UI-потока при смене <see cref="SolutionWorkspaceViewModel.SolutionPath"/>; при пустом пути — только бандл.
    /// </summary>
    public static void ApplyRepositoryWorkspaceOverlay(string? solutionDirectory)
    {
        lock (Gate)
        {
            if (!_initialized)
                return;

            UiWorkspaceToml? repo = null;
            if (!string.IsNullOrWhiteSpace(solutionDirectory))
            {
                var trimmed = solutionDirectory.Trim();
                var path = Path.Combine(trimmed, ".cascade", "workspace.toml");
                if (File.Exists(path))
                {
                    try
                    {
                        repo = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(File.ReadAllText(path));
                    }
                    catch (Exception ex)
                    {
                        global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: repo workspace.toml ignored — {ex.Message}");
                    }
                }
            }

            var merged = UiWorkspaceTomlMerger.Merge(_bundleWorkspaceToml, repo);
            UiWorkspaceLayoutRuntimeMetrics.ApplyWorkspaceToml(merged);
            AttentionZonePanelRuntime.ApplyWorkspaceToml(merged);
            MarkdownPreviewPlacementRuntime.ApplyWorkspaceToml(merged);
        }
    }

    /// <summary>Сначала файл в <paramref name="uiModesDirectory"/>, иначе встроенный ресурс <c>UiModes/…</c>.</summary>
    private static bool TryReadUiModesFile(string uiModesDirectory, string fileName, [NotNullWhen(true)] out string? text)
    {
        text = null;
        var disk = Path.Combine(uiModesDirectory, fileName);
        try
        {
            if (File.Exists(disk))
            {
                text = File.ReadAllText(disk);
                return true;
            }
        }
        catch
        {
            // fallback на ресурс
        }

        var bundledRel = $"UiModes/{fileName.Replace('\\', '/')}";
        return BundledAppContent.TryReadEmbeddedText(bundledRel, out text);
    }

    private static void LoadFromDirectory(string uiModesDirectory)
    {
        Specs.Clear();
        Families.Clear();
        ChatExpandedWidths.Clear();
        ShowTaskBarByMode.Clear();
        CapabilitiesByMode.Clear();
        WindowTitleOverrideByMode.Clear();
        _bundleWorkspaceToml = null;
        UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();
        AttentionZonePanelRuntime.ResetToCodeDefaults();
        MarkdownPreviewPlacementRuntime.ResetToCodeDefaults();

        if (!TryReadUiModesFile(uiModesDirectory, "index.toml", out var indexTomlText))
        {
            global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: index.toml missing — {Path.Combine(uiModesDirectory, "index.toml")}");
            ApplyBuiltinOnly();
            return;
        }

        UiModesIndexToml? index;
        try
        {
            index = CascadeTomlSerializer.Deserialize<UiModesIndexToml>(indexTomlText);
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

        if (TryReadUiModesFile(uiModesDirectory, "workspace.toml", out var workspaceTomlText))
        {
            try
            {
                var w = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(workspaceTomlText);
                _bundleWorkspaceToml = w;
                UiWorkspaceLayoutRuntimeMetrics.ApplyWorkspaceToml(w);
                AttentionZonePanelRuntime.ApplyWorkspaceToml(w);
                MarkdownPreviewPlacementRuntime.ApplyWorkspaceToml(w);
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

        _bundleSource = UiModesBundleSource.TomlBundle;
    }

    private static void ApplyBuiltinOnly()
    {
        _bundleWorkspaceToml = null;
        _bundleSource = UiModesBundleSource.BuiltinRegistry;
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

    private static bool DefaultShowTaskBarForFamily(UiModeFamily family) =>
        !family.IsDebugFamily() && !family.IsEditorFamily();

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

    private static WorkspaceHealthUiSurface ResolveWorkspaceHealthSurface(string? fromFile, WorkspaceHealthUiSurface inherited)
    {
        if (string.IsNullOrWhiteSpace(fromFile))
            return inherited;
        var v = fromFile.Trim();
        if (string.Equals(v, "dedicated_page", StringComparison.OrdinalIgnoreCase))
            return WorkspaceHealthUiSurface.DedicatedPage;
        if (string.Equals(v, "bottom_strip", StringComparison.OrdinalIgnoreCase))
            return WorkspaceHealthUiSurface.BottomStrip;
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

        var span = baseCaps.WorkspaceHealthMainColumnSpan;
        if (modeFile.WorkspaceHealthMainColumnSpan is { } s && s >= 1 && s <= 12)
            span = s;

        var surface = ResolveWorkspaceHealthSurface(modeFile.WorkspaceHealthSurface, baseCaps.WorkspaceHealthSurface);

        return new UiModeCapabilities(
            QuickActions: modeFile.QuickActions ?? baseCaps.QuickActions,
            AgentOperationsPanel: modeFile.AgentOperationsPanel ?? baseCaps.AgentOperationsPanel,
            AgentTrace: modeFile.AgentTrace ?? baseCaps.AgentTrace,
            AutonomousAgentTelemetry: modeFile.AutonomousAgentTelemetry ?? baseCaps.AutonomousAgentTelemetry,
            WorkspaceHealthOnTerminalTab: modeFile.WorkspaceHealthOnTerminalTab
                ?? baseCaps.WorkspaceHealthOnTerminalTab,
            WorkspaceHealthMainColumnSpan: span,
            InstrumentationTabs: modeFile.InstrumentationTabs ?? baseCaps.InstrumentationTabs,
            HypothesesTab: modeFile.HypothesesTab ?? baseCaps.HypothesesTab,
            RiskSummaryCard: modeFile.RiskSummaryCard ?? baseCaps.RiskSummaryCard,
            ResultSummaryCard: modeFile.ResultSummaryCard ?? baseCaps.ResultSummaryCard,
            WorkspaceHealthStripVisible: modeFile.WorkspaceHealthStrip ?? baseCaps.WorkspaceHealthStripVisible,
            WorkspaceHealthSurface: surface,
            MainToolbarVisible: modeFile.MainToolbar ?? baseCaps.MainToolbarVisible,
            ProblemsPanelVisible: modeFile.ProblemsPanel ?? baseCaps.ProblemsPanelVisible,
            EicasAlertsBarEnabled: modeFile.EicasAlertsBar ?? baseCaps.EicasAlertsBarEnabled);
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
                ?? baseSpec.SelectTerminalTabWhenTerminalShown,
            InstrumentationDockVisible: o.InstrumentationDockVisible ?? baseSpec.InstrumentationDockVisible);
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

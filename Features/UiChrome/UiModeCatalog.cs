using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome.Application;

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
public static partial class UiModeCatalog
{
    private static readonly object Gate = new();
    private static bool _initialized;
    private static UiModesBundleSource _bundleSource;
    private static IReadOnlyList<string> _orderedModeIds = UiModeLayoutRegistry.OrderedModeIds;
    private static readonly Dictionary<string, UiModeLayoutSpec> Specs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeFamily> Families = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> MfdRegionExpandedWidths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, bool> ShowTaskBarByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, UiModeCapabilities> CapabilitiesByMode = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string?> WindowTitleOverrideByMode = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Снимок <c>UiModes/workspace.toml</c> из бандла для merge с репозиторием (ADR 0021 §2.1).</summary>
    private static Features.Workspace.RepositoryWorkspaceToml? _bundleWorkspaceToml;

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
            MfdRegionExpandedWidths.Clear();
            ShowTaskBarByMode.Clear();
            CapabilitiesByMode.Clear();
            WindowTitleOverrideByMode.Clear();
            _bundleWorkspaceToml = null;
            _bundleSource = UiModesBundleSource.Unknown;
            UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();
            AttentionZonePanelRuntime.ResetToCodeDefaults();
            MarkdownPreviewPlacementRuntime.ResetToCodeDefaults();
            LocLimitsRuntime.ResetToCodeDefaults();
            InstrumentPlacementRuntime.ResetToCodeDefaults();
        }
    }

    /// <summary>
    /// Накладывает <c>.cascade/workspace.toml</c> из корня открытого решения на метрики и <c>routing</c> бандла.
    /// Вызывать с UI-потока при смене <see cref="SolutionWorkspaceViewModel.SolutionPath"/>; при пустом пути — только бандл.
    /// </summary>
    public static void ApplyRepositoryWorkspaceTomlOverlay(string? solutionDirectory)
    {
        lock (Gate)
        {
            if (!_initialized)
                return;

            RepositoryWorkspaceTomlOverlayApplicator.Apply(_bundleWorkspaceToml, solutionDirectory);
        }
    }

    /// <summary>Сначала файл в <paramref name="uiModesDirectory"/> (опциональный override), иначе <c>EmbeddedResource</c> в сборке — без папки <c>UiModes</c> рядом с exe достаточно манифеста.</summary>
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
        if (BundledAppContent.TryReadEmbeddedText(bundledRel, out text))
            return true;

        // Как шипнутый Content рядом с exe: при устаревшей копии CascadeIDE.dll без EmbeddedResource (напр. только main пересобран).
        var shipped = Path.Combine(AppContext.BaseDirectory, "UiModes", fileName);
        try
        {
            if (File.Exists(shipped))
            {
                text = File.ReadAllText(shipped);
                return !string.IsNullOrWhiteSpace(text);
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static void LoadFromDirectory(string uiModesDirectory)
    {
        Specs.Clear();
        Families.Clear();
        MfdRegionExpandedWidths.Clear();
        ShowTaskBarByMode.Clear();
        CapabilitiesByMode.Clear();
        WindowTitleOverrideByMode.Clear();
        _bundleWorkspaceToml = null;
        UiWorkspaceLayoutRuntimeMetrics.ResetToCodeDefaults();
        AttentionZonePanelRuntime.ResetToCodeDefaults();
        MarkdownPreviewPlacementRuntime.ResetToCodeDefaults();
        LocLimitsRuntime.ResetToCodeDefaults();
        InstrumentPlacementRuntime.ResetToCodeDefaults();

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

        if (index?.Bundle is null || index.Bundle.SchemaVersion < 1 || index.Bundle.Modes is null || index.Bundle.Modes.Count == 0)
        {
            global::System.Diagnostics.Debug.WriteLine("UiModeCatalog: index invalid or empty modes");
            ApplyBuiltinOnly();
            return;
        }

        if (TryReadUiModesFile(uiModesDirectory, "workspace.toml", out var workspaceTomlText))
        {
            try
            {
                var w = CascadeTomlSerializer.Deserialize<Features.Workspace.RepositoryWorkspaceToml>(workspaceTomlText);
                _bundleWorkspaceToml = w;
                UiWorkspaceLayoutRuntimeMetrics.ApplyWorkspaceToml(w);
                AttentionZonePanelRuntime.ApplyWorkspaceToml(w);
                MarkdownPreviewPlacementRuntime.ApplyWorkspaceToml(w);
                LocLimitsRuntime.ApplyWorkspaceToml(w);
                InstrumentPlacementRuntime.ApplyWorkspaceInstrumentRouting(w?.Routing?.Instruments);
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"UiModeCatalog: workspace.toml ignored — {ex.Message}");
            }
        }

        _orderedModeIds = index.Bundle.Modes
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
            MfdRegionExpandedWidths[id] = r.MfdRegionExpandedWidthPx;
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
            MfdRegionExpandedWidths[id] = UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels(id);
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
        int MfdRegionExpandedWidthPx,
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
                UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels(id),
                DefaultShowTaskBarForFamily(fam),
                UiModeCapabilities.DefaultsForFamily(fam),
                null);
        }
    }

    /// <summary>
    /// Дефолты capabilities для семьи Editor до <see cref="Initialize"/> и вне словаря каталога.
    /// Источник — только <see cref="BundledAppContent.TryReadEmbeddedText"/> (EmbeddedResource в манифесте сборки);
    /// диск и <c>Content</c> копия рядом с exe <b>не</b> используются — работает и без шипнутой папки <c>UiModes/</c>.
    /// </summary>
    internal static bool TryGetEditorCapabilitiesFromEmbeddedResource([NotNullWhen(true)] out UiModeCapabilities? caps)
    {
        caps = null;
        if (!BundledAppContent.TryReadEmbeddedText("UiModes/Flight.toml", out var flightText)
            || !BundledAppContent.TryReadEmbeddedText("UiModes/Editor.toml", out var editorText))
            return false;

        UiModeFileToml? flightFile;
        UiModeFileToml? editorFile;
        try
        {
            flightFile = CascadeTomlSerializer.Deserialize<UiModeFileToml>(flightText);
            editorFile = CascadeTomlSerializer.Deserialize<UiModeFileToml>(editorText);
        }
        catch
        {
            return false;
        }

        if (editorFile is null
            || string.IsNullOrWhiteSpace(editorFile.Meta?.Inherits)
            || !string.Equals(editorFile.Meta.Inherits.Trim(), "Flight", StringComparison.OrdinalIgnoreCase))
            return false;

        var flightCaps = ResolveCapabilities(flightFile, null, null, UiModeFamily.Flight);
        var fakeFlightParent = new ResolvedMode(
            UiModeLayoutRegistry.Get("Flight"),
            UiModeFamily.Flight,
            UiModeLayoutRegistry.GetMfdRegionExpandedWidthPixels("Flight"),
            true,
            flightCaps,
            null);

        caps = ResolveCapabilities(editorFile, "Flight", fakeFlightParent, UiModeFamily.Editor);
        return true;
    }
}

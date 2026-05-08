using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Привязки окна настроек к <see cref="Models.CascadeIdeSettings.HybridIndex"/> (ADR 0106).
/// </summary>
public partial class MainWindowViewModel
{
    private static readonly IReadOnlyList<string> HciScopeModeUiChoices = ["workspace+solution", "workspace"];

    /// <summary>Варианты <c>scope_mode</c> для ComboBox настроек (instance — для Avalonia binding).</summary>
    public IReadOnlyList<string> HciScopeModeUiOptions => HciScopeModeUiChoices;

    [ObservableProperty] private bool _hciIntegrationEnabled;
    [ObservableProperty] private string _hciIndexDir = "";
    [ObservableProperty] private int _hciDebounceMs = 750;
    [ObservableProperty] private bool _hciAutoReindexOnSolutionOpen = true;
    [ObservableProperty] private bool _hciWatchFiles = true;
    [ObservableProperty] private string _hciScopeMode = "workspace+solution";
    [ObservableProperty] private bool _hciPauseWhenMcpStdioHost;
}

namespace CascadeIDE.Features.UiChrome;

/// <summary>Продуктовая «семья» UI-режима: одна ось для веток в коде и привязок вместо набора <c>Is*Mode</c>.</summary>
public enum UiModeFamily
{
    Focus,
    /// <summary>Максимум места редактору: скрытый хром по capabilities; вызовы — палитра/MCP.</summary>
    Editor,
    Balanced,
    Power,
    AgentChat,
    Debug,
    /// <summary>Полигон для экспериментов с раскладкой; дефолты capabilities как у <see cref="Balanced"/>.</summary>
    Flight,
}

/// <summary>Соответствие стабильного id режима (после <see cref="UiChromeViewModel.NormalizeUiMode"/>) семейству.</summary>
public static class UiModeFamilyResolver
{
    public static UiModeFamily FromNormalizedMode(string normalizedMode) =>
        UiModeCatalog.GetFamily(normalizedMode);
}

/// <summary>Предикаты по значению <see cref="UiModeFamily"/>: одна точка для каждого семейства вместо размножения <c>== UiModeFamily.*</c> в VM.</summary>
public static class UiModeFamilyExtensions
{
    public static bool IsFocusFamily(this UiModeFamily family) =>
        family == UiModeFamily.Focus;

    public static bool IsEditorFamily(this UiModeFamily family) =>
        family == UiModeFamily.Editor;

    public static bool IsBalancedFamily(this UiModeFamily family) =>
        family == UiModeFamily.Balanced;

    public static bool IsPowerFamily(this UiModeFamily family) =>
        family == UiModeFamily.Power;

    public static bool IsAgentChatFamily(this UiModeFamily family) =>
        family == UiModeFamily.AgentChat;

    public static bool IsDebugFamily(this UiModeFamily family) =>
        family == UiModeFamily.Debug;

    public static bool IsFlightFamily(this UiModeFamily family) =>
        family == UiModeFamily.Flight;
}

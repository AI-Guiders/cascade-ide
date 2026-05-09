#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Пороги ширины области мини-карты на PFD (колбэк ширины viewport).</summary>
[ComputingUnit("policy-code-nav-viewport")]
public static class CodeNavigationMapViewportPolicy
{
    public const double IgnoreBelowWidth = 40;
    public const double MinClampedWidth = 80;
    public const double MaxClampedWidth = 2400;
    public const double ChangeEpsilon = 3;

    /// <summary>Вернуть <c>false</c>, если сообщение viewport игнорируем или ширина фактически не меняется.</summary>
    public static bool ShouldApplyMeasuredWidth(double measuredWidth, double currentWidth, out double clampedWidth)
    {
        clampedWidth = default;
        if (double.IsNaN(measuredWidth) || measuredWidth < IgnoreBelowWidth)
            return false;

        clampedWidth = Math.Clamp(measuredWidth, MinClampedWidth, MaxClampedWidth);
        if (Math.Abs(clampedWidth - currentWidth) < ChangeEpsilon)
            return false;

        return true;
    }
}

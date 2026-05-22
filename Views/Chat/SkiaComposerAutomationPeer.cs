#nullable enable
using Avalonia.Automation.Peers;

namespace CascadeIDE.Views;

/// <summary>Минимальный a11y-peer: значение composer для screen readers (ADR 0123).</summary>
internal sealed class SkiaComposerAutomationPeer(SkiaChatSurfaceControl owner) : ControlAutomationPeer(owner)
{
    private SkiaChatSurfaceControl SkiaOwner => (SkiaChatSurfaceControl)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Edit;

    protected override bool IsContentElementCore() => true;

    protected override string GetClassNameCore() => nameof(SkiaChatSurfaceControl);

    protected override string GetNameCore() =>
        string.IsNullOrWhiteSpace(SkiaOwner.ComposerPlaceholder)
            ? "Intercom composer"
            : SkiaOwner.ComposerPlaceholder;

    protected override string GetAutomationIdCore() => "intercom-composer";

    protected override bool IsKeyboardFocusableCore() => SkiaOwner.ShowIntercomComposer && SkiaOwner.IsComposerEnabled;

    protected override bool HasKeyboardFocusCore() =>
        SkiaOwner.IsKeyboardFocusWithin && !SkiaOwner.IsNavigatorSearchInputActive;
}

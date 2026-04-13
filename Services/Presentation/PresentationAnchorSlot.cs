namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Якорь зоны в строке <c>presentation</c> и опциональная доля ширины в группе (ADR 0017, веса внутри одного экрана).
/// <see cref="Weight"/> — <c>null</c>, если в группе режим «без коэффициентов» (равные доли); иначе положительное число, во всей группе сумма = 1.
/// </summary>
public readonly record struct PresentationAnchorSlot(PresentationAnchorKind Kind, double? Weight);

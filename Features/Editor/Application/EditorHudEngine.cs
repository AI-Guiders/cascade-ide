namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Политика и композиция Editor HUD (inline vs banner vs PFD/MFD) — ADR 0103.
/// v1: точка расширения; стабилизированный ввод сейчас ведёт в VM снаружи.
/// </summary>
public sealed class EditorHudEngine
{
    /// <summary>Зарезервировано для нормализованного слоя уведомлений (диагностики, hover) после подключения DAL.</summary>
    public void OnStabilizedInput(EditorInputDelta delta)
    {
    }
}

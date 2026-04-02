using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Build;

/// <summary>
/// Вкладка «Build output» нижней панели: текст вывода сборки и связанных операций.
/// </summary>
public partial class BuildOutputPanelViewModel : ViewModelBase
{
    /// <summary>
    /// Максимальный размер накопленного вывода (чтобы не ловить OOM на больших логах).
    /// </summary>
    public const int MaxChars = 250_000;

    private OutputAccumulator _acc = new(MaxChars);

    [ObservableProperty]
    private string _buildOutput = "";

    public void Clear()
    {
        _acc = new OutputAccumulator(MaxChars);
        BuildOutput = "";
    }

    public void Set(string text)
    {
        _acc = new OutputAccumulator(MaxChars);
        _acc.Append((text ?? "").AsSpan());
        BuildOutput = _acc.ToStringAndTrim();
    }

    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        _acc.Append(text.AsSpan());
        BuildOutput = _acc.ToStringAndTrim();
    }
}

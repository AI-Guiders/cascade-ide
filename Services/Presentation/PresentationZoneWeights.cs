namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Единая нормализация долей зон (P/F/M) для одного экрана <c>presentation</c>.
/// Источник истины для расчёта геометрии зон; UI-представления не должны пересчитывать доли локально.
/// </summary>
public static class PresentationZoneWeights
{
    private const double SumTolerance = 1e-9;

    /// <summary>
    /// Нормализованные доли зон для первого экрана (в порядке якорей в группе):
    /// - без коэффициентов: равные доли;
    /// - с коэффициентами: нормализация к сумме 1 (устойчиво к погрешности).
    /// </summary>
    public static bool TryGetFirstScreenWeights(
        PresentationParseResult parse,
        out IReadOnlyList<double> weights)
    {
        weights = Array.Empty<double>();
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return false;

        return TryNormalize(parse.Screens[0], out weights);
    }

    /// <summary>Нормализовать доли в группе якорей одного экрана.</summary>
    public static bool TryNormalize(
        IReadOnlyList<PresentationAnchorSlot> screen,
        out IReadOnlyList<double> weights)
    {
        weights = Array.Empty<double>();
        if (screen.Count == 0)
            return false;

        // Один якорь => одна зона на экране.
        if (screen.Count == 1)
        {
            weights = [1.0];
            return true;
        }

        var any = false;
        var all = true;
        for (var i = 0; i < screen.Count; i++)
        {
            var has = screen[i].Weight.HasValue;
            any |= has;
            all &= has;
        }

        if (any && !all)
            return false;

        if (!any)
        {
            var equal = 1.0 / screen.Count;
            var values = new double[screen.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = equal;
            weights = values;
            return true;
        }

        var sum = 0.0;
        for (var i = 0; i < screen.Count; i++)
        {
            var v = screen[i].Weight!.Value;
            if (v <= 0 || double.IsNaN(v) || double.IsInfinity(v))
                return false;
            sum += v;
        }

        if (sum <= 0 || double.IsNaN(sum) || double.IsInfinity(sum))
            return false;

        // При нормальной сумме ≈ 1 всё равно нормализуем, чтобы снять накопление погрешностей.
        var normalized = new double[screen.Count];
        for (var i = 0; i < screen.Count; i++)
            normalized[i] = screen[i].Weight!.Value / sum;

        // Защитный фикс хвоста: сумма ровно 1 в double-арифметике текущего кадра.
        var tail = 0.0;
        for (var i = 0; i < normalized.Length - 1; i++)
            tail += normalized[i];
        normalized[^1] = Math.Max(0, 1.0 - tail);

        // Если хвост "съехал" из-за аномалии, откатываемся на обычную нормализацию.
        var finalSum = 0.0;
        for (var i = 0; i < normalized.Length; i++)
            finalSum += normalized[i];
        if (Math.Abs(finalSum - 1.0) > SumTolerance)
            return false;

        weights = normalized;
        return true;
    }
}


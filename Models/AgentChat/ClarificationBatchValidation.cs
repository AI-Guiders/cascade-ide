using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CascadeIDE.Models.AgentChat;

/// <summary>Валидация доменной модели пакетов уточнений (ADR 0031) без UI.</summary>
public static class ClarificationBatchValidation
{
    /// <summary>Проверяет пакет и опционально ответы; при успехе <paramref name="error"/> — null.</summary>
    public static bool TryValidate(ClarificationBatch batch, ClarificationResponse? response, [NotNullWhen(false)] out string? error)
    {
        error = null;
        if (batch.Items.Count == 0)
        {
            error = "Пакет уточнений не содержит ни одного пункта.";
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < batch.Items.Count; i++)
        {
            var item = batch.Items[i];
            var id = (item.Id ?? "").Trim();
            if (id.Length == 0)
            {
                error = string.Format(CultureInfo.InvariantCulture, "Пункт [{0}]: пустой идентификатор.", i);
                return false;
            }

            if (!seen.Add(id))
            {
                error = string.Format(CultureInfo.InvariantCulture, "Дублируется идентификатор пункта «{0}».", id);
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.Prompt))
            {
                error = string.Format(CultureInfo.InvariantCulture, "Пункт «{0}»: пустой текст вопроса.", id);
                return false;
            }

            if (item.AnswerStyle == ClarificationAnswerStyle.SingleChoice)
            {
                if (item.ChoiceOptions is null || item.ChoiceOptions.Count == 0)
                {
                    error = string.Format(
                        CultureInfo.InvariantCulture,
                        "Пункт «{0}»: для SingleChoice нужен непустой список вариантов.",
                        id);
                    return false;
                }
            }
        }

        if (response is null)
        {
            return true;
        }

        if (response.BatchId != batch.Id)
        {
            error = "Идентификатор пакета в ответе не совпадает с пакетом.";
            return false;
        }

        var answers = response.AnswersByItemId;
        var trimmedKeyToOriginal = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in answers)
        {
            var trimmed = (kv.Key ?? "").Trim();
            if (trimmed.Length == 0)
            {
                error = "В ответе есть пункт с пустым ключом (после trim).";
                return false;
            }

            if (!trimmedKeyToOriginal.TryAdd(trimmed, kv.Key ?? trimmed))
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "В ответе дублируется ключ пункта после нормализации (trim): «{0}».",
                    trimmed);
                return false;
            }
        }

        foreach (var item in batch.Items)
        {
            var id = item.Id.Trim();
            if (!trimmedKeyToOriginal.TryGetValue(id, out _))
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "Нет непустого ответа для пункта «{0}».",
                    id);
                return false;
            }

            var text = answers[trimmedKeyToOriginal[id]];
            if (string.IsNullOrWhiteSpace(text))
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "Нет непустого ответа для пункта «{0}».",
                    id);
                return false;
            }

            if (item.AnswerStyle == ClarificationAnswerStyle.YesNo)
            {
                var t = text.Trim();
                if (!IsYesNoToken(t))
                {
                    error = string.Format(
                        CultureInfo.InvariantCulture,
                        "Пункт «{0}»: ожидался ответ да/нет (получено: «{1}»).",
                        id,
                        text);
                    return false;
                }
            }

            if (item.AnswerStyle == ClarificationAnswerStyle.SingleChoice && item.ChoiceOptions is not null)
            {
                var t = text.Trim();
                if (!item.ChoiceOptions.Contains(t, StringComparer.Ordinal))
                {
                    error = string.Format(
                        CultureInfo.InvariantCulture,
                        "Пункт «{0}»: ответ не входит в список вариантов.",
                        id);
                    return false;
                }
            }
        }

        foreach (var trimmed in trimmedKeyToOriginal.Keys)
        {
            if (seen.Contains(trimmed))
                continue;
            error = string.Format(
                CultureInfo.InvariantCulture,
                "В ответе есть лишний ключ «{0}», отсутствующий в пакете.",
                trimmed);
            return false;
        }

        return true;
    }

    private static bool IsYesNoToken(string t)
    {
        if (t.Length == 0)
            return false;
        var lower = t.ToLowerInvariant();
        return lower is "да" or "нет" or "yes" or "no" or "y" or "n";
    }
}

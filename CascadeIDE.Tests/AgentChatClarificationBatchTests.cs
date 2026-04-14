using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentChatClarificationBatchTests
{
    [Fact]
    public void TryValidate_ValidBatch_NoResponse_Ok()
    {
        var id = Guid.NewGuid();
        var batch = new ClarificationBatch(
            id,
            [
                new ClarificationItem("a", "Первый?"),
                new ClarificationItem("b", "Второй?", ClarificationAnswerStyle.FreeText),
            ],
            "Тест");

        Assert.True(ClarificationBatchValidation.TryValidate(batch, null, out var err));
        Assert.Null(err);
    }

    [Fact]
    public void TryValidate_DuplicateItemId_Fails()
    {
        var batch = new ClarificationBatch(
            Guid.NewGuid(),
            [
                new ClarificationItem("x", "Один"),
                new ClarificationItem("x", "Другой"),
            ]);

        Assert.False(ClarificationBatchValidation.TryValidate(batch, null, out var err));
        Assert.Contains("Дублируется", err, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_CompleteResponse_Ok()
    {
        var id = Guid.NewGuid();
        var batch = new ClarificationBatch(
            id,
            [
                new ClarificationItem("q1", "Имя файла?"),
                new ClarificationItem("q2", "Ок?", ClarificationAnswerStyle.YesNo),
            ]);

        var response = new ClarificationResponse(
            id,
            new Dictionary<string, string> { ["q1"] = "foo.cs", ["q2"] = "да" });

        Assert.True(ClarificationBatchValidation.TryValidate(batch, response, out var err));
        Assert.Null(err);
    }

    [Fact]
    public void TryValidate_SingleChoice_InvalidAnswer_Fails()
    {
        var id = Guid.NewGuid();
        var batch = new ClarificationBatch(
            id,
            [
                new ClarificationItem(
                    "mode",
                    "Режим?",
                    ClarificationAnswerStyle.SingleChoice,
                    ["A", "B"]),
            ]);

        var response = new ClarificationResponse(id, new Dictionary<string, string> { ["mode"] = "C" });

        Assert.False(ClarificationBatchValidation.TryValidate(batch, response, out var err));
        Assert.Contains("не входит", err, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_ResponseKeysTrimmed_MatchesBatchIds()
    {
        var id = Guid.NewGuid();
        var batch = new ClarificationBatch(
            id,
            [new ClarificationItem("q1", "Текст?")]);

        var response = new ClarificationResponse(
            id,
            new Dictionary<string, string> { ["  q1  "] = "ответ" });

        Assert.True(ClarificationBatchValidation.TryValidate(batch, response, out var err));
        Assert.Null(err);
    }

    [Fact]
    public void Json_RoundTrip_Batch()
    {
        var id = Guid.NewGuid();
        var batch = new ClarificationBatch(
            id,
            [new ClarificationItem("only", "Вопрос?")],
            "Заголовок");

        var json = JsonSerializer.Serialize(batch);
        var back = JsonSerializer.Deserialize<ClarificationBatch>(json);
        Assert.NotNull(back);
        Assert.Equal(batch.Id, back.Id);
        Assert.Equal(batch.Title, back.Title);
        Assert.Single(back.Items);
        Assert.Equal("only", back.Items[0].Id);
    }
}

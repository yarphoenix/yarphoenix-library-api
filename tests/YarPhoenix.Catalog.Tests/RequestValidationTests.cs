using YarPhoenix.Ai.Contracts;
using YarPhoenix.Ai.Endpoints;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Shared;

namespace YarPhoenix.Catalog.Tests;

// K8: неполное или раздутое тело запроса — это 400, а не 500 и не счёт за токены.
public sealed class RequestValidationTests
{
    private static AskAssistantRequest Ask(
        string? selection = null,
        string? question = null,
        string? context = null,
        string? cfi = null) =>
        new(Guid.NewGuid(), "ask", selection, context, question, "en", cfi);

    [Fact]
    public void FindMissing_ReportsRequiredFieldLeftNull()
    {
        // Так выглядит запись после десериализации JSON без поля provider.
        var request = new StartReadingRequest(null!, "OL66554W");

        Assert.Equal(nameof(StartReadingRequest.Provider), RequiredFields.FindMissing(request));
    }

    [Fact]
    public void FindMissing_AcceptsCompleteRequest()
    {
        Assert.Null(RequiredFields.FindMissing(new StartReadingRequest("openlibrary", "OL66554W")));
    }

    [Fact]
    public void FindMissing_DoesNotRequireNullableFields()
    {
        Assert.Null(RequiredFields.FindMissing(new UpdateShelfRequest(null, null)));
        Assert.Null(RequiredFields.FindMissing(Ask()));
    }

    [Fact]
    public void FindMissing_IgnoresValueTypes()
    {
        // Числа и Guid при отсутствии в JSON получают значение по умолчанию — это не null.
        Assert.Null(RequiredFields.FindMissing(new ProgressRequest("epubcfi(/6/2)", 0, "UTC")));
    }

    [Fact]
    public void FindTooLongField_AcceptsOrdinaryQuestion()
    {
        Assert.Null(AssistantEndpoints.FindTooLongField(Ask(selection: "a passage", question: "Who is this?")));
    }

    [Theory]
    [InlineData("selection", 4001)]
    [InlineData("question", 1001)]
    [InlineData("context", 8001)]
    [InlineData("cfi", 1025)]
    public void FindTooLongField_NamesFieldOverItsLimit(string field, int length)
    {
        var text = new string('x', length);
        var request = field switch
        {
            "selection" => Ask(selection: text),
            "question" => Ask(question: text),
            "context" => Ask(context: text),
            _ => Ask(cfi: text),
        };

        Assert.Equal(field, AssistantEndpoints.FindTooLongField(request));
    }
}

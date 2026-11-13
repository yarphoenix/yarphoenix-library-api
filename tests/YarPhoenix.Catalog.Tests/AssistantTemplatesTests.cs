using YarPhoenix.Ai.Application;

namespace YarPhoenix.Catalog.Tests;

public sealed class AssistantTemplatesTests
{
    private const string Haiku = "claude-haiku-4-5";
    private const string Sonnet = "claude-sonnet-5";

    [Theory]
    [InlineData(AssistantTemplates.Meaning)]
    [InlineData(AssistantTemplates.Translate)]
    [InlineData(AssistantTemplates.Summarize)]
    public void CheapTemplates_RouteToHaiku(string template)
    {
        var ok = AssistantTemplates.TryBuildPlan(
            template, "выделенный текст", null, null, "ru", Haiku, Sonnet, out var plan);

        Assert.True(ok);
        Assert.Equal(Haiku, plan.Model);
        Assert.Contains("выделенный текст", plan.UserMessage);
    }

    [Fact]
    public void FreeQuestion_RoutesToSonnet()
    {
        var ok = AssistantTemplates.TryBuildPlan(
            AssistantTemplates.Ask, null, null, "Кто такой Пьер?", "ru", Haiku, Sonnet, out var plan);

        Assert.True(ok);
        Assert.Equal(Sonnet, plan.Model);
        Assert.Contains("Кто такой Пьер?", plan.UserMessage);
    }

    [Fact]
    public void ContextIsIncluded_WhenProvided()
    {
        AssistantTemplates.TryBuildPlan(
            AssistantTemplates.Meaning, "слово", "окно контекста", null, "ru", Haiku, Sonnet, out var plan);

        Assert.Contains("окно контекста", plan.UserMessage);
    }

    [Theory]
    [InlineData("unknown", "sel", null)]
    [InlineData(AssistantTemplates.Meaning, null, null)]
    [InlineData(AssistantTemplates.Ask, null, null)]
    public void InvalidInput_IsRejected(string template, string? selection, string? question)
    {
        var ok = AssistantTemplates.TryBuildPlan(
            template, selection, null, question, "ru", Haiku, Sonnet, out _);

        Assert.False(ok);
    }

    [Fact]
    public void SystemPrompt_ContainsAntiSpoilerRules()
    {
        Assert.Contains("only the text", AssistantTemplates.SystemPrompt);
        Assert.Contains("Never speculate", AssistantTemplates.SystemPrompt);
    }

    [Theory]
    [InlineData(AssistantTemplates.Explain)]
    [InlineData(AssistantTemplates.AuthorMeans)]
    [InlineData(AssistantTemplates.Reference)]
    [InlineData(AssistantTemplates.Scene)]
    public void InterpretiveTemplates_RouteToSonnet(string template)
    {
        var ok = AssistantTemplates.TryBuildPlan(
            template, "выделенный текст", "прочитанный контекст", null, "ru", Haiku, Sonnet, out var plan);

        Assert.True(ok);
        Assert.Equal(Sonnet, plan.Model);
    }

    [Fact]
    public void Characters_RoutesToSonnet_WithoutSelection()
    {
        var ok = AssistantTemplates.TryBuildPlan(
            AssistantTemplates.Characters, null, "прочитанный контекст", null, "ru", Haiku, Sonnet, out var plan);

        Assert.True(ok);
        Assert.Equal(Sonnet, plan.Model);
    }

    [Theory]
    [InlineData(AssistantTemplates.Explain, true)]
    [InlineData(AssistantTemplates.Characters, true)]
    [InlineData(AssistantTemplates.Meaning, false)]
    [InlineData(AssistantTemplates.Translate, false)]
    public void RequiresReadContext_OnlyForInterpretiveTemplates(string template, bool expected)
    {
        Assert.Equal(expected, AssistantTemplates.RequiresReadContext(template));
    }

    [Fact]
    public void Reference_ForbidsLinkingToLaterPlot()
    {
        AssistantTemplates.TryBuildPlan(
            AssistantTemplates.Reference, "an allusion", null, null, "en", Haiku, Sonnet, out var plan);

        Assert.Contains("later in this book", plan.UserMessage);
    }
}

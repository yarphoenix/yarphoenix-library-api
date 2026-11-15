using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Tests;

public sealed class PublicDomainRuleTests
{
    private const int CurrentYear = 2026;

    [Theory]
    [InlineData(1817, true)]
    [InlineData(1951, true)]
    [InlineData(1952, false)]
    [InlineData(1990, false)]
    public void IsPublicDomain_Requires75YearsAfterDeath(int deathYear, bool expected)
    {
        Assert.Equal(expected, PublicDomainRule.IsPublicDomain([deathYear], "Some Author", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_ChecksEveryContributor()
    {
        // Автор умер давно, но переводчик — недавно: перевод ещё охраняется.
        int?[] deathYears = [1910, 1985];

        Assert.False(PublicDomainRule.IsPublicDomain(deathYears, "Leo Tolstoy", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_TreatsAncientAuthorsAsPublicDomain()
    {
        Assert.True(PublicDomainRule.IsPublicDomain([-19], "Virgil", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_SkipsUnknownDeathYear()
    {
        int?[] deathYears = [1900, null];

        Assert.False(PublicDomainRule.IsPublicDomain(deathYears, "Some Author", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_SkipsWorksWithoutKnownCreators()
    {
        Assert.False(PublicDomainRule.IsPublicDomain([], "Anonymous", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_LetsWhitelistOverrideUnknownDeathYear()
    {
        int?[] deathYears = [null];

        Assert.True(PublicDomainRule.IsPublicDomain(deathYears, "Jane Austen", CurrentYear));
    }

    [Fact]
    public void IsPublicDomain_WhitelistDoesNotOverrideRecentDeath()
    {
        // Классик в вайтлисте, но издание с недавно умершим переводчиком всё равно не проходит.
        int?[] deathYears = [1910, 1985];

        Assert.False(PublicDomainRule.IsPublicDomain(deathYears, "Leo Tolstoy", CurrentYear));
    }

    [Theory]
    [InlineData("Лев Николаевич Толстой", true)]
    [InlineData("Leo Tolstoy", true)]
    [InlineData("Tolstoy, Leo, graf, 1828-1910", true)]
    [InlineData("Fyodor Dostoevsky", true)]
    [InlineData("Jane Austen", true)]
    [InlineData("Arthur Conan Doyle", true)]
    [InlineData("Stephen King", false)]
    [InlineData("J. K. Rowling", false)]
    [InlineData("", false)]
    public void Whitelist_AllowsOnlyKnownClassics(string author, bool expected)
    {
        Assert.Equal(expected, PublicDomainWhitelist.IsAllowed(author));
    }
}

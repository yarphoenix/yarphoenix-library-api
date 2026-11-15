using YarPhoenix.Catalog.Infrastructure.Collection;

namespace YarPhoenix.Catalog.Tests;

public sealed class GutenbergCatalogTests
{
    // Формат официального фида pg_catalog.csv; заголовок второй книги — многострочный, как в жизни.
    private const string Feed = """
        Text#,Type,Issued,Title,Language,Authors,Subjects,LoCC,Bookshelves
        1342,Text,1998-06-01,Pride and Prejudice,en,"Austen, Jane, 1775-1817",Courtship -- Fiction,PR,Best Books Ever Listings
        2,Text,1972-12-01,"The United States Bill of Rights
        The Ten Original Amendments",en,United States,Civil rights,JK; KF,Politics
        20000,Sound,2006-01-01,Pride and Prejudice (audio),en,"Austen, Jane, 1775-1817",,PR,
        61,Text,1993-01-01,The Communist Manifesto,en,"Marx, Karl, 1818-1883; Engels, Friedrich, 1820-1895; Moore, Samuel, 1838-1911 [Translator]",Socialism,HX,
        """;

    private static GutenbergCatalog Parse(string feed) => GutenbergCatalog.Parse(new StringReader(feed));

    [Fact]
    public void Parse_KeepsOnlyTextEditions()
    {
        var catalog = Parse(Feed);

        Assert.Equal(3, catalog.Count);
        Assert.Null(catalog.Find(20000));
    }

    [Fact]
    public void Parse_ReadsQuotedFieldsWithCommasAndLineBreaks()
    {
        var edition = Parse(Feed).Find(2);

        Assert.NotNull(edition);
        Assert.StartsWith("The United States Bill of Rights", edition.Title);
        Assert.EndsWith("The Ten Original Amendments", edition.Title);
        Assert.Equal("en", edition.Language);
    }

    [Fact]
    public void Parse_ReadsEveryContributorWithDeathYear()
    {
        var edition = Parse(Feed).Find(61);

        Assert.NotNull(edition);
        Assert.Equal(["Marx, Karl", "Engels, Friedrich", "Moore, Samuel"], edition.People.Select(p => p.Name));
        Assert.Equal([1883, 1895, 1911], edition.People.Select(p => p.DeathYear));
    }

    [Fact]
    public void Parse_LeavesDeathYearUnknown_WhenAuthorHasNoDates()
    {
        var edition = Parse(Feed).Find(2);

        Assert.NotNull(edition);
        var person = Assert.Single(edition.People);
        Assert.Equal("United States", person.Name);
        Assert.Null(person.DeathYear);
    }

    [Fact]
    public void Parse_Throws_WhenExpectedColumnsAreMissing()
    {
        Assert.Throws<InvalidDataException>(() => Parse("Id,Name\n1,Something"));
    }

    [Fact]
    public void TryGetDeathYear_FindsPersonByNameInAnyWordOrder()
    {
        var catalog = Parse(Feed);

        Assert.True(catalog.TryGetDeathYear("Jane Austen", out var deathYear));
        Assert.Equal(1817, deathYear);
        Assert.False(catalog.TryGetDeathYear("Someone Else", out _));
    }

    [Theory]
    [InlineData("1775-1817", 1817)]
    [InlineData(" 1850?-1900?", 1900)]
    [InlineData("-1616", 1616)]
    [InlineData("70 BCE-19 BCE", -19)]
    [InlineData("active 1300-1350", 1350)]
    [InlineData("active 1920-1940", null)]
    [InlineData("1860-", null)]
    [InlineData("1900", null)]
    public void ParseDeathYear_ReadsLifeDates(string lifeDates, int? expected)
    {
        Assert.Equal(expected, GutenbergCatalog.ParseDeathYear(lifeDates));
    }

    [Theory]
    [InlineData("Wells, H. G. (Herbert George)", "H. G. Wells")]
    [InlineData("Brontë, Charlotte", "Charlotte Bronte")]
    [InlineData("Maude, Louise", "louise maude")]
    public void NameKey_IsTheSameForDifferentSpellingsOfOneName(string catalogName, string otherName)
    {
        Assert.Equal(GutenbergCatalog.NameKey(catalogName), GutenbergCatalog.NameKey(otherName));
    }
}

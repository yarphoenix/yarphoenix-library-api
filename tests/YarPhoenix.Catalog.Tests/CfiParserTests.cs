using YarPhoenix.Ai.Application;

namespace YarPhoenix.Catalog.Tests;

public sealed class CfiParserTests
{
    private static readonly string[] Chapters =
    [
        "Chapter one text about the beginning.",
        "Chapter two where the middle happens now.",
        "Chapter three with the ending and the twist.",
    ];

    [Fact]
    public void Resolve_UsesSpineIndexFromCfi()
    {
        // /6/4 → itemref шаг 4 → индекс 1 (вторая глава).
        var boundary = CfiParser.Resolve("epubcfi(/6/4[ch2]!/4/2/1:0)", null, Chapters);

        Assert.NotNull(boundary);
        Assert.Equal(1, boundary.Value.ChapterIndex);
        Assert.Equal(0, boundary.Value.Offset);
    }

    [Fact]
    public void Resolve_AnchorsToSelectionEnd()
    {
        var boundary = CfiParser.Resolve("epubcfi(/6/4[ch2]!/4)", "the middle", Chapters);

        Assert.NotNull(boundary);
        Assert.Equal(1, boundary.Value.ChapterIndex);
        // Граница — конец выделения внутри главы.
        var expected = Chapters[1].IndexOf("the middle", StringComparison.Ordinal) + "the middle".Length;
        Assert.Equal(expected, boundary.Value.Offset);
    }

    [Fact]
    public void Resolve_FallsBackToSelectionSearch_WhenCfiUnparseable()
    {
        var boundary = CfiParser.Resolve("garbage", "the beginning", Chapters);

        Assert.NotNull(boundary);
        Assert.Equal(0, boundary.Value.ChapterIndex);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoCfiAndNoSelection()
    {
        Assert.Null(CfiParser.Resolve(null, null, Chapters));
    }

    [Fact]
    public void BuildReadSoFar_NeverIncludesTextPastBoundary()
    {
        // Граница — середина второй главы; третьей главы (со «спойлером») быть не должно.
        var readSoFar = CfiParser.BuildReadSoFar(Chapters, chapterIndex: 1, offset: 10);

        Assert.Contains("Chapter one", readSoFar);
        Assert.Contains(Chapters[1][..10], readSoFar);
        Assert.DoesNotContain("twist", readSoFar);
        Assert.DoesNotContain("ending", readSoFar);
    }

    [Fact]
    public void BuildReadSoFar_ClampsOffsetBeyondChapterLength()
    {
        var readSoFar = CfiParser.BuildReadSoFar(Chapters, chapterIndex: 0, offset: 9999);

        Assert.Equal(Chapters[0], readSoFar);
    }
}

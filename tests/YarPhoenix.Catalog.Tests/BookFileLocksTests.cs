using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Tests;

public sealed class BookFileLocksTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task AcquireAsync_MakesSecondCallerWaitUntilFirstReleases()
    {
        var locks = new BookFileLocks();
        var first = await locks.AcquireAsync("openlibrary:OL1W", CancellationToken.None);

        var second = locks.AcquireAsync("openlibrary:OL1W", CancellationToken.None);
        Assert.False(second.IsCompleted);

        first.Dispose();
        using var acquired = await second.WaitAsync(Patience);
    }

    [Fact]
    public async Task AcquireAsync_DoesNotBlockOtherBooks()
    {
        var locks = new BookFileLocks();
        using var first = await locks.AcquireAsync("openlibrary:OL1W", CancellationToken.None);

        using var other = await locks.AcquireAsync("openlibrary:OL2W", CancellationToken.None).WaitAsync(Patience);
    }

    [Fact]
    public async Task AcquireAsync_StaysUsable_AfterWaiterGivesUp()
    {
        var locks = new BookFileLocks();
        var first = await locks.AcquireAsync("openlibrary:OL1W", CancellationToken.None);

        using var cancellation = new CancellationTokenSource();
        var waiter = locks.AcquireAsync("openlibrary:OL1W", cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        first.Dispose();
        using var next = await locks.AcquireAsync("openlibrary:OL1W", CancellationToken.None).WaitAsync(Patience);
    }
}

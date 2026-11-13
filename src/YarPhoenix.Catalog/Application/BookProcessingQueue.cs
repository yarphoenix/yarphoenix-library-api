using System.Threading.Channels;

namespace YarPhoenix.Catalog.Application;

// Очередь фоновой обработки загруженных EPUB (ТЗ §5.4): загрузка отвечает
// мгновенно, парсинг и матчинг выполняются воркером.
public sealed class BookProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid bookId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(bookId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

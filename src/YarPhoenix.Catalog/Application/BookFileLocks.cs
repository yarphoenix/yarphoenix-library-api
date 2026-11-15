namespace YarPhoenix.Catalog.Application;

// Блокировка по ключу книги в пределах процесса (монолит — один экземпляр, ТЗ §3). «Читать»
// по одной и той же книге выполняется по очереди: второй клик дожидается первого и видит уже
// скачанный файл, а не начинает вторую загрузку и не падает на уникальном ключе полки.
public sealed class BookFileLocks
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Entry> _entries = [];

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken)
    {
        Entry? entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out entry))
            {
                entry = new Entry();
                _entries[key] = entry;
            }

            entry.Users++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            Forget(key, entry);
            throw;
        }

        return new Releaser(this, key, entry);
    }

    // Запись живёт, пока блокировку кто-то держит или ждёт, — словарь не растёт с числом книг.
    private void Forget(string key, Entry entry)
    {
        lock (_gate)
        {
            if (--entry.Users == 0)
            {
                _entries.Remove(key);
            }
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int Users { get; set; }
    }

    private sealed class Releaser(BookFileLocks owner, string key, Entry entry) : IDisposable
    {
        private bool _released;

        public void Dispose()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            entry.Semaphore.Release();
            owner.Forget(key, entry);
        }
    }
}

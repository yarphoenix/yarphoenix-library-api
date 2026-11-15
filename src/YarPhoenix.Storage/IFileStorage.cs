namespace YarPhoenix.Storage;

// Контракт модуля Storage для остальных модулей монолита (ТЗ §5.3).
public interface IFileStorage
{
    // Кладёт файл в приватный бакет книг; возвращает FileId (ключ объекта).
    Task<string> UploadBookAsync(
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken);

    // Отдаёт поток книги по FileId (контролируемая раздача из приватного бакета).
    Task<Stream> OpenBookAsync(string fileId, CancellationToken cancellationToken);

    // Переливает книгу прямо в поток назначения, не собирая файл в памяти, — для отдачи читалке.
    Task CopyBookToAsync(string fileId, Stream destination, CancellationToken cancellationToken);

    // Удаляет файл книги (при удалении аккаунта — книги-сироты, ТЗ §5.2.1).
    Task DeleteBookAsync(string fileId, CancellationToken cancellationToken);

    // Кладёт аватар в публичный бакет; возвращает ключ объекта (новый на каждую
    // загрузку — старый ключ вызывающий удаляет, заодно обход кэша браузера).
    Task<string> UploadAvatarAsync(
        Guid userId,
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken);

    // Удаляет аватар из публичного бакета.
    Task DeleteAvatarAsync(string key, CancellationToken cancellationToken);

    // Публичный URL аватара по ключу (для ссылки в профиле и хедере).
    string BuildAvatarUrl(string key);
}

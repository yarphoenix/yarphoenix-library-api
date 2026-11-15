namespace YarPhoenix.Catalog.Domain;

// Состояние импорта «читаемой коллекции» — одна строка. Позволяет продолжить прерванный
// проход с той же страницы и не начинать новый раньше срока после перезапуска сервиса.
public sealed class CollectionImportState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    // Страница поиска Open Library, с которой продолжать текущий проход.
    public int NextPage { get; set; } = 1;

    // Когда завершился последний полный проход; null — коллекция ещё ни разу не собрана целиком.
    public DateTime? LastCompletedAtUtc { get; set; }
}

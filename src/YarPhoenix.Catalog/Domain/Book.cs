namespace YarPhoenix.Catalog.Domain;

// Одна сущность на оба «мира» (ТЗ §4.1): каталожная запись, загруженный файл
// или «золотой случай» — оба сразу. Переключатели — ExternalId/Provider и FileId.
public sealed class Book
{
    public Guid Id { get; set; }

    // Внешняя запись уникальна парой (Provider, ExternalId) — ТЗ §4.1.
    public BookProvider? Provider { get; set; }

    public string? ExternalId { get; set; }

    // Ссылка на файл в MinIO; заполнена → книгу можно читать (мир B).
    public string? FileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public int? CoverId { get; set; }

    public string? Description { get; set; }

    public string? Isbn { get; set; }

    public string? Language { get; set; }

    public int? FirstPublishYear { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Откуда берётся читаемый файл. У книги коллекции — источник, из которого файл лениво
    // скачивается по «Читать»; у загруженной пользователем — Upload.
    public ReadableSource ReadableSource { get; set; }

    // Идентификатор в источнике: slug Standard Ebooks ("jane-austen/pride-and-prejudice")
    // или номер книги Project Gutenberg ("1342").
    public string? SourceRef { get; set; }

    // Запасное издание Gutenberg для книги Standard Ebooks: адрес файла SE строится по шаблону
    // и может не сработать — тогда «Читать» берёт это издание.
    public int? FallbackGutenbergId { get; set; }

    // Темы Open Library, уже очищенные от машинных тегов.
    public string[] Subjects { get; set; } = [];

    // Ключи жанров каталога (те же, что принимает параметр genre).
    public string[] Genres { get; set; } = [];

    // Число изданий по Open Library — порядок выдачи коллекции.
    public int Popularity { get; set; }

    // Книга входит в «читаемую коллекцию»: есть проверенный источник текста, автор и обложка.
    public bool InCollection { get; set; }

    public bool IsReadable => FileId is not null;
}

public enum BookProvider
{
    OpenLibrary,
    InternetArchive,
}

public enum ReadableSource
{
    None,
    StandardEbooks,
    Gutenberg,
    Upload,
}

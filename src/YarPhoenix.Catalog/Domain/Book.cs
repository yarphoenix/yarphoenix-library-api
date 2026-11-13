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

    public bool IsReadable => FileId is not null;
}

public enum BookProvider
{
    OpenLibrary,
    InternetArchive,
}

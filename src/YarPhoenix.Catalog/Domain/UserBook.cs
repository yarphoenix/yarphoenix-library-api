namespace YarPhoenix.Catalog.Domain;

// Полки — не сущности, а состояние связи «пользователь ↔ книга» (ТЗ §4.2).
public sealed class UserBook
{
    public Guid UserId { get; set; }

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    public BookStatus Status { get; set; }

    // Отдельный флаг, не значение Status: книга может быть Read И избранной (§4.2).
    public bool IsFavorite { get; set; }

    // Позиция чтения EPUB (CFI) — хранится на сервере, синхронизируется между
    // устройствами и служит границей анти-спойлера для AI (§7.2).
    public string? CfiPosition { get; set; }

    // 0..100, проценты прочитанного.
    public double Progress { get; set; }

    public DateTime AddedAtUtc { get; set; }

    public DateTime? LastReadAtUtc { get; set; }
}

public enum BookStatus
{
    WantToRead,
    Reading,
    Read,
}

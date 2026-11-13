using System.Text.Json;
using System.Text.Json.Serialization;

namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

internal sealed class OpenLibrarySearchResponse
{
    [JsonPropertyName("numFound")]
    public int NumFound { get; set; }

    [JsonPropertyName("docs")]
    public List<OpenLibraryDoc> Docs { get; set; } = [];
}

internal sealed class OpenLibraryDoc
{
    // Формат: "/works/OL45883W".
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author_name")]
    public List<string>? AuthorNames { get; set; }

    [JsonPropertyName("cover_i")]
    public int? CoverId { get; set; }

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; set; }

    // Идентификаторы Internet Archive у работы — родной мост к PD-файлу (ТЗ §4.1).
    [JsonPropertyName("ia")]
    public List<string>? Ia { get; set; }

    // Открытый скан (не lending) — первичный сигнал PD-читаемости.
    [JsonPropertyName("public_scan_b")]
    public bool PublicScan { get; set; }

    // "public" | "borrowable" | "no_ebook" — подтверждение открытого доступа.
    [JsonPropertyName("ebook_access")]
    public string? EbookAccess { get; set; }
}

internal sealed class OpenLibraryWork
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    // Либо строка, либо объект {"type": "/type/text", "value": "..."} — особенность OL.
    [JsonPropertyName("description")]
    public JsonElement? Description { get; set; }

    [JsonPropertyName("covers")]
    public List<int>? Covers { get; set; }

    [JsonPropertyName("subjects")]
    public List<string>? Subjects { get; set; }

    [JsonPropertyName("first_publish_date")]
    public string? FirstPublishDate { get; set; }

    [JsonPropertyName("authors")]
    public List<OpenLibraryWorkAuthorRef>? Authors { get; set; }
}

internal sealed class OpenLibraryWorkAuthorRef
{
    [JsonPropertyName("author")]
    public OpenLibraryKeyRef? Author { get; set; }
}

internal sealed class OpenLibraryKeyRef
{
    // Формат: "/authors/OL23919A".
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}

internal sealed class OpenLibraryAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

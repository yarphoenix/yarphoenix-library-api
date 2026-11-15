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

    [JsonPropertyName("subject")]
    public List<string>? Subjects { get; set; }

    [JsonPropertyName("edition_count")]
    public int EditionCount { get; set; }

    // Номера книг Project Gutenberg у работы; у одной работы их бывает несколько
    // (переводы, тома, переиздания) и встречаются повторы.
    [JsonPropertyName("id_project_gutenberg")]
    public List<string>? GutenbergIds { get; set; }

    // Slug издания Standard Ebooks: "jane-austen/pride-and-prejudice".
    [JsonPropertyName("id_standard_ebooks")]
    public List<string>? StandardEbooksIds { get; set; }
}

internal sealed class OpenLibraryWork
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    // Либо строка, либо объект {"type": "/type/text", "value": "..."} — особенность OL.
    [JsonPropertyName("description")]
    public JsonElement? Description { get; set; }

    // Встречаются -1 и null на месте удалённых обложек.
    [JsonPropertyName("covers")]
    public List<int?>? Covers { get; set; }

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

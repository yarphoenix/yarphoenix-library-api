using System.Text.Json;
using System.Text.Json.Serialization;

namespace YarPhoenix.Catalog.Infrastructure.InternetArchive;

// Ответ Metadata API (archive.org/metadata/{id}): список файлов + блок metadata.
internal sealed class IaMetadataResponse
{
    [JsonPropertyName("files")]
    public List<IaFile>? Files { get; set; }

    [JsonPropertyName("metadata")]
    public IaMetadata? Metadata { get; set; }
}

internal sealed class IaMetadata
{
    // "true" у lending-книг с ограничением доступа (ТЗ §5.1.2).
    [JsonPropertyName("access-restricted-item")]
    public string? AccessRestrictedItem { get; set; }

    // Бывает и строкой, и массивом строк — коллекция inlibrary метит lending.
    [JsonPropertyName("collection")]
    public JsonElement? Collection { get; set; }
}

internal sealed class IaFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    // Размер приходит строкой ("1301467").
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    // "true" у файлов недоступных для скачивания (lending/DRM).
    [JsonPropertyName("private")]
    public string? Private { get; set; }
}

// Выбор открытого EPUB из files[] (ТЗ §5.1.2): не приватный и не LCP-вариант (DRM).
internal static class IaFileSelector
{
    public static IaFile? PickOpenEpub(IReadOnlyList<IaFile> files) =>
        files.FirstOrDefault(file =>
            file.Name.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)
            && !file.Name.Contains("_lcp", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.Private, "true", StringComparison.OrdinalIgnoreCase));
}

// Фильтр lending (ТЗ §5.1.2): берём только полностью открытые элементы.
internal static class IaLendingFilter
{
    public static bool IsRestricted(IaMetadataResponse metadata)
    {
        var meta = metadata.Metadata;
        if (meta is null)
        {
            // Нет блока metadata — считаем элемент недоступным (безопасный дефолт).
            return true;
        }

        if (string.Equals(meta.AccessRestrictedItem, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Коллекция inlibrary — это lending-каталог; printdisabled НЕ метит lending
        // (встречается и у открытых книг), поэтому его не проверяем.
        return CollectionContains(meta.Collection, "inlibrary");
    }

    private static bool CollectionContains(JsonElement? collection, string value)
    {
        if (collection is not { } element)
        {
            return false;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => string.Equals(element.GetString(), value, StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), value, StringComparison.OrdinalIgnoreCase)),
            _ => false,
        };
    }
}

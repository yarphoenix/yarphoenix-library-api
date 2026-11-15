using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure;
using YarPhoenix.Shared;

namespace YarPhoenix.Catalog.Application;

public sealed class CatalogService(IBookMetadataProvider provider, CatalogDbContext dbContext, HybridCache cache)
{
    private const string ProviderName = "openlibrary";

    // Больше слов в запросе — больше условий ILIKE; длинную фразу пользователь так не ищет.
    private const int MaxSearchWords = 5;

    // Пока OL доступен, кэш просто ускоряет ответы; при отказе провайдера
    // закэшированные запросы продолжают отдаваться (ТЗ §8.3), некэшированные
    // деградируют в Unavailable, а не в 500.
    private static readonly HybridCacheEntryOptions SearchCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromMinutes(10),
    };

    private static readonly HybridCacheEntryOptions DetailsCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromMinutes(30),
    };

    public Task<Result<BookListResponse>> SearchAsync(
        BookSearchQuery query,
        CancellationToken cancellationToken) =>
        query.Scope == CatalogScope.All
            ? SearchOpenLibraryAsync(query, cancellationToken)
            : SearchCollectionAsync(query, cancellationToken);

    public async Task<Result<BookDetailsResponse>> GetDetailsAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(
            b => b.Provider == BookProvider.OpenLibrary && b.ExternalId == externalId && b.InCollection,
            cancellationToken);

        if (book is not null)
        {
            return Result.Success(await BuildCollectionDetailsAsync(book, cancellationToken));
        }

        var key = $"catalog:details:{externalId}";
        try
        {
            var response = await cache.GetOrCreateAsync(
                key,
                async token => await provider.GetDetailsAsync(externalId, token),
                DetailsCacheOptions,
                cancellationToken: cancellationToken);
            return response is null
                ? Result.Failure<BookDetailsResponse>(Error.NotFound("Book was not found."))
                : Result.Success(response);
        }
        catch (Exception exception) when (ProviderFailure.Is(exception, cancellationToken))
        {
            return Result.Failure<BookDetailsResponse>(
                Error.Unavailable("Book catalog is temporarily unavailable."));
        }
    }

    // Витрина по умолчанию — читаемая коллекция из своей БД: каждую книгу на ней можно открыть,
    // а ответ не зависит от доступности Open Library. Язык интерфейса выдачу не фильтрует:
    // тексты коллекции английские при любом языке кнопок.
    private async Task<Result<BookListResponse>> SearchCollectionAsync(
        BookSearchQuery query,
        CancellationToken cancellationToken)
    {
        var books = dbContext.Books.AsNoTracking().Where(b => b.InCollection);

        if (!string.IsNullOrWhiteSpace(query.Genre))
        {
            var genre = query.Genre;
            books = books.Where(b => b.Genres.Contains(genre));
        }

        // Каждое слово должно встретиться в названии или в авторе: «austen pride» находит
        // «Pride and Prejudice» Джейн Остин.
        foreach (var word in SearchWords(query.Text))
        {
            var pattern = $"%{EscapeLike(word)}%";
            books = books.Where(b =>
                EF.Functions.ILike(b.Title, pattern, "\\") || EF.Functions.ILike(b.Author, pattern, "\\"));
        }

        var total = await books.CountAsync(cancellationToken);
        var items = await books
            .OrderByDescending(b => b.Popularity)
            .ThenBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BookSummaryResponse(
                ProviderName,
                b.ExternalId!,
                b.Title,
                b.Author,
                b.CoverId,
                b.FirstPublishYear,
                true))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)query.PageSize);
        return Result.Success(new BookListResponse(items, query.Page, query.PageSize, total, totalPages));
    }

    private async Task<Result<BookListResponse>> SearchOpenLibraryAsync(
        BookSearchQuery query,
        CancellationToken cancellationToken)
    {
        var key = $"catalog:search:{query.Page}:{query.PageSize}:{query.Text}:{query.Genre}:{query.Language}";
        try
        {
            var response = await cache.GetOrCreateAsync(
                key,
                async token => await provider.SearchAsync(query, token),
                SearchCacheOptions,
                cancellationToken: cancellationToken);
            return Result.Success(await MarkReadableAsync(response, cancellationToken));
        }
        catch (Exception exception) when (ProviderFailure.Is(exception, cancellationToken))
        {
            return Result.Failure<BookListResponse>(
                Error.Unavailable("Book catalog is temporarily unavailable."));
        }
    }

    // Читаемость — знание нашей БД, а не Open Library, поэтому накладывается поверх кэша:
    // книга, попавшая в коллекцию час назад, помечается сразу.
    private async Task<BookListResponse> MarkReadableAsync(
        BookListResponse response,
        CancellationToken cancellationToken)
    {
        var externalIds = response.Items.Select(item => item.ExternalId).ToList();
        if (externalIds.Count == 0)
        {
            return response;
        }

        var readable = await dbContext.Books
            .AsNoTracking()
            .Where(b => b.Provider == BookProvider.OpenLibrary
                && b.InCollection
                && externalIds.Contains(b.ExternalId!))
            .Select(b => b.ExternalId!)
            .ToListAsync(cancellationToken);

        if (readable.Count == 0)
        {
            return response;
        }

        var readableIds = readable.ToHashSet(StringComparer.Ordinal);
        return response with
        {
            Items = response.Items
                .Select(item => item with { IsReadable = readableIds.Contains(item.ExternalId) })
                .ToList(),
        };
    }

    // Книга коллекции отдаётся из своей БД. Описания в поисковой выдаче Open Library нет, поэтому
    // оно подтягивается при первом открытии страницы и сохраняется; пустая строка в БД значит
    // «уже спрашивали, описания нет».
    private async Task<BookDetailsResponse> BuildCollectionDetailsAsync(
        Book book,
        CancellationToken cancellationToken)
    {
        if (book.Description is null)
        {
            try
            {
                var details = await provider.GetDetailsAsync(book.ExternalId!, cancellationToken);
                book.Description = details?.Description ?? string.Empty;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (ProviderFailure.Is(exception, cancellationToken))
            {
                // Open Library недоступен — страница книги всё равно открывается, просто без описания.
            }
        }

        return new BookDetailsResponse(
            ProviderName,
            book.ExternalId!,
            book.Title,
            book.Author,
            book.CoverId,
            string.IsNullOrEmpty(book.Description) ? null : book.Description,
            book.FirstPublishYear,
            book.Subjects,
            HasReadableFile: book.FileId is not null,
            IsPublicDomain: true);
    }

    internal static IReadOnlyList<string> SearchWords(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(MaxSearchWords).ToList();

    // Пользовательский текст в шаблоне LIKE: «%» и «_» — обычные символы, а не подстановки.
    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

using Microsoft.Extensions.Caching.Hybrid;
using Polly.CircuitBreaker;
using Polly.Timeout;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Shared;

namespace YarPhoenix.Catalog.Application;

public sealed class CatalogService(IBookMetadataProvider provider, HybridCache cache)
{
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

    public async Task<Result<BookListResponse>> SearchAsync(
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
            return Result.Success(response);
        }
        catch (Exception exception) when (IsProviderFailure(exception, cancellationToken))
        {
            return Result.Failure<BookListResponse>(
                Error.Unavailable("Book catalog is temporarily unavailable."));
        }
    }

    public async Task<Result<BookDetailsResponse>> GetDetailsAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var key = $"catalog:details:{externalId}";
        try
        {
            var response = await cache.GetOrCreateAsync(
                key,
                async token => await BuildDetailsAsync(externalId, token),
                DetailsCacheOptions,
                cancellationToken: cancellationToken);
            return response is null
                ? Result.Failure<BookDetailsResponse>(Error.NotFound("Book was not found."))
                : Result.Success(response);
        }
        catch (Exception exception) when (IsProviderFailure(exception, cancellationToken))
        {
            return Result.Failure<BookDetailsResponse>(
                Error.Unavailable("Book catalog is temporarily unavailable."));
        }
    }

    // Детали + определение PD-читаемости одним кэш-фактором (ТЗ §4.1, §5.1.2):
    // OL сообщает про открытый скан, вайтлист — авторитетная проверка статуса.
    private async Task<BookDetailsResponse?> BuildDetailsAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var details = await provider.GetDetailsAsync(externalId, cancellationToken);
        if (details is null)
        {
            return null;
        }

        var candidate = await provider.FindPublicDomainCandidateAsync(externalId, cancellationToken);
        var isPublicDomain = candidate is not null && PublicDomainWhitelist.IsAllowed(candidate.Author);

        return details with { IsPublicDomain = isPublicDomain };
    }

    private static bool IsProviderFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or BrokenCircuitException or TimeoutRejectedException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}

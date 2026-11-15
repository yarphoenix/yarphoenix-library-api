using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace YarPhoenix.Catalog.Application;

internal static class ProviderFailure
{
    // Сбой внешнего источника, а не ошибка в нашем коде: сеть, разомкнутая цепь, таймаут попытки,
    // HTML-страница ошибки вместо JSON. Такие случаи — честный 503, а не 500 (ТЗ §8.3).
    public static bool Is(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or BrokenCircuitException or TimeoutRejectedException
            or IOException or JsonException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}

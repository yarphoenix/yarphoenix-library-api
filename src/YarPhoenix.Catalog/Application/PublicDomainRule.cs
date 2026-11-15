namespace YarPhoenix.Catalog.Application;

// Правило общественного достояния для читаемой коллекции. Срок охраны в РФ — 70 лет после смерти
// автора и ещё 4 года для работавших во время войны; берём 75 с запасом и считаем по всем
// создателям издания: переводчик и автор предисловия — тоже авторы.
public static class PublicDomainRule
{
    public const int YearsAfterDeath = 75;

    public static bool IsPublicDomain(IReadOnlyCollection<int?> deathYears, string? author, int currentYear)
    {
        var threshold = currentYear - YearsAfterDeath;
        if (deathYears.Any(year => year > threshold))
        {
            return false;
        }

        // Год смерти кого-то из создателей неизвестен — статус не доказан. Исключение — бесспорная
        // классика из вайтлиста: её статус проверен вручную.
        var allKnown = deathYears.Count > 0 && deathYears.All(year => year is not null);
        return allKnown || PublicDomainWhitelist.IsAllowed(author);
    }
}

using Microsoft.EntityFrameworkCore;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure;
using YarPhoenix.Shared;
using YarPhoenix.Storage;

namespace YarPhoenix.Catalog.Application;

public sealed class LibraryService(
    CatalogDbContext dbContext,
    IBookMetadataProvider metadataProvider,
    IPublicDomainSource publicDomainSource,
    IFileStorage fileStorage)
{
    public async Task<IReadOnlyList<LibraryBookResponse>> GetShelfAsync(
        Guid userId,
        string shelf,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UserBooks
            .AsNoTracking()
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId);

        query = shelf switch
        {
            "reading" => query.Where(ub => ub.Status == BookStatus.Reading),
            "want" => query.Where(ub => ub.Status == BookStatus.WantToRead),
            "read" => query.Where(ub => ub.Status == BookStatus.Read),
            "favorites" => query.Where(ub => ub.IsFavorite),
            _ => query,
        };

        var items = await query
            .OrderByDescending(ub => ub.LastReadAtUtc ?? ub.AddedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    // «Добавить в библиотеку» = закладка WantToRead; работает для любой книги,
    // включая каталожную без файла (ТЗ §4.4).
    public async Task<Result<LibraryBookResponse>> AddExternalAsync(
        Guid userId,
        AddToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(request.Provider, out var provider))
        {
            return Result.Failure<LibraryBookResponse>(Error.Validation("Unknown provider."));
        }

        var book = await dbContext.Books.SingleOrDefaultAsync(
            b => b.Provider == provider && b.ExternalId == request.ExternalId,
            cancellationToken);

        if (book is null)
        {
            var details = await metadataProvider.GetDetailsAsync(request.ExternalId, cancellationToken);
            if (details is null)
            {
                return Result.Failure<LibraryBookResponse>(Error.NotFound("Book was not found."));
            }

            book = new Book
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                ExternalId = request.ExternalId,
                Title = details.Title,
                Author = details.Author,
                CoverId = details.CoverId,
                Description = details.Description,
                FirstPublishYear = details.FirstPublishYear,
            };
            dbContext.Books.Add(book);
        }

        var userBook = await dbContext.UserBooks.SingleOrDefaultAsync(
            ub => ub.UserId == userId && ub.BookId == book.Id,
            cancellationToken);

        if (userBook is null)
        {
            userBook = new UserBook
            {
                UserId = userId,
                BookId = book.Id,
                Status = BookStatus.WantToRead,
                AddedAtUtc = DateTime.UtcNow,
            };
            dbContext.UserBooks.Add(userBook);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        userBook.Book = book;
        return Result.Success(ToResponse(userBook));
    }

    // «Читать» для PD-книги без файла (ТЗ §5.1.2, §4.4): ленивый ингест с Internet
    // Archive в «золотой случай» — та же строка Book получает FileId, Provider
    // остаётся OpenLibrary (родство провайдеров, §4.1). Скачиваем один раз: далее
    // FileId уже стоит и IA не дёргается.
    public async Task<Result<LibraryBookResponse>> EnsureReadableAsync(
        Guid userId,
        StartReadingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(request.Provider, out var provider))
        {
            return Result.Failure<LibraryBookResponse>(Error.Validation("Unknown provider."));
        }

        var book = await dbContext.Books.SingleOrDefaultAsync(
            b => b.Provider == provider && b.ExternalId == request.ExternalId,
            cancellationToken);

        if (book is null || book.FileId is null)
        {
            var ingest = await IngestPublicDomainFileAsync(request.ExternalId, cancellationToken);
            if (ingest.IsFailure)
            {
                return Result.Failure<LibraryBookResponse>(ingest.Error);
            }

            if (book is null)
            {
                var details = await metadataProvider.GetDetailsAsync(request.ExternalId, cancellationToken);
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Provider = provider,
                    ExternalId = request.ExternalId,
                    Title = details?.Title ?? string.Empty,
                    Author = details?.Author ?? string.Empty,
                    CoverId = details?.CoverId,
                    Description = details?.Description,
                    FirstPublishYear = details?.FirstPublishYear,
                };
                dbContext.Books.Add(book);
            }

            book.FileId = ingest.Value;
        }

        var userBook = await dbContext.UserBooks.SingleOrDefaultAsync(
            ub => ub.UserId == userId && ub.BookId == book.Id,
            cancellationToken);

        if (userBook is null)
        {
            userBook = new UserBook
            {
                UserId = userId,
                BookId = book.Id,
                AddedAtUtc = DateTime.UtcNow,
            };
            dbContext.UserBooks.Add(userBook);
        }

        // Открытие читалки = чтение (auto-add, §4.4); Read не понижаем до Reading.
        if (userBook.Status != BookStatus.Read)
        {
            userBook.Status = BookStatus.Reading;
        }

        userBook.LastReadAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        userBook.Book = book;
        return Result.Success(ToResponse(userBook));
    }

    // Резолвим ocaid по OL, применяем вайтлист (авторитетная проверка §5.1.2),
    // перебираем сканы (часть — lending или мусорные google-скан-EPUB) и первый
    // открытый EPUB кладём в MinIO. Возвращаем FileId.
    private async Task<Result<string>> IngestPublicDomainFileAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var candidate = await metadataProvider.FindPublicDomainCandidateAsync(externalId, cancellationToken);
        if (candidate is null || !PublicDomainWhitelist.IsAllowed(candidate.Author))
        {
            return Result.Failure<string>(Error.Validation("This book has no readable file yet."));
        }

        PublicDomainFile? file = null;
        foreach (var identifier in candidate.Identifiers.Take(5))
        {
            file = await publicDomainSource.ResolveFileAsync(identifier, cancellationToken);
            if (file is not null)
            {
                break;
            }
        }

        if (file is null)
        {
            return Result.Failure<string>(
                Error.Unavailable("The public-domain file is not available right now."));
        }

        await using var content = await publicDomainSource.DownloadAsync(
            file.Identifier, file.FileName, cancellationToken);

        var fileId = await fileStorage.UploadBookAsync(
            content, content.Length, "application/epub+zip", cancellationToken);

        return Result.Success(fileId);
    }

    public async Task<LibraryBookResponse?> LookupAsync(
        Guid userId,
        string providerName,
        string externalId,
        CancellationToken cancellationToken)
    {
        if (!TryParseProvider(providerName, out var provider))
        {
            return null;
        }

        var userBook = await dbContext.UserBooks
            .AsNoTracking()
            .Include(ub => ub.Book)
            .SingleOrDefaultAsync(
                ub => ub.UserId == userId
                    && ub.Book!.Provider == provider
                    && ub.Book.ExternalId == externalId,
                cancellationToken);

        return userBook is null ? null : ToResponse(userBook);
    }

    public async Task<Result<LibraryBookResponse>> UpdateShelfAsync(
        Guid userId,
        Guid bookId,
        UpdateShelfRequest request,
        CancellationToken cancellationToken)
    {
        var userBook = await dbContext.UserBooks
            .Include(ub => ub.Book)
            .SingleOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId, cancellationToken);

        if (userBook?.Book is null)
        {
            return Result.Failure<LibraryBookResponse>(Error.NotFound("Book is not in the library."));
        }

        if (request.Status is not null)
        {
            if (!TryParseStatus(request.Status, out var status))
            {
                return Result.Failure<LibraryBookResponse>(Error.Validation("Unknown status."));
            }

            // «Читать» доступно только при наличии файла: текст, а не полка,
            // открывает чтение и AI (ТЗ §4.4).
            if (status == BookStatus.Reading && !userBook.Book.IsReadable)
            {
                return Result.Failure<LibraryBookResponse>(
                    Error.Validation("This book has no readable file yet."));
            }

            userBook.Status = status;
        }

        if (request.IsFavorite is { } favorite)
        {
            userBook.IsFavorite = favorite;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(userBook));
    }

    public async Task<Result> RemoveAsync(Guid userId, Guid bookId, CancellationToken cancellationToken)
    {
        var removed = await dbContext.UserBooks
            .Where(ub => ub.UserId == userId && ub.BookId == bookId)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0 ? Result.Success() : Result.Failure(Error.NotFound("Book is not in the library."));
    }

    // Сохранение позиции — само по себе «Читать»: создаёт/переводит UserBook
    // в Reading (auto-add, ТЗ §4.4) и двигает стрик в той же транзакции (§4.5).
    public async Task<Result<LibraryBookResponse>> UpdateProgressAsync(
        Guid userId,
        Guid bookId,
        ProgressRequest request,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(b => b.Id == bookId, cancellationToken);
        if (book is null)
        {
            return Result.Failure<LibraryBookResponse>(Error.NotFound("Book was not found."));
        }

        if (!book.IsReadable)
        {
            return Result.Failure<LibraryBookResponse>(Error.Validation("This book has no readable file."));
        }

        var userBook = await dbContext.UserBooks.SingleOrDefaultAsync(
            ub => ub.UserId == userId && ub.BookId == bookId,
            cancellationToken);

        if (userBook is null)
        {
            userBook = new UserBook
            {
                UserId = userId,
                BookId = bookId,
                AddedAtUtc = DateTime.UtcNow,
            };
            dbContext.UserBooks.Add(userBook);
        }

        if (userBook.Status != BookStatus.Read)
        {
            userBook.Status = BookStatus.Reading;
        }

        userBook.CfiPosition = request.CfiPosition;
        userBook.Progress = Math.Clamp(request.Progress, 0, 100);
        userBook.LastReadAtUtc = DateTime.UtcNow;

        await AdvanceStreakAsync(userId, request.TimeZoneId, cancellationToken);

        // Один SaveChanges — прогресс, стрик и награды атомарны (§4.5).
        await dbContext.SaveChangesAsync(cancellationToken);

        userBook.Book = book;
        return Result.Success(ToResponse(userBook));
    }

    public async Task<LibraryStatsResponse> GetStatsAsync(
        Guid userId,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var shelves = await dbContext.UserBooks
            .AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .GroupBy(ub => ub.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var favorites = await dbContext.UserBooks
            .CountAsync(ub => ub.UserId == userId && ub.IsFavorite, cancellationToken);

        var profile = await dbContext.ReaderProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? new ReaderProfile { UserId = userId };

        var achievements = await dbContext.UserAchievements
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Threshold)
            .Select(a => a.Threshold)
            .ToListAsync(cancellationToken);

        var today = StreakLogic.TodayFor(timeZoneId);
        var count = (BookStatus status) => shelves.SingleOrDefault(s => s.Status == status)?.Count ?? 0;

        return new LibraryStatsResponse(
            shelves.Sum(s => s.Count),
            count(BookStatus.Reading),
            count(BookStatus.WantToRead),
            count(BookStatus.Read),
            favorites,
            StreakLogic.EffectiveStreak(profile, today),
            profile.LongestStreak,
            achievements);
    }

    // Загрузка EPUB: книга сразу читаема (FileId), метаданные пока — имя файла;
    // парсинг OPF и матчинг доуточнят их в фоне (ТЗ §5.4).
    public async Task<LibraryBookResponse> CreateUploadedBookAsync(
        Guid userId,
        string fileId,
        string fileName,
        CancellationToken cancellationToken)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            FileId = fileId,
            Title = Path.GetFileNameWithoutExtension(fileName),
        };

        var userBook = new UserBook
        {
            UserId = userId,
            BookId = book.Id,
            Status = BookStatus.WantToRead,
            AddedAtUtc = DateTime.UtcNow,
        };

        dbContext.Books.Add(book);
        dbContext.UserBooks.Add(userBook);
        await dbContext.SaveChangesAsync(cancellationToken);

        userBook.Book = book;
        return ToResponse(userBook);
    }

    public async Task<Result<LibraryBookResponse>> GetOneAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var userBook = await dbContext.UserBooks
            .AsNoTracking()
            .Include(ub => ub.Book)
            .SingleOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId, cancellationToken);

        return userBook?.Book is null
            ? Result.Failure<LibraryBookResponse>(Error.NotFound("Book is not in the library."))
            : Result.Success(ToResponse(userBook));
    }

    // FileId для контролируемой раздачи: книга должна быть на полке пользователя.
    public async Task<Result<string>> GetFileIdAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var userBook = await dbContext.UserBooks
            .AsNoTracking()
            .Include(ub => ub.Book)
            .SingleOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId, cancellationToken);

        if (userBook?.Book is null)
        {
            return Result.Failure<string>(Error.NotFound("Book is not in the library."));
        }

        return userBook.Book.FileId is { } fileId
            ? Result.Success(fileId)
            : Result.Failure<string>(Error.Validation("This book has no readable file."));
    }

    private async Task AdvanceStreakAsync(Guid userId, string timeZoneId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.ReaderProfiles
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new ReaderProfile { UserId = userId };
            dbContext.ReaderProfiles.Add(profile);
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            profile.TimeZoneId = timeZoneId;
        }

        StreakLogic.Advance(profile, StreakLogic.TodayFor(profile.TimeZoneId));

        // Идемпотентная выдача порогов: дубликаты отсекает и проверка, и PK.
        var unlocked = await dbContext.UserAchievements
            .Where(a => a.UserId == userId)
            .Select(a => a.Threshold)
            .ToListAsync(cancellationToken);

        foreach (var threshold in UserAchievement.StreakThresholds)
        {
            if (profile.CurrentStreak >= threshold && !unlocked.Contains(threshold))
            {
                dbContext.UserAchievements.Add(new UserAchievement
                {
                    UserId = userId,
                    Threshold = threshold,
                    UnlockedAtUtc = DateTime.UtcNow,
                });
            }
        }
    }

    private static LibraryBookResponse ToResponse(UserBook userBook)
    {
        var book = userBook.Book!;
        return new LibraryBookResponse(
            book.Id,
            book.Title,
            book.Author,
            book.CoverId,
            book.Provider?.ToString().ToLowerInvariant(),
            book.ExternalId,
            book.IsReadable,
            userBook.Status switch
            {
                BookStatus.WantToRead => "wantToRead",
                BookStatus.Reading => "reading",
                _ => "read",
            },
            userBook.IsFavorite,
            userBook.Progress,
            userBook.CfiPosition,
            userBook.AddedAtUtc,
            userBook.LastReadAtUtc);
    }

    private static bool TryParseProvider(string value, out BookProvider provider)
    {
        switch (value.ToLowerInvariant())
        {
            case "openlibrary":
                provider = BookProvider.OpenLibrary;
                return true;
            case "internetarchive":
                provider = BookProvider.InternetArchive;
                return true;
            default:
                provider = default;
                return false;
        }
    }

    private static bool TryParseStatus(string value, out BookStatus status)
    {
        switch (value)
        {
            case "wantToRead":
                status = BookStatus.WantToRead;
                return true;
            case "reading":
                status = BookStatus.Reading;
                return true;
            case "read":
                status = BookStatus.Read;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

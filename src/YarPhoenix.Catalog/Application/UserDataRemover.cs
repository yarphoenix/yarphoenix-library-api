using Microsoft.EntityFrameworkCore;
using YarPhoenix.Catalog.Infrastructure;
using YarPhoenix.Shared;
using YarPhoenix.Storage;

namespace YarPhoenix.Catalog.Application;

// Жизненный цикл данных при удалении аккаунта (ТЗ §5.2.1): полки, стрик, достижения —
// всегда; загруженные книги-сироты (свой файл, нет каталожной записи и больше нет
// чужих полок) — удаляем вместе с файлом в MinIO. «Золотые» (есть ExternalId) не трогаем:
// каталожная запись общая.
public sealed class UserDataRemover(CatalogDbContext dbContext, IFileStorage fileStorage) : IUserDataRemover
{
    public async Task RemoveUserDataAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Кандидаты в сироты фиксируем ДО удаления полок: загруженные книги (свой
        // файл, нет каталожной записи). Всё set-based, без трекера — иначе каскад по
        // отслеживаемому графу пытается удалить уже удалённые полки (concurrency).
        var candidates = await dbContext.UserBooks
            .AsNoTracking()
            .Where(ub => ub.UserId == userId
                && ub.Book!.ExternalId == null
                && ub.Book.FileId != null)
            .Select(ub => new { ub.BookId, ub.Book!.FileId })
            .ToListAsync(cancellationToken);

        await dbContext.UserBooks
            .Where(ub => ub.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ReaderProfiles
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.UserAchievements
            .Where(a => a.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            // Осталась чужая полка на эту книгу — не сирота, файл общий.
            var stillReferenced = await dbContext.UserBooks
                .AnyAsync(ub => ub.BookId == candidate.BookId, cancellationToken);
            if (stillReferenced)
            {
                continue;
            }

            await dbContext.Books
                .Where(b => b.Id == candidate.BookId)
                .ExecuteDeleteAsync(cancellationToken);
            await fileStorage.DeleteBookAsync(candidate.FileId!, cancellationToken);
        }
    }
}

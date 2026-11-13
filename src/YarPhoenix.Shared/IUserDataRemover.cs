namespace YarPhoenix.Shared;

// Удаление пользовательских данных при удалении аккаунта (ТЗ §5.2.1). Живёт в Shared,
// чтобы модуль Auth мог его вызвать, не завися от Catalog; реализация — в Catalog
// (владеет UserBook/ReaderProfile/достижениями и файлами-сиротами).
public interface IUserDataRemover
{
    Task RemoveUserDataAsync(Guid userId, CancellationToken cancellationToken);
}

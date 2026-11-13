using YarPhoenix.Shared;

namespace YarPhoenix.Auth.Application;

// Внешняя зависимость «почтовый провайдер» (ТЗ §5.2): подтверждение почты и сброс
// пароля — одна и та же токен-механика, поэтому и контракт один. Возвращает Result:
// провайдер лежит → мягкая деградация «письмо не ушло», а не 500 (§8.3).
public interface IAuthEmailSender
{
    Task<Result> SendConfirmationEmailAsync(string email, string link, CancellationToken cancellationToken);

    Task<Result> SendPasswordResetEmailAsync(string email, string link, CancellationToken cancellationToken);
}

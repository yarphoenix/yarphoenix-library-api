using Microsoft.Extensions.Logging;
using YarPhoenix.Auth.Application;
using YarPhoenix.Shared;

namespace YarPhoenix.Auth.Infrastructure;

// Dev-реализация: пишет ссылки в лог вместо отправки. Реальный провайдер
// (Resend/Mailgun/Postmark) подключается этой же абстракцией без правок домена.
public sealed class LoggingAuthEmailSender(ILogger<LoggingAuthEmailSender> logger) : IAuthEmailSender
{
    public Task<Result> SendConfirmationEmailAsync(string email, string link, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV EMAIL] Confirmation link for {Email}: {Link}", email, link);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendPasswordResetEmailAsync(string email, string link, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV EMAIL] Password reset link for {Email}: {Link}", email, link);
        return Task.FromResult(Result.Success());
    }
}

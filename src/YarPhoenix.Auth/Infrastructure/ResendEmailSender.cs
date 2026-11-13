using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YarPhoenix.Auth.Application;
using YarPhoenix.Shared;

namespace YarPhoenix.Auth.Infrastructure;

// Продовая реализация IAuthEmailSender через Resend (ТЗ §5.2). Своя копия политик
// устойчивости — в AuthModule (§8). Ошибки провайдера → Result.Failure (мягкая
// деградация «письмо не ушло»), никогда не бросаем наружу: регистрация/сброс не 500.
public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IAuthEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task<Result> SendConfirmationEmailAsync(string email, string link, CancellationToken cancellationToken) =>
        SendAsync(
            email,
            "Confirm your email · Подтвердите почту — YarPhoenix Library",
            "Confirm your email address to finish signing up.",
            "Подтвердите адрес почты, чтобы завершить регистрацию.",
            "Confirm email · Подтвердить почту",
            link,
            cancellationToken);

    public Task<Result> SendPasswordResetEmailAsync(string email, string link, CancellationToken cancellationToken) =>
        SendAsync(
            email,
            "Reset your password · Сброс пароля — YarPhoenix Library",
            "We received a request to reset your password.",
            "Мы получили запрос на сброс пароля.",
            "Reset password · Сбросить пароль",
            link,
            cancellationToken);

    private async Task<Result> SendAsync(
        string to,
        string subject,
        string introEn,
        string introRu,
        string cta,
        string link,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                from = _options.From,
                to = new[] { to },
                subject,
                html = BuildHtml(introEn, introRu, cta, link),
            };

            using var response = await httpClient.PostAsJsonAsync("/emails", payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Resend responded {Status}: {Body}", (int)response.StatusCode, body);
            return Result.Failure(Error.Unavailable("The email could not be sent right now."));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Resend request failed for {Email}", to);
            return Result.Failure(Error.Unavailable("The email could not be sent right now."));
        }
    }

    private static string BuildHtml(string introEn, string introRu, string cta, string link) =>
        $"""
        <div style="font-family: system-ui, -apple-system, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px; color: #1a1a1a;">
          <h2 style="margin: 0 0 16px;">YarPhoenix Library</h2>
          <p style="margin: 0 0 8px; font-size: 15px;">{introEn}</p>
          <p style="margin: 0 0 24px; font-size: 15px; color: #555;">{introRu}</p>
          <a href="{link}" style="display: inline-block; background: #c8622d; color: #fff; text-decoration: none; padding: 12px 20px; border-radius: 8px; font-weight: 600;">{cta}</a>
          <p style="margin: 24px 0 0; font-size: 12px; color: #999; word-break: break-all;">{link}</p>
        </div>
        """;
}
